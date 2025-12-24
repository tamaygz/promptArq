using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;

namespace PromptArqApp
{
    /// <summary>
    /// Manages WebView2 lifecycle, initialization, navigation, and message passing.
    /// Handles Vite server monitoring and JavaScript execution.
    /// </summary>
    public class WebView2Manager : IDisposable
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<WebView2Manager>();
        
        private readonly WebView2 _webView;
        private readonly Action<string> _updateStatus;
        private readonly int _vitePort;
        private bool _isViteReady = false;
        private Action<ExecutionResult>? _onExecutionResult;
        private bool _disposed = false;
        private readonly object _disposeLock = new object();

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
            
            Logger.Information("WebView2Manager created for port {VitePort}", vitePort);
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
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebView2Manager));
            }

            try
            {
                Logger.Information("Initializing WebView2");
                _updateStatus("Initializing WebView2...");
                
                // Wire up event handlers
                _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

                // Initialize WebView2
                await _webView.EnsureCoreWebView2Async(null);
                
                Logger.Information("WebView2 initialization request completed");
            }
            catch (WebView2RuntimeNotFoundException ex)
            {
                Logger.Error(ex, "WebView2 Runtime not found");
                _updateStatus("WebView2 Runtime not installed");
                
                MessageBox.Show(
                    "WebView2 Runtime is not installed on this system.\n\n" +
                    "Please download and install it from:\n" +
                    "https://developer.microsoft.com/microsoft-edge/webview2/",
                    "WebView2 Runtime Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize WebView2");
                _updateStatus($"WebView2 initialization failed: {ex.Message}");
                
                MessageBox.Show(
                    $"Failed to initialize WebView2:\n\n{ex.Message}\n\n" +
                    "Please check the logs for more details.",
                    "WebView2 Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
        }

        /// <summary>
        /// Starts monitoring for Vite server availability
        /// </summary>
        public void StartViteMonitoring()
        {
            if (_disposed)
            {
                Logger.Warning("Attempted to start Vite monitoring on disposed WebView2Manager");
                return;
            }

            Logger.Information("Starting Vite server monitoring on port {VitePort}", _vitePort);
            
            Task.Run(async () =>
            {
                try
                {
                    _updateStatus("Starting servers...");

                    for (int i = 0; i < 60; i++)
                    {
                        if (_disposed)
                        {
                            Logger.Information("Vite monitoring cancelled due to disposal");
                            return;
                        }

                        try
                        {
                            using var client = new HttpClient();
                            client.Timeout = TimeSpan.FromSeconds(1);
                            var response = await client.GetAsync($"http://localhost:{_vitePort}");

                            if (response.IsSuccessStatusCode)
                            {
                                _isViteReady = true;
                                _updateStatus("Vite server is running");
                                Logger.Information("Vite server detected as ready on port {VitePort}", _vitePort);
                                break;
                            }
                        }
                        catch (HttpRequestException)
                        {
                            // Server not ready yet, keep waiting
                        }
                        catch (TaskCanceledException)
                        {
                            // Timeout, keep waiting
                        }

                        await Task.Delay(500);
                    }

                    if (!_isViteReady)
                    {
                        _updateStatus("Vite server startup timeout");
                        Logger.Warning("Vite server did not become ready within timeout period");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in Vite monitoring");
                }
            });
        }

        /// <summary>
        /// Executes JavaScript code in the WebView2 and returns the result
        /// </summary>
        public async Task<string> ExecuteJavaScriptAsync(string script)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebView2Manager));
            }

            if (_webView?.CoreWebView2 == null)
            {
                Logger.Error("Attempted to execute JavaScript before WebView2 initialization");
                throw new InvalidOperationException("WebView2 is not initialized");
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                Logger.Warning("Attempted to execute empty JavaScript");
                throw new ArgumentException("Script cannot be null or empty", nameof(script));
            }

            try
            {
                Logger.Debug("Executing JavaScript (length: {Length})", script.Length);
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                Logger.Debug("JavaScript execution completed");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing JavaScript");
                throw;
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(
            object? sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            try
            {
                if (e.IsSuccess)
                {
                    Logger.Information("WebView2 initialized successfully");
                    _updateStatus("WebView2 initialized. Waiting for Vite server...");

                    // Wire up WebMessageReceived for async execution results
                    _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                    // Wait for Vite and navigate
                    _ = WaitForViteAndNavigateAsync();
                }
                else
                {
                    var error = e.InitializationException?.Message ?? "Unknown error";
                    Logger.Error(e.InitializationException, "WebView2 initialization failed");
                    _updateStatus($"WebView2 initialization failed: {error}");
                    
                    MessageBox.Show(
                        $"WebView2 failed to initialize:\n\n{error}",
                        "Initialization Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in WebView2 initialization completed handler");
            }
        }

        private async Task WaitForViteAndNavigateAsync()
        {
            try
            {
                Logger.Information("Waiting for Vite server to be ready");

                for (int i = 0; i < 120; i++)
                {
                    if (_disposed)
                    {
                        Logger.Information("Navigation cancelled due to disposal");
                        return;
                    }

                    if (_isViteReady)
                    {
                        Logger.Information("Vite is ready, navigating to http://localhost:{VitePort}", _vitePort);
                        _webView.Source = new Uri($"http://localhost:{_vitePort}");
                        _updateStatus("Connected to Vite server");
                        return;
                    }

                    if (i == 20)
                    {
                        _updateStatus("Attempting to connect...");
                        try
                        {
                            Logger.Debug("Attempting early navigation to Vite server");
                            _webView.Source = new Uri($"http://localhost:{_vitePort}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning(ex, "Early navigation attempt failed");
                        }
                    }

                    await Task.Delay(500);
                }

                Logger.Warning("Vite server did not start within timeout period");
                _updateStatus("Vite server did not start in time");
                
                NotificationManager.ShowToastBottomRight(
                    "Vite server did not start within 60 seconds. Check logs for errors.",
                    5000
                );
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error waiting for Vite server and navigating");
            }
        }

        private void CoreWebView2_WebMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                if (_disposed)
                {
                    Logger.Debug("Ignoring web message - manager is disposed");
                    return;
                }

                // Use WebMessageAsJson which is already a parsed JSON string
                var json = e.WebMessageAsJson;
                var preview = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                Logger.Debug("Received web message: {MessagePreview}", preview);

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

                    Logger.Information("Parsed execution result - Success: {Success}, Result length: {Length}",
                        success, result?.Length ?? 0);

                    // Notify callback
                    _onExecutionResult?.Invoke(executionResult);
                }
                else
                {
                    Logger.Debug("Received non-executeResult message type");
                }
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, "JSON parsing error in web message");
                
                var errorResult = new ExecutionResult
                {
                    Success = false,
                    Error = $"Message parsing error: {ex.Message}"
                };

                _onExecutionResult?.Invoke(errorResult);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error processing web message");

                var errorResult = new ExecutionResult
                {
                    Success = false,
                    Error = ex.Message
                };

                _onExecutionResult?.Invoke(errorResult);
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;

                Logger.Information("Disposing WebView2Manager");
                _disposed = true;

                try
                {
                    // Unsubscribe from events
                    if (_webView != null)
                    {
                        _webView.CoreWebView2InitializationCompleted -= WebView_CoreWebView2InitializationCompleted;
                        
                        if (_webView.CoreWebView2 != null)
                        {
                            _webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                        }
                    }
                    
                    Logger.Information("WebView2Manager disposed successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error during WebView2Manager disposal");
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
