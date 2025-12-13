# PromptArq Windows App - Architecture

## Overview

The PromptArq Windows application is a native .NET 8.0 Windows Forms application that hosts a Vite web application using WebView2. It provides a seamless desktop experience with global hotkey support, system tray integration, and automatic server management.

The Windows app follows a **thin client architecture** where all business logic resides in the web application. The Windows app delegates all operations to the web app's JavaScript API (`window.windowsAppAPI`), providing only desktop-specific UX features like global hotkeys, system tray, and paste-to-active-window functionality.

**See Also:**
- [WindowsAPI.md](WindowsAPI.md) - Complete API documentation for web app ↔ Windows app communication
- [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) - Technical deep dive on WebView2 async patterns
- [CommandPalette.md](CommandPalette.md) - Command palette implementation details

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Desktop Application                  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                    PromptArq.exe                          │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  MainForm (Primary Window)                          │ │ │
│  │  │  - Borderless window with custom styling            │ │ │
│  │  │  - Dark theme with rounded corners                  │ │ │
│  │  │  - System tray NotifyIcon with context menu         │ │ │
│  │  │  - StatusStrip (DEBUG mode only)                    │ │ │
│  │  │  - WebView2 control                                 │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  WebView2 Control                                   │ │ │
│  │  │  ┌───────────────────────────────────────────────┐  │ │ │
│  │  │  │  Vite Web Application                         │  │ │ │
│  │  │  │  http://localhost:5000                        │  │ │ │
│  │  │  │  - React UI                                   │  │ │ │
│  │  │  │  - Prompt management                          │  │ │ │
│  │  │  │  - Projects & Categories                      │  │ │ │
│  │  │  └───────────────────────────────────────────────┘  │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  CommandPaletteForm (Modal Dialog)                 │ │ │
│  │  │  - Borderless modal with opacity                   │ │ │
│  │  │  - Multi-stage workflow state machine              │ │ │
│  │  │  - Prompt search and selection                     │ │ │
│  │  │  - Placeholder filling                             │ │ │
│  │  │  - Paste/Copy actions                              │ │ │
│  │  │  - Toast notifications                             │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  SettingsForm (Modal Dialog)                       │ │ │
│  │  │  - Hotkey configuration                            │ │ │
│  │  │  - Window preferences                              │ │ │
│  │  │  - Single instance enforcement                     │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  HotkeyManager                                      │ │ │
│  │  │  - Windows API RegisterHotKey                      │ │ │
│  │  │  - WM_HOTKEY message handling                      │ │ │
│  │  │  - Action dispatch                                 │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  UnifiedServerManager (Static)                      │ │ │
│  │  │  - Vite dev server process management              │ │ │
│  │  │  - LocalStorageServer lifecycle                    │ │ │
│  │  │  - Multi-strategy shutdown                         │ │ │
│  │  │  - Port monitoring and cleanup                     │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  AppSettings                                        │ │ │
│  │  │  - JSON-based configuration                        │ │ │
│  │  │  - Stored in %APPDATA%\PromptArq\settings.json    │ │ │
│  │  │  - Hotkey definitions                              │ │ │
│  │  │  - Window dimensions                               │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  Child Processes                                          │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  Vite Dev Server (npm run dev)                      │ │ │
│  │  │  Port: 5000                                         │ │ │
│  │  │  - Serves web application                           │ │ │
│  │  │  - Hot module replacement                           │ │ │
│  │  │  - Process lifecycle managed by app                 │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  LocalStorageServer (In-Process)                    │ │ │
│  │  │  Port: 5001                                         │ │ │
│  │  │  - HTTP server for storage access                   │ │ │
│  │  │  - Bridges web app to file system                   │ │ │
│  │  │  - Provides CORS-enabled API                        │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### MainForm.cs
The primary application window and entry point for user interaction.

**Responsibilities:**
- Hosts WebView2 control
- Manages window appearance (borderless, dark title bar, rounded corners)
- System tray integration with NotifyIcon
- Hotkey registration and action dispatch
- Server lifecycle coordination
- Settings and command palette dialogs
- Window state persistence
- **Delegates to web app API** via `window.windowsAppAPI` (see [WindowsAPI.md](WindowsAPI.md))

**Key Features:**
- Borderless design with `FormBorderStyle.Sizable` (resizable but no title bar)
- Dark theme using DWM API (`DWMWA_USE_IMMERSIVE_DARK_MODE`, `DWMWA_CAPTION_COLOR`)
- Status bar visible only in DEBUG builds
- Graceful shutdown with multiple cleanup strategies
- WebView2 message passing for async operations (see [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md))

**Web App Integration:**
```csharp
// Synchronous API calls (getPrompts, getPlaceholders, fillContent)
var result = await _webView.CoreWebView2.ExecuteScriptAsync(
    "window.windowsAppAPI.getPrompts()"
);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);

// Async API calls (executePrompt) use message passing
_webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
```

**Windows API Integration:**
```csharp
DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
```

