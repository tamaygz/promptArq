# Command Palette - Detailed Documentation

## Overview

The Command Palette is a keyboard-first modal dialog that provides quick access to prompts and actions without opening the main window. It implements a multi-stage workflow state machine for an intuitive user experience.

## Architecture

### Workflow State Machine

The command palette uses a finite state machine with four states:

```
┌─────────────────┐
│ SelectingPrompt │ ◄─── Entry point
└────────┬────────┘
         │ Select prompt
         ▼
┌─────────────────┐
│ SelectingAction │
└────────┬────────┘
         │ Select action
         ▼
    ┌────────────┐
    │ Has        │
    │Placeholders│
    └──┬─────┬───┘
       │     │
   Yes │     │ No
       │     │
       │     └─────────────────┐
       ▼                       │
┌──────────────────┐           │
│FillingPlaceholder│           │
└────────┬─────────┘           │
         │                     │
         │ All filled          │
         └─────────┬───────────┘
                   ▼
          ┌───────────────┐
          │Execute Action │
          └───────────────┘
```

### State Transitions

| Current State | Event | Next State |
|--------------|-------|------------|
| SelectingPrompt | Enter key on prompt | SelectingAction |
| SelectingPrompt | Escape | Close dialog |
| SelectingAction | Enter on "Paste" | FillingPlaceholder (if has) or Execute |
| SelectingAction | Enter on "Copy" | FillingPlaceholder (if has) or Execute |
| SelectingAction | Escape | SelectingPrompt (back) |
| FillingPlaceholder | Enter on value | Next placeholder or Execute |
| FillingPlaceholder | Escape | SelectingAction (back) |

## User Interface

### Visual Design
- **Size**: 700×500 pixels
- **Position**: Centered on screen
- **Style**: Borderless with rounded corners
- **Opacity**: 97% (slightly transparent)
- **Colors**:
  - Background: `#1E1E1E` (dark gray)
  - Header: `#282828` (slightly lighter)
  - Search box: `#323232` (light gray)
  - Text: White
  - Hint text: Gray

### Layout Components

```
┌────────────────────────────────────────────┐
│  ┌──────────────────────────────────────┐  │  Header Panel
│  │  Search Box                          │  │  (80px height)
│  └──────────────────────────────────────┘  │
│  Hint: Type to search... ESC to close      │
├────────────────────────────────────────────┤
│  ┌──────────────────────────────────────┐  │
│  │  Prompt 1                            │  │
│  │  Prompt 2  ◄── Selected              │  │  Results List
│  │  Prompt 3                            │  │  (Scrollable)
│  │  ...                                 │  │
│  └──────────────────────────────────────┘  │
└────────────────────────────────────────────┘
```

## Features

### 1. Prompt Search

**Fuzzy Search Implementation:**
- Searches in prompt **title** and **description**
- Case-insensitive matching
- Substring matching (not full fuzzy)
- Results limited to 50 items for performance

**Code Reference:**
```csharp
var filtered = _allPrompts
    .Where(p => 
        p.Title.ToLowerInvariant().Contains(query) ||
        p.Description.ToLowerInvariant().Contains(query) ||
        p.Tags.Any(t => t.ToLowerInvariant().Contains(query)))
    .Take(50)
    .ToList();
```

**Search Tips:**
- Type partial words (e.g., "cod" matches "Code Review")
- Search tags (e.g., "python" finds all Python-tagged prompts)
- Empty search shows first 50 prompts

### 2. Keyboard Navigation

#### Search Box (SelectingPrompt state)
- `↓` - Select first item in list, transfer focus
- `↑` - Select last item in list, transfer focus
- `Enter` - Select highlighted prompt (or first if none)
- `Escape` - Close dialog
- `Type` - Filter results in real-time

#### Results List (SelectingPrompt state)
- `↓` - Move selection down
- `↑` - Move selection up
- `Enter` - Select highlighted item
- `Escape` - Return focus to search box
- `Backspace` - Return focus to search box

#### Placeholder Filling (FillingPlaceholder state)
- `Enter` - Confirm current placeholder value, move to next
- `Escape` - Cancel and return to action selection

**Implementation Detail:**
The first Down arrow press from search box now correctly selects the first item without requiring two keystrokes. This was fixed by not auto-selecting index 0 in `FilterResults()` for `SelectingPrompt` state.

### 3. Actions

Two primary actions available after selecting a prompt:

#### Paste to Active Window
- **Function**: Pastes resolved prompt to currently active application
- **Implementation**:
  1. Copy prompt to clipboard
  2. Use `SendKeys.Send("^v")` to paste
  3. Show success toast
- **Use Case**: Quick insertion into IDE, text editor, chat app

