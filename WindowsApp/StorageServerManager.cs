using System;
using System.Diagnostics;

namespace PromptArqApp
{
    /// <summary>
    /// Manages the LocalStorageServer lifecycle.
    /// Ensures the HTTP server on port 5001 is properly terminated.
    /// </summary>
    public static class StorageServerManager
    {
        private static LocalStorageServer? _server;
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a storage server instance for tracking and cleanup.
        /// </summary>
        public static void RegisterServer(LocalStorageServer server)
        {
            lock (_lock)
            {
                // Clean up any existing server first
                CleanupServer();
                
                _server = server;
                Debug.WriteLine($"[StorageManager] Registered storage server on port 5001");
            }
        }

        /// <summary>
        /// Stops the storage server and releases resources.
        /// This method is safe to call multiple times and from different threads.
        /// </summary>
        public static void CleanupServer()
        {
            lock (_lock)
            {
                if (_server == null)
                {
                    Debug.WriteLine("[StorageManager] No server to cleanup");
                    return;
                }

                try
                {
                    Debug.WriteLine("[StorageManager] Stopping storage server");
                    _server.Stop();
                    _server.Dispose();
                    Debug.WriteLine("[StorageManager] Storage server stopped successfully");
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("[StorageManager] Server was already disposed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[StorageManager] Error during cleanup: {ex.Message}");
                }
                finally
                {
                    _server = null;
                }
            }
        }
    }
}
