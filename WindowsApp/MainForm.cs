using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace PromptArqApp
{
    public class MainForm : Form
    {
        private WebView2 _webView = null!;
        private MenuStrip _menuStrip = null!;
        private ToolStripMenuItem _fileMenu = null!;
        private ToolStripMenuItem _viewMenu = null!;
        private ToolStripMenuItem _helpMenu = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private NotifyIcon _notifyIcon = null!;
        
        private AppSettings _settings = null!;
        private HotkeyManager _hotkeyManager = null!;
        private Process? _viteProcess;
        private const int VitePort = 5173;
        private bool _isViteReady = false;

        public MainForm()
        {
            _settings = AppSettings.Load();
            if (_settings.Hotkeys.Count == 0)
            {
                _settings.SetDefaultHotkeys();
                _settings.Save();
            }

            InitializeComponent();
            _hotkeyManager = new HotkeyManager(Handle);
            RegisterHotkeys();
            StartViteServer();
        }

        private void InitializeComponent()
        {
            Text = "PromptArq";
            Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;

            // Create menu strip
            _menuStrip = new MenuStrip();

            // File menu
            _fileMenu = new ToolStripMenuItem("&File");
            _fileMenu.DropDownItems.Add("&Settings", null, (s, e) => ShowSettings());
            _fileMenu.DropDownItems.Add(new ToolStripSeparator());
            _fileMenu.DropDownItems.Add("E&xit", null, (s, e) => Close());

            // View menu
            _viewMenu = new ToolStripMenuItem("&View");
            _viewMenu.DropDownItems.Add("&Refresh", null, (s, e) => _webView?.Reload());
            _viewMenu.DropDownItems.Add("&Developer Tools", null, (s, e) => _webView?.CoreWebView2?.OpenDevToolsWindow());
            _viewMenu.DropDownItems.Add(new ToolStripSeparator());
            _viewMenu.DropDownItems.Add("&Toggle Fullscreen", null, (s, e) => ToggleFullscreen());

            // Help menu
            _helpMenu = new ToolStripMenuItem("&Help");
            _helpMenu.DropDownItems.Add("&About", null, (s, e) => ShowAbout());

            _menuStrip.Items.Add(_fileMenu);
            _menuStrip.Items.Add(_viewMenu);
            _menuStrip.Items.Add(_helpMenu);

            // Status strip
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Initializing...");
            _statusStrip.Items.Add(_statusLabel);

            // System tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "PromptArq",
                Visible = true
            };
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
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
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;

            // Events
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
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
            // Wait for Vite server to be ready (max 30 seconds)
            for (int i = 0; i < 60; i++)
            {
                if (_isViteReady)
                {
                    _webView.Source = new Uri($"http://localhost:{VitePort}");
                    _statusLabel.Text = "Connected to Vite server";
                    return;
                }
                await Task.Delay(500);
            }
            _statusLabel.Text = "Vite server did not start in time";
        }

        private void StartViteServer()
        {
            Task.Run(() =>
            {
                try
                {
                    // Get the project root directory (parent of WindowsApp)
                    string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", ".."));
                    
                    _viteProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "npm",
                            Arguments = "run dev",
                            WorkingDirectory = projectRoot,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    _viteProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.WriteLine($"Vite: {e.Data}");
                            if (e.Data.Contains("Local:") || e.Data.Contains($"localhost:{VitePort}"))
                            {
                                _isViteReady = true;
                                this.Invoke((MethodInvoker)delegate {
                                    _statusLabel.Text = "Vite server is running";
                                });
                            }
                        }
                    };

                    _viteProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Debug.WriteLine($"Vite Error: {e.Data}");
                        }
                    };

                    _viteProcess.Start();
                    _viteProcess.BeginOutputReadLine();
                    _viteProcess.BeginErrorReadLine();

                    this.Invoke((MethodInvoker)delegate {
                        _statusLabel.Text = "Starting Vite server...";
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate {
                        _statusLabel.Text = $"Failed to start Vite: {ex.Message}";
                        MessageBox.Show(
                            $"Failed to start Vite development server:\n{ex.Message}\n\nMake sure Node.js and npm are installed.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    });
                }
            });
        }

        private void StopViteServer()
        {
            if (_viteProcess != null && !_viteProcess.HasExited)
            {
                try
                {
                    _viteProcess.Kill();
                    _viteProcess.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping Vite: {ex.Message}");
                }
            }
        }

        private void RegisterHotkeys()
        {
            foreach (var hotkey in _settings.Hotkeys)
            {
                Action action = hotkey.Action switch
                {
                    "Show/Hide Window" => () => this.Invoke((MethodInvoker)delegate { ToggleWindow(); }),
                    "New Prompt" => () => this.Invoke((MethodInvoker)delegate { ExecuteJavaScript("document.querySelector('[data-action=\"new-prompt\"]')?.click()"); }),
                    "Settings" => () => this.Invoke((MethodInvoker)delegate { ShowSettings(); }),
                    _ => () => { }
                };

                _hotkeyManager.RegisterHotkey(hotkey, action);
            }
        }

        private void ExecuteJavaScript(string script)
        {
            if (_webView?.CoreWebView2 != null)
            {
                _ = _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private void ShowSettings()
        {
            using var settingsForm = new SettingsForm(_settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                _hotkeyManager.UnregisterAll();
                RegisterHotkeys();
                MessageBox.Show("Settings saved. Hotkeys have been updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "PromptArq Windows Application\n\n" +
                "A desktop wrapper for the PromptArq web application.\n\n" +
                "Built with C# and WebView2\n" +
                "Vite app integration with global hotkeys",
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

        private void ToggleFullscreen()
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
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
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "Do you want to minimize to tray instead of closing?",
                    "Close PromptArq",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    e.Cancel = true;
                    HideWindow();
                    return;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _settings.Save();
            _hotkeyManager?.Dispose();
            StopViteServer();
            _notifyIcon?.Dispose();
        }

        protected override void WndProc(ref Message m)
        {
            if (!_hotkeyManager.ProcessHotkey(m))
            {
                base.WndProc(ref m);
            }
        }
    }
}
