app_name = "trueface_integration"
app_title = "TrueFace Integration"
app_publisher = "SNRG Electricals"
app_description = "TrueFace 3000 biometric attendance integration for ERPNext"
app_email = "admin@snrgelectricals.com"
app_license = "mit"

after_install = "trueface_integration.setup.after_install"
after_migrate = "trueface_integration.setup.after_migrate"

fixtures = [
	{"dt": "Workspace", "filters": [["name", "=", "TrueFace Integration"]]},
	{"dt": "Custom Field", "filters": [["dt", "=", "Employee"]]},
]
