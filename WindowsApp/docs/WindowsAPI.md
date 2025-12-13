# Windows App API

## Overview

The Windows App API (`window.windowsAppAPI`) is a JavaScript interface exposed by the web application to enable communication with the Windows desktop application. It follows a **thin client** architecture where all business logic resides in the web app, and the Windows app delegates all operations to this API.

**Location:** `src/lib/windows-api.ts`

**Initialization:** `initWindowsAppAPI()` called from `App.tsx` on mount and state changes

## Communication Patterns

The API uses two different communication patterns based on whether operations are synchronous or asynchronous:

### Pattern 1: Synchronous Methods

**Used for:** `getPrompts()`, `getPlaceholders()`, `fillContent()`

These methods return values directly through ExecuteScriptAsync's return value:

```typescript
// JavaScript - Return value directly
getPrompts(searchQuery?: string): PromptMetadata[] {
  const results = prompts.filter(/*...*/).map(/*...*/);
  return results; // ✅ Synchronous return
}
```

```csharp
// C# - Use return value
var script = "window.windowsAppAPI.getPrompts()";
var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);
```

**Why it works:** No async operations, return value is immediate and serialized by ExecuteScriptAsync.

### Pattern 2: Async Methods (Message Passing)

**Used for:** `executePrompt()`

Async operations use WebView2's message passing API because ExecuteScriptAsync cannot handle async JavaScript functions:

```typescript
// JavaScript - Post message when done
executePrompt(promptId: string, content?: string): void {
  (async () => {
    try {
      const result = await executeLLM(/*...*/); // Async operation
      window.chrome.webview.postMessage({
        type: 'executeResult',
        success: true,
        result: result
      });
    } catch (error) {
      window.chrome.webview.postMessage({
        type: 'executeResult',
        success: false,
        error: error.message
      });
    }
  })();
}
```

```csharp
// C# - Use event handler + TaskCompletionSource
private TaskCompletionSource<ExecutionResult>? _executionTcs;

void CoreWebView2_WebMessageReceived(/*...*/) {
    var json = e.WebMessageAsJson;
    var message = JsonDocument.Parse(json).RootElement;
    
    if (message.GetProperty("type").GetString() == "executeResult") {
        var result = new ExecutionResult { /*...*/ };
        _executionTcs?.TrySetResult(result);
    }
}

async Task<ExecutionResult> ExecutePromptInWebApp(string promptId) {
    _executionTcs = new TaskCompletionSource<ExecutionResult>();
    
    var script = $"window.windowsAppAPI.executePrompt('{promptId}')";
    await _webView.CoreWebView2.ExecuteScriptAsync(script);
    
    return await _executionTcs.Task; // Waits for message
}
```

**Why it's needed:** ExecuteScriptAsync returns Promise objects as `{}`, not the resolved value. Message passing is the official WebView2 pattern for async communication.

See [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) for detailed technical explanation.

## API Methods

### getPrompts()

**Signature:** `getPrompts(searchQuery?: string): PromptMetadata[]`

**Pattern:** Synchronous (return value)

**Description:** Returns list of prompts with metadata.

**Parameters:**
- `searchQuery` (optional): Filter prompts by title/content

**Returns:** Array of `PromptMetadata` objects:
```typescript
interface PromptMetadata {
  id: string
  title: string
  description: string
  content: string
  projectId: string
  projectName: string
  categoryId: string
  categoryName: string
  tags: string[]
  isArchived: boolean
  hasPlaceholders: boolean      // Computed by web app
  placeholders: string[]         // Extracted by web app
  executeLLM: boolean           // From prompt.execute_llm
}
```

**Example:**
```csharp
var script = "window.windowsAppAPI.getPrompts('refactor')";
var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);
```

---

### getPlaceholders()

**Signature:** `getPlaceholders(promptId: string): string[]`

**Pattern:** Synchronous (return value)

**Description:** Extracts placeholder names from prompt content using regex.

**Parameters:**
- `promptId`: Prompt ID to extract placeholders from

**Returns:** Array of unique placeholder names (e.g., `["name", "role", "context"]`)

