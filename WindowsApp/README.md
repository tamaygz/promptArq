# PromptArq Windows Desktop Application

A native Windows desktop application providing seamless access to PromptArq with global hotkeys, command palette, and system tray integration.

## Quick Start

### Installation

**Prerequisites:**
- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (pre-installed on Windows 11)

**From Release:**
1. Download latest release
2. Extract to folder
3. Run `PromptArq.exe`

**From Source:**
```bash
cd WindowsApp
Scripts\build.bat
Scripts\run.bat
```
Or use dotnet directly:
```bash
cd WindowsApp
dotnet build
dotnet run
```

### First Launch

The application will:
- Start Vite dev server (port 5000)
- Start LocalStorage server (port 5001)
- Load web application in WebView2
- Create settings in `%APPDATA%\PromptArq`

**Tip:** First launch takes 2-3 seconds for servers to start.

## Key Features

### 🎯 Command Palette
Press `Ctrl+K` to quickly search and use prompts without opening the main window.

**Workflow:**
1. Search prompts by title/description
2. Select action (Paste or Copy)
3. Fill placeholders (if any)
4. Prompt automatically executes

### ⌨️ Global Hotkeys
System-wide shortcuts work even when minimized:
- `Ctrl+Alt+P` - Show/Hide Window
- `Ctrl+K` - Command Palette
- `Ctrl+Alt+S` - Settings
- `Ctrl+Shift+N` - New Prompt
- `Ctrl+Alt+Q` - Quit App

### 🖥️ Native Integration
- Borderless window with dark theme
- System tray icon with menu
- Rounded corners and modern styling
- Status bar (debug mode only)

### ⚙️ Settings
Customize hotkeys, window preferences, and startup behavior. Settings persist between sessions.

## Documentation

Comprehensive documentation available in [`docs/`](docs/):
- **[Architecture.md](docs/Architecture.md)** - Technical architecture, components, data flow
- **[UserGuide.md](docs/UserGuide.md)** - Installation, features, usage instructions
- **[CommandPalette.md](docs/CommandPalette.md)** - Detailed command palette documentation
- **[Development.md](docs/Development.md)** - Building, debugging, contributing

## Quick Reference

### Keyboard Shortcuts
| Hotkey | Action |
|--------|--------|
| `Ctrl+Alt+P` | Show/Hide Window |
| `Ctrl+K` | Command Palette |
| `Ctrl+Alt+S` | Settings |
| `Ctrl+Shift+N` | New Prompt |
| `Ctrl+Alt+Q` | Quit App |

### Command Palette Navigation
| Key | Action |
|-----|--------|
| `↓` `↑` | Navigate list |
| `Enter` | Select item |
| `Escape` | Close/Cancel |
| Click outside | Close |

## Building

### Debug Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build -c Release
```

### Self-Contained Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/PromptArq.exe`

## Architecture

```
┌─────────────────────────────────────────┐
│          PromptArq.exe                  │
│  ┌───────────────────────────────────┐  │
│  │  MainForm (Windows Forms)         │  │
│  │  - WebView2 (Chromium)            │  │
│  │  - System Tray Icon               │  │
│  │  - Hotkey Manager                 │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  CommandPaletteForm               │  │
│  │  - Search & Select Prompts        │  │
│  │  - Multi-stage Workflow           │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  UnifiedServerManager             │  │
│  │  - Vite Dev Server (5000)         │  │
│  │  - LocalStorage Server (5001)     │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Technology Stack
- **.NET 8.0** - Windows Forms framework
- **WebView2** - Chromium-based web rendering
- **Vite** - Web application development
- **Node.js** - Development server
- **Newtonsoft.Json** - Configuration management

## System Requirements
- **OS:** Windows 10 (1809+) or Windows 11
- **Runtime:** .NET 8.0 Runtime
- **WebView2:** Microsoft Edge WebView2 Runtime
- **Memory:** 200-300 MB
- **Disk:** 50 MB (application + dependencies)

## Troubleshooting

### App won't start
- Verify .NET 8.0 installed: `dotnet --version`
- Check WebView2 Runtime installed
- Review Event Viewer for errors

### Hotkeys not working
- Check for conflicts with other applications
- Try different key combinations in Settings
- Run as administrator if issues persist

### Command palette empty
- Wait 2-3 seconds for web app to load
- Verify main window can access web app
- Restart application

### Servers won't stop
- Application uses multiple cleanup strategies
- Check Task Manager for orphaned processes
- Manually kill Node.js processes if needed

## Contributing

See [Development.md](docs/Development.md) for:
- Setting up development environment
- Building and debugging
- Adding new features
- Code style guidelines

## License

See [LICENSE](../LICENSE) file for details.

## Support

- **Documentation:** [docs/](docs/) folder
- **Issues:** [GitHub Issues](https://github.com/tamaygz/promptArq/issues)
- **Discussions:** [GitHub Discussions](https://github.com/tamaygz/promptArq/discussions)

## Version

Current version: 1.0.0

See [releases](https://github.com/tamaygz/promptArq/releases) for changelog and downloads.
