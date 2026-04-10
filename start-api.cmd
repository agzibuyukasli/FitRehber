@echo off
setlocal
set ASPNETCORE_ENVIRONMENT=Development
set Logging__EventLog__LogLevel__Default=None
cd /d "%~dp0src\DietitianClinic.API"
.\bin\Debug\net8.0\DietitianClinic.API.exe
