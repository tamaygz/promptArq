# Command Palette Feature - Alfred-Style Prompt Launcher

## Overview

The Command Palette is an Alfred/Spotlight-inspired feature that allows you to quickly search and perform actions on your prompts using a global hotkey. No need to navigate through the web interface - press **Ctrl+K** anywhere and instantly access all your prompts and their actions.

## Features

### ?? Quick Search
- **Fuzzy search** across all prompts
- Search by:
  - Title
  - Description
  - Content
  - Project name
  - Category
  - Tags

### ?? Keyboard-First Navigation
- **Arrow keys** to navigate results
- **Enter** to select
- **Escape** or **Backspace** to go back
- **Type to filter** in real-time

### ?? Two-Level Interface

#### Level 1: Prompt Search
- Shows all your prompts in a beautiful list
- Live filtering as you type
- Displays prompt title, description, and project badge
- Color-coded project indicators

#### Level 2: Action Menu
When you select a prompt, you get contextual actions:
- **Execute** - Run the prompt with the LLM
- **Copy to Clipboard** - Copy prompt content
- **Fill Placeholders** - Fill in template variables (if prompt has placeholders)
- **Open in Editor** - Edit the prompt
- **Improve with AI** - Enhance the prompt using AI
- **Export** - Save to JSON file
- **Share** - Generate a share link
- **Archive/Restore** - Archive or restore from archive

## Usage

### Opening the Palette
Press **Ctrl+K** anywhere (even when the app is hidden or minimized)

Alternative: Set a custom hotkey in Settings

### Searching for Prompts
1. Start typing to filter prompts
2. Use **Up/Down arrows** to navigate
3. Press **Enter** to see actions for that prompt

### Executing an Action
1. Navigate to desired action with arrow keys
2. Press **Enter** to execute
3. Some actions will bring the main window to focus (e.g., Execute, Open in Editor)
4. Others work in the background (e.g., Copy, Export)

### Going Back
- Press **Escape** or **Backspace** to return to prompt search
- Press **Escape** again to close the palette

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+K` | Open Command Palette |
| `Type` | Filter results |
| `?` `?` | Navigate list |
| `Enter` | Select item |
| `Esc` | Go back / Close |
| `Backspace` | Go back (from actions) |

## Visual Design

The Command Palette features a modern, dark-themed interface:
- **Semi-transparent overlay** (97% opacity)
- **Rounded corners** for a polished look
- **Color-coded project badges** for quick visual identification
- **Hover states** with accent colors
- **Clear typography** with Segoe UI font
- **Status hints** at the bottom

## Technical Implementation

### Architecture
- **CommandPaletteForm.cs** - Main palette UI
- **PromptAction.cs** - Action definitions and prompt data models
- **MainForm.cs** - Integration with main app and WebView2
- **Settings.cs** - Hotkey configuration

### Data Flow
1. **Hotkey pressed** ? Triggers ShowCommandPalette()
2. **Fetch prompts** ? Reads from localStorage via JavaScript execution
3. **Display results** ? Renders in CommandPaletteForm
4. **Action selected** ? Executes corresponding JavaScript or C# method
5. **Result** ? Shows feedback or brings focus to main window

### Communication with Web App
The palette communicates with the React web app through:
- **LocalStorage reading** - Gets prompts, projects, categories, tags
- **JavaScript execution** - Triggers web app dialogs and actions
- **SessionStorage** - Passes context between palette and web app
- **Custom events** - Dispatches events to trigger web app features

## Customization

### Changing the Hotkey
1. Open **Settings** (Ctrl+Alt+S)
2. Find "Command Palette" hotkey
3. Click to edit
4. Press your desired key combination
5. Save

### Styling
The palette uses hardcoded colors for dark theme compatibility:
- Background: `RGB(30, 30, 30)`
- Header: `RGB(40, 40, 40)`
- Selection: `RGB(60, 120, 180)`
- Text: White/Light Gray
- Muted text: Gray

To customize, edit colors in `CommandPaletteForm.SetupCustomComponents()`.

## Troubleshooting

### Palette doesn't open
- Check if hotkey is already in use by another application
- Verify WebView2 is initialized (wait a few seconds after app start)
- Check Settings to ensure hotkey is registered

### No prompts showing
- Ensure you have created prompts in the web app
- Check browser console for JavaScript errors
- Verify localStorage is accessible

### Actions not working
- Some actions require the Vite server to be running
- Make sure you're connected (check status bar)
- Try refreshing the web app first

### Palette appears on wrong monitor
- The palette centers on the screen where the main app is located
- Move the main window to your desired monitor first

## Future Enhancements

Potential improvements for future versions:
- [ ] Recent prompts at the top
- [ ] Favorite/pinned prompts
- [ ] Action history
- [ ] Custom action shortcuts (e.g., Ctrl+E for execute)
- [ ] Preview pane showing prompt content
- [ ] Multi-select for batch operations
- [ ] Search syntax (e.g., `project:Marketing tag:urgent`)
- [ ] Themes (light/dark/custom)
- [ ] Plugin system for custom actions

## Performance

- **Instant** - Opens in under 100ms
- **Efficient** - Only loads prompts when opened
- **Non-blocking** - Runs on UI thread but doesn't freeze main app
- **Memory-friendly** - Disposes properly when closed

## Accessibility

- Full keyboard navigation
- High contrast colors for readability
- Clear visual feedback for selections
- Descriptive labels and hints
- Screen reader compatible (basic support)

## Integration with Existing Features

The Command Palette seamlessly integrates with:
- ? Execute Dialog
- ? Placeholder Dialog
- ? Share Dialog
- ? Export functionality
- ? Archive system
- ? AI Improve feature
- ? Version history
- ? Project/Category/Tag system

## Code Example: Adding a Custom Action

To add a new action to the Command Palette:

1. **Add to PromptActionType enum:**
```csharp
public enum PromptActionType
{
    // ... existing actions ...
    MyCustomAction
}
```

2. **Add to action list in CommandPaletteForm.ShowActionsForPrompt():**
```csharp
_currentActions.Add(new PromptAction 
{ 
    Type = PromptActionType.MyCustomAction, 
    Name = "My Action", 
    Description = "Does something awesome", 
    Icon = "??", 
    IsEnabled = true 
});
```

3. **Handle in MainForm.CommandPalette_ActionSelected():**
```csharp
case PromptActionType.MyCustomAction:
    await MyCustomActionHandler(e.Prompt);
    break;
```

4. **Implement handler:**
```csharp
private async Task MyCustomActionHandler(PromptInfo prompt)
{
    // Your custom logic here
    MessageBox.Show($"Executing custom action for: {prompt.Title}");
}
```

## Conclusion

The Command Palette brings power-user efficiency to promptArq, making it faster than ever to find and work with your prompts. Whether you're a developer who loves keyboard shortcuts or just want a quicker way to access your prompts, the Command Palette has you covered.

**Press Ctrl+K and experience the speed!** ??
