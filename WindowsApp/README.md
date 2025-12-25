# PromptArq Windows Desktop Application

A native Windows desktop application providing seamless access to PromptArq with global hotkeys, command palette, and system tray integration.

## Quick Start

### Installation

**Prerequisites:**
- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (pre-installed on Windows 11)

**From Source:**
```bash
cd WindowsApp
dotnet build
dotnet run
```

Or use convenience scripts:
```bash
Scripts\build.bat
Scripts\run.bat
```

### First Launch

The application will:
- Start Vite dev server (port 5000)
- Start LocalStorage server (port 5001)
- Load web application in WebView2
- Create settings in `%APPDATA%\PromptArq\settings.json`

**Note:** First launch takes 2-3 seconds for servers to start.

## Key Features

### 🎯 Command Palette
Press `Ctrl+K` to quickly search and use prompts without opening the main window.

**Workflow:**
1. Search prompts by title/description
2. Select action (Paste or Copy)
3. Fill placeholders (if any)
4. Prompt pasted to active window or copied to clipboard

### ⌨️ Global Hotkeys
Access PromptArq from anywhere in Windows:

| Hotkey | Action |
|--------|--------|
| `Ctrl+Alt+P` | Show/Hide main window |
| `Ctrl+K` | Open command palette |
| `Ctrl+Alt+S` | Open settings |
| `Ctrl+Shift+N` | Create new prompt |
| `Ctrl+Alt+Q` | Quit application |

**All hotkeys are customizable in Settings.**

### 📌 System Tray
Minimizes to system tray for quick access:
- Left-click: Show/hide window
- Right-click: Context menu

### 🎨 Theming System
Native theming system with customizable color schemes:
- **Built-in themes:** Dark Blue (default), Light, High Contrast
- **Theme switching:** Change themes via Settings dialog
- **Hot-reload support:** Update themes without restarting (dev mode)
- **Custom themes:** Create your own themes with JSON files

See [docs/ThemeGuide.md](docs/ThemeGuide.md) for theming documentation.

## Architecture

Built with component-based architecture for maintainability:

**Core Components:**
- **ThemeManager** - Theme loading and application
- **WindowStyleManager** - Consistent dark mode styling
- **NotificationManager** - Toast notifications
- **WebView2Manager** - WebView2 lifecycle management
- **WindowsAppAPIBridge** - Web app communication
- **HotkeyManager** - Global hotkey handling
- **UnifiedServerManager** - Server process management

See [docs/Architecture.md](docs/Architecture.md) for detailed architecture documentation.

## Development

### Project Structure
```
WindowsApp/
├── MainForm.cs                 # Primary window
├── CommandPaletteForm.cs       # Quick access dialog
├── SettingsForm.cs             # Configuration UI
├── Theming/                    # Theme system
│   ├── Theme.cs                # Theme model
│   ├── ThemeLoader.cs          # Theme file I/O
│   ├── ThemeManager.cs         # Theme management service
│   └── ThemeApplicator.cs      # Theme application logic
├── Themes/                     # Built-in theme files
│   ├── DarkBlue.theme.json     # Default dark theme
│   ├── Light.theme.json        # Light theme
│   └── HighContrast.theme.json # High contrast theme
├── WindowStyleManager.cs       # Window styling component
├── NotificationManager.cs      # Toast notifications component
├── WebView2Manager.cs          # WebView2 management component
├── WindowsAppAPIBridge.cs      # Web API bridge component
├── HotkeyManager.cs            # Hotkey registration
├── UnifiedServerManager.cs     # Server lifecycle
├── LocalStorageServer.cs       # Storage HTTP server
├── Settings.cs                 # Configuration model
└── docs/                       # Documentation
    ├── Architecture.md         # System architecture
    ├── WindowsAPI.md           # API reference
    ├── CommandPalette.md       # Command palette details
    ├── ThemeGuide.md           # Theming documentation
    ├── Development.md          # Dev guidelines
    └── UserGuide.md            # User documentation
```

### Building

**Debug Build:**
```bash
dotnet build
```

**Release Build:**
```bash
dotnet build -c Release
```

**Publish (Single File):**
```bash
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

### Running

**Development:**
```bash
dotnet run
```

**Production:**
```bash
.\bin\Release\net8.0-windows\PromptArq.exe
```

### Testing

The application automatically handles:
- Server startup and shutdown
- WebView2 initialization
- Hotkey registration
- Settings persistence

**Manual Testing:**
1. Launch application
2. Wait for "Ready" status
3. Test hotkeys
4. Test command palette (Ctrl+K)
5. Verify system tray behavior

## Configuration

Settings are stored in: `%APPDATA%\PromptArq\settings.json`

**Example:**
```json
{
  "Hotkeys": [
    {
      "Action": "Command Palette",
      "Key": "K",
      "Modifiers": ["Control"]
    }
  ],
  "WindowSize": {
    "Width": 1200,
    "Height": 800
  },
  "CurrentTheme": "DarkBlue"
}
```

### Theme Files

Theme files are stored in: `%APPDATA%\PromptArq\Themes\`

Create custom themes by adding `.theme.json` files to this directory.
See [docs/ThemeGuide.md](docs/ThemeGuide.md) for details.

## Troubleshooting

### Servers don't start
- Ports 5000 and 5001 may be in use
- Check Task Manager for orphaned node.exe processes
- Run `npm run kill` in project root

### WebView2 not loading
- Ensure WebView2 Runtime is installed
- Check firewall settings
- Verify localhost access

### Hotkeys not working
- Check for hotkey conflicts with other applications
- Try reconfiguring in Settings
- Restart application

### Application won't close
- Use Ctrl+Alt+Q or system tray → Exit
- If hung, kill process in Task Manager

## Documentation

- [Architecture.md](docs/Architecture.md) - System design and components
- [ThemeGuide.md](docs/ThemeGuide.md) - Theming system documentation
- [WindowsAPI.md](docs/WindowsAPI.md) - Web app ↔ Windows app API
- [CommandPalette.md](docs/CommandPalette.md) - Command palette workflows
- [Development.md](docs/Development.md) - Development guidelines
- [UserGuide.md](docs/UserGuide.md) - End-user documentation

## License

See [LICENSE](../LICENSE) file in repository root.
