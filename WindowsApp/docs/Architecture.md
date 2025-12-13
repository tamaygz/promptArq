# PromptArq Windows App - Architecture

## Overview

The PromptArq Windows application is a native .NET 8.0 Windows Forms application that hosts a Vite web application using WebView2. It provides a seamless desktop experience with global hotkey support, system tray integration, and automatic server management.

The Windows app follows a **thin client architecture** with **component-based design** where:
- All business logic resides in the web application
- Desktop-specific functionality is encapsulated in reusable components
- The Windows app provides only desktop UX features (global hotkeys, system tray, notifications)
- Communication with the web app is delegated through specialized bridge components

**See Also:**
- [WindowsAPI.md](WindowsAPI.md) - Complete API documentation for web app ↔ Windows app communication
- [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) - Technical deep dive on WebView2 async patterns
- [CommandPalette.md](CommandPalette.md) - Command palette implementation details

## Component Architecture

The application is built using a modular component-based architecture that separates concerns and promotes code reuse:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Windows Desktop Application                  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                    PromptArq.exe                          │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  MainForm (Primary Window)                          │ │ │
│  │  │  - Coordinates components                           │ │ │
│  │  │  - Minimal UI logic                                 │ │ │
│  │  │  - Delegates to specialized components              │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                     ▲                                     │ │
│  │                     │ uses                                │ │
│  │                     ▼                                     │ │
│  │  ┌──────────────────┬──────────────────┬──────────────┐ │ │
│  │  │ WindowStyle      │ Notification     │ WebView2     │ │ │
│  │  │ Manager          │ Manager          │ Manager      │ │ │
│  │  │ (Static)         │ (Static)         │              │ │ │
│  │  └──────────────────┴──────────────────┴──────────────┘ │ │
│  │                                          │               │ │
│  │                                          ▼               │ │
│  │                     ┌────────────────────────────────┐  │ │
│  │                     │ WindowsAppAPIBridge            │  │ │
│  │                     │ - Delegates to web app API     │  │ │
│  │                     │ - Handles async communication  │  │ │
│  │                     └────────────────────────────────┘  │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  CommandPaletteForm (Modal Dialog)                 │ │ │
│  │  │  - Uses NotificationManager for toasts            │ │ │
│  │  │  - Delegates API calls via function references    │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  SettingsForm (Modal Dialog)                       │ │ │
│  │  │  - Uses WindowStyleManager                         │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐ │ │
│  │  │  HotkeyManager + UnifiedServerManager              │ │ │
│  │  └─────────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. WindowStyleManager.cs (Static)
**Purpose:** Centralized window styling for consistent dark mode appearance across all forms.

**Responsibilities:**
- Apply dark title bar using DWM API
- Manage window border colors
- Handle rounded window corners
- Provide consistent styling across MainForm, SettingsForm, and other windows

**Key Methods:**
```csharp
public static void ApplyDarkTitleBar(Form form, int? captionColor = null, int? borderColor = null)
```

**Windows API Used:**
- `DwmSetWindowAttribute` - Set dark mode and colors
- `DwmExtendFrameIntoClientArea` - Extend frame for styling
- `CreateRoundRectRgn` - Rounded corners (GDI32)

**Usage:**
```csharp
// In form initialization
HandleCreated += (s, e) => WindowStyleManager.ApplyDarkTitleBar(this, 
    captionColor: 0x00663300, borderColor: 0x00663300);
```

### 2. NotificationManager.cs (Static)
**Purpose:** Manages toast notifications across all forms with customizable positioning and styling.

**Responsibilities:**
- Display temporary toast notifications
- Customizable positioning (bottom-right, bottom-center, top-right, top-center, custom)
- Auto-dismiss with configurable duration
- Consistent styling with rounded corners

**Key Methods:**
```csharp
public static void ShowToast(string message, int durationMs, ToastOptions? options = null)
```

**Features:**
- Non-blocking notifications
- Multi-screen support (positions relative to cursor screen)
- Rounded corners using GDI32 `CreateRoundRectRgn`
- Opacity control
- Custom colors and fonts

