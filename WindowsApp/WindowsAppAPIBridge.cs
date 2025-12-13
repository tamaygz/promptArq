using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace PromptArqApp
{
    /// <summary>
    /// Bridge between Windows Forms and web app JavaScript API.
    /// Delegates all business logic to window.windowsAppAPI in the web app.
    /// </summary>
    public class WindowsAppAPIBridge
    {
        private readonly WebView2Manager _webView2Manager;
        private TaskCompletionSource<ExecutionResult>? _executionTcs;

        /// <summary>
        /// Creates a new WindowsAppAPIBridge instance
        /// </summary>
        /// <param name="webView2Manager">The WebView2Manager to use for JavaScript execution</param>
        public WindowsAppAPIBridge(WebView2Manager webView2Manager)
        {
            _webView2Manager = webView2Manager ?? throw new ArgumentNullException(nameof(webView2Manager));

            // Subscribe to execution results from WebView2Manager
            _webView2Manager.SetExecutionResultCallback(OnExecutionResult);
        }

        #region Public API Methods

        /// <summary>
        /// Gets all prompts from web app using window.windowsAppAPI.getPrompts()
        /// All business logic stays in the web app - this just fetches metadata
        /// </summary>
        public async Task<List<PromptInfo>> GetPromptsAsync()
        {
            var script = @"
                (() => {
                    try {
                        if (!window.windowsAppAPI || !window.windowsAppAPI.getPrompts) {
                            console.error('[C# Bridge] Windows App API not available');
                            return [];
                        }
                        
                        // Return the array directly - C# will handle JSON serialization
                        const prompts = window.windowsAppAPI.getPrompts();
                        console.log('[C# Bridge] Returning', prompts.length, 'prompts to C#');
                        return prompts;
                    } catch (e) {
                        console.error('[C# Bridge] Error:', e);
                        return [];
                    }
                })()
            ";

            var result = await _webView2Manager.ExecuteJavaScriptAsync(script);
            Debug.WriteLine($"[WindowsAppAPIBridge] GetPrompts raw result length: {result.Length}");

            try
            {
                var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Debug.WriteLine($"[WindowsAppAPIBridge] ✅ Successfully deserialized {prompts?.Count ?? 0} prompts");
                return prompts ?? new List<PromptInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] ❌ Deserialization error: {ex.Message}");
                return new List<PromptInfo>();
            }
        }

        /// <summary>
        /// Gets placeholders for a prompt using window.windowsAppAPI.getPlaceholders()
        /// All placeholder parsing logic stays in the web app
        /// </summary>
        public async Task<string[]> GetPlaceholdersAsync(string promptId)
        {
            var script = $@"
                (() => {{
                    try {{
                        if (window.windowsAppAPI && window.windowsAppAPI.getPlaceholders) {{
                            return window.windowsAppAPI.getPlaceholders('{promptId}');
                        }} else {{
                            console.error('Windows App API not available');
                            return [];
                        }}
                    }} catch (e) {{
                        console.error('Error getting placeholders:', e);
                        return [];
                    }}
                }})()
            ";

            var result = await _webView2Manager.ExecuteJavaScriptAsync(script);

            var placeholders = JsonSerializer.Deserialize<string[]>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return placeholders ?? Array.Empty<string>();
        }

        /// <summary>
        /// Fills placeholders in content using window.windowsAppAPI.fillContent()
        /// All string replacement logic stays in the web app
        /// </summary>
        public async Task<string> FillContentAsync(string promptId, Dictionary<string, string> values)
        {
            // Convert dictionary to JSON
            var valuesJson = JsonSerializer.Serialize(values);
            var escapedValuesJson = valuesJson.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");

            var script = $@"
                (() => {{
                    try {{
                        if (window.windowsAppAPI && window.windowsAppAPI.fillContent) {{
                            const values = JSON.parse('{escapedValuesJson}');
                            const filled = window.windowsAppAPI.fillContent('{promptId}', values);
                            return {{ success: true, content: filled }};
                        }} else {{
                            console.error('Windows App API not available');
                            return {{ success: false, error: 'API not available' }};
                        }}
                    }} catch (e) {{
                        console.error('Error filling content:', e);
                        return {{ success: false, error: e.message }};
                    }}
                }})()
            ";

            var result = await _webView2Manager.ExecuteJavaScriptAsync(script);

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            if (root.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                return root.GetProperty("content").GetString() ?? "";
            }
            else
            {
                var error = root.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : "Unknown error";
                throw new Exception($"Failed to fill content: {error}");
            }
        }

        /// <summary>
        /// Executes a prompt using window.windowsAppAPI.executePrompt()
        /// Handles both direct copy and LLM execution - all logic in web app
        /// Uses message passing for async LLM operations
        /// </summary>
        public async Task<ExecutionResult> ExecutePromptAsync(string promptId, string? content = null)
        {
            // Check if already executing
            if (_executionTcs != null)
            {
                Debug.WriteLine("[WindowsAppAPIBridge] Execution already in progress");
                return new ExecutionResult
                {
                    Success = false,
                    Error = "Another execution is already in progress"
                };
            }

            // Properly escape content for JavaScript string
            var contentArg = content != null
                ? $"'{content.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r")}'"
                : "undefined";

            // Create TaskCompletionSource for this execution
            _executionTcs = new TaskCompletionSource<ExecutionResult>();

            // Trigger execution via JavaScript (fire-and-forget)
            // Result will come back via WebMessageReceived event -> OnExecutionResult callback
            var script = $@"
                (() => {{
                    try {{
                        if (!window.windowsAppAPI || !window.windowsAppAPI.executePrompt) {{
                            console.error('[C# Bridge] Windows App API not available');
                            window.chrome.webview.postMessage({{
                                type: 'executeResult',
                                success: false,
                                error: 'API not available'
                            }});
                            return;
                        }}
                        
                        console.log('[C# Bridge] Triggering execution for prompt:', '{promptId}');
                        
                        // Call executePrompt - it will handle async execution and post result
                        window.windowsAppAPI.executePrompt('{promptId}', {contentArg});
                    }} catch (e) {{
                        console.error('[C# Bridge] Error triggering execution:', e);
                        window.chrome.webview.postMessage({{
                            type: 'executeResult',
                            success: false,
                            error: e.message || String(e)
                        }});
                    }}
                }})()
            ";

            try
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] Triggering execution for prompt: {promptId}");

                // Trigger the execution
                await _webView2Manager.ExecuteJavaScriptAsync(script);

                // Wait for result with timeout (60 seconds for LLM execution)
                var resultTask = _executionTcs.Task;
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));

                var completedTask = await Task.WhenAny(resultTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine("[WindowsAppAPIBridge] Execution timed out");
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = "Execution timed out after 60 seconds"
                    };
                }

                var result = await resultTask;
                Debug.WriteLine($"[WindowsAppAPIBridge] ✅ Success: {result.Success}, Result length: {result.Result?.Length ?? 0}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] Exception: {ex.Message}");
                return new ExecutionResult
                {
                    Success = false,
                    Error = $"C# Exception: {ex.Message}"
                };
            }
            finally
            {
                // Clean up TaskCompletionSource
                _executionTcs = null;
            }
        }

        /// <summary>
        /// Gets all system prompts from web app using window.windowsAppAPI.getSystemPrompts()
        /// </summary>
        public async Task<List<SystemPromptInfo>> GetSystemPromptsAsync()
        {
            var script = @"
                (() => {
                    try {
                        if (!window.windowsAppAPI || !window.windowsAppAPI.getSystemPrompts) {
                            console.error('[C# Bridge] Windows App API not available');
                            return [];
                        }
                        
                        const systemPrompts = window.windowsAppAPI.getSystemPrompts();
                        console.log('[C# Bridge] Returning', systemPrompts.length, 'system prompts to C#');
                        return systemPrompts;
                    } catch (e) {
                        console.error('[C# Bridge] Error:', e);
                        return [];
                    }
                })()
            ";

            var result = await _webView2Manager.ExecuteJavaScriptAsync(script);
            Debug.WriteLine($"[WindowsAppAPIBridge] GetSystemPrompts raw result length: {result.Length}");

            try
            {
                var systemPrompts = JsonSerializer.Deserialize<List<SystemPromptInfo>>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Debug.WriteLine($"[WindowsAppAPIBridge] ✅ Successfully deserialized {systemPrompts?.Count ?? 0} system prompts");
                return systemPrompts ?? new List<SystemPromptInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] ❌ Deserialization error: {ex.Message}");
                return new List<SystemPromptInfo>();
            }
        }

        /// <summary>
        /// Executes a one-time prompt with a system prompt (no saved prompt required)
        /// Used for the "Co-Author One Time Prompt" feature
        /// Uses message passing for async LLM operations
        /// </summary>
        public async Task<ExecutionResult> ExecuteOneTimePromptAsync(string systemPromptContent, string userPrompt)
        {
            // Check if already executing
            if (_executionTcs != null)
            {
                Debug.WriteLine("[WindowsAppAPIBridge] Execution already in progress");
                return new ExecutionResult
                {
                    Success = false,
                    Error = "Another execution is already in progress"
                };
            }

            // Properly escape strings for JavaScript
            var escapedSystemPrompt = systemPromptContent
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
            var escapedUserPrompt = userPrompt
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            // Create TaskCompletionSource for this execution
            _executionTcs = new TaskCompletionSource<ExecutionResult>();

            var script = $@"
                (() => {{
                    try {{
                        if (!window.windowsAppAPI || !window.windowsAppAPI.executeOneTimePrompt) {{
                            console.error('[C# Bridge] Windows App API not available');
                            window.chrome.webview.postMessage({{
                                type: 'executeResult',
                                success: false,
                                error: 'API not available'
                            }});
                            return;
                        }}
                        
                        console.log('[C# Bridge] Triggering one-time prompt execution');
                        
                        // Call executeOneTimePrompt - it will handle async execution and post result
                        window.windowsAppAPI.executeOneTimePrompt('{escapedSystemPrompt}', '{escapedUserPrompt}');
                    }} catch (e) {{
                        console.error('[C# Bridge] Error triggering execution:', e);
                        window.chrome.webview.postMessage({{
                            type: 'executeResult',
                            success: false,
                            error: e.message || String(e)
                        }});
                    }}
                }})()
            ";

            try
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] Triggering one-time prompt execution");

                // Trigger the execution
                await _webView2Manager.ExecuteJavaScriptAsync(script);

                // Wait for result with timeout (60 seconds for LLM execution)
                var resultTask = _executionTcs.Task;
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));

                var completedTask = await Task.WhenAny(resultTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine("[WindowsAppAPIBridge] Execution timed out");
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = "Execution timed out after 60 seconds"
                    };
                }

                var result = await resultTask;
                Debug.WriteLine($"[WindowsAppAPIBridge] ✅ Success: {result.Success}, Result length: {result.Result?.Length ?? 0}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsAppAPIBridge] Exception: {ex.Message}");
                return new ExecutionResult
                {
                    Success = false,
                    Error = $"C# Exception: {ex.Message}"
                };
            }
            finally
            {
                // Clean up TaskCompletionSource
                _executionTcs = null;
            }
        }

        #endregion

        #region Delegates for CommandPaletteForm

        /// <summary>
        /// Delegate for getting placeholders (used by CommandPaletteForm)
        /// </summary>
        public Func<string, Task<string[]>> GetPlaceholdersDelegate => GetPlaceholdersAsync;

        /// <summary>
        /// Delegate for filling content (used by CommandPaletteForm)
        /// </summary>
        public Func<string, Dictionary<string, string>, Task<string>> FillContentDelegate => FillContentAsync;

        /// <summary>
        /// Delegate for executing prompts (used by CommandPaletteForm)
        /// </summary>
        public Func<string, string?, Task<ExecutionResult>> ExecutePromptDelegate => ExecutePromptAsync;

        /// <summary>
        /// Delegate for getting system prompts (used by CommandPaletteForm)
        /// </summary>
        public Func<Task<List<SystemPromptInfo>>> GetSystemPromptsDelegate => GetSystemPromptsAsync;

        /// <summary>
        /// Delegate for executing one-time prompts (used by CommandPaletteForm)
        /// </summary>
        public Func<string, string, Task<ExecutionResult>> ExecuteOneTimePromptDelegate => ExecuteOneTimePromptAsync;

        #endregion

        #region Private Methods

        private void OnExecutionResult(ExecutionResult result)
        {
            // Complete the pending task with the result
            _executionTcs?.TrySetResult(result);
        }

        #endregion
    }
}
