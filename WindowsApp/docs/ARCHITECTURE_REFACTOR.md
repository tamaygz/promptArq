# Windows App Architecture Refactor

## Overview

The Windows app has been refactored to be a **"thin client"** with zero duplicate business logic. All prompt processing, placeholder parsing, LLM execution, and business logic now resides exclusively in the web application.

## Key Principle

> **The Windows app is a dumb terminal.** It provides desktop-specific UX (command palette, global hotkeys, paste to active window) but contains NO business logic. All logic lives in the web app.

## Architecture Pattern

### Communication Flow

```
User Input (Hotkey)
  ↓
Windows App (Command Palette UI)
  ↓
Delegates (GetPlaceholdersFromWebApp, FillContentInWebApp, ExecutePromptInWebApp)
  ↓
MainForm (WebView2 JavaScript Execution)
  ↓
window.windowsAppAPI (Exposed by web app)
  ↓
Web App Business Logic (TypeScript)
  ↓
Return Results
  ↓
Windows App (Desktop Actions: Paste, Copy, Toast)
```

## Windows App API

Located in: `src/lib/windows-api.ts`

Exposed as: `window.windowsAppAPI`

### API Methods

#### `getPrompts(searchQuery?: string): Promise<PromptMetadata[]>`
- Returns list of prompts with metadata
- Includes: hasPlaceholders, executeLLM flags computed by web app
- Handles search filtering

#### `getPrompt(promptId: string): Promise<PromptMetadata | null>`
- Get single prompt by ID
- Returns same metadata structure

#### `getPlaceholders(promptId: string): Promise<string[]>`
- Extract placeholder names from prompt content
- Uses web app's regex logic (no duplicate parsing)
- Returns array of unique placeholder names

#### `fillContent(promptId: string, values: Record<string, string>): Promise<string>`
- Fill placeholders in prompt content
- Uses web app's string replacement logic
- Returns filled content string

#### `executePrompt(promptId: string, content?: string): Promise<ExecutionResult>`
- Execute prompt (direct or through LLM)
- Checks `execute_llm` flag
- For LLM execution: resolves system prompts, calls Spark AI API
- Returns: `{ success: boolean, result?: string, error?: string }`

## What Windows App Does

### ✅ Desktop UX Only
- Command palette UI (forms, listboxes, textboxes)
- Global hotkey registration (Win+Space)
- Clipboard operations
- Paste to active window via SendKeys
- Toast notifications
- System tray integration
- Window management

### ❌ NO Business Logic
- ❌ NO regex placeholder parsing
- ❌ NO string replacement
- ❌ NO LLM prompt construction
- ❌ NO system prompt resolution
- ❌ NO API calls to Spark
- ❌ NO execute_llm flag logic
- ❌ NO data structure manipulation

## Code Organization

### Windows App Files

**MainForm.cs** (~900 lines)
- WebView2 host
- Delegate implementations that call web app API:
  - `GetPromptsFromWebApp()` → `window.windowsAppAPI.getPrompts()`
  - `GetPlaceholdersFromWebApp()` → `window.windowsAppAPI.getPlaceholders()`
  - `FillContentInWebApp()` → `window.windowsAppAPI.fillContent()`
  - `ExecutePromptInWebApp()` → `window.windowsAppAPI.executePrompt()`
- Desktop actions: paste, copy, toast notifications
- Command palette event handlers (minimal logic)

**CommandPaletteForm.cs** (~910 lines)
- Command palette UI state machine
- Workflow states: SelectingPrompt → SelectingAction → FillingPlaceholder → ChoosingOutput
- Delegates to MainForm for all web app API calls
- Handles UI transitions and desktop actions only

**PromptAction.cs**
- Data models: PromptInfo, PromptAction, ExecutionResult
- No logic, pure data structures

### Web App Files

**src/lib/windows-api.ts** (~230 lines)
- Complete API implementation
- All business logic for Windows app interactions
- Uses existing web app modules (prompt-resolver, spark-utils)

**src/App.tsx**
- Initializes Windows App API with `initWindowsAppAPI()`
- Re-initializes on state changes to keep API fresh

## Workflow Examples

### Example 1: Execute Prompt with Placeholders and LLM

1. User presses Win+Space → Command palette opens
2. User types and selects prompt → CommandPaletteForm shows actions
3. User selects "Fill Placeholders" → Calls `GetPlaceholdersFromWebApp(promptId)`
4. Web app extracts placeholders via regex → Returns `["name", "role"]`
5. User fills values → `{ "name": "John", "role": "Developer" }`
6. User presses Enter → Calls `FillContentInWebApp(promptId, values)`
7. Web app fills placeholders → Returns filled string
8. CommandPaletteForm shows output options (Execute & Paste)
9. User selects "Execute & Paste" → Calls `ExecutePromptInWebApp(promptId, content)`
10. Web app:
    - Checks `execute_llm = true`
    - Resolves system prompts hierarchy
    - Constructs LLM prompt with system + user content
    - Calls Spark AI API with gpt-4o-mini
    - Returns `{ success: true, result: "AI response..." }`
