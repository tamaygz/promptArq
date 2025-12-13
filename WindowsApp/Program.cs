using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PromptArqApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
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
            finally
            {
                // Final cleanup - this runs even if debugger stops
                Debug.WriteLine("[Program] Application exiting, performing final cleanup");
                UnifiedServerManager.Stop();
            }
        }
        
        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            Debug.WriteLine("[Program] ApplicationExit event triggered");
            UnifiedServerManager.Stop();
        }
        
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            Debug.WriteLine("[Program] ProcessExit event triggered");
            UnifiedServerManager.Stop();
        }
        
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("[Program] UnhandledException event triggered");
            UnifiedServerManager.Stop();
        }
    }
}
