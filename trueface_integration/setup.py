import frappe


EMPLOYEE_CUSTOM_FIELDS = {
	"Employee": [
		{
			"fieldname": "custom_biometric_employee_code",
			"fieldtype": "Data",
			"label": "Biometric Employee Code",
			"insert_after": "attendance_device_id",
			"unique": 1,
		}
	]
}


def after_install():
	ensure_custom_fields()


def after_migrate():
	ensure_custom_fields()


def ensure_custom_fields():
	from frappe.custom.doctype.custom_field.custom_field import create_custom_fields

	create_custom_fields(EMPLOYEE_CUSTOM_FIELDS, update=True)
	frappe.clear_cache(doctype="Employee")
