@echo off
REM Publish script for PromptArq Windows Application
REM Creates a self-contained executable with all dependencies

echo Publishing PromptArq Windows Application...
echo This will create a standalone executable with all dependencies included.
echo.

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

REM Publish as self-contained
echo Publishing for Windows x64 (self-contained)...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed.
    pause
    exit /b 1
)
echo.

echo Publish completed successfully!
echo.
echo Standalone executable location: 
echo bin\Release\net8.0-windows\win-x64\publish\PromptArq.exe
echo.
echo You can distribute this executable to other Windows machines.
echo Recipients will need:
echo   - Windows 10 or later
echo   - WebView2 Runtime (usually pre-installed on Windows 11)
echo   - Node.js and npm (to run the Vite dev server)
echo.
pause
