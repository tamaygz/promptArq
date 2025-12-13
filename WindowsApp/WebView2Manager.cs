using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptArqApp
{
    /// <summary>
    /// Manages WebView2 lifecycle, initialization, navigation, and message passing.
    /// Handles Vite server monitoring and JavaScript execution.
    /// </summary>
    public class WebView2Manager
    {
        private readonly WebView2 _webView;
        private readonly Action<string> _updateStatus;
        private readonly int _vitePort;
        private bool _isViteReady = false;
        private Action<ExecutionResult>? _onExecutionResult;

        /// <summary>
        /// Creates a new WebView2Manager instance
        /// </summary>
        /// <param name="webView">The WebView2 control to manage</param>
        /// <param name="updateStatus">Callback to update status messages</param>
        /// <param name="vitePort">Port number where Vite server runs (default: 5000)</param>
        public WebView2Manager(WebView2 webView, Action<string> updateStatus, int vitePort = 5000)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
            _vitePort = vitePort;
        }

        /// <summary>
        /// Sets the callback for execution results from JavaScript
        /// </summary>
        public void SetExecutionResultCallback(Action<ExecutionResult> callback)
        {
            _onExecutionResult = callback;
        }

        /// <summary>
        /// Initializes the WebView2 control and starts monitoring for Vite server
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _updateStatus("Initializing WebView2...");
                
                // Wire up event handlers
                _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

                // Initialize WebView2
                await _webView.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                _updateStatus($"WebView2 initialization failed: {ex.Message}");
                MessageBox.Show(
                    $"Failed to initialize WebView2: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Starts monitoring for Vite server availability
        /// </summary>
        public void StartViteMonitoring()
        {
            Task.Run(async () =>
            {
                _updateStatus("Starting servers...");

                for (int i = 0; i < 60; i++)
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(1);
                        var response = await client.GetAsync($"http://localhost:{_vitePort}");

                        if (response.IsSuccessStatusCode)
                        {
                            _isViteReady = true;
                            _updateStatus("Vite server is running");
                            Debug.WriteLine("[WebView2Manager] Vite server detected as ready");
                            break;
                        }
                    }
                    catch
                    {
                        // Server not ready yet, keep waiting
                    }

                    await Task.Delay(500);
                }

                if (!_isViteReady)
                {
                    _updateStatus("Vite server startup timeout");
                    Debug.WriteLine("[WebView2Manager] Vite server did not become ready in time");
                }
            });
        }

        /// <summary>
        /// Executes JavaScript code in the WebView2 and returns the result
        /// </summary>
        public async Task<string> ExecuteJavaScriptAsync(string script)
        {
            if (_webView?.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not initialized");
            }

            return await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void WebView_CoreWebView2InitializationCompleted(
            object? sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _updateStatus("WebView2 initialized. Waiting for Vite server...");

                // Wire up WebMessageReceived for async execution results
                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Wait for Vite and navigate
                _ = WaitForViteAndNavigateAsync();
            }
            else
            {
                _updateStatus($"WebView2 initialization failed: {e.InitializationException?.Message}");
            }
        }

        private async Task WaitForViteAndNavigateAsync()
        {
            Debug.WriteLine("[WebView2Manager] Waiting for Vite server to be ready...");

            for (int i = 0; i < 60; i++)
            {
                if (_isViteReady)
                {
                    Debug.WriteLine($"[WebView2Manager] Vite is ready! Navigating to http://localhost:{_vitePort}");
                    _webView.Source = new Uri($"http://localhost:{_vitePort}");
                    _updateStatus("Connected to Vite server");
                    return;
                }

                if (i == 20)
                {
                    _updateStatus("Attempting to connect...");
                    try
                    {
                        _webView.Source = new Uri($"http://localhost:{_vitePort}");
                    }
                    catch { }
                }

                await Task.Delay(500);
            }

            Debug.WriteLine("[WebView2Manager] Vite server did not start in time");
            _updateStatus("Vite server did not start in time");
            MessageBox.Show(
                "The Vite development server did not start within 30 seconds.\nCheck the console output for errors.",
                "Timeout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private void CoreWebView2_WebMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Use WebMessageAsJson which is already a parsed JSON string
                var json = e.WebMessageAsJson;
                Debug.WriteLine($"[WebView2Manager] Received message: {json.Substring(0, Math.Min(500, json.Length))}");

                using var doc = JsonDocument.Parse(json);
                var message = doc.RootElement;

                if (message.TryGetProperty("type", out var typeElement) &&
                    typeElement.GetString() == "executeResult")
                {
                    // Parse execution result
                    var success = message.TryGetProperty("success", out var successElement) &&
                                  successElement.GetBoolean();

                    var result = message.TryGetProperty("result", out var resultElement) ?
                                 resultElement.GetString() : null;

                    var error = message.TryGetProperty("error", out var errorElement) ?
                                errorElement.GetString() : null;

                    var executionResult = new ExecutionResult
                    {
                        Success = success,
                        Result = result ?? string.Empty,
                        Error = error ?? string.Empty
                    };

                    Debug.WriteLine($"[WebView2Manager] ✅ Parsed result - Success: {success}, Result length: {result?.Length ?? 0}");

                    // Notify callback
                    _onExecutionResult?.Invoke(executionResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebView2Manager] ❌ Error parsing message: {ex.Message}");
                Debug.WriteLine($"[WebView2Manager] Stack trace: {ex.StackTrace}");

                var errorResult = new ExecutionResult
                {
                    Success = false,
                    Error = ex.Message
                };

                _onExecutionResult?.Invoke(errorResult);
            }
        }
    }
}
