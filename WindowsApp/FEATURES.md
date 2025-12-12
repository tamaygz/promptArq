# PromptArq Windows App - Features & Architecture

## Core Features

### 1. Native Windows Integration

The application provides a true native Windows experience:

- **WebView2 Integration**: Uses Microsoft Edge WebView2 to render the web application with full Chromium engine support
- **Native Menus**: Standard Windows File/View/Help menus
- **System Tray**: Minimize to system tray with quick access menu
- **Window State Persistence**: Remembers your window size and position

### 2. Automatic Vite Server Management

No need to manually start the development server:

- **Auto-start**: Vite server starts automatically when you launch the app
- **Process Management**: Server lifecycle is managed by the app
- **Status Monitoring**: Real-time status updates in the status bar
- **Clean Shutdown**: Server is properly terminated when you close the app

### 3. Global Hotkeys

System-wide keyboard shortcuts work even when the app is minimized:

- **Windows API Integration**: Uses native Windows hotkey registration
- **Fully Configurable**: Change any hotkey through the Settings UI
- **Conflict Detection**: Won't register conflicting hotkeys
- **Persistent**: Settings are saved between sessions

### 4. Settings Management

User preferences are automatically saved:

- **JSON Configuration**: Settings stored in `%APPDATA%\PromptArq\settings.json`
- **Hotkey Configuration**: Add, remove, or modify hotkeys
- **Window Preferences**: Size and position saved automatically
- **Default Values**: Sensible defaults on first run

## Technical Architecture

### Technology Stack

- **Framework**: .NET 8.0 Windows Forms
- **Web Rendering**: Microsoft.Web.WebView2
- **JSON**: Newtonsoft.Json for configuration
- **Process Management**: System.Diagnostics.Process for Vite
- **Windows API**: P/Invoke for global hotkeys

### Project Structure

```
WindowsApp/
├── Program.cs              # Application entry point
├── MainForm.cs             # Main window with WebView2
├── SettingsForm.cs         # Settings dialog UI
├── HotkeyManager.cs        # Global hotkey registration
├── Settings.cs             # Configuration persistence
├── PromptArqApp.csproj     # Project file
└── PromptArqApp.sln        # Solution file
```

### Key Components

#### MainForm.cs
The main application window that:
- Hosts the WebView2 control
- Manages the Vite server process
- Handles window events (resize, close, minimize)
- Provides menu and status bar
- Integrates with system tray

#### HotkeyManager.cs
Manages global hotkeys using Windows API:
- RegisterHotKey/UnregisterHotKey P/Invoke calls
- Message processing for WM_HOTKEY
- Dynamic registration/unregistration
- Cleanup on disposal

#### Settings.cs
Handles configuration persistence:
- Load/Save to JSON file
- Default hotkey initialization
- Window state management
- AppData directory handling

#### SettingsForm.cs
Provides the settings UI:
- DataGridView for hotkey editing
- Add/Remove/Reset functionality
- Key and modifier selection
- Validation and saving

## How It Works

### Application Startup Flow

1. **Program.cs** initializes Windows Forms and creates MainForm
2. **MainForm constructor** loads settings and initializes UI
3. **StartViteServer()** launches npm run dev in background
4. **WebView2 initialization** prepares the web control
5. **WaitForViteAndNavigate()** monitors for Vite server ready
6. **Navigate** to localhost:5173 when server is ready
7. **RegisterHotkeys()** registers global keyboard shortcuts

### Vite Server Management

The app manages the Vite process lifecycle:

```csharp
// Start Vite in project root
_viteProcess = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "npm",
        Arguments = "run dev",
        WorkingDirectory = projectRoot,
        RedirectStandardOutput = true,
        CreateNoWindow = true
    }
};

// Monitor output for "localhost:5173"
_viteProcess.OutputDataReceived += (sender, e) => {
    if (e.Data.Contains("localhost:5173")) {
        _isViteReady = true;
        // Navigate WebView2 to localhost
    }
};
```

### Hotkey Processing

Global hotkeys use Windows API:

```csharp
// Register a hotkey
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

// Process hotkey messages
protected override void WndProc(ref Message m) {
    if (m.Msg == WM_HOTKEY) {
        // Execute associated action
    }
}
```

## Customization Guide

