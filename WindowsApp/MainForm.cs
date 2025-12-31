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
using Serilog;
using PromptArqApp.Theming;
using PromptArqApp.Core.Services;
using PromptArqApp.Workflow.Registry;

namespace PromptArqApp
{
    public partial class MainForm : BorderlessFormBase
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<MainForm>();
        private WebView2 _webView = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private NotifyIcon _notifyIcon = null!;

        private AppSettings _settings = null!;
        private PromptHistory _history = null!;
        private HotkeyManager _hotkeyManager = null!;
        private CommandPaletteForm? _commandPalette;
        private SettingsForm? _settingsForm;
        private const int VitePort = 5000;

        // Component managers
        private WebView2Manager _webViewManager = null!;
        private WindowsAppAPIBridge _apiManager = null!;
        
        // Services
        private IClipboardService? _clipboardService;
        private IWindowService? _windowService;

        // For status bar dragging
        private bool _isDraggingStatusBar = false;
        private Point _statusBarDragStart;

        private bool _disposed = false;

        public MainForm()
        {
            try
            {
                Logger.Information("Initializing MainForm");

                _settings = AppSettings.Load();
                if (_settings.Hotkeys.Count == 0)
                {
                    _settings.SetDefaultHotkeys();
                    _settings.Save();
                }

                _history = PromptHistory.Load();
                
                // Initialize services
                _clipboardService = ServiceConfiguration.GetService<IClipboardService>();
                _windowService = ServiceConfiguration.GetService<IWindowService>();

                InitializeComponent();
                InitializeCustomComponents();
                _hotkeyManager = new HotkeyManager(Handle);
                RegisterHotkeys();

                // Register with ThemeManager and apply theme
                ThemeManager.Instance.RegisterForm(this);
                ThemeManager.Instance.ApplyThemeToForm(this);
                
                // Apply rounded corners after theme is applied
                // Use BeginInvoke to ensure it runs after the form is fully initialized
                this.Load += (s, e) => ApplyInitialRoundedCorners();

                // Subscribe to theme changes
                EventHandler<ThemeChangedEventArgs> themeChangedHandler = (s, e) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => ThemeManager.Instance.ApplyThemeToForm(this)));
                    }
                    else
                    {
                        ThemeManager.Instance.ApplyThemeToForm(this);
                    }
                };
                ThemeManager.Instance.ThemeChanged += themeChangedHandler;
                
                // Cleanup on closing
                FormClosing += (s, e) =>
                {
                    ThemeManager.Instance.ThemeChanged -= themeChangedHandler;
                };

                // Start all servers through unified manager
                UnifiedServerManager.Start();

                // Initialize command palette with history and settings
                _commandPalette = new CommandPaletteForm(_history, _settings);
                _commandPalette.ActionSelected += CommandPalette_ActionSelected;
                
                // Delegates will be wired up in MainForm_Load after component managers are initialized
                
                Logger.Information("MainForm initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing MainForm");
                throw;
            }
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
                    Logger.Debug("Successfully loaded custom icon");
                    return new Icon(stream);
                }
                Logger.Warning("Icon resource not found: {ResourceName}", resourceName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load custom icon");
            }
            return null;
        }

        private void InitializeCustomComponents()
        {
            Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
            var customIcon = LoadAppIcon();
            Icon = customIcon ?? SystemIcons.Application;

            // Status strip (only in DEBUG mode)
            _statusStrip = new StatusStrip
            {
                Dock = DockStyle.None, // Don't dock, we'll position manually
                SizingGrip = false,
                AutoSize = false, // Disable auto-sizing so our manual sizing takes effect
                LayoutStyle = ToolStripLayoutStyle.Flow,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            _statusLabel = new ToolStripStatusLabel("Initializing...");
            _statusStrip.Items.Add(_statusLabel);
            
            // Enable status bar dragging for window movement
            _statusStrip.MouseDown += StatusStrip_MouseDown;
            _statusStrip.MouseMove += StatusStrip_MouseMove;
            _statusStrip.MouseUp += StatusStrip_MouseUp;
            
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

            // WebView2 - positioned with margin to leave space for resize borders
            _webView = new WebView2
            {
                Location = new Point(0, 0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Layout
            Controls.Add(_webView);
            Controls.Add(_statusStrip);

            // Events
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;
            Load += (s, e) => UpdateWebViewBounds();
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            try
            {
                Logger.Information("MainForm loading");
                
                // Initialize component managers
                _webViewManager = new WebView2Manager(_webView, UpdateStatus, VitePort);
                await _webViewManager.InitializeAsync();
                _apiManager = new WindowsAppAPIBridge(_webViewManager);
                
                // Wire up delegates for command palette
                if (_commandPalette != null)
                {
                    _commandPalette.NotifyAction = (message) => NotificationManager.ShowToast(message, 2000);
                    _commandPalette.GetPlaceholdersFromWebApp = _apiManager.GetPlaceholdersDelegate;
                    _commandPalette.FillContentInWebApp = _apiManager.FillContentDelegate;
                    _commandPalette.ExecutePromptInWebApp = _apiManager.ExecutePromptDelegate;
                    _commandPalette.GetSystemPromptsFromWebApp = _apiManager.GetSystemPromptsDelegate;
                    _commandPalette.ExecuteOneTimePromptFromWebApp = _apiManager.ExecuteOneTimePromptDelegate;
                }
                
                UpdateStatus("Ready");
                Logger.Information("MainForm loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading MainForm");
                MessageBox.Show(
                    $"Error loading application:\n\n{ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }
        
        private void ApplyInitialRoundedCorners()
        {
            // Apply rounded corners after form is loaded and theme is ready
            if (IsHandleCreated && FormBorderStyle == FormBorderStyle.None)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                if (theme?.Window?.CornerRadius > 0)
                {
                    ThemeApplicator.ApplyRoundedCorners(this, theme.Window.CornerRadius);
                }
            }
        }

        // Status bar dragging for window movement
        private void StatusStrip_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDraggingStatusBar = true;
                _statusBarDragStart = e.Location;
                // Capture mouse to receive events even when cursor leaves window
                _statusStrip.Capture = true;
            }
        }

        private void StatusStrip_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDraggingStatusBar)
            {
                Point delta = new Point(e.Location.X - _statusBarDragStart.X, e.Location.Y - _statusBarDragStart.Y);
                Location = new Point(Location.X + delta.X, Location.Y + delta.Y);
            }
        }

        private void StatusStrip_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDraggingStatusBar = false;
                // Release mouse capture
                _statusStrip.Capture = false;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainForm_Load(this, e);
            
            // Handle StartMinimized setting
            if (_settings.StartMinimized)
            {
                WindowState = FormWindowState.Minimized;
                // If MinimizeToTray is also enabled, hide the window
                if (_settings.MinimizeToTray)
                {
                    Hide();
                }
            }
        }

        private void RegisterHotkeys()
        {
            foreach (var hotkey in _settings.Hotkeys)
            {
                Action action = hotkey.Action switch
                {
                    "Show/Hide Window" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate { ToggleWindow(); }),
                    "New Prompt" => () => this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                        if (_webView?.CoreWebView2 != null)
                        {
                            _ = _webView.CoreWebView2.ExecuteScriptAsync(@"
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
                        }
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
                Logger.Warning("Command Palette or WebView not ready");
                NotificationManager.ShowToast("Command Palette or WebView not ready", 2000);
                return;
            }

            try
            {
                Logger.Debug("Showing command palette");
                
                // Retry logic with delay to ensure web app is fully initialized
                List<PromptInfo> prompts = new List<PromptInfo>();
                int maxRetries = 3;
                int retryDelayMs = 500;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    prompts = await _apiManager.GetPromptsAsync();
                    Logger.Debug("Attempt {Attempt}/{MaxRetries}: Fetched {Count} prompts", attempt, maxRetries, prompts.Count);

                    if (prompts.Count > 0)
                    {
                        break; // Success, exit retry loop
                    }

                    if (attempt < maxRetries)
                    {
                        Logger.Debug("No prompts yet, waiting {Delay}ms before retry", retryDelayMs);
                        await Task.Delay(retryDelayMs);
                        retryDelayMs *= 2; // Exponential backoff: 500ms, 1000ms, 2000ms
                    }
                }

                if (prompts.Count == 0)
                {
                    Logger.Warning("No prompts found after {MaxRetries} attempts", maxRetries);
                    NotificationManager.ShowToast("No prompts found! Try creating a prompt in the web app first.", 5000);
                    return;
                }

                // Store the currently focused window before showing the palette
                _windowService?.RefreshLastFocus();

                _commandPalette.ShowPalette(prompts);
                Logger.Information("Command palette shown with {Count} prompts", prompts.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error showing command palette");
                MessageBox.Show($"Failed to load prompts:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void CommandPalette_ActionSelected(object? sender, PromptActionEventArgs e)
        {
            if (_webView?.CoreWebView2 == null)
                return;

            try
            {
                // Handle LLM execution for prompts without placeholders (execute_llm=true)
                if ((e.Action.Type == PromptActionType.Paste || e.Action.Type == PromptActionType.Copy) && e.Prompt.ExecuteLLM)
                {
                    NotificationManager.ShowToast("Executing through LLM...", 2000);
                    
                    // Execute using web app API
                    var result = await _apiManager.ExecutePromptAsync(e.Prompt.Id, e.Prompt.Content);
                    
                    if (result.Success && result.Result != null)
                    {
                        // Copy/paste the result using IClipboardService
                        if (e.Action.Type == PromptActionType.Paste)
                        {
                            if (_clipboardService != null)
                                _clipboardService.SetText(result.Result);
                            else
                                Clipboard.SetText(result.Result); // Fallback
                                
                            await Task.Delay(300);
                            SendKeys.SendWait("^v");
                            NotificationManager.ShowToast("LLM result pasted!", 2000);
                        }
                        else
                        {
                            if (_clipboardService != null)
                                _clipboardService.SetText(result.Result);
                            else
                                Clipboard.SetText(result.Result); // Fallback
                                
                            NotificationManager.ShowToast("LLM result copied!", 2000);
                        }
                    }
                    else
                    {
                        NotificationManager.ShowToast($"LLM execution failed: {result.Error}", 3000);
                    }
                }
                // Handle opening in editor
                else if (e.Action.Type == PromptActionType.OpenInEditor)
                {
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
                }
                else
                {
                    Logger.Warning("Unexpected action type received: {ActionType}", e.Action.Type);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing action");
                NotificationManager.ShowToast($"Error: {ex.Message}", 3000);
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
            var workflowRegistry = ServiceConfiguration.GetService<IWorkflowRegistry>();
            _settingsForm = new SettingsForm(_settings, workflowRegistry);
            
            var result = _settingsForm.ShowDialog();
            
            if (result == DialogResult.OK)
            {
                _hotkeyManager.UnregisterAll();
                RegisterHotkeys();
                // MessageBox.Show("Settings saved. Hotkeys have been updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NotificationManager.ShowToast("Settings saved. Hotkeys have been updated.", 2000);
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
                // Only hide to tray if the setting is enabled
                if (_settings.MinimizeToTray)
                {
                    Hide();
                    _notifyIcon.ShowBalloonTip(1000, "PromptArq", "Application minimized to tray", ToolTipIcon.Info);
                }
            }
            else if (WindowState == FormWindowState.Normal || WindowState == FormWindowState.Maximized)
            {
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
                UpdateWebViewBounds();
                
                // Reapply rounded corners after resize
                ThemeManager.Instance.ApplyThemeToForm(this);
            }
        }

        private void UpdateWebViewBounds()
        {
            const int borderWidth = 8; // 8px border for resize area
            if (_webView != null && _statusStrip != null)
            {
                // Calculate available height for WebView and StatusStrip
                int statusBarHeight = _statusStrip.Visible ? 22 : 0; // Fixed height for status bar
                int availableHeight = ClientSize.Height - (borderWidth * 2);
                int webViewHeight = availableHeight - statusBarHeight;
                
                // Position WebView with border margin at top and sides
                _webView.Location = new Point(borderWidth, borderWidth);
                _webView.Size = new Size(
                    Math.Max(0, ClientSize.Width - (borderWidth * 2)),
                    Math.Max(0, webViewHeight)
                );
                
                // Position StatusStrip to fill full width at bottom (inside border)
                _statusStrip.Location = new Point(borderWidth, ClientSize.Height - borderWidth - statusBarHeight);
                _statusStrip.Size = new Size(
                    Math.Max(0, ClientSize.Width - (borderWidth * 2)),
                    statusBarHeight
                );
                
                // Force layout update
                _statusStrip.PerformLayout();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Logger.Information("MainForm closing");
            
            try
            {
                // Save settings
                _settings?.Save();
                
                // Dispose managers and components
                _hotkeyManager?.Dispose();
                _webViewManager?.Dispose();
                _commandPalette?.Dispose();
                _notifyIcon?.Dispose();
                
                // Stop all servers through unified manager
                UnifiedServerManager.Stop();
                
                Logger.Information("MainForm closing cleanup complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during MainForm closing");
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Let hotkey manager process first
            if (_hotkeyManager?.ProcessHotkey(m) ?? false)
                return;
            
            // Let base class handle window chrome
            base.WndProc(ref m);
        }

        private void PasteToActiveWindow(string text)
        {
            try
            {
                if (_clipboardService != null)
                    _clipboardService.SetText(text);
                else
                    Clipboard.SetText(text); // Fallback
                this.WindowState = FormWindowState.Minimized;
                System.Threading.Thread.Sleep(300);
                SendKeys.SendWait("^v");
                MessageBox.Show("Text pasted to active window!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error pasting to active window");
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