### CommandPaletteForm.cs
A modal dialog providing quick access to prompts with a multi-stage workflow.

**Workflow States:**
1. **SelectingPrompt** - Search and select a prompt
2. **SelectingAction** - Choose Paste or Copy action
3. **FillingPlaceholder** - Fill in prompt placeholders (if any)
4. **ChoosingOutput** - Select output destination (not yet implemented)

**Features:**
- Fuzzy search through prompts
- Keyboard-first navigation (Enter, Escape, Arrow keys)
- Toast notifications for user feedback
- Click-outside-to-close behavior
- Borderless with rounded corners and opacity
- Automatic placeholder detection and filling
- **LLM execution** via web app API for prompts with `execute_llm=true`

**Architecture Highlights:**
- State machine pattern for workflow management
- **Zero business logic** - delegates all operations to MainForm → Web App API
- Uses function delegates: `GetPlaceholdersFromWebApp`, `FillContentInWebApp`, `ExecutePromptInWebApp`
- SendKeys for clipboard pasting to active window
- GDI32 `CreateRoundRectRgn` for rounded corners

**See:** [CommandPalette.md](CommandPalette.md) for detailed workflow documentation

### HotkeyManager.cs
Manages global system-wide hotkeys using Windows API.

**Functionality:**
- Registers hotkeys with `RegisterHotKey` Windows API
- Processes `WM_HOTKEY` messages via `WndProc` override in MainForm
- Maintains hotkey-to-action mapping
- Unregisters hotkeys on disposal

**Supported Modifiers:**
- Control (Ctrl)
- Alt
- Shift
- Windows (Win)

**Default Hotkeys:**
- `Ctrl+Alt+P` - Show/Hide Window
- `Ctrl+K` - Command Palette
- `Ctrl+Alt+S` - Settings
- `Ctrl+Shift+N` - New Prompt
- `Ctrl+Alt+Q` - Quit App

### UnifiedServerManager.cs
Static singleton managing all server processes.

**Managed Services:**
- Vite dev server (external process on port 5000)
- LocalStorageServer (in-process HTTP server on port 5001)

**Shutdown Strategy:**
Multi-layered approach for reliable cleanup:
1. **Graceful shutdown** - Clean disposal
2. **Process tree kill** - Terminate child processes
3. **Command line detection** - Kill Node.js processes by command pattern
4. **Port-based kill** - Nuclear option using netstat
5. **Verification** - Confirm ports released

**Port Management:**
```csharp
private static readonly int[] ManagedPorts = { 5000, 3001, 5001 };
```

**Idempotency:**
Safe to call `Start()` and `Stop()` multiple times without side effects.

### LocalStorageServer.cs
In-process HTTP server providing storage access to the web application.

**Purpose:**
Bridges the web application's need for persistent storage with the file system when running in the Windows app (as opposed to GitHub Spark's KV store).

**Endpoints:**
- `GET /storage/{key}` - Retrieve stored value
- `POST /storage/{key}` - Store value
- `DELETE /storage/{key}` - Remove value

**Implementation:**
- Uses `HttpListener` on port 5001
- CORS-enabled for localhost origins
- JSON-based data storage in `%APPDATA%\PromptArq\storage.json`
- Thread-safe file access with locking

### Settings.cs
Configuration persistence using JSON serialization.

**Storage Location:**
```
%APPDATA%\PromptArq\settings.json
```

**Configuration Options:**
- Hotkey definitions (action, key, modifiers)
- Window dimensions (width, height)
- Startup preferences (start minimized)

**Data Structure:**
```csharp
public class AppSettings
{
    public List<HotkeyConfig> Hotkeys { get; set; }
    public int WindowWidth { get; set; } = 1400;
    public int WindowHeight { get; set; } = 900;
    public bool StartMinimized { get; set; } = false;
}
```

## Data Flow

### Application Startup
```
1. Program.Main()
   └─> Application.Run(new MainForm())
       ├─> AppSettings.Load()
       ├─> InitializeComponent()
       ├─> InitializeCustomComponents()
       │   ├─> Setup WebView2
       │   ├─> Setup StatusStrip
       │   ├─> Setup NotifyIcon
       │   └─> Apply dark theme
       ├─> HotkeyManager initialization
       ├─> UnifiedServerManager.Start()
       │   ├─> StartStorageServer() [port 5001]
       │   └─> StartViteDevServer() [port 5000]
       ├─> MonitorViteStartup()
       └─> Initialize CommandPaletteForm
```

### Command Palette Flow
```
User presses Ctrl+K
   └─> HotkeyManager detects WM_HOTKEY
       └─> MainForm.ShowCommandPalette()
           └─> CommandPaletteForm.ShowPalette()
               ├─> Reset workflow state to SelectingPrompt
               ├─> Load prompts from web app storage
               ├─> Show dialog (centered, topmost)
               └─> User interaction:
                   ├─> Type to search prompts
                   ├─> Arrow keys to navigate
                   ├─> Enter to select
                   │   └─> Transition to SelectingAction
                   │       └─> Show Paste/Copy actions
                   │           └─> Enter to select action
                   │               ├─> If placeholders exist:
                   │               │   └─> Transition to FillingPlaceholder
                   │               │       └─> Fill each placeholder
                   │               │           └─> Execute action
                   │               └─> If no placeholders:
                   │                   └─> Execute action immediately
                   └─> Escape to close
```

