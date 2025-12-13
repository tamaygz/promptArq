using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromptArqApp
{
    public partial class MainForm : Form
    {
        private WebView2 _webView = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private NotifyIcon _notifyIcon = null!;

        private AppSettings _settings = null!;
        private HotkeyManager _hotkeyManager = null!;
        private CommandPaletteForm? _commandPalette;
        private SettingsForm? _settingsForm;
        private const int VitePort = 5000;
        private bool _isViteReady = false;

        // Windows API constants for dark title bar
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        public MainForm()
        {
            _settings = AppSettings.Load();
            if (_settings.Hotkeys.Count == 0)
            {
                _settings.SetDefaultHotkeys();
                _settings.Save();
            }

            InitializeComponent();
            InitializeCustomComponents();
            _hotkeyManager = new HotkeyManager(Handle);
            RegisterHotkeys();

            // Start all servers through unified manager
            UnifiedServerManager.Start();
            
            // Monitor for Vite startup
            MonitorViteStartup();

            // Initialize command palette
            _commandPalette = new CommandPaletteForm();
            _commandPalette.ActionSelected += CommandPalette_ActionSelected;
        }

        private Icon? LoadAppIcon()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "PromptArqApp.app_icon.ico";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load custom icon: {ex.Message}");
            }
            return null;
        }

        private void SetDarkTitleBar()
        {
            if (Handle != IntPtr.Zero)
            {
                try
                {
                    // Enable dark mode for title bar
                    int useDarkMode = 1;
                    DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

                    // Extend the frame into the client area
                    MARGINS margins = new MARGINS
                    {
                        cxLeftWidth = 8,
                        cxRightWidth = 8,
                        cyBottomHeight = 22,
                        cyTopHeight = 22
                    };

                    DwmExtendFrameIntoClientArea(Handle, ref margins);

                    // Set dark blue color for caption (RGB to BGR format: 0x00BBGGRR)
                    // Dark blue: RGB(0, 51, 102) -> BGR 0x00663300
                    int captionColor = 0x00663300;
                    DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                    // Set dark blue color for border
                    int borderColor = 0x00663300;
                    DwmSetWindowAttribute(Handle, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to set dark title bar: {ex.Message}");
                }
            }
        }

        private void InitializeCustomComponents()
        {
            Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
            var customIcon = LoadAppIcon();
            Icon = customIcon ?? SystemIcons.Application;

            // Status strip (only in DEBUG mode)
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Initializing...");
            _statusStrip.Items.Add(_statusLabel);
#if !DEBUG
            _statusStrip.Visible = false;
#endif

            // System tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = customIcon ?? SystemIcons.Application,
                Text = "PromptArq",
                Visible = true
            };
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("About", null, (s, e) => ShowAbout());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // WebView2
            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            // Layout
            Controls.Add(_webView);
            Controls.Add(_statusStrip);

            // Events
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;

            // Apply dark title bar when handle is created
            HandleCreated += (s, e) => SetDarkTitleBar();
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            // Ensure dark title bar is applied after load
            SetDarkTitleBar();
            await InitializeWebView();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainForm_Load(this, e);
        }

        private async Task InitializeWebView()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _statusLabel.Text = "WebView2 initialized. Waiting for Vite server...";
                _ = WaitForViteAndNavigate();
            }
            else
            {
                _statusLabel.Text = $"WebView2 initialization failed: {e.InitializationException?.Message}";
            }
        }

        private async Task WaitForViteAndNavigate()
        {
            Console.WriteLine("Waiting for Vite server to be ready...");
            for (int i = 0; i < 60; i++)
            {
                if (_isViteReady)
                {
                    Console.WriteLine($"Vite is ready! Navigating to http://localhost:{VitePort}");
                    _webView.Source = new Uri($"http://localhost:{VitePort}");
                    _statusLabel.Text = "Connected to Vite server";
                    return;
                }

                if (i == 20)
                {
                    _statusLabel.Text = "Attempting to connect...";
                    try
                    {
                        _webView.Source = new Uri($"http://localhost:{VitePort}");
                    }
                    catch { }
                }

                await Task.Delay(500);
            }
            Console.WriteLine("Vite server did not start in time");
            _statusLabel.Text = "Vite server did not start in time";
            MessageBox.Show(
                "The Vite development server did not start within 30 seconds.\nCheck the console output for errors.",
                "Timeout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private void MonitorViteStartup()
        {
            // Monitor for Vite readiness by polling the port
            Task.Run(async () =>
            {
                this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                    _statusLabel.Text = "Starting servers...";
                });
                
                for (int i = 0; i < 60; i++)
                {
                    try
                    {
                        using var client = new System.Net.Http.HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(1);
                        var response = await client.GetAsync($"http://localhost:{VitePort}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _isViteReady = true;
                            this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                                _statusLabel.Text = "Vite server is running";
                            });
                            Debug.WriteLine("[MainForm] Vite server detected as ready");
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
                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        _statusLabel.Text = "Vite server startup timeout";
                    });
                    Debug.WriteLine("[MainForm] Vite server did not become ready in time");
                }
            });
        }

        private void RegisterHotkeys()
        {
            foreach (var hotkey in _settings.Hotkeys)
            {
                Action action = hotkey.Action switch
                {
                    "Show/Hide Window" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate { ToggleWindow(); }),
                    "New Prompt" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        ExecuteJavaScript(@"
                            (function() {
                                const buttons = Array.from(document.querySelectorAll('button'));
                                const newPromptBtn = buttons.find(btn => 
                                    btn.textContent.includes('New Prompt') || 
                                    btn.textContent.includes('new prompt')
                                );
                                
                                if (newPromptBtn) {
                                    newPromptBtn.click();
                                    return true;
                                }
                                
                                window.dispatchEvent(new CustomEvent('createNewPrompt'));
                                return false;
                            })();
                        ");
                    }),
                    "Settings" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate { ShowSettings(); }),
                    "Command Palette" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate { ShowCommandPalette(); }),
                    "Quit App" => () => this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { Close(); }),
                    _ => () => { }
                };

                _hotkeyManager.RegisterHotkey(hotkey, action);
            }
        }

        private async void ShowCommandPalette()
        {
            if (_commandPalette == null || _webView?.CoreWebView2 == null)
            {
                MessageBox.Show("Command Palette or WebView not ready", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var prompts = await GetPromptsFromWebApp();
                Debug.WriteLine($"Fetched {prompts.Count} prompts from web app");

                if (prompts.Count == 0)
                {
                    MessageBox.Show(
                        "No prompts found!\n\n" +
                        "This could mean:\n" +
                        "1. You haven't created any prompts yet\n" +
                        "2. The web app hasn't loaded yet\n" +
                        "3. localStorage is not accessible\n\n" +
                        "Try creating a prompt in the web app first.",
                        "No Prompts",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                _commandPalette.ShowPalette(prompts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing command palette: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Failed to load prompts:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task<List<PromptInfo>?> FetchPromptsFromStorageServer()
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

            try
            {
                // Fetch all required data from storage server
                var promptsJson = await httpClient.GetStringAsync("http://localhost:5001/get?key=promptarq_prompts");
                var projectsJson = await httpClient.GetStringAsync("http://localhost:5001/get?key=promptarq_projects");
                var categoriesJson = await httpClient.GetStringAsync("http://localhost:5001/get?key=promptarq_categories");
                var tagsJson = await httpClient.GetStringAsync("http://localhost:5001/get?key=promptarq_tags");

                if (string.IsNullOrEmpty(promptsJson) || promptsJson == "null")
                    return null;

                // Deserialize using JsonDocument for flexibility
                using var promptsDoc = JsonDocument.Parse(promptsJson);
                using var projectsDoc = string.IsNullOrEmpty(projectsJson) || projectsJson == "null" ? null : JsonDocument.Parse(projectsJson);
                using var categoriesDoc = string.IsNullOrEmpty(categoriesJson) || categoriesJson == "null" ? null : JsonDocument.Parse(categoriesJson);
                using var tagsDoc = string.IsNullOrEmpty(tagsJson) || tagsJson == "null" ? null : JsonDocument.Parse(tagsJson);

                var result = new List<PromptInfo>();

                foreach (var promptElem in promptsDoc.RootElement.EnumerateArray())
                {
                    var projectId = promptElem.TryGetProperty("projectId", out var projId) ? projId.GetString() : null;
                    var categoryId = promptElem.TryGetProperty("categoryId", out var catId) ? catId.GetString() : null;

                    var promptTagIds = new List<string>();
                    if (promptElem.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tagId in tagsProp.EnumerateArray())
                        {
                            promptTagIds.Add(tagId.GetString() ?? "");
                        }
                    }

                    // Find project - FIXED: Use unique variable name 'projIdProp'
                    string projectName = "";
                    if (projectsDoc != null && !string.IsNullOrEmpty(projectId))
                    {
                        foreach (var proj in projectsDoc.RootElement.EnumerateArray())
                        {
                            if (proj.TryGetProperty("id", out var projIdProp) && projIdProp.GetString() == projectId)
                            {
                                projectName = proj.TryGetProperty("name", out var projName) ? projName.GetString() ?? "" : "";
                                break;
                            }
                        }
                    }

                    // Find category - FIXED: Use unique variable name 'catIdProp'
                    string categoryName = "";
                    if (categoriesDoc != null && !string.IsNullOrEmpty(categoryId))
                    {
                        foreach (var cat in categoriesDoc.RootElement.EnumerateArray())
                        {
                            if (cat.TryGetProperty("id", out var catIdProp) && catIdProp.GetString() == categoryId)
                            {
                                categoryName = cat.TryGetProperty("name", out var catName) ? catName.GetString() ?? "" : "";
                                break;
                            }
                        }
                    }

                    // Find tag names - FIXED: Use unique variable name 'tagIdProp'
                    var promptTags = new List<string>();
                    if (tagsDoc != null && promptTagIds.Count > 0)
                    {
                        foreach (var tag in tagsDoc.RootElement.EnumerateArray())
                        {
                            if (tag.TryGetProperty("id", out var tagIdProp))
                            {
                                var tagIdStr = tagIdProp.GetString();
                                if (!string.IsNullOrEmpty(tagIdStr) && promptTagIds.Contains(tagIdStr))
                                {
                                    promptTags.Add(tag.TryGetProperty("name", out var tagName) ? tagName.GetString() ?? "" : "");
                                }
                            }
                        }
                    }

                    var content = promptElem.TryGetProperty("content", out var cont) ? cont.GetString() ?? "" : "";
                    var hasPlaceholders = System.Text.RegularExpressions.Regex.IsMatch(content, @"\{\{[^}]+\}\}");

                    // FIXED: Use unique variable name 'promptIdProp'
                    result.Add(new PromptInfo
                    {
                        Id = promptElem.TryGetProperty("id", out var promptIdProp) ? promptIdProp.GetString() ?? "" : "",
                        Title = promptElem.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        Description = promptElem.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        Content = content,
                        ProjectName = projectName,
                        CategoryName = categoryName,
                        Tags = promptTags.ToArray(),
                        IsArchived = promptElem.TryGetProperty("isArchived", out var arch) && arch.GetBoolean(),
                        HasPlaceholders = hasPlaceholders
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching from storage server: {ex.Message}");
                return null;
            }
        }


        // REPLACE GetPromptsFromWebApp() method with this:

        private async Task<List<PromptInfo>> GetPromptsFromWebApp()
        {
            // Try to fetch from HTTP storage server first (shared database)
            try
            {
                var prompts = await FetchPromptsFromStorageServer();
                if (prompts != null && prompts.Count >= 0)  // Even 0 is valid!
                {
                    Debug.WriteLine($"Fetched {prompts.Count} prompts from storage server");
                    return prompts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch from storage server, falling back to localStorage: {ex.Message}");
            }

            // Fallback to localStorage query (for backwards compatibility)
            var script = @"
        (function() {
            try {
                const prompts = JSON.parse(localStorage.getItem('promptarq_prompts') || '[]');
                const projects = JSON.parse(localStorage.getItem('promptarq_projects') || '[]');
                const categories = JSON.parse(localStorage.getItem('promptarq_categories') || '[]');
                const tags = JSON.parse(localStorage.getItem('promptarq_tags') || '[]');
                
                console.log('Prompts count:', prompts.length);
                console.log('Projects count:', projects.length);
                
                const result = prompts.map(p => {
                    const project = projects.find(pr => pr.id === p.projectId);
                    const category = categories.find(c => c.id === p.categoryId);
                    const promptTags = tags.filter(t => p.tags.includes(t.id)).map(t => t.name);
                    const hasPlaceholders = /\{\{[^}]+\}\}/.test(p.content);
                    
                    return {
                        id: p.id,
                        title: p.title,
                        description: p.description || '',
                        content: p.content,
                        projectName: project?.name || '',
                        categoryName: category?.name || '',
                        tags: promptTags,
                        isArchived: p.isArchived || false,
                        hasPlaceholders: hasPlaceholders
                    };
                });
                
                console.log('Mapped prompts:', result.length);
                return JSON.stringify(result);
            } catch (e) {
                console.error('Error fetching prompts:', e);
                return JSON.stringify([]);
            }
        })();
    ";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            Debug.WriteLine($"Raw result from JavaScript: {result}");

            var json = result.Trim('"').Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "");
            Debug.WriteLine($"Processed JSON: {json.Substring(0, Math.Min(500, json.Length))}...");

            try
            {
                var prompts = JsonSerializer.Deserialize<List<PromptInfo>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Debug.WriteLine($"Deserialized {prompts?.Count ?? 0} prompts");
                return prompts ?? new List<PromptInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON Deserialization error: {ex.Message}");
                Debug.WriteLine($"JSON content: {json}");
                throw;
            }
        }


        private async void CommandPalette_ActionSelected(object? sender, PromptActionEventArgs e)
        {
            if (_webView?.CoreWebView2 == null)
                return;

            try
            {
                switch (e.Action.Type)
                {
                    case PromptActionType.Copy:
                        Clipboard.SetText(e.Prompt.Content);
                        MessageBox.Show("Prompt content copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    //case PromptActionType.FillPlaceholders:
                    //    HandleFillPlaceholders(e.Prompt);
                    //    break;
                    case PromptActionType.OpenInEditor:
                        var openScript = $@"
                            (function() {{
                                const buttons = document.querySelectorAll('button, div[role=""button""]');
                                const items = Array.from(buttons).filter(el => 
                                    el.textContent.includes('{e.Prompt.Title.Replace("'", "\\'")}')
                                );
                                if (items.length > 0) items[0].click();
                            }})();
                        ";
                        await _webView.CoreWebView2.ExecuteScriptAsync(openScript);
                        ShowWindow();
                        break;

                    default:
                        MessageBox.Show($"Action '{e.Action.Name}' will be implemented soon!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing action: {ex.Message}");
                MessageBox.Show($"Failed to execute action: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ExecuteJavaScript(string script)
        {
            if (_webView?.CoreWebView2 != null)
            {
                try
                {
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error executing JavaScript: {ex.Message}");
                }
            }
        }

        private void ShowSettings()
        {
            // If settings form is already open, close it (toggle behavior)
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Close();
                _settingsForm.Dispose();
                _settingsForm = null;
                return;
            }

            // Create and show new settings form
            _settingsForm = new SettingsForm(_settings);
            
            var result = _settingsForm.ShowDialog();
            
            if (result == DialogResult.OK)
            {
                _hotkeyManager.UnregisterAll();
                RegisterHotkeys();
                MessageBox.Show("Settings saved. Hotkeys have been updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
            // Clean up
            _settingsForm?.Dispose();
            _settingsForm = null;
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "PromptArq Windows Application\n\n" +
                "A desktop wrapper for the PromptArq web application.\n\n" +
                "Built with C# and WebView2\n" +
                "Vite app integration with global hotkeys\n" +
                "Command Palette - Press Ctrl+K to search prompts",
                "About PromptArq",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ToggleWindow()
        {
            if (WindowState == FormWindowState.Minimized || !Visible)
            {
                ShowWindow();
            }
            else
            {
                HideWindow();
            }
        }

        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void HideWindow()
        {
            Hide();
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _notifyIcon.ShowBalloonTip(1000, "PromptArq", "Application minimized to tray", ToolTipIcon.Info);
            }
            else if (WindowState == FormWindowState.Normal || WindowState == FormWindowState.Maximized)
            {
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("[MainForm] FormClosing event triggered");
            
            _settings.Save();
            _hotkeyManager?.Dispose();
            _commandPalette?.Dispose();
            _notifyIcon?.Dispose();
            
            // Stop all servers through unified manager
            UnifiedServerManager.Stop();
            
            Debug.WriteLine("[MainForm] FormClosing cleanup complete");
        }

        protected override void WndProc(ref Message m)
        {
            if (!_hotkeyManager?.ProcessHotkey(m) ?? true)
            {
                base.WndProc(ref m);
            }
        }

        private void PasteToActiveWindow(string text)
        {
            try
            {
                Clipboard.SetText(text);
                this.WindowState = FormWindowState.Minimized;
                System.Threading.Thread.Sleep(300);
                SendKeys.SendWait("^v");
                MessageBox.Show("Text pasted to active window!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pasting to active window: {ex.Message}");
                MessageBox.Show(
                    $"Failed to paste. Text is in clipboard, use Ctrl+V manually.\n\nError: {ex.Message}",
                    "Paste Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
    }
}
