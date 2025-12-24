# WindowsApp AGENTS.md
This file provides guidance to AI agents when working with the Windows desktop application code in this directory.

## Overview
The WindowsApp is a native Windows desktop application (.NET 8.0 with Windows Forms) that provides:
- WebView2 hosting of the PromptArq web application
- Global hotkey support for system-wide access
- Command palette (Ctrl+K) for quick prompt execution
- LocalStorage HTTP server for cross-browser data persistence
- System tray integration and native Windows features
- Bridge API for web-native communication

## Architecture

### Core Components
Each component follows a single-responsibility pattern with clear interfaces:

- **Program.cs** - Application entry point with comprehensive cleanup handlers
- **MainForm.cs** - Primary window, coordinates all components
- **UnifiedServerManager.cs** - Manages all server processes (Vite, LocalStorage, OAuth proxy)
- **LocalStorageServer.cs** - HTTP server on port 5001 for SQLite-backed storage
- **WebView2Manager.cs** - WebView2 lifecycle, initialization, JavaScript execution
- **WindowsAppAPIBridge.cs** - Bridge between C# and JavaScript via WebView2 messaging
- **CommandPaletteForm.cs** - Quick-access dialog with multi-step workflows
- **HotkeyManager.cs** - Global hotkey registration via Windows API
- **WindowStyleManager.cs** - Consistent dark mode styling (static utility)
- **NotificationManager.cs** - Toast notification system (static utility)
- **Settings.cs** - Settings persistence with atomic writes and JSON serialization
- **PromptHistory.cs** - Tracks prompt usage and placeholder values

### Data Flow
1. User triggers action (hotkey, UI, command palette)
2. MainForm or CommandPaletteForm handles user input
3. WindowsAppAPIBridge translates to JavaScript calls
4. JavaScript executes in WebView2, calls window.windowsAppAPI
5. Results returned via WebView2 message passing or ExecuteScriptAsync
6. C# receives results and performs native actions (clipboard, paste, notifications)

### Server Architecture
- **Vite Dev Server** (port 5000) - React app with HMR
- **OAuth Proxy** (port 3001) - Handled by server.js via concurrently
- **LocalStorage Server** (port 5001) - In-process HTTP server with SQLite backend
  - Database: `%APPDATA%/PromptArq/promptarq.db`
  - Table: `kv_store` (key TEXT, value TEXT, updated_at INTEGER)
  - CORS enabled for local development

### Multi-Strategy Server Shutdown
UnifiedServerManager uses multiple cleanup strategies for robustness:
1. **Graceful** - Close handles, wait for exit
2. **Process Tree Kill** - taskkill /F /T
3. **Command Line Detection** - Kill node.exe processes by command line matching
4. **Port-based Kill** - Nuclear option, kill by port occupancy
5. **Verification** - Check ports are released

## Coding Conventions

### C# Patterns
- **Dispose Pattern**: All IDisposable classes must implement proper disposal with lock-based safety
- **Null Safety**: Use nullable reference types (`string?`, null-coalescing, null-conditional operators)
- **Async/Await**: All I/O operations must be async (file, network, JavaScript execution)
- **Lock-based Thread Safety**: Use `lock (_lock)` for critical sections in managers
- **Structured Logging**: Use Serilog with contextual loggers (`LoggerConfig.ForContext<T>()`)
- **Static Utilities**: WindowStyleManager and NotificationManager are static helper classes
- **Component Pattern**: Each manager encapsulates its concern and exposes clean APIs

### JavaScript Bridge Pattern
- **Web → C#**: Use `window.chrome.webview.postMessage({ type: 'executeResult', ... })`
- **C# → Web**: Use `_webView2Manager.ExecuteJavaScriptAsync(script)`
- **Async Execution**: Use TaskCompletionSource for bridging async JS operations
- **String Escaping**: Always use `EscapeForJavaScript()` for string parameters in JS scripts
- **Error Handling**: Wrap JS calls in try-catch, return error objects

### Naming Conventions
- Private fields: `_camelCase` with underscore prefix
- Public properties: `PascalCase`
- Constants: `PascalCase` or `UPPER_CASE` for Win32 API constants
- Event handlers: `ComponentName_EventName` (e.g., `SearchBox_KeyDown`)
- Async methods: `MethodNameAsync` suffix

### Error Handling
- Use structured logging with context (file paths, PIDs, ports, etc.)
- Catch specific exceptions first (SqliteException, HttpListenerException, WebView2RuntimeNotFoundException)
- Log at appropriate levels: Debug, Information, Warning, Error, Fatal
- Return error results rather than throwing when appropriate (ExecutionResult pattern)
- Graceful degradation: Show notifications to user, don't crash

