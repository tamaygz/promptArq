# WebView2 Async Communication - Technical Deep Dive

## Problem Statement

WebView2's `ExecuteScriptAsync` cannot handle asynchronous JavaScript functions. When JavaScript returns a Promise, `ExecuteScriptAsync` serializes the Promise object itself (which becomes `{}`) rather than waiting for and returning the resolved value.

## Root Cause

```javascript
// ❌ DOESN'T WORK
async function executePrompt(id) {
    const result = await callAPI();
    return result;
}

// C# sees: {}  (Promise object serialized as empty JSON)
```

```javascript
// ❌ ALSO DOESN'T WORK  
(async () => {
    const result = await callAPI();
    return result;
})()

// C# sees: {}  (Promise object serialized as empty JSON)
```

```javascript
// ❌ STILL DOESN'T WORK
return window.api.executePrompt(id)
    .then(result => result)
    .catch(e => ({ error: e.message }));

// C# sees: {}  (Promise object serialized as empty JSON)
```

**Why:** `ExecuteScriptAsync` only captures **synchronous** return values. It doesn't await JavaScript Promises.

## Solution: Message Passing API

WebView2 provides a dedicated message passing API for async communication:

### JavaScript Side

```typescript
window.chrome.webview.postMessage(data)
```

- Available in WebView2 environment only
- Sends data from JavaScript to C# host
- Automatically serializes objects to JSON

### C# Side

```csharp
CoreWebView2.WebMessageReceived += Handler
```

- Event fires when JavaScript calls `postMessage()`
- Access message via `e.WebMessageAsJson` property
- Parse JSON and handle result

## Implementation Pattern

### 1. JavaScript: Fire-and-Forget with Callback

```typescript
executePrompt(promptId: string, content?: string): void {
  // Start async work but don't return Promise
  (async () => {
    try {
      const result = await actuallyExecutePrompt(promptId, content);
      
      // Post result back to C#
      window.chrome?.webview?.postMessage({
        type: 'executeResult',
        success: true,
        result: result
      });
    } catch (error) {
      window.chrome?.webview?.postMessage({
        type: 'executeResult',
        success: false,
        error: error.message
      });
    }
  })();
  // Function returns void immediately
}
```

**Key Points:**
- Function signature returns `void`, not `Promise`
- IIFE starts async work without returning Promise
- Use optional chaining (`?.`) for safety
- Always post message on both success and error

### 2. C#: TaskCompletionSource Bridge

```csharp
public class MainForm : Form
{
    private TaskCompletionSource<ExecutionResult>? _executionTcs = null;

    // Initialize event handler
    private void WebView_CoreWebView2InitializationCompleted(/*...*/) {
        if (e.IsSuccess) {
            _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }
    }

    // Event handler
    private void CoreWebView2_WebMessageReceived(
        object? sender, 
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        try {
            // Get JSON from message
            var json = e.WebMessageAsJson;
            
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement;
            
            // Check message type
            if (message.TryGetProperty("type", out var typeElement) && 
                typeElement.GetString() == "executeResult")
            {
                // Parse execution result
                var result = new ExecutionResult {
                    Success = message.GetProperty("success").GetBoolean(),
                    Result = message.TryGetProperty("result", out var r) ? r.GetString() : null,
                    Error = message.TryGetProperty("error", out var err) ? err.GetString() : null
                };
                
                // Complete the pending task
                _executionTcs?.TrySetResult(result);
            }
        } catch (Exception ex) {
            _executionTcs?.TrySetException(ex);
        }
    }

    // Public API method
    private async Task<ExecutionResult> ExecutePromptInWebApp(
        string promptId, 
        string? content = null)
    {
        // Prevent concurrent executions
        if (_executionTcs != null) {
            return new ExecutionResult {
                Success = false,
                Error = "Another execution is already in progress"
            };
        }

        // Create TaskCompletionSource for this execution
        _executionTcs = new TaskCompletionSource<ExecutionResult>();

        try {
            // Trigger execution (fire-and-forget)
            var script = $"window.windowsAppAPI.executePrompt('{promptId}')";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            
            // Wait for result with timeout
            var resultTask = _executionTcs.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
            
            var completedTask = await Task.WhenAny(resultTask, timeoutTask);
            
            if (completedTask == timeoutTask) {
                return new ExecutionResult {
                    Success = false,
                    Error = "Execution timed out after 60 seconds"
                };
            }
            
            return await resultTask;
        } finally {
            // Clean up
            _executionTcs = null;
        }
    }
}
```

