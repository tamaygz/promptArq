using System;
using System.Diagnostics;
using System.Windows.Forms;
using Serilog;

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

            // Ensure cleanup on ALL possible exit paths
            Application.ApplicationExit += OnApplicationExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            try
            {
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
                UnifiedServerManager.Stop();
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
