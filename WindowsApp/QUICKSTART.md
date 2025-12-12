# PromptArq Windows App - Quick Start Guide

This guide will help you get the PromptArq Windows application up and running quickly.

## What You Need

1. **Windows 10 or later**
2. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Node.js (v16+) and npm** - [Download here](https://nodejs.org/)
4. **WebView2 Runtime** - Usually pre-installed on Windows 11, [download here](https://developer.microsoft.com/microsoft-edge/webview2/) if needed

## First Time Setup

### 1. Install Dependencies

Before running the Windows app, make sure the Vite project dependencies are installed:

```bash
# Navigate to the project root (parent of WindowsApp)
cd ..

# Install npm dependencies
npm install
```

### 2. Build the Windows App

**Option A: Using the build script (Recommended)**
```cmd
cd WindowsApp
build.bat
```

**Option B: Manual build**
```cmd
cd WindowsApp
dotnet restore
dotnet build -c Release
```

### 3. Run the Application

**Option A: Run from build output**
```cmd
bin\Release\net8.0-windows\PromptArq.exe
```

**Option B: Run with dotnet**
```cmd
dotnet run
```

## What Happens When You Run It

1. The application window opens
2. The Vite development server automatically starts in the background
3. After a few seconds, the PromptArq web interface loads in the window
4. The app is now ready to use!

## Using the Application

### Accessing from System Tray

- **Minimize to tray**: Click the minimize button or close the window (you'll be asked)
- **Show from tray**: Double-click the tray icon
- **Quick actions**: Right-click the tray icon for a menu

### Hotkeys (Default)

- **Ctrl+Alt+P**: Show/Hide the application window
- **Ctrl+Shift+N**: Create a new prompt
- **Ctrl+Alt+S**: Open settings

### Configuring Hotkeys

1. Open the app
2. Go to **File → Settings** (or press Ctrl+Alt+S)
3. Modify existing hotkeys or add new ones:
   - Select the key combination
   - Choose modifiers (Ctrl, Alt, Shift, Win)
   - Click **Save**
4. Changes take effect immediately

### Developer Tools

Access the browser developer tools for debugging:
- Go to **View → Developer Tools**
- Or press F12 (if configured as a hotkey)

## Creating a Standalone Executable

To create a version you can share with others without requiring .NET installation:

```cmd
build-publish.bat
```

This creates a self-contained executable at:
```
bin\Release\net8.0-windows\win-x64\publish\PromptArq.exe
```

You can copy this entire `publish` folder to another Windows machine and run it directly.

**Note**: Recipients will still need:
- WebView2 Runtime (pre-installed on Windows 11)
- Node.js and npm (to run the Vite server)

## Troubleshooting

### "WebView2 not found"
Install the WebView2 Runtime: https://developer.microsoft.com/microsoft-edge/webview2/

### "Failed to start Vite"
- Check that Node.js and npm are installed: `node --version` and `npm --version`
- Make sure npm dependencies are installed in the parent directory: `npm install`
- Verify port 5173 is not in use by another application

### "Hotkeys not working"
- Ensure no other application is using the same key combination
- Try different key combinations in Settings
- Check that the app is running (look for the system tray icon)

### App shows a blank page
- Wait a few more seconds for the Vite server to start
- Check **View → Refresh** to reload
- Verify the Vite server is running (look at the status bar)

## Advanced Configuration

### Changing the Vite Port

If you need to use a different port:

1. Edit `MainForm.cs` and change the `VitePort` constant
2. Update your `package.json` or `vite.config.ts` to use the same port
3. Rebuild the application

### Custom Actions

To add custom hotkey actions:

1. Edit `MainForm.cs`
2. Find the `RegisterHotkeys()` method
3. Add a new action case in the switch statement
4. Rebuild the application

### Settings Location

User settings are stored at:
```
%APPDATA%\PromptArq\settings.json
```

You can manually edit this file to change settings, but it's recommended to use the Settings UI.

## Next Steps

- Explore the PromptArq web interface features
- Configure your preferred hotkeys
- Customize the window size and position (saved automatically)
- Check out the main [README](./README.md) for full feature documentation

## Need Help?

- Check the [main README](./README.md) for detailed documentation
- Review the code - it's well-commented
- Open an issue on GitHub

---

**Enjoy using PromptArq!** 🚀
