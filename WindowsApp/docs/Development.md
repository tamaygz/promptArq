# Development Guide

## Table of Contents
- [Development Environment Setup](#development-environment-setup)
- [Building the Application](#building-the-application)
- [Project Structure](#project-structure)
- [Server Management](#server-management)
- [Debugging](#debugging)
- [Testing](#testing)
- [Publishing](#publishing)
- [Contributing](#contributing)

## Development Environment Setup

### Required Tools
- **Visual Studio 2022** (Community Edition or higher)
  - Workload: .NET Desktop Development
  - Individual component: .NET 8.0 SDK
- **VS Code** (optional, for web app development)
- **Node.js** v16 or later
- **npm** (comes with Node.js)
- **Git** for version control

### Optional Tools
- **Windows Terminal** - Better command line experience
- **PowerShell 7** - Enhanced scripting capabilities
- **Process Explorer** - Advanced process monitoring
- **Fiddler/Wireshark** - Network debugging

### Initial Setup

1. **Clone Repository**
   ```bash
   git clone https://github.com/yourusername/promptArq.git
   cd promptArq
   ```

2. **Install Web App Dependencies**
   ```bash
   npm install
   ```

3. **Restore .NET Dependencies**
   ```bash
   cd WindowsApp
   dotnet restore
   ```

4. **Verify WebView2 Runtime**
   ```powershell
   # Check if WebView2 is installed
   Get-AppxPackage -Name "Microsoft.WebView2Runtime"
   ```
   If not installed, download from: https://developer.microsoft.com/microsoft-edge/webview2/

## Building the Application

### Debug Build
```bash
cd WindowsApp
dotnet build
```

Output: `bin/Debug/net8.0-windows/PromptArq.exe`

### Release Build
```bash
cd WindowsApp
dotnet build -c Release
```

Output: `bin/Release/net8.0-windows/PromptArq.exe`

### Running from Source
```bash
cd WindowsApp
dotnet run
```

### Build Scripts

Build scripts are located in the `Scripts/` folder.

#### Windows Batch Scripts

**Scripts/build.bat** - Quick debug build
```batch
@echo off
cd /d "%~dp0.."
dotnet build
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
) else (
    echo Build failed!
    exit /b 1
)
```

**Scripts/run.bat** - Build and run
```batch
@echo off
cd /d "%~dp0.."
call Scripts\build.bat
if %ERRORLEVEL% EQU 0 (
    dotnet run
)
```

**Scripts/build-publish.bat** - Create self-contained release
```batch
@echo off
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
echo Published to: bin\Release\net8.0-windows\win-x64\publish\
```

### Build Configuration

**PromptArqApp.csproj** - Key settings:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon>app_icon.ico</ApplicationIcon>
    <AssemblyName>PromptArq</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2420.47" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="app_icon.ico" />
  </ItemGroup>
</Project>
```

## Project Structure

### WindowsApp Folder
```
WindowsApp/
├── Program.cs                  # Entry point, cleanup handlers
├── MainForm.cs                 # Main application window
├── MainForm.Designer.cs        # Auto-generated UI code
├── MainForm.resx              # Form resources
├── CommandPaletteForm.cs       # Command palette dialog
├── CommandPaletteForm.Designer.cs
├── CommandPaletteForm.resx
├── SettingsForm.cs            # Settings dialog
├── SettingsForm.resx
├── Settings.cs                # Settings model & persistence
├── HotkeyManager.cs           # Global hotkey handling
├── UnifiedServerManager.cs    # Server lifecycle management
├── LocalStorageServer.cs      # In-process HTTP server
├── PromptAction.cs            # Action type definitions
├── app_icon.ico               # Application icon
├── PromptArqApp.csproj        # Project file
├── PromptArqApp.sln           # Solution file
├── Scripts/                   # Build and utility scripts
│   ├── build.bat              # Build script
│   ├── run.bat                # Run script
│   └── build-publish.bat      # Publish script
└── docs/                      # Documentation
    ├── Architecture.md
    ├── UserGuide.md
    ├── CommandPalette.md
    └── Development.md (this file)
```

### Key File Responsibilities

| File | Purpose | Key Features |
|------|---------|--------------|
| Program.cs | Application entry | Cleanup handlers, global exception handling |
| MainForm.cs | Main UI window | WebView2 hosting, hotkey dispatch, tray icon |
| CommandPaletteForm.cs | Quick prompt access | State machine, keyboard navigation, toast notifications |
| Settings.cs | Configuration | JSON persistence, hotkey definitions |
| HotkeyManager.cs | Global hotkeys | Windows API, hotkey registration |
| UnifiedServerManager.cs | Server lifecycle | Multi-strategy shutdown, port management |
| LocalStorageServer.cs | Storage bridge | HTTP server, file system access |

## Server Management

### Managed Servers

The application manages two servers:

1. **Vite Dev Server** (Port 5000)
   - External Node.js process
   - Started via `npm run dev`
   - Serves web application

2. **LocalStorage Server** (Port 5001)
   - In-process HTTP server
   - Provides storage API for web app
   - CORS-enabled for localhost

### Server Lifecycle

#### Startup Sequence
```
UnifiedServerManager.Start()
├─> StartStorageServer()
│   └─> new LocalStorageServer()
│       └─> HttpListener on http://localhost:5001/
└─> StartViteDevServer()
    └─> Process.Start("npm", "run dev")
        └─> Monitors output for "ready" message
```

#### Shutdown Sequence
```
UnifiedServerManager.Stop()
├─> StopStorageServerGracefully()
│   └─> _storageServer.Dispose()
├─> StopViteDevProcessGracefully()
│   └─> _viteDevProcess.Kill()
├─> KillAllProcessTrees()
│   └─> Recursive child process termination
├─> KillNodeProcessesByCommandLine()
│   └─> Kill processes matching "vite" or "tsx"
├─> KillProcessesByPort()
│   └─> netstat -ano | findstr "5000 5001 3001"
└─> VerifyPortsReleased()
    └─> Check ports are free
```

### Development Scripts

#### TestViteCleanup.ps1
PowerShell script to verify server cleanup:
```powershell
# Start app
Start-Process -FilePath "bin\Debug\net8.0-windows\PromptArq.exe"
Start-Sleep -Seconds 5

# Stop app
Stop-Process -Name "PromptArq" -Force
Start-Sleep -Seconds 5

# Check for orphaned processes
Get-Process | Where-Object {$_.ProcessName -match "node|vite|tsx"}
```

#### TestAllServers.ps1
Tests all server ports:
```powershell
$ports = @(5000, 5001, 3001)
foreach ($port in $ports) {
    $listener = Test-NetConnection -ComputerName localhost -Port $port
    if ($listener.TcpTestSucceeded) {
        Write-Host "Port $port: OPEN" -ForegroundColor Green
    } else {
        Write-Host "Port $port: CLOSED" -ForegroundColor Red
    }
}
```

### Port Configuration

| Port | Service | Protocol | Configurable |
|------|---------|----------|--------------|
| 5000 | Vite Dev Server | HTTP | Yes (vite.config.ts) |
| 5001 | LocalStorage Server | HTTP | Yes (LocalStorageServer.cs) |
| 3001 | OAuth Proxy | HTTP | Yes (server.js) |

**Changing Ports:**
1. Update `UnifiedServerManager.ManagedPorts` array
2. Update `vite.config.ts` for Vite port
3. Update `LocalStorageServer.cs` for storage port
4. Update `server.js` for OAuth proxy port

### Debugging Server Issues

#### Check Server Status
```csharp
bool isRunning = UnifiedServerManager.IsRunning;
```

#### View Debug Output
Run from Visual Studio or `dotnet run` to see:
```
[UnifiedServerManager] Starting all servers...
[UnifiedServerManager] Starting LocalStorage server on port 5001...
[LocalStorageServer] Server started on http://localhost:5001/
[UnifiedServerManager] Starting Vite dev server on port 5000...
[ViteProcess] VITE v4.0.0  ready in 1234 ms
[UnifiedServerManager] All servers started successfully
```

#### Manual Port Checks
```powershell
# Check what's listening on ports
netstat -ano | findstr "5000 5001 3001"

# Kill process by PID
taskkill /PID <pid> /F

# List Node.js processes
Get-Process | Where-Object {$_.ProcessName -match "node"}
```

## Debugging

### Visual Studio Debugging

1. Open `PromptArqApp.sln` in Visual Studio
2. Set breakpoints in code
3. Press `F5` to start debugging
4. Use Debug windows:
   - **Locals** - Variable values
   - **Call Stack** - Execution path
   - **Output** - Debug.WriteLine messages
   - **Immediate** - Execute code at runtime

### Debug Configuration
```csharp
#if DEBUG
    _statusStrip.Visible = true;  // Show status bar
    Debug.WriteLine("Debug mode enabled");
#endif
```

### Useful Debug Points

**Application Startup:**
- `Program.Main()` - Entry point
- `MainForm()` constructor - Initialization
- `UnifiedServerManager.Start()` - Server startup

**Hotkey Handling:**
- `MainForm.WndProc()` - Windows messages
- `HotkeyManager.ProcessHotkey()` - Hotkey dispatch

**Command Palette:**
- `CommandPaletteForm.ShowPalette()` - Dialog open
- `CommandPaletteForm.FilterResults()` - Search
- `CommandPaletteForm.ExecuteAction()` - Action execution

**Shutdown:**
- `MainForm.FormClosing()` - Window close
- `UnifiedServerManager.Stop()` - Cleanup

### Logging Best Practices

```csharp
// Use Debug.WriteLine for development logging
Debug.WriteLine($"[ClassName] Action performed: {details}");

// Use consistent prefixes
Debug.WriteLine("[UnifiedServerManager] Starting servers...");
Debug.WriteLine("[CommandPalette] Prompt selected: {promptTitle}");

// Log errors with context
catch (Exception ex)
{
    Debug.WriteLine($"[ClassName] Error: {ex.Message}");
    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
}
```

### Debugging Tools

#### Process Explorer
- View process tree
- Monitor handle leaks
- Check port usage
- Inspect process command lines

#### Fiddler
- Monitor HTTP traffic
- Inspect WebView2 requests
- Debug CORS issues
- Analyze API calls

## Testing

### Manual Testing Checklist

**Application Lifecycle:**
- [ ] App starts successfully
- [ ] Vite server starts
- [ ] LocalStorage server starts
- [ ] WebView2 loads web app
- [ ] Status bar shows correct status (debug mode)
- [ ] App minimizes to tray
- [ ] App restores from tray
- [ ] App closes cleanly (no orphaned processes)

**Hotkeys:**
- [ ] All default hotkeys register
- [ ] Hotkeys work when app minimized
- [ ] Hotkeys work when another app has focus
- [ ] Custom hotkeys save and load
- [ ] Conflicting hotkeys fail gracefully

**Command Palette:**
- [ ] Opens with Ctrl+K
- [ ] Search filters prompts
- [ ] Arrow keys navigate
- [ ] Enter selects prompt
- [ ] Escape closes dialog
- [ ] Placeholders detected
- [ ] Placeholder filling works
- [ ] Paste action works
- [ ] Copy action works
- [ ] Toast notifications appear
- [ ] Click outside closes

**Settings:**
- [ ] Settings open
- [ ] Hotkeys can be modified
- [ ] Settings save to disk
- [ ] Settings load on restart
- [ ] Only one settings window opens

### Automated Testing

Currently no automated tests. Future considerations:
- Unit tests for `Settings`, `HotkeyManager`
- Integration tests for `UnifiedServerManager`
- UI automation tests with FlaUI

### Performance Testing

**Startup Time:**
```csharp
var stopwatch = Stopwatch.StartNew();
UnifiedServerManager.Start();
stopwatch.Stop();
Debug.WriteLine($"Startup took: {stopwatch.ElapsedMilliseconds}ms");
```

**Memory Profiling:**
- Use Visual Studio Diagnostic Tools
- Monitor memory usage over time
- Check for memory leaks

## Publishing

### Self-Contained Executable

Creates a single EXE with .NET runtime embedded:
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

**Output:** `bin/Release/net8.0-windows/win-x64/publish/PromptArq.exe`

**File Size:** ~150-200 MB (includes .NET runtime)

### Framework-Dependent Executable

Smaller size, requires .NET 8 installed:
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

**Output:** `bin/Release/net8.0-windows/win-x64/publish/PromptArq.exe`

**File Size:** ~1-2 MB

### Release Checklist

- [ ] Update version number in AssemblyInfo
- [ ] Test release build thoroughly
- [ ] Include README.md in package
- [ ] Include LICENSE file
- [ ] Create installer (optional, e.g., Inno Setup)
- [ ] Code sign executable (optional)
- [ ] Create GitHub release
- [ ] Update documentation

### Distribution Methods

**ZIP Archive:**
```bash
cd bin/Release/net8.0-windows/win-x64/publish/
tar -a -c -f PromptArq-v1.0.0-win-x64.zip *
```

**Installer (Inno Setup):**
Create `setup.iss`:
```ini
[Setup]
AppName=PromptArq
AppVersion=1.0.0
DefaultDirName={pf}\PromptArq
OutputBaseFilename=PromptArq-Setup

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{commonprograms}\PromptArq"; Filename: "{app}\PromptArq.exe"
```

## Contributing

### Code Style
- Follow C# naming conventions
- Use 4 spaces for indentation
- Add XML documentation for public APIs
- Keep methods focused and concise
- Use meaningful variable names

### Commit Guidelines
- Use clear, descriptive commit messages
- Reference issue numbers: "Fix #123: Description"
- Keep commits atomic (one logical change)
- Test before committing

### Pull Request Process
1. Fork repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Make changes and commit
4. Push to fork: `git push origin feature/my-feature`
5. Create pull request
6. Address review comments
7. Merge when approved

### Adding New Features

#### New Hotkey Action
1. Add action to `Settings.SetDefaultHotkeys()`
2. Add handler in `MainForm.RegisterHotkeys()`
3. Update documentation

#### New Command Palette Action
1. Add `PromptActionType` enum value
2. Implement in `CommandPaletteForm.ExecuteAction()`
3. Add to `ShowActions()` display
4. Test workflow thoroughly

#### New Server
1. Create server class (inherit IDisposable)
2. Add port to `UnifiedServerManager.ManagedPorts`
3. Implement `Start()` and `Stop()` methods
4. Add to all cleanup strategies
5. Update documentation

### Debugging Tips

**Problem:** App won't start
- Check .NET 8 installed: `dotnet --version`
- Verify WebView2 Runtime installed
- Check Event Viewer for errors

**Problem:** Servers won't stop
- Enable verbose debug output
- Check Task Manager for orphaned processes
- Review `UnifiedServerManager` shutdown logs

**Problem:** Hotkeys not working
- Check for conflicts with other apps
- Verify registration success in debug output
- Test with administrator privileges

**Problem:** WebView2 won't load
- Check Vite server started (port 5000)
- Verify network settings (localhost)
- Check firewall rules

## Resources

### Official Documentation
- [.NET Windows Forms](https://docs.microsoft.com/dotnet/desktop/winforms/)
- [WebView2](https://docs.microsoft.com/microsoft-edge/webview2/)
- [Vite](https://vitejs.dev/)

### Community
- GitHub Issues: Report bugs and request features
- Discussions: Ask questions and share ideas

### Tools
- [Visual Studio](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [Process Explorer](https://docs.microsoft.com/sysinternals/downloads/process-explorer)
- [Fiddler](https://www.telerik.com/fiddler)
