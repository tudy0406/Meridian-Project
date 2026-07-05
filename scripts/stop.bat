@echo off
REM Double-click to stop the whole Meridian stack.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0stop.ps1"
pause
