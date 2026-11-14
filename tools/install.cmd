@echo off
setlocal

net session >nul 2>&1
if not errorlevel 1 goto run

echo Requesting administrator rights for the certificate trust step...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b

:run
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
echo.
pause
