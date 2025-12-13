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
        private PromptHistory _history = null!;
        private HotkeyManager _hotkeyManager = null!;
        private CommandPaletteForm? _commandPalette;
        private SettingsForm? _settingsForm;
        private const int VitePort = 5000;

        // Component managers
        private WebView2Manager _webViewManager = null!;
        private WindowsAppAPIBridge _apiManager = null!;

        public MainForm()
        {
            _settings = AppSettings.Load();
            if (_settings.Hotkeys.Count == 0)
            {
                _settings.SetDefaultHotkeys();
                _settings.Save();
            }

            _history = PromptHistory.Load();

            InitializeComponent();
            InitializeCustomComponents();
            _hotkeyManager = new HotkeyManager(Handle);
            RegisterHotkeys();

            // Start all servers through unified manager
            UnifiedServerManager.Start();

            // Initialize command palette with history and settings
            _commandPalette = new CommandPaletteForm(_history, _settings);
            _commandPalette.ActionSelected += CommandPalette_ActionSelected;
            
            // Delegates will be wired up in MainForm_Load after component managers are initialized
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

            // Layout
            Controls.Add(_webView);
            Controls.Add(_statusStrip);

            // Events
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;

            // Apply dark title bar when handle is created
            HandleCreated += (s, e) => WindowStyleManager.ApplyDarkTitleBar(this, captionColor: 0x00663300, borderColor: 0x00663300);
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            WindowStyleManager.ApplyDarkTitleBar(this, captionColor: 0x00663300, borderColor: 0x00663300);
            
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
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainForm_Load(this, e);
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
                MessageBox.Show("Command Palette or WebView not ready", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Retry logic with delay to ensure web app is fully initialized
                List<PromptInfo> prompts = new List<PromptInfo>();
                int maxRetries = 3;
                int retryDelayMs = 500;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    prompts = await _apiManager.GetPromptsAsync();
                    Debug.WriteLine($"[ShowCommandPalette] Attempt {attempt}/{maxRetries}: Fetched {prompts.Count} prompts");

                    if (prompts.Count > 0)
                    {
                        break; // Success, exit retry loop
                    }

                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[ShowCommandPalette] No prompts yet, waiting {retryDelayMs}ms before retry...");
                        await Task.Delay(retryDelayMs);
                        retryDelayMs *= 2; // Exponential backoff: 500ms, 1000ms, 2000ms
                    }
                }

                if (prompts.Count == 0)
                {
                    MessageBox.Show(
                        "No prompts found!\n\n" +
                        "This could mean:\n" +
                        "1. You haven't created any prompts yet\n" +
                        "2. The web app is still loading (wait a moment and try again)\n" +
                        "3. localStorage is not accessible\n\n" +
                        "Try creating a prompt in the web app first.",
                        "No Prompts",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                _commandPalette.ShowPalette(prompts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShowCommandPalette] Error showing command palette: {ex.Message}");
                Debug.WriteLine($"[ShowCommandPalette] Stack trace: {ex.StackTrace}");
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
                        // Copy/paste the result
                        if (e.Action.Type == PromptActionType.Paste)
                        {
                            Clipboard.SetText(result.Result);
                            await Task.Delay(300);
                            SendKeys.SendWait("^v");
                            NotificationManager.ShowToast("LLM result pasted!", 2000);
                        }
                        else
                        {
                            Clipboard.SetText(result.Result);
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
                    Debug.WriteLine($"Warning: Action '{e.Action.Type}' received but should be handled by CommandPaletteForm");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing action: {ex.Message}");
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
