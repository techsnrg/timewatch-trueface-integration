import frappe

from trueface_integration.services.punch_receiver import TrueFacePunchReceiver


def reprocess_unmatched(limit=500):
	rows = frappe.get_all(
		"TrueFace Punch Log",
		filters={"processing_status": "Unmatched Employee"},
		fields=["name", "device", "raw_payload"],
		limit=limit,
	)
	receiver = TrueFacePunchReceiver()
	results = []
	for row in rows:
		device_serial = frappe.db.get_value("TrueFace Device", row.device, "device_serial") or row.device
		results.append(receiver.receive(device_serial, [frappe.parse_json(row.raw_payload)]))
	return results