#### Copy to Clipboard
- **Function**: Copies resolved prompt to clipboard
- **Implementation**:
  1. Copy prompt to clipboard
  2. Show success toast
- **Use Case**: Manual pasting, review before use

**Action Execution Flow:**
```csharp
private void ExecuteAction(PromptActionType actionType)
{
    string finalContent = _selectedPrompt.Content;
    
    // Apply placeholder values if any
    if (_placeholderValues.Count > 0)
    {
        foreach (var kvp in _placeholderValues)
        {
            finalContent = finalContent.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
    }
    
    // Execute action
    Clipboard.SetText(finalContent);
    
    if (actionType == PromptActionType.Paste)
    {
        SendKeys.Send("^v");
        ShowToast("Pasted to active window");
    }
    else
    {
        ShowToast("Copied to clipboard");
    }
    
    Hide();
}
```

### 4. Placeholder Detection

**Placeholder Format:**
Placeholders use double curly braces: `{{placeholder_name}}`

**Detection Regex:**
```csharp
private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
```

**Examples:**
- `{{name}}` - Single word
- `{{project_name}}` - Underscore allowed
- `{{API_KEY}}` - Uppercase allowed
- `{{ spaced }}` - **NOT** detected (no spaces inside braces)

**Extraction:**
```csharp
var matches = PlaceholderRegex.Matches(prompt.Content);
foreach (Match match in matches)
{
    string placeholderName = match.Groups[1].Value;
    _placeholders.Add(placeholderName);
}
```

### 5. Placeholder Filling UI

When placeholders are detected:

**Visual Changes:**
- Hint label updates: `"Fill placeholder: {{placeholder_name}}"`
- Search box becomes input for placeholder value
- Results list hidden
- Previous values cleared

**User Flow:**
1. User sees prompt: `"Fill placeholder: {{name}}"`
2. Types value: `"John Doe"`
3. Presses Enter
4. If more placeholders exist, repeat from step 1
5. If no more placeholders, execute action automatically

**Multi-placeholder Example:**
```
Prompt: "Hello {{name}}, welcome to {{project}}!"

Step 1: "Fill placeholder: {{name}}"
User enters: "Alice"

Step 2: "Fill placeholder: {{project}}"
User enters: "PromptArq"

Result: "Hello Alice, welcome to PromptArq!"
```

### 6. Toast Notifications

Non-blocking notifications for user feedback.

**Implementation:**
```csharp
private void ShowToast(string message)
{
    Label toast = new Label
    {
        Text = message,
        AutoSize = true,
        BackColor = Color.FromArgb(40, 40, 40),
        ForeColor = Color.White,
        Padding = new Padding(15, 10, 15, 10),
        Font = new Font("Segoe UI", 10F)
    };
    
    toast.Location = new Point(
        (ClientSize.Width - toast.Width) / 2,
        ClientSize.Height - 80
    );
    
    Controls.Add(toast);
    toast.BringToFront();
    
    // Auto-hide after 2 seconds
    Timer timer = new Timer { Interval = 2000 };
    timer.Tick += (s, e) => { 
        Controls.Remove(toast); 
        toast.Dispose(); 
        timer.Dispose(); 
    };
    timer.Start();
}
```

**Toast Appearance:**
- **Position**: Bottom center of dialog
- **Duration**: 2 seconds
- **Style**: Dark background, white text
- **Timing**: Appears on action completion

### 7. Click-Outside-to-Close

**Implementation:**
Uses `Form.Deactivate` event to detect focus loss.

```csharp
protected override void OnDeactivate(EventArgs e)
{
    base.OnDeactivate(e);
    
    // Close if user clicks outside (unless settings dialog is open)
    if (Visible && !_isClosing)
    {
        Hide();
    }
}
```

**Behavior:**
- Click anywhere outside dialog → closes
- Does **not** close if child dialog opens (e.g., settings)
- `Escape` key always closes as alternative

## Technical Implementation

### Form Properties
```csharp
FormBorderStyle = FormBorderStyle.None;  // Borderless
StartPosition = FormStartPosition.CenterScreen;  // Centered
Size = new Size(700, 500);  // Fixed size
BackColor = Color.FromArgb(30, 30, 30);  // Dark theme
Opacity = 0.97;  // Slightly transparent
TopMost = true;  // Always on top
ShowInTaskbar = false;  // Hidden from taskbar
```

### Rounded Corners
Uses GDI32 API for rounded rectangle region:
```csharp
[DllImport("gdi32.dll")]
private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));
```

### Prompt Data Loading
Prompts are loaded from the web application's storage via HTTP:

```csharp
private async Task LoadPromptsAsync()
{
    using var client = new HttpClient();
    var response = await client.GetStringAsync("http://localhost:5001/storage/prompts");
    _allPrompts = JsonConvert.DeserializeObject<List<PromptInfo>>(response);
}
```