**Example:**
```csharp
var script = $"window.windowsAppAPI.getPlaceholders('{promptId}')";
var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
var placeholders = JsonSerializer.Deserialize<string[]>(result);
```

**Regex Pattern:** `\{\{([^}]+)\}\}` - Matches `{{placeholder}}` syntax

---

### fillContent()

**Signature:** `fillContent(promptId: string, values: Record<string, string>): string | null`

**Pattern:** Synchronous (return value)

**Description:** Fills placeholders in prompt content with provided values.

**Parameters:**
- `promptId`: Prompt ID to fill
- `values`: Object mapping placeholder names to values

**Returns:** Filled content string, or `null` if prompt not found

**Example:**
```csharp
var valuesJson = JsonSerializer.Serialize(new Dictionary<string, string> {
    {"name", "John"},
    {"role", "Developer"}
});
var script = $"window.windowsAppAPI.fillContent('{promptId}', {valuesJson})";
var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
var filledContent = result?.Trim('"'); // Remove JSON quotes
```

**Behavior:**
- Case-insensitive placeholder matching
- Preserves whitespace around `{{placeholder}}`
- Leaves unfilled placeholders unchanged

---

### executePrompt()

**Signature:** `executePrompt(promptId: string, content?: string): void`

**Pattern:** Async (message passing)

**Description:** Executes a prompt, either directly or through LLM.

**Parameters:**
- `promptId`: Prompt ID to execute
- `content` (optional): Override content (for filled prompts)

**Returns:** `void` (result sent via `postMessage`)

**Message Format:**
```typescript
{
  type: 'executeResult',
  success: boolean,
  result?: string,    // On success
  error?: string      // On failure
}
```

**Execution Logic:**
1. If `prompt.execute_llm` is `false`: Returns content directly
2. If `prompt.execute_llm` is `true`:
   - Checks LLM support (Spark environment or GitHub auth)
   - Resolves system prompt based on hierarchy (prompt → project → category → tag)
   - Constructs LLM prompt with system prompt
   - Calls `executeLLM()` to send to AI service
   - Returns AI-generated result

**Example:**
```csharp
// Setup event handler
_webView.CoreWebView2.WebMessageReceived += (sender, e) => {
    var json = e.WebMessageAsJson;
    var message = JsonDocument.Parse(json).RootElement;
    
    if (message.GetProperty("type").GetString() == "executeResult") {
        var success = message.GetProperty("success").GetBoolean();
        var result = message.TryGetProperty("result", out var r) ? 
                     r.GetString() : null;
        // Handle result...
    }
};

// Trigger execution
var script = $"window.windowsAppAPI.executePrompt('{promptId}', '{content}')";
await _webView.CoreWebView2.ExecuteScriptAsync(script);

// Wait for WebMessageReceived event (using TaskCompletionSource)
```

**Error Cases:**
- Prompt not found: `{ success: false, error: 'Prompt not found' }`
- LLM not available: `{ success: false, error: 'AI features require...' }`
- API error: `{ success: false, error: <error message> }`
- No response: `{ success: false, error: 'No response from AI service' }`

**Timeout:** Windows app implements 60-second timeout on C# side.

## Data Models

### C# Side (PromptAction.cs)

```csharp
public class PromptInfo
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public string ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string[] Tags { get; set; }
    public string[] Placeholders { get; set; }
    public bool IsArchived { get; set; }
    public bool HasPlaceholders { get; set; }
    public bool ExecuteLLM { get; set; }
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
```

### TypeScript Side (windows-api.ts)

```typescript
interface PromptMetadata {
  id: string
  title: string
  description: string
  content: string
  projectId: string
  projectName: string
  categoryId: string
  categoryName: string
  tags: string[]
  isArchived: boolean
  hasPlaceholders: boolean
  placeholders: string[]
  executeLLM: boolean
}

interface ExecutionResult {
  success: boolean
  result?: string
  error?: string
}
```

## Usage Examples

### Complete Command Palette Flow