11. Windows app copies result to clipboard
12. Windows app pastes to active window via SendKeys

### Example 2: Direct Copy (No Placeholders, No LLM)

1. User presses Win+Space → Command palette opens
2. User selects prompt → CommandPaletteForm shows actions
3. User selects "Copy to Clipboard"
4. CommandPaletteForm copies `prompt.content` to clipboard
5. Shows toast notification

## Benefits

### 1. **Zero Code Duplication**
- Regex placeholder parsing: 1 place (web app)
- String replacement: 1 place (web app)
- LLM execution: 1 place (web app)
- System prompt resolution: 1 place (web app)

### 2. **Single Source of Truth**
- All business logic changes happen in web app
- Windows app automatically gets updates via API
- No sync issues between implementations

### 3. **Easier Maintenance**
- Bug fixes in one place
- Feature additions in one place
- Clear separation of concerns

### 4. **Testability**
- Web app logic testable in TypeScript
- Windows app only tests UI and desktop actions
- API boundary is clear contract

### 5. **Future-Proof**
- Could add Mac app, Linux app using same API
- Could add browser extension using same API
- Could add mobile app using same API

## Migration Summary

### Before Refactor
```csharp
// Windows app had duplicate logic
var regex = new Regex(@"\{\{([^}]+)\}\}");
var matches = regex.Matches(content);
foreach (Match match in matches) {
    placeholders.Add(match.Groups[1].Value.Trim());
}
```

### After Refactor
```csharp
// Windows app just calls web app
var placeholders = await GetPlaceholdersFromWebApp(promptId);
// Web app handles all parsing logic
```

### Before Refactor
```csharp
// Windows app had complex UI manipulation for LLM
var script = @"
    const executeButtons = document.querySelectorAll('button');
    // ... 50 lines of clicking buttons and waiting ...
";
```

### After Refactor
```csharp
// Windows app uses clean API
var result = await ExecutePromptInWebApp(promptId, content);
if (result.Success) {
    Clipboard.SetText(result.Result);
    SendKeys.SendWait("^v");
}
```

## Testing Checklist

- [ ] Prompt without placeholders, execute_llm=false → Direct copy/paste works
- [ ] Prompt without placeholders, execute_llm=true → LLM execution works
- [ ] Prompt with placeholders, execute_llm=false → Fill → Paste works
- [ ] Prompt with placeholders, execute_llm=true → Fill → Execute → Paste works
- [ ] Search/filter prompts works
- [ ] Escape key navigation works at each workflow state
- [ ] Toast notifications appear correctly
- [ ] Paste to active window works
- [ ] Copy to clipboard works
- [ ] "Copy Generated Prompt" works (bypasses LLM)

## Future Enhancements

### Potential API Extensions

1. **getSystemPrompts()** - Let Windows app show system prompt picker
2. **getModels()** - Let Windows app show model picker
3. **savePrompt()** - Let Windows app create/edit prompts
4. **getHistory()** - Let Windows app show execution history
5. **validatePrompt()** - Real-time validation as user types

### Potential Windows App Features

1. **Quick Capture** - Capture text from active window as new prompt
2. **Hotkey per Prompt** - Register custom hotkeys for favorite prompts
3. **Mini-Mode** - Floating mini command palette
4. **Clipboard History** - Track recent executions
5. **Offline Mode** - Cache prompts for offline use

## Key Files Changed

1. ✅ **src/lib/windows-api.ts** - CREATED - Complete API implementation
2. ✅ **src/App.tsx** - MODIFIED - Initialize API on mount and state changes
3. ✅ **WindowsApp/MainForm.cs** - MODIFIED - Simplified, added delegate implementations
4. ✅ **WindowsApp/CommandPaletteForm.cs** - MODIFIED - Removed regex, added delegates
5. ✅ **WindowsApp/PromptAction.cs** - MODIFIED - Added ExecutionResult class

## Lessons Learned

1. **API First** - Define clean API boundary before implementation
2. **Single Responsibility** - Windows app = UI, Web app = Logic
3. **Avoid Shortcuts** - No TODOs, no placeholders, full implementation
4. **Think Long-Term** - Design for multiple clients from day 1
5. **Leverage Existing** - Reuse web app's proven logic (spark-utils, prompt-resolver)

---

**Date:** 2024-01-XX  
**Status:** ✅ Complete  
**Next Steps:** Test all workflows, then document in user guide