**Usage:**
```csharp
// Simple toast
NotificationManager.ShowToast("Operation complete!", 2000);

// Custom styled toast
NotificationManager.ShowToast("Error occurred", 3000, new ToastOptions {
    Position = ToastPosition.TopRight,
    BackColor = Color.DarkRed,
    Opacity = 0.9
});
```

### 3. WebView2Manager.cs
**Purpose:** Manages WebView2 lifecycle, initialization, navigation, and communication.

**Responsibilities:**
- WebView2 initialization and configuration
- Vite server monitoring (polls port 5000)
- Automatic navigation when server is ready
- JavaScript execution
- Web message passing for async operations
- Status updates via callback

**Key Methods:**
```csharp
public WebView2Manager(WebView2 webView, Action<string> updateStatus, int vitePort = 5000)
public async Task InitializeAsync()
public void StartViteMonitoring()
public void SetExecutionResultCallback(Action<ExecutionResult> callback)
```

**Features:**
- Automatic retry with exponential backoff for Vite connection
- Event-based initialization completion
- Message passing for LLM execution results
- Graceful error handling

**Usage:**
```csharp
// Initialize in MainForm
_webViewManager = new WebView2Manager(_webView, UpdateStatus, VitePort);
await _webViewManager.InitializeAsync();
```

### 4. WindowsAppAPIBridge.cs
**Purpose:** Bridge between Windows Forms and web app JavaScript API, delegating all business logic to the web application.

**Responsibilities:**
- Execute JavaScript to call `window.windowsAppAPI` methods
- Serialize/deserialize data between C# and JavaScript
- Handle synchronous API calls (getPrompts, getPlaceholders, fillContent)
- Handle asynchronous API calls (executePrompt) with message passing
- Manage execution state and timeouts

**Key Methods:**
```csharp
public async Task<List<PromptInfo>> GetPromptsAsync()
public async Task<string[]> GetPlaceholdersAsync(string promptId)
public async Task<string> FillContentAsync(string promptId, Dictionary<string, string> values)
public async Task<ExecutionResult> ExecutePromptAsync(string promptId, string? content = null)
```

**Architecture Highlights:**
- Zero business logic in C# - all logic stays in web app
- Uses `WebView2Manager.ExecuteScriptAsync` for JavaScript execution
- JSON serialization for data transfer
- 60-second timeout for LLM executions
- TaskCompletionSource pattern for async message handling

**Communication Pattern:**
```csharp
// Synchronous API call
var script = "(() => window.windowsAppAPI.getPrompts())()";
var result = await webView2Manager.ExecuteScriptAsync(script);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);

// Asynchronous API call (with message passing)
var script = "window.windowsAppAPI.executePrompt(promptId, content)";
await webView2Manager.ExecuteScriptAsync(script);
// Result comes back via WebView2.WebMessageReceived event
```

### 5. MainForm.cs
The primary application window that coordinates all components.

**Responsibilities:**
- Host WebView2 control
- Initialize and coordinate component managers
- System tray integration with NotifyIcon
- Hotkey registration and action dispatch
- Settings and command palette dialogs
- Window state management
- Server lifecycle coordination

**Component Dependencies:**
```csharp
private WebView2Manager _webViewManager = null!;
private WindowsAppAPIBridge _apiManager = null!;
// WindowStyleManager and NotificationManager used as static classes
```

**Key Features:**
- Borderless design with `FormBorderStyle.Sizable`
- Status bar (DEBUG builds only)
- Delegates all web app communication to `WindowsAppAPIBridge`
- Wires up CommandPaletteForm with notification delegate

**Initialization Flow:**
```csharp
private async void MainForm_Load(object? sender, EventArgs e)
{
    // Apply dark title bar
    WindowStyleManager.ApplyDarkTitleBar(this, ...);
    
    // Initialize component managers
    _webViewManager = new WebView2Manager(_webView, UpdateStatus, VitePort);
    await _webViewManager.InitializeAsync();
    _apiManager = new WindowsAppAPIBridge(_webViewManager);
    
    // Wire up delegates
    if (_commandPalette != null)
    {
        _commandPalette.NotifyAction = (msg) => NotificationManager.ShowToast(msg, 2000);
    }
}
```