```csharp
public class CommandPaletteForm : Form
{
    // 1. Load prompts
    private async Task LoadPrompts()
    {
        var script = "window.windowsAppAPI.getPrompts()";
        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);
        
        // Display in listbox...
    }
    
    // 2. Get placeholders when prompt selected
    private async Task<string[]> GetPlaceholders(string promptId)
    {
        var script = $"window.windowsAppAPI.getPlaceholders('{promptId}')";
        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return JsonSerializer.Deserialize<string[]>(result);
    }
    
    // 3. Fill placeholders with user input
    private async Task<string> FillPrompt(string promptId, Dictionary<string, string> values)
    {
        var valuesJson = JsonSerializer.Serialize(values);
        var script = $"window.windowsAppAPI.fillContent('{promptId}', {valuesJson})";
        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return result?.Trim('"');
    }
    
    // 4. Execute prompt through LLM
    private async Task<ExecutionResult> ExecutePrompt(string promptId, string content)
    {
        _executionTcs = new TaskCompletionSource<ExecutionResult>();
        
        var contentArg = content.Replace("\\", "\\\\")
                                .Replace("'", "\\'")
                                .Replace("\n", "\\n");
        var script = $"window.windowsAppAPI.executePrompt('{promptId}', '{contentArg}')";
        
        await _webView.CoreWebView2.ExecuteScriptAsync(script);
        
        return await _executionTcs.Task; // Completes in WebMessageReceived
    }
}
```

## Design Principles

### 1. Thin Client Architecture
- **All business logic in web app** - Windows app has ZERO duplicate logic
- **Windows app delegates everything** - Just calls API methods
- **Single source of truth** - Web app's logic is used by browser and desktop

### 2. Clean Separation
- **Web app:** Prompt processing, placeholder parsing, LLM execution, system prompt resolution
- **Windows app:** Desktop UX (hotkeys, system tray, paste to window, command palette UI)

### 3. API-First Design
- API boundary is explicit and documented
- Could support multiple clients (Mac app, Linux app, browser extension) using same API
- All communication goes through well-defined methods

### 4. Error Handling
- API returns structured results with `success` and `error` fields
- Windows app handles errors with user-friendly messages
- Timeout protection prevents infinite waits

## Future Extensions

### Potential API Additions

```typescript
// Create/edit prompts from Windows app
savePrompt(prompt: Prompt): Promise<void>

// System prompt management
getSystemPrompts(): SystemPrompt[]
assignSystemPrompt(promptId: string, systemPromptId: string): void

// Model selection
getAvailableModels(): string[]
executePromptWithModel(promptId: string, modelId: string): void

// Execution history
getExecutionHistory(promptId: string): ExecutionHistory[]

// Real-time validation
validatePrompt(content: string): ValidationResult
```

### Potential Windows Features

- **Quick Capture** - Capture text from active window as new prompt
- **Per-Prompt Hotkeys** - Register custom hotkeys for favorite prompts
- **Mini-Mode** - Floating mini command palette
- **Clipboard History** - Track recent executions
- **Offline Mode** - Cache prompts for offline use

## Troubleshooting

### Common Issues

**Problem:** `window.windowsAppAPI` is undefined

**Solution:** 
- Ensure web app is loaded (check Vite server status)
- Check that `initWindowsAppAPI()` is called in `App.tsx`
- Verify WebView2 initialization completed

**Problem:** ExecuteScriptAsync returns `{}`

**Solution:**
- For sync methods: Ensure function returns value synchronously (no `async`/`await`)
- For async methods: Must use message passing, not return value

**Problem:** Execution times out

**Solution:**
- Check network connectivity (LLM requires internet)
- Verify Spark environment or GitHub authentication
- Check browser console for JavaScript errors
- Increase timeout if legitimate slow operations

## References

- [WebView2 Documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)
- [ASYNC_COMMUNICATION.md](ASYNC_COMMUNICATION.md) - Deep dive on async pattern
- [Architecture.md](Architecture.md) - Overall Windows app architecture
- [CommandPalette.md](CommandPalette.md) - Command palette implementation