### Adding Custom Hotkey Actions

Edit `MainForm.cs` in the `RegisterHotkeys()` method:

```csharp
Action action = hotkey.Action switch
{
    "Show/Hide Window" => () => this.Invoke((MethodInvoker)delegate { ToggleWindow(); }),
    "New Prompt" => () => this.Invoke((MethodInvoker)delegate { ExecuteJavaScript("..."); }),
    "Settings" => () => this.Invoke((MethodInvoker)delegate { ShowSettings(); }),
    "Your Custom Action" => () => this.Invoke((MethodInvoker)delegate { YourMethod(); }),
    _ => () => { }
};
```

### Changing the Vite Port

If you need a different port:

1. Update `VitePort` constant in `MainForm.cs`
2. Update your `package.json` or `vite.config.ts`
3. Rebuild the application

### Customizing the UI

The application uses standard Windows Forms:

- Menu items: Edit `InitializeComponent()` in `MainForm.cs`
- Status bar: Modify `_statusLabel` text
- System tray icon: Replace `SystemIcons.Application` with custom icon
- Window appearance: Adjust `Size`, `FormBorderStyle`, etc.

### Adding Menu Items

In `MainForm.cs`, `InitializeComponent()`:

```csharp
_fileMenu.DropDownItems.Add("Your Menu Item", null, (s, e) => YourMethod());
```

## Advanced Features

### JavaScript Execution

Execute JavaScript in the web page:

```csharp
private void ExecuteJavaScript(string script)
{
    if (_webView?.CoreWebView2 != null)
    {
        await _webView.CoreWebView2.ExecuteScriptAsync(script);
    }
}
```

### Developer Tools

Access WebView2 developer tools:
- From menu: View → Developer Tools
- Programmatically: `_webView.CoreWebView2.OpenDevToolsWindow()`

### Full Screen Mode

Toggle borderless fullscreen:

```csharp
private void ToggleFullscreen()
{
    if (FormBorderStyle == FormBorderStyle.None) {
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
    } else {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
    }
}
```

## Performance Considerations

### Memory Usage

- WebView2 uses approximately 50-100MB
- Vite process adds 30-50MB
- Total footprint: ~150-200MB typical

### Startup Time

- Cold start: 2-5 seconds (Vite compilation)
- Warm start: 1-2 seconds (Vite cache)
- WebView2 initialization: <1 second

### CPU Usage

- Idle: <1% CPU
- Vite compilation: 10-30% CPU (brief)
- WebView2 rendering: 1-5% CPU (interactive)

## Security Considerations

### WebView2 Security

- Same security model as Microsoft Edge
- Isolated process architecture
- Automatic security updates via Windows Update

### Process Isolation

- Vite server runs in separate process
- WebView2 renderer is sandboxed
- No direct file system access from web content

### Settings Storage

- Configuration stored in user's AppData
- No sensitive data (only hotkey preferences)
- JSON format for easy inspection

## Future Enhancements

Possible future improvements:

- [ ] Custom application icon
- [ ] Installer (MSI/MSIX package)
- [ ] Auto-update mechanism
- [ ] Multiple window/tab support
- [ ] Custom URL schemes
- [ ] Offline mode support
- [ ] Theme customization
- [ ] Portable mode (no installation)
- [ ] Command line arguments
- [ ] Tray menu quick actions

## Troubleshooting

### Common Issues

**Issue**: WebView2 not found
- **Solution**: Install WebView2 Runtime from Microsoft

**Issue**: Vite server won't start
- **Solution**: Check Node.js/npm installation and port availability

**Issue**: Hotkeys not working
- **Solution**: Check for key conflicts, try different combinations

**Issue**: Window state not saved
- **Solution**: Check write permissions to `%APPDATA%\PromptArq`

### Debug Mode

Enable debug output by:
1. Running with `dotnet run` from command line
2. Output appears in console
3. Check `Debug.WriteLine()` statements

### Logs Location

- Application logs: Console output
- Vite logs: Redirected to application output
- Settings: `%APPDATA%\PromptArq\settings.json`

## Contributing

To contribute improvements:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly on Windows
5. Submit a pull request

---

**Need more help?** Check the [README](./README.md) or [Quick Start Guide](./QUICKSTART.md).
