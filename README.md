# TrueFace 3000 ERPNext Integration

This repository contains two pieces:

- `trueface_integration`: a Frappe/ERPNext app that receives TrueFace punch batches, stores raw punch logs, maps users to Employees, and creates `Employee Checkin` rows.
- `connector/TrueFaceConnector`: a Windows Worker Service skeleton for the TrueFace NetSDK. It handles local queuing/retry and posts normalized punches to ERPNext.

The connector is intended to run on a Windows machine on the same LAN as the TrueFace 3000 because the provided SDK ships Windows native DLLs.

## ERPNext Install

From a Frappe bench:

```bash
bench get-app /path/to/this/repo
bench --site your-site install-app trueface_integration
bench --site your-site migrate
```

After install:

1. Open **TrueFace Integration Settings** and set an API token.
2. Create a **TrueFace Device** with the device serial/IP.
3. Ensure each ERPNext Employee has `custom_biometric_employee_code` equal to the TrueFace user ID.

## Connector

The connector project targets Windows and .NET 8. Copy the SDK DLLs into the published connector folder on the Windows host, configure `appsettings.json`, then run it as a Windows Service.

The SDK adapter is intentionally isolated behind `ITrueFaceSdkClient` so the service can be tested without the physical device.