### Hotkey Processing
```
User presses global hotkey
   └─> Windows sends WM_HOTKEY to MainForm
       └─> MainForm.WndProc() intercepts
           └─> HotkeyManager.ProcessHotkey(hotkeyId)
               └─> Lookup action by ID
                   └─> Invoke registered action
                       ├─> Show/Hide Window
                       ├─> Command Palette
                       ├─> Settings
                       ├─> New Prompt
                       └─> Quit App
```

### Application Shutdown
```
User closes window or presses Ctrl+Alt+Q
   └─> MainForm.Close() or FormClosing event
       └─> UnifiedServerManager.Stop()
           ├─> StopStorageServerGracefully()
           ├─> StopViteDevProcessGracefully()
           ├─> KillAllProcessTrees()
           ├─> KillNodeProcessesByCommandLine()
           ├─> KillProcessesByPort()
           └─> VerifyPortsReleased()
       └─> AppSettings.Save()
       └─> Application exit handlers
           ├─> OnApplicationExit
           ├─> OnProcessExit
           └─> OnUnhandledException
```

## Technology Stack

### Framework & Runtime
- **.NET 8.0** - Latest LTS version of .NET
- **Windows Forms** - Native Windows UI framework
- **C# 12** - Modern C# language features

### Key Dependencies
- **Microsoft.Web.WebView2** - Chromium-based web control
- **Newtonsoft.Json** - JSON serialization for settings
- **System.Diagnostics** - Process management
- **System.Net.Http** - HTTP client for server communication

### Windows APIs
- **dwmapi.dll** - Desktop Window Manager for dark theme
- **user32.dll** - RegisterHotKey/UnregisterHotKey
- **gdi32.dll** - CreateRoundRectRgn for rounded corners

### Development Tools
- **Node.js & npm** - For Vite dev server
- **Vite** - Web application build tool
- **Visual Studio 2022** - IDE (optional)

## Design Patterns

### Singleton Pattern
- `UnifiedServerManager` - Static singleton for centralized server management

### State Machine Pattern
- `CommandPaletteForm.WorkflowState` - Multi-stage workflow transitions

### Observer Pattern
- Event-driven architecture (`ActionSelected`, `FormClosing`, etc.)

### Strategy Pattern
- Multiple shutdown strategies in `UnifiedServerManager.Stop()`

### Façade Pattern
- `UnifiedServerManager` provides simple interface to complex server management

## Security Considerations

### Hotkey Registration
- Checks for registration failures (conflict detection)
- Unregisters on disposal to prevent leaks

### LocalStorageServer
- CORS restricted to localhost
- No authentication (assumes trusted local environment)
- Data stored in user's AppData (per-user isolation)

### Process Management
- Proper cleanup to prevent orphaned processes
- Multiple fallback strategies for reliable shutdown
- Debug logging for troubleshooting

## Performance Characteristics

### Startup Time
- **Cold start**: ~2-3 seconds (includes Vite server startup)
- **WebView2 initialization**: ~500ms
- **Hotkey registration**: <50ms

### Memory Footprint
- **Base application**: ~30-50 MB
- **WebView2 (Chromium)**: ~100-150 MB
- **Vite dev server**: ~50-100 MB
- **Total**: ~200-300 MB

### Response Time
- **Hotkey activation**: <100ms
- **Command palette open**: <50ms
- **Web app interaction**: Dependent on WebView2 rendering

## Extensibility Points

### Adding New Hotkeys
1. Add hotkey definition to `Settings.cs` defaults
2. Add action handler in `MainForm.cs`
3. Update settings UI in `SettingsForm.cs`

### Adding Command Palette Actions
1. Define new `PromptActionType` in `PromptAction.cs`
2. Add action handling in `CommandPaletteForm.ExecuteAction()`
3. Update action display in `ShowActions()`

### Adding New Servers
1. Add port to `UnifiedServerManager.ManagedPorts`
2. Implement startup logic in `UnifiedServerManager.Start()`
3. Add cleanup logic to all shutdown strategies

## Troubleshooting

### Common Issues

**WebView2 not loading:**
- Ensure WebView2 Runtime is installed
- Check Vite server started successfully (port 5000)
- Review debug output in Visual Studio

**Hotkeys not working:**
- Check for conflicts with other applications
- Verify hotkey registered successfully in debug output
- Try different key combinations

**Servers not stopping:**
- Check debug output for shutdown failures
- Manually kill processes via Task Manager
- Review port conflicts with `netstat -ano`

**Settings not persisting:**
- Verify write permissions to `%APPDATA%\PromptArq`
- Check for JSON serialization errors in debug output
- Delete settings.json to reset to defaults
