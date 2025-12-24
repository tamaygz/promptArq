using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Serilog;

namespace PromptArqApp
{
    /// <summary>
    /// Centralized manager for all server processes and components.
    /// Manages Vite dev server (port 5000), OAuth proxy (port 3001), and LocalStorage server (port 5001).
    /// Provides robust startup and shutdown with multiple cleanup strategies.
    /// </summary>
    public static class UnifiedServerManager
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext("UnifiedServerManager");
        private static readonly object _lock = new object();
        private static bool _isStarted = false;
        private static bool _isShuttingDown = false;
        
        // Server components
        private static Process? _viteDevProcess = null;
        private static LocalStorageServer? _storageServer = null;
        
        // Port registry for cleanup
        private static readonly int[] ManagedPorts = { 5000, 3001, 5001 };
        
        /// <summary>
        /// Gets whether the servers are currently running.
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isStarted && !_isShuttingDown;
                }
            }
        }
        
        /// <summary>
        /// Starts all server components in the correct order.
        /// Safe to call multiple times - will only start once.
        /// </summary>
        public static void Start()
        {
            lock (_lock)
            {
                if (_isStarted)
                {
                    Logger.Information("Servers already started");
                    return;
                }
                
                if (_isShuttingDown)
                {
                    Logger.Warning("Cannot start servers during shutdown");
                    return;
                }
                
                try
                {
                    Logger.Information("Starting all servers");
                    
                    // 1. Start LocalStorage server (in-process)
                    StartStorageServer();
                    
                    // 2. Start Vite dev server (includes OAuth proxy via concurrently)
                    StartViteDevServer();
                    
                    _isStarted = true;
                    Logger.Information("All servers started successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error starting servers");
                    // Attempt cleanup if partial start
                    Stop();
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Stops all server components using multiple strategies for reliability.
        /// Safe to call multiple times - idempotent operation.
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                if (_isShuttingDown)
                {
                    Logger.Information("Already shutting down");
                    return;
                }
                
                _isShuttingDown = true;
                
                try
                {
                    Logger.Information("======================================== STOPPING ALL SERVERS ========================================");
                    
                    // Strategy 1: Graceful shutdown
                    StopStorageServerGracefully();
                    StopViteDevProcessGracefully();
                    
                    // Strategy 2: Force kill process trees
                    KillAllProcessTrees();
                    
                    // Strategy 3: Kill by command line detection
                    KillNodeProcessesByCommandLine();
                    
                    // Strategy 4: Kill by port (nuclear option)
                    KillProcessesByPort();
                    
                    // Strategy 5: Verify cleanup
                    VerifyPortsReleased();
                    
                    Logger.Information("======================================== SHUTDOWN COMPLETE ========================================");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error during shutdown");
                }
                finally
                {
                    _isStarted = false;
                    _isShuttingDown = false;
                }
            }
        }
        
        #region Startup Methods
        
        private static void StartStorageServer()
        {
            try
            {
                Logger.Information("Starting LocalStorage server on port 5001");
                _storageServer = new LocalStorageServer();
                _storageServer.Start();
                Logger.Information("LocalStorage server started");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start storage server");
                throw;
            }
        }
        
        private static void StartViteDevServer()
        {
            try
            {
                string projectRoot = FindProjectRoot();
                if (string.IsNullOrEmpty(projectRoot))
                {
                    throw new InvalidOperationException("Could not find project root directory with package.json");
                }
                
                Logger.Information("Starting Vite dev server from: {ProjectRoot}", projectRoot);
                
                _viteDevProcess = new Process
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
                
                // Setup output handlers for monitoring
                _viteDevProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Debug("[Vite] {Output}", e.Data);
                    }
                };
                
                _viteDevProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Logger.Warning("[Vite Error] {Error}", e.Data);
                    }
                };
                
                _viteDevProcess.Start();
                _viteDevProcess.BeginOutputReadLine();
                _viteDevProcess.BeginErrorReadLine();
                
                Logger.Information("Vite dev process started with PID {ProcessId}", _viteDevProcess.Id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start Vite dev server");
                throw;
            }
        }
        
        private static string FindProjectRoot()
        {
            string currentDir = Application.StartupPath;
            
            for (int i = 0; i < 10; i++)
            {
                string parentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));
                if (parentDir == currentDir) break;
                
                string packageJsonPath = Path.Combine(parentDir, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    string content = File.ReadAllText(packageJsonPath);
                    if (content.Contains("vite") && content.Contains("\"dev\""))
                    {
                        return parentDir;
                    }
                }
                
                currentDir = parentDir;
            }
            
            // Fallback
            return Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", ".."));
        }
        
        #endregion
        
        #region Shutdown Strategy 1: Graceful
        
        private static void StopStorageServerGracefully()
        {
            if (_storageServer == null)
            {
                Logger.Debug("No storage server to stop");
                return;
            }
            
            try
            {
                Logger.Information("Stopping storage server gracefully");
                _storageServer.Stop();
                _storageServer.Dispose();
                _storageServer = null;
                Logger.Information("Storage server stopped gracefully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error stopping storage server");
            }
        }
        
        private static void StopViteDevProcessGracefully()
        {
            if (_viteDevProcess == null)
            {
                Logger.Debug("No Vite dev process to stop");
                return;
            }
            
            try
            {
                if (_viteDevProcess.HasExited)
                {
                    Logger.Information("Vite dev process {ProcessId} already exited", _viteDevProcess.Id);
                    _viteDevProcess.Dispose();
                    _viteDevProcess = null;
                    return;
                }
                
                Logger.Information("Attempting graceful stop of Vite dev process {ProcessId}", _viteDevProcess.Id);
                
                // Try to close gracefully first
                _viteDevProcess.CancelOutputRead();
                _viteDevProcess.CancelErrorRead();
                
                if (!_viteDevProcess.WaitForExit(2000))
                {
                    Logger.Warning("Graceful stop timed out, will force kill");
                }
                else
                {
                    Logger.Information("Vite dev process stopped gracefully");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during graceful stop");
            }
            finally
            {
                try
                {
                    _viteDevProcess?.Dispose();
                }
                catch { }
                _viteDevProcess = null;
            }
        }
        
        #endregion
        
        #region Shutdown Strategy 2: Force Kill Process Trees
        
        private static void KillAllProcessTrees()
        {
            Logger.Information("Force killing process trees");
            
            // Kill Vite dev process tree if we have the PID
            if (_viteDevProcess != null)
            {
                try
                {
                    if (!_viteDevProcess.HasExited)
                    {
                        int pid = _viteDevProcess.Id;
                        Logger.Information("Killing process tree for PID {ProcessId}", pid);
                        KillProcessTree(pid);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error killing Vite process tree");
                }
            }
        }
        
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
                string output = killProcess.StandardOutput.ReadToEnd();
                string error = killProcess.StandardError.ReadToEnd();
                killProcess.WaitForExit(3000);
                
                if (!string.IsNullOrEmpty(output))
                    Logger.Debug("taskkill output: {Output}", output.Trim());
                if (!string.IsNullOrEmpty(error))
                    Logger.Warning("taskkill error: {Error}", error.Trim());
                
                Logger.Debug("taskkill exit code: {ExitCode}", killProcess.ExitCode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error using taskkill");
            }
        }
        
        #endregion
        
        #region Shutdown Strategy 3: Kill by Command Line
        
        private static void KillNodeProcessesByCommandLine()
        {
            Logger.Information("Killing Node.js processes by command line detection");
            int killedCount = 0;
            
            try
            {
                var nodeProcesses = Process.GetProcessesByName("node");
                Logger.Debug("Found {Count} node.exe processes", nodeProcesses.Length);
                
                foreach (var proc in nodeProcesses)
                {
                    try
                    {
                        string cmdLine = GetCommandLine(proc);
                        
                        if (!string.IsNullOrEmpty(cmdLine))
                        {
                            // Check if this is one of our managed processes
                            bool isManaged = cmdLine.Contains("vite", StringComparison.OrdinalIgnoreCase) ||
                                           cmdLine.Contains("server.js", StringComparison.OrdinalIgnoreCase) ||
                                           cmdLine.Contains("npm run dev", StringComparison.OrdinalIgnoreCase) ||
                                           cmdLine.Contains("concurrently", StringComparison.OrdinalIgnoreCase);
                            
                            if (isManaged)
                            {
                                Logger.Information("Killing managed node process {ProcessId}: {CommandLine}", proc.Id, cmdLine);
                                proc.Kill(entireProcessTree: true);
                                proc.WaitForExit(1000);
                                killedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error checking/killing process {ProcessId}", proc.Id);
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
                
                Logger.Information("Killed {Count} Node.js process(es) by command line", killedCount);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in command line cleanup");
            }
        }
        
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
                Logger.Debug(ex, "Could not get command line for PID {ProcessId}", process.Id);
            }
            
            return string.Empty;
        }
        
        #endregion
        
        #region Shutdown Strategy 4: Kill by Port
        
        private static void KillProcessesByPort()
        {
            Logger.Information("Killing processes by port (nuclear option)");
            
            foreach (int port in ManagedPorts)
            {
                try
                {
                    var pids = GetProcessIdsByPort(port);
                    if (pids.Count > 0)
                    {
                        Logger.Information("Found {Count} process(es) on port {Port}: {ProcessIds}",
                            pids.Count, port, string.Join(", ", pids));
                        
                        foreach (int pid in pids)
                        {
                            KillProcessTree(pid);
                        }
                    }
                    else
                    {
                        Logger.Debug("No processes found on port {Port}", port);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error killing processes on port {Port}", port);
                }
            }
        }
        
        private static List<int> GetProcessIdsByPort(int port)
        {
            var pids = new List<int>();
            
            try
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var connections = properties.GetActiveTcpConnections();
                var listeners = properties.GetActiveTcpListeners();
                
                // Check active connections
                foreach (var conn in connections)
                {
                    if (conn.LocalEndPoint.Port == port)
                    {
                        try
                        {
                            using var searcher = new ManagementObjectSearcher(
                                $"SELECT ProcessId FROM Win32_Process WHERE ProcessId > 0");
                            
                            // Use netstat to get PID for this connection
                            var netstatPid = GetPidFromNetstat(port);
                            if (netstatPid > 0 && !pids.Contains(netstatPid))
                            {
                                pids.Add(netstatPid);
                            }
                        }
                        catch { }
                    }
                }
                
                // Check listeners
                foreach (var listener in listeners)
                {
                    if (listener.Port == port)
                    {
                        var netstatPid = GetPidFromNetstat(port);
                        if (netstatPid > 0 && !pids.Contains(netstatPid))
                        {
                            pids.Add(netstatPid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting PIDs for port {Port}", port);
            }
            
            return pids;
        }
        
        private static int GetPidFromNetstat(int port)
        {
            try
            {
                using var netstatProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                netstatProcess.Start();
                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();
                
                // Parse netstat output for the port
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains($":{port} ") && (line.Contains("LISTENING") || line.Contains("ESTABLISHED")))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            var lastPart = parts[parts.Length - 1];
                            if (int.TryParse(lastPart, out int pid))
                            {
                                return pid;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error running netstat");
            }
            
            return 0;
        }
        
        #endregion
        
        #region Shutdown Strategy 5: Verification
        
        private static void VerifyPortsReleased()
        {
            Logger.Information("Verifying ports are released");
            
            // Wait a bit for OS to release ports
            Thread.Sleep(500);
            
            try
            {
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var listeners = properties.GetActiveTcpListeners();
                
                foreach (int port in ManagedPorts)
                {
                    bool isInUse = listeners.Any(l => l.Port == port);
                    
                    if (isInUse)
                    {
                        Logger.Warning("Port {Port} is still in use", port);
                    }
                    else
                    {
                        Logger.Information("Port {Port} successfully released", port);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error verifying ports");
            }
        }
        
        #endregion
    }
}
