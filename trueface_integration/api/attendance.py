import json

import frappe
from frappe import _

from trueface_integration.services.punch_receiver import TrueFacePunchReceiver


@frappe.whitelist(allow_guest=True)
def receive_punches(device_id: str, punches, api_token: str | None = None):
	"""Receive normalized TrueFace punch batches from the Windows connector."""
	if isinstance(punches, str):
		try:
			punches = json.loads(punches)
		except Exception:
			frappe.throw(_("Punches must be valid JSON."))

	if not isinstance(punches, list):
		frappe.throw(_("Punches must be a list."))

	receiver = TrueFacePunchReceiver()
	return receiver.receive(device_id=device_id, punches=punches, api_token=api_token)
