param(
    [string]$ServiceName = "TrueFace ERPNext Connector",
    [string]$InstallDir = "C:\TrueFaceConnector",
    [string]$ProjectPath = ".\TrueFaceConnector\TrueFaceConnector.csproj",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found. Install the .NET 8 Hosting Bundle or SDK first."
}

$publishDir = Join-Path $InstallDir "app"
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Write-Host "Publishing connector to $publishDir ..."
dotnet publish $ProjectPath -c $Configuration -r win-x64 --self-contained false -o $publishDir

$exePath = Join-Path $publishDir "TrueFaceConnector.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish did not create $exePath"
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating Windows service..."
New-Service `
    -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName $ServiceName `
    -StartupType Automatic `
    -Description "Syncs TrueFace 3000 biometric punches to ERPNext."

Write-Host "Starting service..."
Start-Service -Name $ServiceName

Write-Host ""
Write-Host "Installed and started: $ServiceName"
Write-Host "Config file: $(Join-Path $publishDir 'appsettings.json')"
Write-Host "Logs: Windows Event Viewer > Windows Logs > Application"
