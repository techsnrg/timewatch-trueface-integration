@echo off
setlocal

set "SERVICE_NAME=TrueFace ERPNext Connector"

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    echo.
    echo Please right-click uninstall.bat and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

sc query "%SERVICE_NAME%" >nul 2>&1
if not "%errorlevel%"=="0" (
    echo Service not found: %SERVICE_NAME%
    pause
    exit /b 0
)

echo Stopping %SERVICE_NAME%...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 3 /nobreak >nul

echo Removing service...
sc delete "%SERVICE_NAME%"

echo.
echo Service removed.
echo The folder C:\TrueFaceConnector was not deleted, so config and queue data are preserved.
echo.
pause
