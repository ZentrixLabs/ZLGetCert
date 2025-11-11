@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

powershell.exe -NoLogo -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT_DIR%sign-app.ps1" -ExePath "%~1"
set "EXITCODE=%ERRORLEVEL%"
exit /b %EXITCODE%