## Build and Run Workflows

### Development Build
```cmd
cd WindowsApp
dotnet build
dotnet run
```

Or use convenience scripts:
```cmd
cd WindowsApp
Scripts\build.bat
Scripts\run.bat
```

### Release Build
```cmd
dotnet build -c Release
```

### Publish (Single-file executable)
```cmd
cd WindowsApp
Scripts\build-publish.bat
```
Or manually:
```cmd
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

### Build Order for Full Application
1. **Web App**: `npm install` → `npm run build` (generates dist/ folder)
2. **Copy Assets**: Copy dist/ contents into WindowsApp/www/ (for production builds)
3. **Windows App**: `dotnet build` or `dotnet publish`

In development, Vite runs on port 5000 and serves directly (no www/ copy needed).

## Key Dependencies

### NuGet Packages
- **Microsoft.Web.WebView2** (1.0.2792.45) - Embeds Chromium-based web view
- **Microsoft.Data.Sqlite** (8.0.0) - SQLite database for LocalStorage server
- **Newtonsoft.Json** (13.0.3) - JSON serialization (Settings.cs, API responses)
- **System.Management** (8.0.0) - WMI queries for process command line detection
- **Serilog** (3.1.1) + Sinks - Structured logging to file, console, debug output

### Runtime Requirements
- .NET 8.0 Runtime (or SDK for development)
- WebView2 Runtime (pre-installed on Windows 11, downloadable for Windows 10)
- Node.js 18+ (for Vite dev server)

## File-Specific Guidelines

### MainForm.cs
- Do NOT directly manage servers; always use UnifiedServerManager
- Use component managers for all concerns (WebView2Manager, HotkeyManager, NotificationManager)
- Handle ActionSelected event from CommandPaletteForm for delegated actions
- WebView2 initialization must complete before loading UI

### UnifiedServerManager.cs
- All server lifecycle managed here (start, stop, kill)
- Thread-safe with lock on _lock object
- Idempotent operations (Start/Stop can be called multiple times safely)
- Must handle partial startup failures with cleanup
- Ports are hardcoded constants: 5000 (Vite), 3001 (OAuth), 5001 (LocalStorage)

### LocalStorageServer.cs
- HTTP listener on port 5001 with CORS enabled
- SQLite database at `%APPDATA%/PromptArq/promptarq.db`
- Endpoints: /keys, /get?key=, /set?key=, /delete?key=, /health
- All operations are async with proper error handling
- Dispose pattern must stop listener gracefully

### WebView2Manager.cs
- Manages WebView2 initialization, navigation, and message passing
- Monitors Vite server availability before navigation
- ExecuteJavaScriptAsync for synchronous calls
- WebMessageReceived for async result callbacks (via TaskCompletionSource)
- Must handle WebView2RuntimeNotFoundException with user-friendly message

### WindowsAppAPIBridge.cs
- All business logic stays in JavaScript (window.windowsAppAPI)
- C# only orchestrates and provides native capabilities
- Use EscapeForJavaScript() for all string parameters in scripts
- Delegates pattern: expose Func<> delegates for CommandPaletteForm
- TaskCompletionSource pattern for async operations (ExecutePromptAsync, ExecuteOneTimePromptAsync)

### CommandPaletteForm.cs
- Multi-step workflow with state machine (WorkflowState enum)
- Dark theme applied via WindowStyleManager
- Owner-drawn ListBox for custom item rendering
- Delegates for calling web app API (set by MainForm)
- History tracking for suggestions (recent prompts, placeholder values)
- Text display panel for large content preview
- Handles Escape for step-back navigation through workflow

### Settings.cs
- JSON file at `%APPDATA%/PromptArq/settings.json`
- Atomic writes with temp file + move pattern
- Automatic backup of existing file before save
- Graceful handling of corrupted files with timestamped backup
- Load returns defaults on any error (never crashes)

### HotkeyManager.cs
- Uses Windows API (user32.dll) for RegisterHotKey/UnregisterHotKey
- Process hotkeys via WndProc message handling (WM_HOTKEY = 0x0312)
- Must unregister all hotkeys on disposal
- Hotkey IDs managed internally (auto-increment)

## Common Patterns to Follow

### Adding a New Component Manager
1. Create class with IDisposable
2. Add private `_disposed` flag and `_disposeLock` object
3. Implement Dispose() with lock and GC.SuppressFinalize(this)
4. Add Serilog logger: `private static readonly ILogger Logger = LoggerConfig.ForContext<ClassName>();`
5. Log all major operations with context
6. Throw ObjectDisposedException if operations attempted after disposal

### Adding a New Web API Bridge Method
1. Add method to WindowsAppAPIBridge.cs
2. Create JavaScript wrapper that calls window.windowsAppAPI
3. Use EscapeForJavaScript() for string parameters
4. Return structured result (JSON object with success, result, error)
5. Handle both sync (ExecuteScriptAsync) and async (WebMessage) patterns
6. Add Func<> delegate property for CommandPaletteForm if needed

### Adding a New Command Palette Workflow
1. Add new WorkflowState enum value
2. Add state-specific UI in FilterResults() switch statement
3. Implement HandleSelection() case for the state
4. Add HandleEscape() case for back navigation
5. Update _hintLabel text to guide user
6. Use _searchBox.Text for user input, _resultsList for options

### Modifying Server Behavior
- **NEVER** modify server startup outside UnifiedServerManager
- **ALWAYS** use the multi-strategy shutdown sequence
- **ALWAYS** verify ports are released after shutdown
- **ALWAYS** log server lifecycle events with PIDs and ports
- Use lock(_lock) for thread-safe state changes

## Testing and Debugging

### Manual Testing Checklist
- [ ] Application starts without errors
- [ ] WebView2 loads Vite server (http://localhost:5000)
- [ ] LocalStorage server responds on port 5001 (/health endpoint)
- [ ] Global hotkeys work (Ctrl+Alt+P, Ctrl+K, etc.)
- [ ] Command palette opens and searches prompts
- [ ] Placeholder filling with suggestions works
- [ ] LLM execution completes successfully
- [ ] Clipboard operations work (copy, paste)
- [ ] System tray icon and menu work
- [ ] Application closes cleanly without orphan processes
- [ ] Settings persist across restarts
- [ ] Logs are written to %APPDATA%/PromptArq/logs/

### Common Issues and Solutions

**Port Already in Use**
- Symptom: HttpListenerException on startup
- Solution: Kill orphaned node.exe processes, verify ports 5000/3001/5001 are free
- Tools: `netstat -ano | findstr "5000"`, Task Manager, `taskkill /F /PID <pid>`

**WebView2 Not Loading**
- Symptom: Blank WebView2, WebView2RuntimeNotFoundException
- Solution: Install WebView2 Runtime from https://developer.microsoft.com/microsoft-edge/webview2/
- Verify: Check if Edge browser is installed (shares runtime)

**Servers Don't Stop on Exit**
- Symptom: Orphan node.exe processes remain
- Solution: Verify UnifiedServerManager.Stop() is called in all exit paths (ApplicationExit, ProcessExit, UnhandledException)
- Check logs for shutdown sequence execution

**JavaScript Execution Fails**
- Symptom: Null reference, timeout, or exception in ExecuteJavaScriptAsync
- Solution: Ensure WebView2 is initialized (CoreWebView2 != null), Vite server is ready, proper string escaping
- Debug: Check browser console logs in WebView2 DevTools (F12)

**Settings Not Persisting**
- Symptom: Settings reset on restart
- Solution: Check file permissions on %APPDATA%/PromptArq/settings.json
- Verify Save() is called before application exit
- Check logs for serialization or I/O errors

### Debugging Tips
- **Enable WebView2 DevTools**: Press F12 in the WebView2 control (in development builds)
- **Log Files**: Check `%APPDATA%/PromptArq/logs/log-{date}.txt` for detailed logs
- **Attach Debugger**: Visual Studio → Debug → Attach to Process → PromptArq.exe
- **Breakpoint Locations**: Program.cs (startup), MainForm.cs (event handlers), UnifiedServerManager.cs (shutdown)
- **Network Inspection**: Use Fiddler or browser DevTools Network tab for HTTP requests to port 5001

## Integration with Web App

### Storage Adapter Detection
Web app detects Windows app via HTTP server on port 5001:
- **HTTP Adapter**: Connects to http://localhost:5001 (see src/lib/http-storage-adapter.ts)
- **Fallback**: Browser localStorage if port 5001 not reachable
- **Key Prefix**: 'promptarq_' prefix used by all adapters, stripped by HTTP adapter

### WindowsAppAPI JavaScript Interface
Exposed at `window.windowsAppAPI` in web app (see src/lib/windows-api.ts):
- `getPrompts()` - Returns array of PromptInfo objects
- `getPlaceholders(promptId)` - Returns array of placeholder names
- `fillContent(promptId, values)` - Returns filled content string
- `executePrompt(promptId, content?)` - Executes prompt, posts result via WebMessage
- `getSystemPrompts()` - Returns array of SystemPromptInfo objects
- `executeOneTimePrompt(systemPrompt, userPrompt)` - Executes one-time prompt with system guidance

### Message Passing Protocol
```javascript
// JavaScript → C# (async execution result)
window.chrome.webview.postMessage({
  type: 'executeResult',
  success: true | false,
  result: 'string result',
  error: 'error message'
});
```

```csharp
// C# → JavaScript (execute command)
var script = $@"
  (() => {{
    window.windowsAppAPI.executePrompt('{promptId}', '{escapedContent}');
  }})()
