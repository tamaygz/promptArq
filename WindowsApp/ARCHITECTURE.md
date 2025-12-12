# PromptArq Windows App - Architecture Overview

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Windows Desktop                         │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │              PromptArq.exe (MainForm)                     │ │
│  │                                                           │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │ │
│  │  │   MenuStrip │  │ StatusStrip │  │   NotifyIcon    │  │ │
│  │  │  (File/View)│  │  (Status)   │  │  (System Tray)  │  │ │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │ │
│  │                                                           │ │
│  │  ┌───────────────────────────────────────────────────┐   │ │
│  │  │           WebView2 Control                        │   │ │
│  │  │  ┌─────────────────────────────────────────────┐  │   │ │
│  │  │  │                                             │  │   │ │
│  │  │  │     PromptArq Web App (React + Vite)       │  │   │ │
│  │  │  │                                             │  │   │ │
│  │  │  │  - Prompt Management UI                     │  │   │ │
│  │  │  │  - Projects & Categories                    │  │   │ │
│  │  │  │  - Template Library                         │  │   │ │
│  │  │  │  - Settings & Configuration                 │  │   │ │
│  │  │  │                                             │  │   │ │
│  │  │  └─────────────────────────────────────────────┘  │   │ │
│  │  │              ↑ http://localhost:5173              │   │ │
│  │  └───────────────────────────────────────────────────┘   │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │          HotkeyManager (Global Hotkeys)            │ │ │
│  │  │  - RegisterHotKey Windows API                      │ │ │
│  │  │  - WM_HOTKEY message processing                    │ │ │
│  │  │  - Ctrl+Alt+P, Ctrl+Shift+N, etc.                 │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │              Vite Process (npm run dev)                   │ │
│  │  - Started automatically by app                           │ │
│  │  - Runs in background                                     │ │
│  │  - Port 5173                                              │ │
│  │  - Monitored for ready state                              │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         File System                             │
│                                                                 │
│  %APPDATA%\PromptArq\                                           │
│  └── settings.json                                              │
│      - Hotkey configurations                                    │
│      - Window size/position                                     │
│      - User preferences                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Component Interaction Flow

### Application Startup

```
1. User launches PromptArq.exe
   ↓
2. Program.cs initializes Windows Forms
   ↓
3. MainForm constructor executes
   ├─→ Load settings from JSON (Settings.cs)
   ├─→ Initialize UI components
   ├─→ Create HotkeyManager
   └─→ Start Vite server process
   ↓
4. MainForm.OnLoad
   └─→ Initialize WebView2 control
   ↓
5. Vite server starts compiling
   ├─→ Monitor stdout for "localhost:5173"
   └─→ Update status bar
   ↓
6. WebView2 initialization completes
   └─→ Wait for Vite ready signal
   ↓
7. Navigate WebView2 to http://localhost:5173
   ↓
8. Register global hotkeys
   └─→ Windows API: RegisterHotKey
   ↓
9. Application ready!
```

### Hotkey Trigger Flow

```
1. User presses Ctrl+Alt+P (anywhere in Windows)
   ↓
2. Windows sends WM_HOTKEY message
   ↓
3. MainForm.WndProc receives message
   ↓
4. HotkeyManager.ProcessHotkey
   └─→ Looks up registered action
   ↓
5. Action executed on UI thread
   └─→ Example: ToggleWindow()
   ↓
6. Window shows/hides
```

### Settings Configuration Flow

```
1. User opens Settings (File → Settings or Ctrl+Alt+S)
   ↓
2. SettingsForm opens as modal dialog
   └─→ Loads current hotkeys into DataGridView
   ↓
3. User modifies hotkeys
   ├─→ Change key combinations
   ├─→ Add new hotkeys
   └─→ Remove unwanted hotkeys
   ↓
4. User clicks Save
   └─→ Validate entries
   ↓
5. Settings.cs saves to JSON
   └─→ %APPDATA%\PromptArq\settings.json
   ↓
6. MainForm unregisters old hotkeys
   ↓
7. MainForm registers new hotkeys
   ↓
8. Changes take effect immediately
```

## Class Responsibilities

### Program.cs
**Purpose**: Application entry point  
**Responsibilities**:
- Initialize Windows Forms subsystem
- Set visual styles
- Create and run MainForm

### MainForm.cs
**Purpose**: Main application window  
**Responsibilities**:
- Host WebView2 control
- Manage Vite server lifecycle
- Handle window events (resize, minimize, close)
- Provide menu and status bar
- Integrate with system tray
- Register and handle hotkeys
- Navigate between application states

**Key Methods**:
- `InitializeComponent()` - Build UI
- `StartViteServer()` - Spawn npm process
- `WaitForViteAndNavigate()` - Monitor Vite ready state
- `RegisterHotkeys()` - Setup global shortcuts
- `ToggleWindow()` - Show/hide functionality
- `ShowSettings()` - Open settings dialog

### SettingsForm.cs
**Purpose**: Settings configuration UI  
**Responsibilities**:
- Display current hotkey configuration
- Allow adding/removing/modifying hotkeys
- Validate user input
- Save changes to settings

**Key Methods**:
- `LoadHotkeys()` - Populate DataGridView
- `AddButton_Click()` - Add new hotkey
- `RemoveButton_Click()` - Remove selected hotkey
- `SaveButton_Click()` - Persist changes

### HotkeyManager.cs
**Purpose**: Global hotkey management  
**Responsibilities**:
- Register hotkeys with Windows
- Process WM_HOTKEY messages
- Map hotkeys to actions
- Clean up on disposal

**Key Methods**:
- `RegisterHotkey()` - P/Invoke RegisterHotKey
- `ProcessHotkey()` - Handle WM_HOTKEY messages
- `UnregisterAll()` - Clean up registrations

