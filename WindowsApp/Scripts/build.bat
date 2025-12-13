@echo off
REM Build script for PromptArq Windows Application
REM This script is in WindowsApp\Scripts\ but operates on the WindowsApp\ directory

echo Building PromptArq Windows Application...
echo.

REM Change to WindowsApp directory (parent of Scripts)
cd /d "%~dp0.."

REM Check if .NET SDK is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8.0 SDK or later.
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK found: 
dotnet --version
echo.

REM Restore dependencies
echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to restore packages.
    pause
    exit /b 1
)
echo.

REM Build the project
echo Building project...
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)
echo.

echo Build completed successfully!
echo Executable location: bin\Release\net8.0-windows\PromptArq.exe
echo.
echo To publish a standalone executable, run: Scripts\build-publish.bat
pause