**Key Points:**
- `TaskCompletionSource` bridges event-based to async/await
- Single `_executionTcs` field prevents concurrent executions
- Timeout handling prevents infinite waits
- `finally` block ensures cleanup
- `TrySetResult`/`TrySetException` are safe for concurrent calls

## Complete Flow

```
C# calls ExecutePromptInWebApp(promptId)
  ↓
Create TaskCompletionSource
  ↓
ExecuteScriptAsync triggers JavaScript (fire-and-forget)
  ↓
JavaScript starts async work
  ↓
C# awaits TaskCompletionSource.Task (blocks)
  ↓
JavaScript completes async work
  ↓
JavaScript calls window.chrome.webview.postMessage(result)
  ↓
C# WebMessageReceived event fires
  ↓
Parse message and call TaskCompletionSource.TrySetResult(result)
  ↓
C# TaskCompletionSource.Task completes
  ↓
ExecutePromptInWebApp returns result
```

## Comparison: Synchronous vs Async Patterns

### Synchronous Operations (ExecuteScriptAsync Return Value)

**Used for:** `getPrompts()`, `getPlaceholders()`, `fillContent()`

**JavaScript:**
```typescript
getPrompts(searchQuery?: string): PromptMetadata[] {
  // Pure synchronous operations (filtering, mapping)
  const results = prompts.filter(/* ... */).map(/* ... */);
  return results; // ✅ Return directly
}
```

**C#:**
```csharp
var script = "window.windowsAppAPI.getPrompts()";
var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result);
```

**Pros:**
- Simple and straightforward
- Return value is immediate
- No event handlers needed
- No state management required

**Cons:**
- Only works for synchronous operations
- Cannot handle async/await, Promises, HTTP calls, etc.

### Async Operations (Message Passing)

**Used for:** `executePrompt()` (calls LLM API via HTTP)

**JavaScript:**
```typescript
executePrompt(promptId: string, content?: string): void {
  (async () => {
    try {
      const result = await executeLLM(/*...*/); // Async HTTP call
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

**C#:**
```csharp
_executionTcs = new TaskCompletionSource<ExecutionResult>();

var script = "window.windowsAppAPI.executePrompt('{promptId}')";
await _webView.CoreWebView2.ExecuteScriptAsync(script);

return await _executionTcs.Task; // Waits for WebMessageReceived
```

**Pros:**
- Handles any async JavaScript operation
- Official WebView2 pattern for async communication
- Supports timeout and cancellation
- Can handle multiple message types

**Cons:**
- More complex implementation
- Requires event handler setup
- Need state management (TaskCompletionSource)
- Must handle concurrency carefully

## Design Decisions

### Why Not Queue Concurrent Executions?

**Decision:** Reject concurrent executions with error message

**Reasoning:**
- Command palette closes after selection (one execution at a time)
- Multiple concurrent LLM calls could hit rate limits
- User confusion about which result is which
- Simpler implementation

**Alternative Considered:**
```csharp
private Queue<(string promptId, TaskCompletionSource<ExecutionResult> tcs)> _executionQueue;
```
Rejected due to added complexity without clear benefit.

### Why 60 Second Timeout?

**Decision:** 60 seconds

**Reasoning:**
- LLM API calls can be slow (network + generation)
- Too short (30s): Fails legitimate slow requests
- Too long (120s): User waits forever on failures
- 60s: Reasonable compromise

**Configurable:** Could be made configurable in future if needed.

### Why Optional Chaining in JavaScript?

```typescript
window.chrome?.webview?.postMessage(/*...*/)
```

**Reasoning:**
- Defensive programming
- Prevents errors if running in non-WebView2 environment
- Fails silently rather than throwing (timeout in C# catches it)

## Testing Strategy

### Unit Tests (JavaScript)

```typescript
describe('executePrompt', () => {
  it('should post message on success', async () => {
    const mockPostMessage = jest.fn();
    window.chrome = { webview: { postMessage: mockPostMessage } };
    
    executePrompt('prompt-1');
    
    await waitFor(() => {
      expect(mockPostMessage).toHaveBeenCalledWith({
        type: 'executeResult',
        success: true,
        result: expect.any(String)
      });
    });
  });
  
  it('should post message on error', async () => {
    const mockPostMessage = jest.fn();
    window.chrome = { webview: { postMessage: mockPostMessage } };
    
    // Trigger error condition
    executePrompt('invalid-id');
    
    await waitFor(() => {
      expect(mockPostMessage).toHaveBeenCalledWith({
        type: 'executeResult',
        success: false,
        error: 'Prompt not found'
      });
    });
  });
});
```

### Integration Tests (C#)

```csharp
[Test]
public async Task ExecutePromptInWebApp_ReturnsSuccess()
{
    // Arrange
    var promptId = "test-prompt-1";
    
    // Act
    var result = await mainForm.ExecutePromptInWebApp(promptId);
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.IsNotNull(result.Result);
}