### Settings.cs
**Purpose**: Configuration persistence  
**Responsibilities**:
- Load settings from JSON
- Save settings to JSON
- Provide default values
- Manage AppData directory

**Key Classes**:
- `AppSettings` - Main settings container
- `HotkeyConfig` - Individual hotkey definition

## Data Flow

### Settings Persistence

```
User Action → SettingsForm → Settings.cs → JSON File
                                              ↓
                                      %APPDATA%\PromptArq\
                                          settings.json
```

### Hotkey Processing

```
Keyboard Input → Windows → WM_HOTKEY → HotkeyManager → Action
                                                          ↓
                                                    MainForm methods
```

### Web Communication

```
WebView2 ←→ http://localhost:5173 ←→ Vite Dev Server
   ↓                                        ↓
JavaScript                            React App
Execution                              (src/)
```

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| UI Framework | Windows Forms | Native Windows UI |
| Web Rendering | WebView2 | Chromium-based browser |
| Process Management | System.Diagnostics | Vite server lifecycle |
| Hotkeys | Windows API (P/Invoke) | Global keyboard shortcuts |
| Settings | Newtonsoft.Json | Configuration serialization |
| Development Server | Vite + npm | Web app hosting |
| Web Framework | React 19 | UI components |

## External Dependencies

### NuGet Packages
- **Microsoft.Web.WebView2** (v1.0.2792.45)
  - Provides WebView2 control
  - Chromium-based web rendering
  
- **Newtonsoft.Json** (v13.0.3)
  - JSON serialization/deserialization
  - Settings persistence

### System Requirements
- **.NET 8.0 Runtime/SDK**
  - Windows Forms support
  - Modern C# language features
  
- **WebView2 Runtime**
  - Pre-installed on Windows 11
  - Downloadable for Windows 10
  
- **Node.js + npm**
  - Required to run Vite dev server
  - Version 16 or later recommended

## Communication Patterns

### Parent → Child (MainForm → WebView2)
```csharp
// Execute JavaScript in web page
await _webView.CoreWebView2.ExecuteScriptAsync("...");
```

### Parent → Process (MainForm → Vite)
```csharp
// Monitor process output
_viteProcess.OutputDataReceived += (sender, e) => { ... };
```

### Windows → App (Hotkey System)
```csharp
// Receive WM_HOTKEY message
protected override void WndProc(ref Message m) { ... }
```

### File System (Settings)
```csharp
// Read/Write JSON
string json = File.ReadAllText(path);
File.WriteAllText(path, json);
```

## Security Considerations

### Sandboxing
- WebView2 runs in isolated process
- Same security model as Microsoft Edge
- Web content cannot access local files directly

### Process Isolation
- Vite server runs in separate process
- Standard Node.js security model
- No privileged access required

### Settings Security
- Stored in user's AppData (not shared)
- No sensitive data (only hotkey preferences)
- JSON format allows manual inspection

### Windows API
- RegisterHotKey requires no special privileges
- Standard user-level API calls
- No elevation required

## Performance Profile

### Memory Usage
- Application: ~20-30 MB
- WebView2: ~50-100 MB
- Vite Process: ~30-50 MB
- **Total: ~150-200 MB**

### CPU Usage
- Idle: <1%
- Vite compilation: 10-30% (brief, during startup)
- UI interaction: 1-5%

### Startup Time
- Cold start: 3-5 seconds
- Warm start: 1-2 seconds
- WebView2 init: <1 second

### Disk Usage
- Application: ~5 MB
- WebView2 cache: ~50-100 MB
- Settings: <1 KB

## Extensibility Points

### Adding New Hotkey Actions

1. Define action in `MainForm.RegisterHotkeys()`:
```csharp
"My Action" => () => this.Invoke((MethodInvoker)delegate { MyMethod(); })
```

2. Implement action method:
```csharp
private void MyMethod() { /* ... */ }
```

3. Add to settings UI (users can assign keys)

### Adding Menu Items

In `MainForm.InitializeComponent()`:
```csharp
_fileMenu.DropDownItems.Add("My Item", null, (s, e) => MyMethod());
```

### Custom JavaScript Integration

```csharp
private async void MyFeature()
{
    await _webView.CoreWebView2.ExecuteScriptAsync(@"
        // JavaScript code here
        document.querySelector('...').click();
    ");
}
```

### Settings Extensions

Add properties to `AppSettings` class:
```csharp
public bool MyNewSetting { get; set; } = true;
```

Auto-persisted with existing infrastructure.

## Deployment Options

### Option 1: Framework-Dependent
```bash
dotnet build -c Release
```
- Requires .NET Runtime on target
- Smaller file size (~5 MB)
- Updates via Windows Update

### Option 2: Self-Contained
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
- Includes .NET Runtime
- Larger file size (~50-70 MB)
- No dependencies

### Option 3: Single File
```bash
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```
- Single executable
- Extracts on first run
- Easiest distribution

## Future Architecture Enhancements

### Potential Improvements

1. **Plugin System**
   - Load actions from external DLLs
   - Custom hotkey actions without recompilation

2. **IPC with Web App**
   - Bidirectional communication
   - Native features accessible from web

3. **Multi-Window**
   - Multiple WebView2 instances
   - Tab support

4. **Offline Mode**
   - Bundle compiled Vite output
   - No npm dependency

5. **Update System**
   - Auto-update mechanism
   - Version checking

6. **Custom Protocol Handler**
   - `promptarq://` URL scheme
   - Deep linking from other apps

---

This architecture provides a solid foundation for a native Windows application hosting a modern web application with system-level integration features.
