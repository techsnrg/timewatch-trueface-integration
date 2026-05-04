import frappe
from frappe.model.document import Document


class TrueFaceDevice(Document):
	def validate(self):
		if not self.port:
			self.port = 37777
		if not self.timezone:
			self.timezone = frappe.db.get_single_value("System Settings", "time_zone") or "Asia/Kolkata"
