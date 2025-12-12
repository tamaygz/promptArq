# PromptArq Windows Application

A native Windows desktop application that hosts the PromptArq Vite web application with global hotkey support.

## Features

- **Embedded Web Application**: Runs the PromptArq Vite app in a native Windows window using WebView2
- **Global Hotkeys**: Configure system-wide keyboard shortcuts for quick access
- **System Tray Integration**: Minimize to system tray for quick access
- **Auto-start Vite Server**: Automatically starts and manages the Vite development server
- **Settings Management**: Persistent configuration for hotkeys and window preferences

## Prerequisites

- Windows 10 or later
- .NET 8.0 Runtime or SDK
- Node.js (v16 or later) and npm
- WebView2 Runtime (usually pre-installed on Windows 11)

## Building the Application

1. Navigate to the WindowsApp directory:
   ```bash
   cd WindowsApp
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the application:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

## Publishing a Standalone Executable

To create a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

The executable will be in `bin/Release/net8.0-windows/win-x64/publish/`

## Default Hotkeys

- **Ctrl+Alt+P**: Show/Hide application window
- **Ctrl+Shift+N**: New Prompt (simulates clicking the new prompt button)
- **Ctrl+Alt+S**: Open Settings

## Configuring Hotkeys

1. Open the application
2. Go to **File → Settings** (or press Ctrl+Alt+S)
3. Modify existing hotkeys or add new ones
4. Click **Save** to apply changes

### Available Actions

- **Show/Hide Window**: Toggle the main application window
- **New Prompt**: Trigger the new prompt action in the web app
- **Settings**: Open the settings dialog
- Custom actions can be added (requires JavaScript integration)

## How It Works

1. **Vite Server Management**: The app automatically starts the Vite development server from the parent directory
2. **WebView2 Integration**: Uses Microsoft Edge WebView2 to render the web application
3. **Hotkey System**: Windows API integration for global keyboard shortcuts
4. **Settings Persistence**: Saves configuration to `%APPDATA%\PromptArq\settings.json`

## System Tray

The application includes a system tray icon with the following features:
- Double-click to show/hide the window
- Right-click for quick menu access
- Minimizing the window sends it to the tray

## Menu Options

### File Menu
- **Settings**: Configure hotkeys and preferences
- **Exit**: Close the application

### View Menu
- **Refresh**: Reload the web application
- **Developer Tools**: Open WebView2 developer tools
- **Toggle Fullscreen**: Switch between windowed and fullscreen mode

### Help Menu
- **About**: Show application information

## Troubleshooting

### WebView2 Not Found
If you get a WebView2 error, install the WebView2 Runtime:
https://developer.microsoft.com/en-us/microsoft-edge/webview2/

### Vite Server Won't Start
- Ensure Node.js and npm are installed and in PATH
- Check that port 5173 is not already in use
- Verify npm dependencies are installed in the parent directory (`npm install`)

### Hotkeys Not Working
- Check if another application is using the same key combination
- Make sure the application is running (can be in system tray)
- Try different key combinations in Settings

## Project Structure

```
WindowsApp/
├── PromptArqApp.csproj    # Project configuration
├── Program.cs              # Application entry point
├── MainForm.cs             # Main window with WebView2
├── SettingsForm.cs         # Settings dialog
├── HotkeyManager.cs        # Global hotkey registration
├── Settings.cs             # Settings persistence
└── README.md               # This file
```

## Development Notes

- The application assumes the Vite project is in the parent directory (`../`)
- Vite server runs on port 5173 by default
- Settings are stored in `%APPDATA%\PromptArq\settings.json`
- The app automatically manages the Vite process lifecycle

## License

Same as the parent PromptArq project (MIT License)
