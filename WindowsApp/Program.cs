using System;
using System.Diagnostics;
using System.Windows.Forms;
using Serilog;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize logging first
            LoggerConfig.Initialize();
            Log.Information("PromptArq application starting");

            // Configure dependency injection
            ServiceConfiguration.Configure();
            Log.Information("Service container configured");

            // Ensure cleanup on ALL possible exit paths
            Application.ApplicationExit += OnApplicationExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            try
            {
                // Initialize ThemeManager before creating forms
                ThemeManager.Initialize();
                Log.Information("ThemeManager initialized");

                // Load settings to get the saved theme
                var settings = AppSettings.Load();
                
                // Load the saved theme (or default if not found)
                if (!string.IsNullOrWhiteSpace(settings.CurrentTheme))
                {
                    bool themeLoaded = ThemeManager.Instance.LoadTheme(settings.CurrentTheme);
                    if (!themeLoaded)
                    {
                        Log.Warning("Failed to load saved theme '{ThemeName}', using default", settings.CurrentTheme);
                        // Try to load DarkBlue as fallback
                        ThemeManager.Instance.LoadTheme("DarkBlue");
                    }
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error in application main loop");
                MessageBox.Show(
                    $"A fatal error occurred:\n\n{ex.Message}\n\nPlease check the logs for details.",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // Final cleanup - this runs even if debugger stops
                Log.Information("Application exiting, performing final cleanup");
                ThemeManager.Instance?.Dispose();
                UnifiedServerManager.Stop();
                ServiceConfiguration.Dispose();
                LoggerConfig.CloseAndFlush();
            }
        }
        
        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            Log.Information("ApplicationExit event triggered");
            UnifiedServerManager.Stop();
        }
        
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Log.Information("ProcessExit event triggered");
            UnifiedServerManager.Stop();
            LoggerConfig.CloseAndFlush();
        }
        
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Unhandled exception occurred. IsTerminating: {IsTerminating}", e.IsTerminating);
            UnifiedServerManager.Stop();
            
            if (e.IsTerminating)
            {
                LoggerConfig.CloseAndFlush();
            }
        }
    }
}
