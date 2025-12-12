using System;
using System.Diagnostics;
using System.Management;

namespace PromptArqApp
{
    /// <summary>
    /// Manages the Vite development server process lifecycle.
    /// Ensures the process is properly terminated even when the debugger stops.
    /// </summary>
    public static class ViteProcessManager
    {
        private static Process? _viteProcess;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a Vite process for tracking and cleanup.
        /// </summary>
        public static void RegisterProcess(Process process)
        {
            lock (_lock)
            {
                // Clean up any existing process first
                CleanupProcess();
                
                _viteProcess = process;
                Debug.WriteLine($"[ViteManager] Registered Vite process with PID {process.Id}");
            }
        }

        /// <summary>
        /// Forcefully terminates the Vite process and all its child processes.
        /// This method is safe to call multiple times and from different threads.
        /// </summary>
        public static void CleanupProcess()
        {
            lock (_lock)
            {
                if (_viteProcess == null)
                {
                    Debug.WriteLine("[ViteManager] No process to cleanup");
                    return;
                }

                try
                {
                    if (_viteProcess.HasExited)
                    {
                        Debug.WriteLine($"[ViteManager] Process {_viteProcess.Id} already exited");
                        _viteProcess = null;
                        return;
                    }

                    int pid = _viteProcess.Id;
                    Debug.WriteLine($"[ViteManager] Stopping Vite process {pid}");

                    // Method 1: Use taskkill to terminate the entire process tree
                    KillProcessTree(pid);

                    // Method 2: Wait for graceful exit
                    if (!_viteProcess.WaitForExit(2000))
                    {
                        Debug.WriteLine($"[ViteManager] Process {pid} did not exit gracefully, forcing kill");
                        
                        // Method 3: Force kill if still running
                        try
                        {
                            _viteProcess.Kill(entireProcessTree: true);
                            _viteProcess.WaitForExit(1000);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ViteManager] Error force killing: {ex.Message}");
                        }
                    }

                    Debug.WriteLine($"[ViteManager] Successfully stopped process {pid}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ViteManager] Error during cleanup: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        _viteProcess?.Dispose();
                    }
                    catch { }
                    
                    _viteProcess = null;
                }

                // Additional cleanup: Kill any orphaned node/npm processes running Vite
                KillOrphanedViteProcesses();
            }
        }

        /// <summary>
        /// Uses taskkill to terminate a process tree.
        /// </summary>
        private static void KillProcessTree(int pid)
        {
            try
            {
                using var killProcess = new Process
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
                
                // Read output for debugging
                string output = killProcess.StandardOutput.ReadToEnd();
                string error = killProcess.StandardError.ReadToEnd();
                
                killProcess.WaitForExit(3000);

                if (!string.IsNullOrEmpty(output))
                    Debug.WriteLine($"[ViteManager] taskkill output: {output}");
                if (!string.IsNullOrEmpty(error))
                    Debug.WriteLine($"[ViteManager] taskkill error: {error}");

                Debug.WriteLine($"[ViteManager] taskkill completed with exit code {killProcess.ExitCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViteManager] Error using taskkill: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds and kills any orphaned Node.js processes running Vite.
        /// This is a safety net in case the main process cleanup fails.
        /// </summary>
        private static void KillOrphanedViteProcesses()
        {
            try
            {
                Debug.WriteLine("[ViteManager] Checking for orphaned Vite processes");
                int killedCount = 0;

                var nodeProcesses = Process.GetProcessesByName("node");
                foreach (var proc in nodeProcesses)
                {
                    try
                    {
                        string cmdLine = GetCommandLine(proc);
                        
                        if (!string.IsNullOrEmpty(cmdLine) && 
                            (cmdLine.Contains("vite", StringComparison.OrdinalIgnoreCase) || 
                             cmdLine.Contains("npm run dev", StringComparison.OrdinalIgnoreCase)))
                        {
                            Debug.WriteLine($"[ViteManager] Found orphaned Vite process {proc.Id}: {cmdLine}");
                            proc.Kill(true);
                            killedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ViteManager] Error checking process {proc.Id}: {ex.Message}");
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                if (killedCount > 0)
                {
                    Debug.WriteLine($"[ViteManager] Killed {killedCount} orphaned Vite process(es)");
                }
                else
                {
                    Debug.WriteLine("[ViteManager] No orphaned Vite processes found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViteManager] Error checking for orphaned processes: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the command line of a process using WMI.
        /// </summary>
        private static string GetCommandLine(Process process)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViteManager] Could not get command line for PID {process.Id}: {ex.Message}");
            }
            
            return string.Empty;
        }
    }
}