**Caching:**
- Prompts loaded once on first open
- Cached for session lifetime
- Reload only on application restart

### State Management
```csharp
private enum WorkflowState
{
    SelectingPrompt,    // Initial state
    SelectingAction,    // After prompt selected
    FillingPlaceholder, // If prompt has placeholders
    ChoosingOutput      // Not yet implemented
}

private WorkflowState _workflowState = WorkflowState.SelectingPrompt;
```

State transitions are explicit and validated:
```csharp
private void TransitionToState(WorkflowState newState)
{
    _workflowState = newState;
    UpdateUIForState();
}
```

## Performance Considerations

### Prompt Search
- **Complexity**: O(n) where n = number of prompts
- **Optimization**: Results limited to 50 items
- **Typical**: <10ms for 1000 prompts

### UI Rendering
- **Initial render**: ~50ms
- **Search filter update**: ~5-10ms
- **Keyboard navigation**: <5ms (native ListBox)

### Memory Usage
- **Base dialog**: ~2-5 MB
- **Prompt cache**: ~1 MB per 1000 prompts
- **Total**: ~5-10 MB

## Limitations

### Current Limitations
1. **No fuzzy matching** - Only substring search
2. **No prompt preview** - Must select to see full content
3. **No prompt editing** - Must open main window
4. **Fixed size** - Cannot resize dialog
5. **No multi-select** - One prompt at a time
6. **No history** - Doesn't remember recent prompts

### Known Issues
1. **Paste timing** - Target window must be ready to receive input
2. **Clipboard overwrite** - Original clipboard content not restored
3. **SendKeys limitations** - May not work with all applications

## Future Enhancements

### Planned Features
- Fuzzy search algorithm (Levenshtein distance)
- Recent prompts / favorites
- Prompt preview pane
- Multi-prompt chaining
- Clipboard history restoration
- Customizable keybindings within palette
- Quick-add placeholder values (templates)
- Prompt editing from palette

### Under Consideration
- Resizable dialog
- Multiple action outputs (file, API, etc.)
- Prompt composition (combine multiple prompts)
- AI-powered prompt suggestions
- Integration with external tools (VS Code, Slack, etc.)

## Troubleshooting

### Dialog doesn't appear
- Check hotkey registration (Settings)
- Verify no modal dialogs blocking
- Try clicking tray icon → Command Palette

### Search doesn't filter
- Check prompts loaded (may take 1-2 seconds)
- Verify web app accessible (main window)
- Restart application

### Paste not working
- Ensure target window accepts text input
- Try manual paste (`Ctrl+V`)
- Check clipboard contains text
- Use Copy action as fallback

### Placeholder not detected
- Verify format: `{{name}}` not `{{ name }}`
- Use alphanumeric + underscore only
- Check for typos in braces

### Navigation requires two keystrokes
- This is fixed in current version
- Ensure running latest build
- First Down arrow should select first item

## Code Reference

### Key Files
- `CommandPaletteForm.cs` - Main implementation
- `CommandPaletteForm.Designer.cs` - Auto-generated UI code
- `PromptAction.cs` - Action type definitions
- `MainForm.cs` - Hotkey integration

### Key Methods
- `ShowPalette()` - Entry point, resets state and shows dialog
- `FilterResults()` - Searches and populates list
- `HandleSelection()` - Processes Enter key on list item
- `ExecuteAction()` - Unified action execution (Paste/Copy)
- `HandlePlaceholder()` - Placeholder value processing
- `ShowToast()` - Non-blocking notification display

### Event Handlers
- `SearchBox_TextChanged` - Real-time search filtering
- `SearchBox_KeyDown` - Keyboard navigation from search box
- `ResultsList_KeyDown` - Keyboard navigation in list
- `OnDeactivate` - Click-outside-to-close

## Best Practices

### For Users
1. Learn hotkey (`Ctrl+K`) for muscle memory
2. Use descriptive prompt titles for easy search
3. Keep frequently-used prompts simple (no placeholders)
4. Use Copy action to review before pasting
5. Press Escape to cancel at any time

### For Developers
1. Keep state transitions explicit
2. Always reset state on `ShowPalette()`
3. Validate state before actions
4. Use async for I/O operations
5. Dispose resources properly
6. Test with various prompt counts (1, 100, 1000)
7. Test placeholder edge cases (0, 1, 5+ placeholders)

## Accessibility

### Current Support
- Keyboard-first navigation (no mouse required)
- High contrast colors (dark theme)
- Clear focus indicators

### Future Improvements
- Screen reader support
- Customizable font sizes
- Light/dark theme toggle
- Color blindness accommodations
