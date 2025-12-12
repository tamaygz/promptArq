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
        private const int VitePort = 5000;
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
            Console.WriteLine("Waiting for Vite server to be ready...");
            // Wait for Vite server to be ready (max 30 seconds)
            for (int i = 0; i < 60; i++)
            {
                if (_isViteReady)
                {
                    Console.WriteLine($"Vite is ready! Navigating to http://localhost:{VitePort}");
                    _webView.Source = new Uri($"http://localhost:{VitePort}");
                    _statusLabel.Text = "Connected to Vite server";
                    return;
                }
                
                // Try navigating anyway after 10 seconds - maybe server is ready but we missed the output
                if (i == 20) // 10 seconds
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

        private void StartViteServer()
        {
            Task.Run(() =>
            {
                try
                {
                    // Get the project root directory (parent of WindowsApp)
                    // Try to find it by looking for package.json
                    string projectRoot = FindProjectRoot();
                    if (string.IsNullOrEmpty(projectRoot))
                    {
                        this.Invoke((MethodInvoker)delegate {
                            _statusLabel.Text = "Error: Could not locate project root";
                            MessageBox.Show(
                                "Could not find the Vite project root directory.\nMake sure the WindowsApp is in the correct location relative to package.json.",
                                "Configuration Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        });
                        return;
                    }
                    
                    _viteProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c npm run dev",
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
                            Console.WriteLine($"Vite: {e.Data}");
                            Debug.WriteLine($"Vite: {e.Data}");
                            // Look for various indicators that Vite is ready
                            if (e.Data.Contains("Local:") || 
                                e.Data.Contains($"localhost:{VitePort}") ||
                                e.Data.Contains("ready in") ||
                                e.Data.Contains("http://"))
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
                            Console.WriteLine($"Vite Error: {e.Data}");
                            Debug.WriteLine($"Vite Error: {e.Data}");
                            // Show critical errors to user
                            if (e.Data.Contains("error") || e.Data.Contains("Error") || e.Data.Contains("ERROR"))
                            {
                                this.Invoke((MethodInvoker)delegate {
                                    _statusLabel.Text = $"Vite error: {e.Data}";
                                });
                            }
                        }
                    };

                    this.Invoke((MethodInvoker)delegate {
                        _statusLabel.Text = "Starting Vite server...";
                    });

                    Console.WriteLine($"Starting Vite from: {projectRoot}");
                    _viteProcess.Start();
                    _viteProcess.BeginOutputReadLine();
                    _viteProcess.BeginErrorReadLine();
                    
                    Console.WriteLine("Vite process started, waiting for output...");
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

        private string FindProjectRoot()
        {
            // Start from the application directory and search upward for package.json
            string currentDir = Application.StartupPath;
            
            for (int i = 0; i < 10; i++) // Limit search depth
            {
                string parentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));
                if (parentDir == currentDir) break; // Reached root
                
                string packageJsonPath = Path.Combine(parentDir, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    // Verify it has vite
                    string content = File.ReadAllText(packageJsonPath);
                    if (content.Contains("vite") && content.Contains("\"dev\""))
                    {
                        return parentDir;
                    }
                }
                
                currentDir = parentDir;
            }
            
            // Fallback to relative path if search fails
            return Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", ".."));
        }

        private void StopViteServer()
        {
            if (_viteProcess != null && !_viteProcess.HasExited)
            {
                try
                {
                    // Kill the entire process tree (cmd.exe -> npm -> node -> vite)
                    KillProcessAndChildren(_viteProcess.Id);
                    
                    // Give it a moment to shutdown gracefully
                    if (!_viteProcess.WaitForExit(3000))
                    {
                        // Force kill if still running
                        _viteProcess.Kill();
                        _viteProcess.WaitForExit(2000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping Vite: {ex.Message}");
                }
            }
        }

        private void KillProcessAndChildren(int pid)
        {
            try
            {
                // Use taskkill to kill the process tree
                // /F = Force termination
                // /T = Terminate all child processes
                var killProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {pid}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                killProcess.Start();
                killProcess.WaitForExit(5000);
                
                Debug.WriteLine($"Killed process tree for PID {pid}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing process tree: {ex.Message}");
            }
        }

        private void RegisterHotkeys()
        {
            foreach (var hotkey in _settings.Hotkeys)
            {
                Action action = hotkey.Action switch
                {
                    "Show/Hide Window" => () => this.Invoke((MethodInvoker)delegate { ToggleWindow(); }),
                    "New Prompt" => () => this.Invoke((MethodInvoker)delegate { 
                        // Try to click new prompt button - uses common selector patterns
                        ExecuteJavaScript(@"
                            const btn = document.querySelector('[data-action=""new-prompt""]') || 
                                       document.querySelector('button:contains(""New Prompt"")') ||
                                       document.querySelector('[aria-label*=""new""][aria-label*=""prompt""]');
                            if (btn) btn.click();
                        ");
                    }),
                    "Settings" => () => this.Invoke((MethodInvoker)delegate { ShowSettings(); }),
                    _ => () => { }
                };

                _hotkeyManager.RegisterHotkey(hotkey, action);
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
            // if (e.CloseReason == CloseReason.UserClosing)
            // {
            //     var result = MessageBox.Show(
            //         "Do you want to minimize to tray instead of closing?",
            //         "Close PromptArq",
            //         MessageBoxButtons.YesNoCancel,
            //         MessageBoxIcon.Question
            //     );

            //     if (result == DialogResult.Yes)
            //     {
            //         e.Cancel = true;
            //         HideWindow();
            //         return;
            //     }
            //     else if (result == DialogResult.Cancel)
            //     {
            //         e.Cancel = true;
            //         return;
            //     }
            // }

            _settings.Save();
            _hotkeyManager?.Dispose();
            StopViteServer();
            _notifyIcon?.Dispose();
            
            // Ensure the Vite process is cleaned up
            _viteProcess?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Ensure Vite server is stopped when form is disposed
                StopViteServer();
                _viteProcess?.Dispose();
                _hotkeyManager?.Dispose();
                _notifyIcon?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (!_hotkeyManager?.ProcessHotkey(m) ?? true)
            {
                base.WndProc(ref m);
            }
        }
    }
}
