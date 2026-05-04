# TrueFace Windows Connector

This folder contains a .NET Worker Service for Windows.

## Install on Windows

There is no `setup.exe` yet. The easiest install is `install.bat`.

1. Publish the connector on Windows:

```powershell
dotnet publish .\TrueFaceConnector\TrueFaceConnector.csproj -c Release -r win-x64 --self-contained true -o .\publish
```

2. Copy these into one folder:

- Everything from `.\publish`
- `install.bat`
- `uninstall.bat`
- TrueFace SDK DLLs such as `dhnetsdk.dll`, `dhconfigsdk.dll`, `dhplay.dll`

3. Edit `appsettings.json` in that folder.
4. Right-click `install.bat` and choose **Run as administrator**.

The installer copies the files to `C:\TrueFaceConnector`, creates the Windows service, and starts it.

Advanced PowerShell install is also available:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\install-service.ps1
```

For live device access, add the vendor C# wrapper (`NetSDKCS`) and native DLLs from the TrueFace SDK beside the published EXE, then compile with `TRUEFACE_NETSDK`.

Without `TRUEFACE_NETSDK`, the service remains buildable for queue/API work but will log that the native SDK binding is not enabled.

## Configuration

Edit `appsettings.json`:

- `ErpNextBaseUrl`: ERPNext site URL.
- `ApiToken`: token set in **TrueFace Integration Settings**.
- `Devices`: one entry per TrueFace 3000 device.

The local SQLite queue keeps unsent punches until ERPNext accepts them.

## Uninstall

Open PowerShell as Administrator:

```powershell
.\uninstall-service.ps1
```

Or right-click `uninstall.bat` and choose **Run as administrator**.
