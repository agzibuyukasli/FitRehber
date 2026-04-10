@echo off
setlocal
start "Dietitian Clinic API" cmd /k "%~dp0start-api.cmd"
timeout /t 8 /nobreak >nul

if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" (
    start "" "%ProgramFiles%\Google\Chrome\Application\chrome.exe" --new-window "http://127.0.0.1:5000/index.html"
) else if exist "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" (
    start "" "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" --new-window "http://127.0.0.1:5000/index.html"
) else (
    start "" "http://127.0.0.1:5000/index.html"
)
