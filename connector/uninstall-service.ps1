param(
    [string]$ServiceName = "TrueFace ERPNext Connector"
)

$ErrorActionPreference = "Stop"

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Removed service: $ServiceName"
} else {
    Write-Host "Service not found: $ServiceName"
}
