@echo off
:: Check for Administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting Administrator privileges for silent installation...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process cmd -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

taskkill /F /IM WingetUpgrade.exe >nul 2>&1
echo Starting Winget Upgrade GUI with Administrator privileges...
cd /d "%~dp0"
dotnet run --project WingetUpgrade/WingetUpgrade.csproj
pause
