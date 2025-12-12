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
            // Ensure cleanup on application exit
            Application.ApplicationExit += OnApplicationExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
        
        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            // Kill any remaining npm/vite processes that might be orphaned
            KillOrphanedViteProcesses();
        }
        
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            // Final cleanup on process exit
            KillOrphanedViteProcesses();
        }
        
        private static void KillOrphanedViteProcesses()
        {
            try
            {
                // Kill any node processes running vite on port 5000
                var processes = Process.GetProcessesByName("node");
                foreach (var proc in processes)
                {
                    try
                    {
                        // Check if this is a vite process by looking at command line
                        var cmdLine = GetCommandLine(proc);
                        if (!string.IsNullOrEmpty(cmdLine) && 
                            (cmdLine.Contains("vite") || cmdLine.Contains("npm run dev")))
                        {
                            proc.Kill();
                            Debug.WriteLine($"Killed orphaned Vite process: {proc.Id}");
                        }
                    }
                    catch { /* Ignore errors for individual processes */ }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up orphaned processes: {ex.Message}");
            }
        }
        
        private static string GetCommandLine(Process process)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                using var objects = searcher.Get();
                foreach (var obj in objects)
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch { /* WMI might not be available or accessible */ }
            return string.Empty;
        }
    }
}
