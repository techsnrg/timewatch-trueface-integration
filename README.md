# TrueFace ERPNext Integration

ERPNext/Frappe app for receiving TrueFace 3000 biometric attendance punches and creating `Employee Checkin` records.

## What This App Includes

- `TrueFace Device`
- `TrueFace Punch Log`
- `TrueFace Integration Settings`
- `TrueFace Integration` workspace
- API endpoint for punch ingestion:

```text
trueface_integration.api.attendance.receive_punches
```

The app stores raw punch logs, deduplicates events, maps TrueFace user IDs to ERPNext Employees, and creates `Employee Checkin` records. It does not create final `Attendance` records directly; HRMS or your existing attendance policy engine should process checkins.

## Install

From your Frappe bench:

```bash
bench get-app https://github.com/techsnrg/timewatch-trueface-integration.git
bench --site your-site-name install-app trueface_integration
bench --site your-site-name migrate
bench restart
```

## Configure

1. Open **TrueFace Integration Settings**.
2. Set an API token.
3. Create a **TrueFace Device** record.
4. Set each Employee's **Biometric Employee Code** to match the TrueFace user ID.

## API Payload

POST to:

```text
/api/method/trueface_integration.api.attendance.receive_punches
```

Example JSON:

```json
{
  "device_id": "TF3000-001",
  "api_token": "your-token",
  "punches": [
    {
      "device_serial": "TF3000-001",
      "record_number": "123",
      "user_id": "EMP-001",
      "card_no": "CARD-9",
      "punch_time": "2026-05-04 09:15:00",
      "direction": "ENTRY",
      "attendance_state": "SIGNIN",
      "status": true
    }
  ]
}
```

## Mapping

- TrueFace `user_id` maps to Employee `custom_biometric_employee_code`.
- Direction/attendance state maps to `Employee Checkin.log_type` where possible:
  - `ENTRY`, `SIGNIN` -> `IN`
  - `EXIT`, `SIGNOUT` -> `OUT`

Unmatched punches remain in **TrueFace Punch Log** with status **Unmatched Employee**.
