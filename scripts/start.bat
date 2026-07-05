@echo off
REM Double-click to start the whole Meridian stack (DB + backend + frontend).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start.ps1"
pause
