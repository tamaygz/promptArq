@echo off
REM Quick launch script for PromptArq Windows Application
REM This script is in WindowsApp\Scripts\ but operates on the WindowsApp\ directory

echo Starting PromptArq...

REM Change to WindowsApp directory (parent of Scripts)
cd /d "%~dp0.."

dotnet run

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Failed to run the application.
    echo Make sure you have built the project first with: Scripts\build.bat
    echo Or run: dotnet build
    pause
)
