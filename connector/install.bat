@echo off
setlocal enabledelayedexpansion

set "SERVICE_NAME=TrueFace ERPNext Connector"
set "INSTALL_DIR=C:\TrueFaceConnector"
set "SOURCE_DIR=%~dp0"
set "EXE_NAME=TrueFaceConnector.exe"

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    echo.
    echo Please right-click install.bat and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

if not exist "%SOURCE_DIR%%EXE_NAME%" (
    echo.
    echo Could not find %EXE_NAME% in:
    echo %SOURCE_DIR%
    echo.
    echo Put the published connector files in this folder first.
    echo If you only have source code, run on Windows:
    echo dotnet publish .\TrueFaceConnector\TrueFaceConnector.csproj -c Release -r win-x64 --self-contained true -o .\publish
    echo Then copy everything from .\publish next to this install.bat.
    echo.
    pause
    exit /b 1
)

echo.
echo Installing %SERVICE_NAME%...
echo Source: %SOURCE_DIR%
echo Target: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo Copying files...
xcopy "%SOURCE_DIR%*" "%INSTALL_DIR%\" /E /I /Y >nul
if not "%errorlevel%"=="0" (
    echo File copy failed.
    pause
    exit /b 1
)

sc query "%SERVICE_NAME%" >nul 2>&1
if "%errorlevel%"=="0" (
    echo Existing service found. Stopping and replacing it...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 3 /nobreak >nul
    sc delete "%SERVICE_NAME%" >nul 2>&1
    timeout /t 3 /nobreak >nul
)

echo Creating Windows service...
sc create "%SERVICE_NAME%" binPath= "\"%INSTALL_DIR%\%EXE_NAME%\"" start= auto DisplayName= "%SERVICE_NAME%"
if not "%errorlevel%"=="0" (
    echo Failed to create service.
    pause
    exit /b 1
)

sc description "%SERVICE_NAME%" "Syncs TrueFace 3000 biometric punches to ERPNext." >nul

echo Starting service...
sc start "%SERVICE_NAME%"
if not "%errorlevel%"=="0" (
    echo.
    echo Service was installed but did not start.
    echo Check appsettings.json and Windows Event Viewer.
    echo Config: %INSTALL_DIR%\appsettings.json
    echo.
    pause
    exit /b 1
)

echo.
echo Installed successfully.
echo Service: %SERVICE_NAME%
echo Folder:  %INSTALL_DIR%
echo Config:  %INSTALL_DIR%\appsettings.json
echo.
pause
