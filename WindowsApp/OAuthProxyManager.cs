using System;
using System.Diagnostics;
using System.Management;

namespace PromptArqApp
{
    /// <summary>
    /// Manages the OAuth proxy server process lifecycle (server.js on port 3001).
    /// Ensures the Node.js Express server is properly terminated.
    /// </summary>
    public static class OAuthProxyManager
    {
        private static Process? _proxyProcess;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers an OAuth proxy process for tracking and cleanup.
        /// </summary>
        public static void RegisterProcess(Process process)
        {
            lock (_lock)
            {
                // Clean up any existing process first
                CleanupProcess();
                
                _proxyProcess = process;
                Debug.WriteLine($"[OAuthProxy] Registered OAuth proxy server with PID {process.Id}");
            }
        }

        /// <summary>
        /// Forcefully terminates the OAuth proxy server and all its child processes.
        /// This method is safe to call multiple times and from different threads.
        /// </summary>
        public static void CleanupProcess()
        {
            lock (_lock)
            {
                if (_proxyProcess == null)
                {
                    Debug.WriteLine("[OAuthProxy] No process to cleanup");
                    return;
                }

                try
                {
                    if (_proxyProcess.HasExited)
                    {
                        Debug.WriteLine($"[OAuthProxy] Process {_proxyProcess.Id} already exited");
                        _proxyProcess = null;
                        return;
                    }

                    int pid = _proxyProcess.Id;
                    Debug.WriteLine($"[OAuthProxy] Stopping OAuth proxy server (PID {pid})");

                    // Method 1: Use taskkill to terminate the entire process tree
                    KillProcessTree(pid);

                    // Method 2: Wait for graceful exit
                    if (!_proxyProcess.WaitForExit(2000))
                    {
                        Debug.WriteLine($"[OAuthProxy] Process {pid} did not exit gracefully, forcing kill");
                        
                        // Method 3: Force kill if still running
                        try
                        {
                            _proxyProcess.Kill(entireProcessTree: true);
                            _proxyProcess.WaitForExit(1000);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[OAuthProxy] Error force killing: {ex.Message}");
                        }
                    }

                    Debug.WriteLine($"[OAuthProxy] Successfully stopped process {pid}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OAuthProxy] Error during cleanup: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        _proxyProcess?.Dispose();
                    }
                    catch { }
                    
                    _proxyProcess = null;
                }

                // Additional cleanup: Kill any orphaned node.exe processes running server.js
                KillOrphanedProxyProcesses();
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
                    Debug.WriteLine($"[OAuthProxy] taskkill output: {output}");
                if (!string.IsNullOrEmpty(error))
                    Debug.WriteLine($"[OAuthProxy] taskkill error: {error}");

                Debug.WriteLine($"[OAuthProxy] taskkill completed with exit code {killProcess.ExitCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OAuthProxy] Error using taskkill: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds and kills any orphaned Node.js processes running server.js.
        /// This is a safety net in case the main process cleanup fails.
        /// </summary>
        private static void KillOrphanedProxyProcesses()
        {
            try
            {
                Debug.WriteLine("[OAuthProxy] Checking for orphaned OAuth proxy processes");
                int killedCount = 0;

                var nodeProcesses = Process.GetProcessesByName("node");
                foreach (var proc in nodeProcesses)
                {
                    try
                    {
                        string cmdLine = GetCommandLine(proc);
                        
                        if (!string.IsNullOrEmpty(cmdLine) && 
                            (cmdLine.Contains("server.js", StringComparison.OrdinalIgnoreCase) || 
                             cmdLine.Contains("oauth", StringComparison.OrdinalIgnoreCase)))
                        {
                            Debug.WriteLine($"[OAuthProxy] Found orphaned OAuth proxy process {proc.Id}: {cmdLine}");
                            proc.Kill(true);
                            killedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[OAuthProxy] Error checking process {proc.Id}: {ex.Message}");
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                if (killedCount > 0)
                {
                    Debug.WriteLine($"[OAuthProxy] Killed {killedCount} orphaned OAuth proxy process(es)");
                }
                else
                {
                    Debug.WriteLine("[OAuthProxy] No orphaned OAuth proxy processes found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OAuthProxy] Error checking for orphaned processes: {ex.Message}");
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
                Debug.WriteLine($"[OAuthProxy] Could not get command line for PID {process.Id}: {ex.Message}");
            }
            
            return string.Empty;
        }
    }
}
