# PromptArq Windows App - User Guide

## Table of Contents
- [Installation](#installation)
- [First Launch](#first-launch)
- [Main Window](#main-window)
- [Global Hotkeys](#global-hotkeys)
- [Command Palette](#command-palette)
- [Settings](#settings)
- [System Tray](#system-tray)
- [Tips & Tricks](#tips--tricks)

## Installation

### Prerequisites
- **Windows 10** or later (Windows 11 recommended)
- **.NET 8.0 Runtime** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **WebView2 Runtime** - Usually pre-installed on Windows 11
  - If needed: [Download here](https://developer.microsoft.com/microsoft-edge/webview2/)

### Installing from Release
1. Download the latest release from the releases page
2. Extract the ZIP file to a folder (e.g., `C:\Program Files\PromptArq`)
3. Run `PromptArq.exe`

### Building from Source
1. Clone the repository
2. Install Node.js dependencies:
   ```bash
   npm install
   ```
3. Build and run the Windows app:
   ```bash
   cd WindowsApp
   dotnet build
   dotnet run
   ```

## First Launch

On first launch, the application will:
1. Create configuration directory at `%APPDATA%\PromptArq`
2. Generate default settings with pre-configured hotkeys
3. Start the Vite development server (port 5000)
4. Start the LocalStorage server (port 5001)
5. Load the web application in WebView2

**First launch takes 2-3 seconds** while servers start up. Subsequent launches are faster as servers initialize in the background.

## Main Window

### Window Appearance
- **Borderless design** with dark theme
- **Resizable** by dragging edges
- **Dark blue title bar** (Windows 11 style)
- **Rounded corners** for modern look
- **Status bar** at bottom (visible only in debug builds)

### Interacting with the Web App
The main window displays the full PromptArq web application. All features from the web app are available:
- Create and edit prompts
- Organize in projects and categories
- Apply templates
- Tag and search prompts
- Export/import prompts

### Minimizing to Tray
Click the **X** button to minimize to system tray (app keeps running). The window doesn't close completely - it stays in the background for quick access via hotkeys.

To fully quit the app:
- Right-click tray icon → **Quit**
- Press `Ctrl+Alt+Q`
- File menu → **Exit** (when visible)

## Global Hotkeys

The application responds to system-wide keyboard shortcuts even when minimized or when another application has focus.

### Default Hotkeys

| Hotkey | Action | Description |
|--------|--------|-------------|
| `Ctrl+Alt+P` | Show/Hide Window | Toggle main window visibility |
| `Ctrl+K` | Command Palette | Open quick command palette |
| `Ctrl+Alt+S` | Settings | Open settings dialog |
| `Ctrl+Shift+N` | New Prompt | Create new prompt (focuses web app) |
| `Ctrl+Alt+Q` | Quit App | Gracefully exit application |

### Customizing Hotkeys
1. Press `Ctrl+Alt+S` or right-click tray icon → **Settings**
2. In the Settings dialog, you'll see all configured hotkeys
3. Click on a hotkey to edit:
   - Change the key (A-Z, 0-9, F1-F12, etc.)
   - Toggle modifiers (Ctrl, Alt, Shift, Win)
4. Click **Save** to apply changes
5. Hotkeys take effect immediately

**Note:** If a hotkey conflicts with another application, registration will fail. Try a different combination.

## Command Palette

The command palette provides quick access to prompts without opening the main window.

### Opening Command Palette
- Press `Ctrl+K` (default hotkey)
- Main window → Help menu → Command Palette

### Using Command Palette

#### Stage 1: Select Prompt
1. Command palette opens with search box focused
2. Type to search prompts by title or description
3. Use **Arrow Up/Down** to navigate results
4. Press **Enter** to select prompt
5. Press **Escape** to cancel

#### Stage 2: Select Action
After selecting a prompt, available actions depend on the prompt type:

**For Direct Execution Prompts** (default):
- **Paste** - Paste prompt to active window
- **Copy to Clipboard** - Copy prompt for manual use
- **Open in Editor** - Edit the prompt

**For LLM Execution Prompts** (marked with sparkle icon):
- **Execute & Paste** - Process through LLM and paste
- **Execute & Copy** - Process through LLM and copy
- **Open in Editor** - Edit the prompt

**For All Prompts with Placeholders:**
- **Fill Placeholders** - Fill template variables first (appears at top)

Navigate with **Arrow keys**, select with **Enter**.

#### Stage 3: Fill Placeholders (if any)
If the prompt contains placeholders like `{{name}}` or `{{project}}`:
1. A text box appears for each placeholder
2. Type the value and press **Enter**
3. Repeat for all placeholders
4. After last placeholder, output options appear

#### Stage 4: Output Options (after filling placeholders)
After filling all placeholders, choose output method:

**Always Available:**
- **Copy Generated Prompt** - Copy the filled template to clipboard

**Additional Options Based on Prompt Type:**
- Direct prompts: **Paste to Active Window**
- LLM prompts: **Execute & Paste**, **Execute & Copy**

#### Stage 5: Action Execution
- **Paste**: Content is copied to clipboard, then pasted to active window using `Ctrl+V`
- **Copy**: Content is copied to clipboard with success toast notification
- **Execute & [Action]**: Content is processed through LLM pipeline before pasting/copying

**Toast notifications** appear briefly to confirm actions (non-blocking).

### Command Palette Tips
- **Click outside** the palette to close it
- **Escape** works at any stage to cancel
- **No placeholders?** Skip directly from action selection to execution
- Search is **fuzzy** - matches partial words and descriptions

## Settings

### Accessing Settings
- Press `Ctrl+Alt+S`
- Right-click system tray icon → **Settings**
- Main window menu → **Settings**

### Settings Dialog Features
- **Hotkey configuration** - Add, edit, or remove hotkeys
- **Window preferences** - Default size, startup behavior
- **Single instance** - Only one settings window can be open
- **Toggle behavior** - Press `Ctrl+Alt+S` again to close settings

### Settings Persistence
Settings are automatically saved to:
```
%APPDATA%\PromptArq\settings.json
```

Changes take effect immediately. No restart required.

## System Tray

### Tray Icon
The PromptArq icon appears in the system tray (notification area) when the app is running.

### Tray Menu
Right-click the tray icon to access:
- **Show/Hide** - Toggle main window visibility
- **Settings** - Open settings dialog
- **Help > About** - View application information
- **Quit** - Exit application gracefully

### Tray Behavior
- Window minimizes to tray (doesn't close)
- Closing the window **does not** exit the app
- App continues running in background for hotkey access
- Single-click tray icon to restore window

## Tips & Tricks

### Keyboard Navigation
- Command palette is **keyboard-first**
- Tab through settings dialog controls
- Arrow keys for list navigation
- Enter to confirm, Escape to cancel

### Quick Workflow
1. Press `Ctrl+K` from anywhere
2. Type prompt name
3. Press Enter twice (select prompt, select action)
4. Fill placeholders if needed
5. Prompt automatically pastes to active window

**Total time: ~5 seconds** from hotkey to pasted prompt!

### Multiple Monitors
The application remembers window position across sessions, including which monitor you prefer.

### Performance
- **First command palette open** loads prompts from storage (~200ms)
- **Subsequent opens** use cached data (instant)
- **Hotkey response** is near-instant (<100ms)

### Debug Mode
When running from Visual Studio or `dotnet run`, a status bar appears at the bottom showing:
- Server status
- Current operation
- Debug messages

This helps troubleshoot issues during development.

### Clipboard Management
When using **Copy to Clipboard** action:
- Previous clipboard content is replaced
- Toast notification confirms copy
- Paste manually with `Ctrl+V` in target app

When using **Paste to Active Window**:
- Clipboard is used temporarily
- Original clipboard content is **not restored**
- Target window must be ready to receive input

### Safe Shutdown
The app ensures clean shutdown:
- Vite server stops
- LocalStorage server stops
- All child processes terminated
- Ports released

If shutdown hangs, multiple fallback strategies activate automatically.

### Troubleshooting Quick Tips

**Command palette not showing prompts?**
- Wait a moment for web app to load storage
- Check main window can access web app
- Restart application

**Hotkeys not responding?**
- Check for conflicts with other apps
- Try different key combination
- Run as administrator if issues persist

**Window won't restore from tray?**
- Double-click tray icon
- Use `Ctrl+Alt+P` hotkey
- Right-click tray → Show

**Web app not loading?**
- Wait 3-5 seconds for servers to start
- Check status bar (debug mode)
- Restart application

**Servers won't stop?**
- App uses multiple cleanup strategies
- May take 5-10 seconds for full shutdown
- Check Task Manager for orphaned processes

## Keyboard Shortcuts Reference

### Global (System-wide)
- `Ctrl+Alt+P` - Show/Hide Window
- `Ctrl+K` - Command Palette
- `Ctrl+Alt+S` - Settings
- `Ctrl+Shift+N` - New Prompt
- `Ctrl+Alt+Q` - Quit App

### Command Palette
- `↑` `↓` - Navigate list
- `Enter` - Select item
- `Escape` - Close/Cancel
- Click outside - Close

### Settings Dialog
- `Tab` - Next control
- `Shift+Tab` - Previous control
- `Enter` - Save
- `Escape` - Cancel

## Advanced Usage

### Running Multiple Instances
**Not supported.** The app uses fixed ports (5000, 5001) and will conflict if multiple instances run.

### Portable Installation
The app can run portably, but settings are still stored in `%APPDATA%\PromptArq`. To make truly portable:
1. Modify `Settings.cs` to use relative path
2. Rebuild application
3. Package with modified settings location

### Command Line Arguments
Currently not supported. App always starts in normal mode.

### Integration with Other Apps
Use the **Command Palette → Paste** feature to integrate with:
- IDEs (VS Code, Visual Studio, JetBrains)
- Text editors (Notepad++, Sublime)
- Chat applications (Slack, Teams)
- Email clients (Outlook, Thunderbird)
- Any application accepting text input

### Prompt Organization
Best practices for command palette efficiency:
- Use **descriptive titles** for easy searching
- Add **keywords** to descriptions
- Keep **frequently-used prompts** at top (starred)
- Use **tags** for categorization

## Getting Help

- **Issues:** Report bugs on GitHub Issues
- **Documentation:** See `/WindowsApp/docs/` folder
- **Architecture:** Read `Architecture.md` for technical details
- **Development:** See `Development.md` for building and extending
