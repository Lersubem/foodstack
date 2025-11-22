@echo off
setlocal

start "" /D "%~dp0Client" "start-client.cmd"
start "" /D "%~dp0Service" "start-service.cmd"

endlocal
exit
