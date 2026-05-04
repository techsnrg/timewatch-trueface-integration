import json

import frappe
from frappe import _
from frappe.utils import now_datetime

from trueface_integration.services.normalization import normalize_punch


class TrueFacePunchReceiver:
	def receive(self, device_id: str, punches: list[dict], api_token: str | None = None):
		self._validate_token(api_token)
		device = self._get_device(device_id)

		result = {
			"received": len(punches),
			"processed": 0,
			"duplicates": 0,
			"unmatched": 0,
			"invalid": 0,
			"failed": 0,
			"logs": [],
		}

		for punch in punches:
			try:
				outcome = self._process_one(device, punch)
				result[outcome["bucket"]] += 1
				result["logs"].append(outcome)
			except Exception as exc:
				frappe.log_error(
					title=_("TrueFace punch processing failed"),
					message=frappe.get_traceback(),
				)
				result["failed"] += 1
				result["logs"].append({"status": "Failed", "error": str(exc)})

		self._mark_received(device, punches)
		frappe.db.commit()
		return result

	def _validate_token(self, api_token):
		settings = frappe.get_single("TrueFace Integration Settings")
		if not settings.enabled:
			frappe.throw(_("TrueFace integration is disabled."), frappe.PermissionError)

		expected = settings.get_password("api_token")
		header_token = frappe.get_request_header("X-TrueFace-Token") if getattr(frappe, "request", None) else None
		provided = api_token or header_token
		if not expected or provided != expected:
			frappe.throw(_("Invalid TrueFace API token."), frappe.PermissionError)

	def _get_device(self, device_id):
		if not device_id:
			frappe.throw(_("Device ID is required."))

		device_name = frappe.db.exists("TrueFace Device", device_id)
		if not device_name:
			device_name = frappe.db.get_value("TrueFace Device", {"device_serial": device_id}, "name")
		if not device_name:
			frappe.throw(_("Unknown TrueFace device: {0}").format(device_id))

		device = frappe.get_doc("TrueFace Device", device_name)
		if not device.enabled:
			frappe.throw(_("TrueFace device {0} is disabled.").format(device.name), frappe.PermissionError)
		return device

	def _process_one(self, device, raw_punch):
		punch = normalize_punch(device.device_serial or device.name, raw_punch)
		existing = frappe.db.get_value(
			"TrueFace Punch Log",
			{"stable_key": punch["stable_key"]},
			["name", "processing_status"],
			as_dict=True,
		)
		if existing:
			return {"bucket": "duplicates", "status": "Duplicate", "log": existing.name}

		log = self._insert_log(device, punch)

		if not punch["punch_time"] or not punch["user_id"]:
			log.processing_status = "Invalid"
			log.error = _("Punch time and user ID are required.")
			log.save(ignore_permissions=True)
			return {"bucket": "invalid", "status": "Invalid", "log": log.name}

		employee = self._get_employee(punch["user_id"])
		if not employee:
			log.processing_status = "Unmatched Employee"
			log.error = _("No Employee found with Biometric Employee Code {0}.").format(punch["user_id"])
			log.save(ignore_permissions=True)
			return {"bucket": "unmatched", "status": "Unmatched Employee", "log": log.name}

		checkin_name = self._find_or_create_checkin(employee, punch, device)
		log.employee = employee
		log.employee_checkin = checkin_name
		log.processing_status = "Processed"
		log.error = None
		log.save(ignore_permissions=True)

		return {
			"bucket": "processed",
			"status": "Processed",
			"log": log.name,
			"employee": employee,
			"employee_checkin": checkin_name,
		}

	def _insert_log(self, device, punch):
		log = frappe.get_doc(
			{
				"doctype": "TrueFace Punch Log",
				"stable_key": punch["stable_key"],
				"device": device.name,
				"device_serial": punch["device_serial"],
				"record_number": punch["record_number"],
				"event_id": punch["event_id"],
				"user_id": punch["user_id"],
				"card_no": punch["card_no"],
				"punch_time": punch["punch_time"],
				"direction": punch["direction"],
				"log_type": punch["log_type"],
				"attendance_state": punch["attendance_state"],
				"status": 1 if punch["status"] else 0,
				"open_method": punch["open_method"],
				"error_code": punch["error_code"],
				"processing_status": "Pending",
				"raw_payload": json.dumps(punch["raw"], default=str, ensure_ascii=False),
			}
		)
		log.insert(ignore_permissions=True)
		return log

	def _get_employee(self, user_id):
		if not frappe.db.has_column("Employee", "custom_biometric_employee_code"):
			frappe.throw(_("Employee field custom_biometric_employee_code is missing."))
		return frappe.db.get_value("Employee", {"custom_biometric_employee_code": user_id}, "name")

	def _find_or_create_checkin(self, employee, punch, device):
		existing = frappe.db.exists(
			"Employee Checkin",
			{
				"employee": employee,
				"time": punch["punch_time"],
			},
		)
		if existing:
			return existing

		doc = frappe.new_doc("Employee Checkin")
		doc.employee = employee
		doc.time = punch["punch_time"]

		if punch["log_type"] and self._has_field("Employee Checkin", "log_type"):
			doc.log_type = punch["log_type"]
		if self._has_field("Employee Checkin", "device_id"):
			doc.device_id = device.device_serial or device.name
		if self._has_field("Employee Checkin", "skip_auto_attendance"):
			doc.skip_auto_attendance = 0

		doc.insert(ignore_permissions=True)
		return doc.name

	def _mark_received(self, device, punches):
		device.db_set("last_sync_at", now_datetime(), update_modified=False)
		record_numbers = []
		for punch in punches:
			normalized = normalize_punch(device.device_serial or device.name, punch)
			if str(normalized["record_number"]).isdigit():
				record_numbers.append(int(normalized["record_number"]))
		if record_numbers:
			device.db_set("last_record_number", max(record_numbers), update_modified=False)

		if frappe.db.exists("DocType", "TrueFace Integration Settings"):
			settings = frappe.get_single("TrueFace Integration Settings")
			settings.db_set("last_received_at", now_datetime(), update_modified=False)

	def _has_field(self, doctype, fieldname):
		try:
			return frappe.db.has_column(doctype, fieldname)
		except Exception:
			return False