### 6. CommandPaletteForm.cs
A modal dialog providing quick access to prompts with a multi-stage workflow.

**Workflow States:**
1. **SelectingPrompt** - Search and select a prompt
2. **SelectingAction** - Choose Paste or Copy action
3. **FillingPlaceholder** - Fill in prompt placeholders (if any)

**Component Integration:**
- Uses `NotificationManager` for toast notifications via delegate
- Receives function delegates from MainForm for API calls:
  - `GetPlaceholdersFromWebApp`
  - `FillContentInWebApp`
  - `ExecutePromptInWebApp`
  - `NotifyAction` (notification delegate)

**Features:**
- Fuzzy search through prompts
- Keyboard-first navigation
- Automatic placeholder detection
- LLM execution for `execute_llm=true` prompts
- Zero business logic (delegates to web app API)

**See:** [CommandPalette.md](CommandPalette.md) for detailed workflow documentation

### 7. SettingsForm.cs
Modal dialog for hotkey configuration and preferences.

**Component Integration:**
- Uses `WindowStyleManager.ApplyDarkTitleBar()` for consistent styling

**Features:**
- Hotkey configuration UI
- Settings persistence
- Single instance enforcement

### 8. HotkeyManager.cs
Manages global system-wide hotkeys using Windows API.

**Functionality:**
- Registers hotkeys with `RegisterHotKey` Windows API
- Processes `WM_HOTKEY` messages
- Maintains hotkey-to-action mapping
- Automatic cleanup on disposal

**Default Hotkeys:**
- `Ctrl+Alt+P` - Show/Hide Window
- `Ctrl+K` - Command Palette
- `Ctrl+Alt+S` - Settings
- `Ctrl+Shift+N` - New Prompt
- `Ctrl+Alt+Q` - Quit App

### 9. UnifiedServerManager.cs (Static)
Manages all server processes with robust lifecycle management.

**Managed Services:**
- Vite dev server (external process, port 5000)
- LocalStorageServer (in-process HTTP server, port 5001)

**Features:**
- Multi-strategy shutdown (graceful → SIGTERM → SIGKILL)
- Port monitoring and cleanup
- Process tree termination
- Error recovery

## Data Flow

### 1. Startup Sequence
```
1. Program.Main()
2. UnifiedServerManager.StartServers()
   - Start Vite dev server (npm run dev)
   - Start LocalStorageServer (in-process)
3. MainForm initialization
4. WindowStyleManager.ApplyDarkTitleBar()
5. WebView2Manager.InitializeAsync()
   - StartViteMonitoring() - polls port 5000
   - When Vite ready → navigate to http://localhost:5000
6. WindowsAppAPIBridge initialization
7. CommandPalette delegate wiring
8. HotkeyManager.RegisterHotkey() for each hotkey
```

### 2. Command Palette Flow (Paste Action)
```
1. User presses Ctrl+K (hotkey)
2. MainForm → ShowCommandPalette()
3. MainForm → _apiManager.GetPromptsAsync()
4. WindowsAppAPIBridge → WebView2Manager.ExecuteScriptAsync()
5. JavaScript → window.windowsAppAPI.getPrompts()
6. Return List<PromptInfo> to CommandPaletteForm
7. User selects prompt
8. If placeholders:
   a. _apiManager.GetPlaceholdersAsync(promptId)
   b. User fills placeholders
   c. _apiManager.FillContentAsync(promptId, values)
9. If execute_llm=true:
   a. NotificationManager.ShowToast("Executing through LLM...")
   b. _apiManager.ExecutePromptAsync(promptId, content)
   c. WebView2 message passing → wait for result
10. Clipboard.SetText(result)
11. SendKeys.SendWait("^v") - paste to active window
12. NotificationManager.ShowToast("Pasted!")
```

### 3. Settings Update Flow
```
1. User modifies hotkey in SettingsForm
2. AppSettings.Save() → %APPDATA%\PromptArq\settings.json
3. MainForm unregisters old hotkeys
4. MainForm registers new hotkeys
5. HotkeyManager updates mappings
```

