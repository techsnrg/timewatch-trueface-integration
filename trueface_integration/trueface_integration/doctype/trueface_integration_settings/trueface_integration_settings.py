from frappe.model.document import Document


class TrueFaceIntegrationSettings(Document):
	def validate(self):
		if not self.duplicate_tolerance_seconds:
			self.duplicate_tolerance_seconds = 2