";
await _webView2Manager.ExecuteJavaScriptAsync(script);
```

## Security Considerations

### Local Environment Trust
- All servers bind to localhost only (127.0.0.1)
- CORS enabled for local development (Access-Control-Allow-Origin: *)
- SQLite database has no authentication (file system ACLs only)
- WebView2 runs in same security context as application

### Input Sanitization
- **JavaScript Strings**: Always use EscapeForJavaScript() to prevent injection
- **SQL**: Use parameterized queries (SqliteCommand.Parameters.AddWithValue)
- **File Paths**: Validate paths are within expected directories (%APPDATA%/PromptArq)
- **JSON**: Use Newtonsoft.Json or System.Text.Json for parsing (no eval)

### Sensitive Data
- GitHub tokens stored in web app's storage (localStorage/SQLite), not in Windows app code
- Settings.json may contain hotkey configurations (no secrets)
- Logs may contain prompt content; consider this when logging

## Troubleshooting for Agents

### When Modifying Server Code
- **CRITICAL**: Test UnifiedServerManager.Stop() thoroughly with multiple stop strategies
- Verify no processes leak using Task Manager after application exit
- Check all ports (5000, 3001, 5001) are released
- Add logging to new shutdown logic

### When Adding New Windows API Bridge Methods
- Test JavaScript string escaping with quotes, newlines, backslashes
- Verify async operations complete (use TaskCompletionSource properly)
- Test with both small and large content (>10KB)
- Handle WebView2 disposal during execution

### When Modifying Command Palette
- Test all state transitions (forward and backward via Escape)
- Verify focus management (searchBox vs resultsList)
- Test with keyboard navigation only (no mouse)
- Ensure state resets properly on Hide()

### When Changing Settings Schema
- Add migration logic in Settings.Load() for old formats
- Maintain backward compatibility with existing settings.json files
- Document schema changes in commit message

## Additional Resources

- **Windows App Docs**: See WindowsApp/docs/ for architecture, API reference, user guide
- **Main AGENTS.md**: See root /AGENTS.md for web app guidance and storage adapter logic
- **WebView2 Docs**: https://docs.microsoft.com/microsoft-edge/webview2/
- **.NET 8 Docs**: https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8
- **Serilog Docs**: https://serilog.net/

## Quick Reference

### File Locations
- Settings: `%APPDATA%/PromptArq/settings.json`
- Database: `%APPDATA%/PromptArq/promptarq.db`
- Logs: `%APPDATA%/PromptArq/logs/log-{date}.txt`

### Port Registry
- 5000: Vite dev server (React app)
- 3001: OAuth proxy (GitHub authentication)
- 5001: LocalStorage HTTP server (SQLite backend)

### Key Classes by Concern
- **Server Management**: UnifiedServerManager, LocalStorageServer
- **UI Components**: MainForm, CommandPaletteForm, SettingsForm
- **Web Bridge**: WebView2Manager, WindowsAppAPIBridge
- **Native Integration**: HotkeyManager, NotificationManager, WindowStyleManager
- **Data Persistence**: Settings, PromptHistory, LocalStorageServer

### Code Modification Rules
1. **Never** bypass UnifiedServerManager for server lifecycle
2. **Always** use WindowsAppAPIBridge for web API calls (business logic stays in JS)
3. **Always** escape strings with EscapeForJavaScript() before embedding in JS
4. **Always** implement IDisposable for classes managing resources
5. **Always** use Serilog for logging (never Console.WriteLine)
6. **Always** handle exceptions gracefully with user notifications
7. **Always** test server shutdown thoroughly (no process leaks)
8. **Never** store secrets in C# code (use web app's storage)
9. **Always** maintain backward compatibility for Settings.json schema
10. **Always** update this AGENTS.md when adding new patterns or components