[Test]
public async Task ExecutePromptInWebApp_TimesOutAfter60Seconds()
{
    // Arrange - Inject slow mock
    var promptId = "slow-prompt";
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await mainForm.ExecutePromptInWebApp(promptId);
    stopwatch.Stop();
    
    // Assert
    Assert.IsFalse(result.Success);
    Assert.That(result.Error, Does.Contain("timeout"));
    Assert.That(stopwatch.ElapsedSeconds, Is.InRange(60, 62));
}
```

## Common Pitfalls

### ❌ Pitfall 1: Returning Promise
```javascript
// WRONG - Returns Promise object
async executePrompt(id) {
    return await callAPI();
}
```

**Fix:** Use fire-and-forget IIFE with postMessage callback.

### ❌ Pitfall 2: Not Handling Errors
```javascript
// WRONG - Error crashes silently
(async () => {
    const result = await callAPI();
    window.chrome.webview.postMessage({ result });
})();
```

**Fix:** Wrap in try/catch and post error messages.

### ❌ Pitfall 3: Forgetting Cleanup
```csharp
// WRONG - _executionTcs leaks on exception
_executionTcs = new TaskCompletionSource<ExecutionResult>();
await _webView.CoreWebView2.ExecuteScriptAsync(script);
return await _executionTcs.Task;
```

**Fix:** Use `finally` block to reset `_executionTcs`.

### ❌ Pitfall 4: Using TryGetWebMessageAsString
```csharp
// WRONG - Escapes JSON string
var json = e.TryGetWebMessageAsString();
var message = JsonSerializer.Deserialize<JsonElement>(json);
```

**Fix:** Use `e.WebMessageAsJson` property directly.

## Future Extensions

### Multiple Message Types

```typescript
// JavaScript
window.chrome.webview.postMessage({
    type: 'progress',
    percent: 50
});

window.chrome.webview.postMessage({
    type: 'executeResult',
    success: true,
    result: '...'
});
```

```csharp
// C#
void CoreWebView2_WebMessageReceived(/*...*/) {
    var type = message.GetProperty("type").GetString();
    
    switch (type) {
        case "progress":
            UpdateProgressBar(message.GetProperty("percent").GetInt32());
            break;
        case "executeResult":
            _executionTcs?.TrySetResult(ParseResult(message));
            break;
    }
}
```

### Request-Response Correlation

For concurrent executions:

```typescript
// JavaScript
const requestId = crypto.randomUUID();
(async () => {
    const result = await execute();
    window.chrome.webview.postMessage({
        type: 'executeResult',
        requestId: requestId,
        result: result
    });
})();
```

```csharp
// C#
private Dictionary<string, TaskCompletionSource<ExecutionResult>> _pendingRequests;

void CoreWebView2_WebMessageReceived(/*...*/) {
    var requestId = message.GetProperty("requestId").GetString();
    if (_pendingRequests.TryGetValue(requestId, out var tcs)) {
        tcs.TrySetResult(ParseResult(message));
        _pendingRequests.Remove(requestId);
    }
}
```

## References

- [WebView2 Documentation - CoreWebView2.ExecuteScriptAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.executescriptasync)
- [WebView2 Documentation - WebMessageReceived Event](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.webmessagereceived)
- [GitHub Issue #2295 - ExecuteScriptAsync doesn't return fetch PromiseResult](https://github.com/MicrosoftEdge/WebView2Feedback/issues/2295)
- [Stack Overflow - WebView2 return promise from ExecuteScriptAsync](https://stackoverflow.com/questions/66204382/webview2-return-promise-from-executescriptasync)

## Summary

**Key Takeaway:** WebView2's `ExecuteScriptAsync` is for **synchronous** operations only. For **async** operations, use the message passing API (`window.chrome.webview.postMessage` + `WebMessageReceived` event).

This is not a workaround or hack - it's the **official pattern** documented by Microsoft for async communication in WebView2.
