@echo off
setlocal

cd /d "%~dp0"
dotnet test

pause
