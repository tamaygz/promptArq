@echo off
REM Quick launch script for PromptArq Windows Application

echo Starting PromptArq...
dotnet run

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Failed to run the application.
    echo Make sure you have built the project first with: build.bat
    echo Or run: dotnet build
    pause
)
