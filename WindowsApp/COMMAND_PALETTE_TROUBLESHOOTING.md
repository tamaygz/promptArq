## Command Palette Troubleshooting Guide

### Problem: Empty Command Palette Window

The Command Palette opens but shows no prompts. Here's how to diagnose:

### Step 1: Check if you have prompts in the web app

1. Open the Windows app
2. Wait for the web app to load completely
3. Create at least ONE prompt:
   - Click "New Prompt"
   - Enter a title (e.g., "Test Prompt")
   - Enter some content
   - Click "Save Version"

### Step 2: Verify localStorage has data

1. In the web app, press F12 to open DevTools
2. Go to Console tab
3. Type: `localStorage.getItem('prompts')`
4. Press Enter
5. You should see JSON data with your prompts

If it shows `null` or `[]`, you need to create prompts first.

### Step 3: Test the Command Palette

1. Press **Ctrl+K** (or Ctrl+Alt+K depending on your setup)
2. The palette should show your prompts

### Step 4: Check Debug Output

If you copied the updated MainForm_Complete.cs.txt with debugging:

1. Open Visual Studio Output window (View ? Output)
2. Press Ctrl+K to open palette
3. Look for messages like:
   ```
   Fetched X prompts from web app
   Raw result from JavaScript: ...
   Processed JSON: ...
   Deserialized X prompts
   ```

### Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "WebView not ready" | Wait 5-10 seconds after app starts before pressing Ctrl+K |
| "No prompts found" | Create prompts in the web app first |
| JSON parsing error | The web app structure might have changed - check console logs |
| Empty list but no errors | The prompts might be archived - check `isArchived` flag |

### Quick Test with Dummy Data

Add this to your system tray context menu temporarily:

```csharp
// In InitializeCustomComponents, add:
contextMenu.Items.Add("Test Palette", null, (s, e) => TestCommandPalette());

// Add this method:
private void TestCommandPalette()
{
    var testPrompts = new List<PromptInfo>
    {
        new PromptInfo 
        { 
            Id = "test-1", 
            Title = "Test Prompt", 
            Description = "This is a test", 
            Content = "Hello world", 
            ProjectName = "Test"
        }
    };
    _commandPalette?.ShowPalette(testPrompts);
}
```

Right-click the tray icon and click "Test Palette" - if this shows prompts, then the issue is with fetching from localStorage.

### Debugging the JavaScript Fetch

Open DevTools console and run this manually:

```javascript
(function() {
    const prompts = JSON.parse(localStorage.getItem('prompts') || '[]');
    console.log('Prompts:', prompts);
    const result = prompts.map(p => ({
        id: p.id,
        title: p.title,
        description: p.description || '',
        content: p.content
    }));
    console.log('Mapped:', result);
    return JSON.stringify(result);
})();
```

This will show you exactly what data is available.

### Expected Behavior

When working correctly:
1. Press Ctrl+K
2. Palette appears with dark background
3. Search box is focused
4. List shows all your prompts
5. Type to filter prompts
6. Press Enter on a prompt to see actions
7. Select an action (Copy, Open in Editor, etc.)

### Still Not Working?

Check these:
1. ? WebView2 is installed
2. ? Vite server is running (status bar shows "Connected")
3. ? Web app has loaded (you can see the UI)
4. ? You have created at least one prompt
5. ? The prompt was saved (check localStorage)
6. ? You're pressing the correct hotkey (check Settings)

### Alternative: Check Settings

1. Click Settings in the app
2. Find "Command Palette" hotkey
3. Verify it's set to Ctrl+K
4. Try changing it to a different key combination
5. Save and test again