## Component Benefits

The component-based architecture provides:

1. **Code Reuse** - Components can be used across multiple forms
   - `WindowStyleManager` used by MainForm, SettingsForm
   - `NotificationManager` used by MainForm, CommandPaletteForm

2. **Separation of Concerns** - Each component has a single responsibility
   - Window styling → WindowStyleManager
   - Notifications → NotificationManager
   - WebView2 lifecycle → WebView2Manager
   - Web API communication → WindowsAppAPIBridge

3. **Testability** - Components can be tested independently
   - Mock WebView2Manager for testing WindowsAppAPIBridge
   - Test NotificationManager without forms

4. **Maintainability** - Changes localized to specific components
   - Update notification styling in one place
   - Modify WebView2 initialization logic without touching MainForm

5. **Reduced Duplication** - Eliminated ~640 lines of duplicate code
   - MainForm: 1041 → 446 lines (-595)
   - CommandPaletteForm: 911 → 864 lines (-47)

## Communication Patterns

### Synchronous API Calls
Used for: `getPrompts()`, `getPlaceholders()`, `fillContent()`

```csharp
var script = "(() => window.windowsAppAPI.getPrompts())()";
var result = await ExecuteScriptAsync(script);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);
```

### Asynchronous API Calls
Used for: `executePrompt()` (LLM execution can take time)

```csharp
// Trigger execution (fire-and-forget)
var script = "window.windowsAppAPI.executePrompt(promptId, content)";
await ExecuteScriptAsync(script);

// Result comes back via WebMessageReceived event
webView.CoreWebView2.WebMessageReceived += (s, e) => {
    var json = e.WebMessageAsJson;
    var message = JsonDocument.Parse(json);
    if (message.GetProperty("type").GetString() == "executeResult") {
        var result = ParseExecutionResult(message);
        _executionTcs?.TrySetResult(result);
    }
};

// Wait for result with timeout
var resultTask = _executionTcs.Task;
var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
var completedTask = await Task.WhenAny(resultTask, timeoutTask);
```

See [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) for detailed async patterns.

## Security Considerations

1. **WebView2 Isolation**
   - Web content runs in isolated process
   - JavaScript cannot directly access C# memory
   - All communication via controlled APIs

2. **Hotkey Collision**
   - Validates hotkey availability before registration
   - Gracefully handles registration failures
   - User can reconfigure conflicting hotkeys

3. **Server Ports**
   - Fixed ports (5000, 5001) may conflict
   - Future: Dynamic port allocation
   - Localhost-only binding (not exposed to network)

4. **Settings Storage**
   - JSON in %APPDATA%\PromptArq
   - User-specific, not system-wide
   - No sensitive credentials stored

## Performance Considerations

1. **WebView2 Initialization**
   - Asynchronous with timeout handling
   - Retry logic for Vite connection
   - Status feedback via callback

2. **Prompt Search**
   - In-memory fuzzy search
   - Efficient string matching
   - No database queries

3. **Server Monitoring**
   - Background polling (500ms intervals)
   - Stops after successful connection
   - Minimal CPU overhead

4. **Memory Management**
   - Toast notifications auto-dispose
   - WebView2 properly disposed on shutdown
   - Server processes terminated cleanly

## Future Enhancements

1. **Component Improvements**
   - Make NotificationManager position-aware of multiple monitors
   - Add WindowStyleManager support for light theme
   - Extend WebView2Manager with more configuration options

2. **Additional Components**
   - ThemeManager for consistent colors
   - DialogManager for modal dialog coordination
   - UpdateManager for auto-update functionality

3. **Testing Infrastructure**
   - Unit tests for each component
   - Integration tests for component interaction
   - Mock implementations for isolated testing

## Related Documentation

- [WindowsAPI.md](WindowsAPI.md) - Web app JavaScript API reference
- [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) - WebView2 async patterns
- [CommandPalette.md](CommandPalette.md) - Command palette workflows
- [Development.md](Development.md) - Development setup and guidelines
- [UserGuide.md](UserGuide.md) - End-user documentation
