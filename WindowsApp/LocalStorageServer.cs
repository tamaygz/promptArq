using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog;

namespace PromptArqApp
{
    /// <summary>
    /// Simple HTTP server that provides a REST API for storage operations
    /// Both WebView2 and external browsers can connect to this
    /// </summary>
    public class LocalStorageServer : IDisposable
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<LocalStorageServer>();
        
        private readonly HttpListener _listener;
        private readonly string _dbPath;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private const int Port = 5001; // Different from Vite (5000)
        private bool _disposed = false;
        private readonly object _disposeLock = new object();

        public LocalStorageServer()
        {
            try
            {
                _dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PromptArq",
                    "promptarq.db"
                );

                Logger.Information("Initializing LocalStorageServer with database at {DbPath}", _dbPath);

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(_dbPath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("Could not determine database directory");
                }

                if (!Directory.Exists(directory))
                {
                    Logger.Information("Creating database directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Initialize database
                InitializeDatabase();

                // Setup HTTP listener
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _cancellationTokenSource = new CancellationTokenSource();

                Logger.Information("LocalStorageServer initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize LocalStorageServer");
                throw;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                Logger.Debug("Initializing database at {DbPath}", _dbPath);

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS kv_store (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        updated_at INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                ";
                command.ExecuteNonQuery();

                Logger.Information("SQLite database initialized successfully");
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "SQLite error initializing database at {DbPath}", _dbPath);
                throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error initializing database at {DbPath}", _dbPath);
                throw;
            }
        }

        public void Start()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    Logger.Error("Attempt to start disposed LocalStorageServer");
                    throw new ObjectDisposedException(nameof(LocalStorageServer));
                }

                try
                {
                    Logger.Information("Starting LocalStorageServer on port {Port}", Port);
                    
                    _listener.Start();
                    _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource!.Token));
                    
                    Logger.Information("LocalStorageServer started successfully on http://localhost:{Port}/", Port);
                }
                catch (HttpListenerException ex)
                {
                    Logger.Error(ex, "Failed to start HTTP listener on port {Port}. Port may already be in use.", Port);
                    throw new InvalidOperationException($"Failed to start server on port {Port}: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected error starting LocalStorageServer");
                    throw;
                }
            }
        }

        public void Stop()
        {
            lock (_disposeLock)
            {
                if (_disposed || _cancellationTokenSource == null)
                {
                    Logger.Debug("LocalStorageServer already stopped or disposed");
                    return;
                }

                try
                {
                    Logger.Information("Stopping LocalStorageServer");
                    
                    _cancellationTokenSource?.Cancel();
                    _listener.Stop();
                    
                    if (_listenerTask != null && !_listenerTask.Wait(TimeSpan.FromSeconds(2)))
                    {
                        Logger.Warning("LocalStorageServer listener task did not complete within timeout");
                    }
                    
                    Logger.Information("LocalStorageServer stopped successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error stopping LocalStorageServer");
                }
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            Logger.Debug("LocalStorageServer listener started");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Logger.Warning(ex, "Listener exception, likely due to shutdown");
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    Logger.Debug("Listener operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected error in listener loop");
                }
            }
            
            Logger.Debug("LocalStorageServer listener stopped");
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            string path = "unknown";
            string method = "unknown";
            
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                method = request.HttpMethod ?? "unknown";
                path = request.Url?.AbsolutePath ?? "/";

                Logger.Debug("Handling {Method} request for {Path}", method, path);

                // Enable CORS for local development
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // Handle OPTIONS preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                // Route handling
                switch (path)
                {
                    case "/keys":
                        await HandleKeys(response);
                        break;

                    case "/get":
                        await HandleGet(request, response);
                        break;

                    case "/set":
                        await HandleSet(request, response);
                        break;

                    case "/delete":
                        await HandleDelete(request, response);
                        break;

                    case "/health":
                        await HandleHealth(response);
                        break;

                    default:
                        Logger.Warning("404 Not Found: {Method} {Path}", method, path);
                        response.StatusCode = 404;
                        await WriteJsonResponse(response, new { error = "Not found" });
                        break;
                }
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "Database error handling {Method} request for {Path}", method, path);
                try
                {
                    context.Response.StatusCode = 500;
                    await WriteJsonResponse(context.Response, new { error = "Database error", details = ex.Message });
                }
                catch { }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error handling {Method} request for {Path}", method, path);
                try
                {
                    context.Response.StatusCode = 500;
                    await WriteJsonResponse(context.Response, new { error = "Internal server error", details = ex.Message });
                }
                catch { }
            }
        }

        private async Task HandleKeys(HttpListenerResponse response)
        {
            try
            {
                Logger.Debug("Handling /keys request");

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT key FROM kv_store";

                var keys = new List<string>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        keys.Add(reader.GetString(0));
                    }
                }

                response.StatusCode = 200;
                await WriteJsonResponse(response, keys);
                
                Logger.Debug("Returned {Count} keys", keys.Count);
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "Database error in HandleKeys");
                throw;
            }
        }

        private async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var key = request.QueryString["key"];
                if (string.IsNullOrEmpty(key))
                {
                    Logger.Warning("GET request missing key parameter");
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "Missing key parameter" });
                    return;
                }

                Logger.Debug("Getting value for key: {Key}", key);

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT value FROM kv_store WHERE key = @key";
                command.Parameters.AddWithValue("@key", key);

                var value = await command.ExecuteScalarAsync() as string;

                response.StatusCode = 200;
                if (value != null)
                {
                    // Return raw JSON value
                    response.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes(value);
                    await response.OutputStream.WriteAsync(buffer);
                    Logger.Debug("Returned value for key: {Key}", key);
                }
                else
                {
                    await WriteJsonResponse(response, null);
                    Logger.Debug("No value found for key: {Key}", key);
                }

                response.Close();
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "Database error in HandleGet");
                throw;
            }
        }

        private async Task HandleSet(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var key = request.QueryString["key"];
                if (string.IsNullOrEmpty(key))
                {
                    Logger.Warning("SET request missing key parameter");
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "Missing key parameter" });
                    return;
                }

                // Read value from request body
                string value;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    value = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(value))
                {
                    Logger.Warning("SET request for key {Key} has empty value", key);
                }

                Logger.Debug("Setting key: {Key}, value length: {Length}", key, value?.Length ?? 0);

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO kv_store (key, value, updated_at)
                    VALUES (@key, @value, strftime('%s', 'now'))
                    ON CONFLICT(key)
                    DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at
                ";
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@value", value ?? string.Empty);

                await command.ExecuteNonQueryAsync();

                response.StatusCode = 200;
                await WriteJsonResponse(response, new { success = true });
                
                Logger.Debug("Successfully set key: {Key}", key);
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "Database error in HandleSet");
                throw;
            }
        }

        private async Task HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var key = request.QueryString["key"];
                if (string.IsNullOrEmpty(key))
                {
                    Logger.Warning("DELETE request missing key parameter");
                    response.StatusCode = 400;
                    await WriteJsonResponse(response, new { error = "Missing key parameter" });
                    return;
                }

                Logger.Debug("Deleting key: {Key}", key);

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM kv_store WHERE key = @key";
                command.Parameters.AddWithValue("@key", key);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                response.StatusCode = 200;
                await WriteJsonResponse(response, new { success = true });
                
                Logger.Debug("Deleted key: {Key}, rows affected: {RowsAffected}", key, rowsAffected);
            }
            catch (SqliteException ex)
            {
                Logger.Error(ex, "Database error in HandleDelete");
                throw;
            }
        }

        private async Task HandleHealth(HttpListenerResponse response)
        {
            try
            {
                // Verify database connectivity
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();
                
                response.StatusCode = 200;
                await WriteJsonResponse(response, new { status = "ok", database = _dbPath, port = Port });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Health check failed");
                response.StatusCode = 503;
                await WriteJsonResponse(response, new { status = "error", error = ex.Message });
            }
        }

        private async Task WriteJsonResponse(HttpListenerResponse response, object? data)
        {
            try
            {
                response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(data);
                var buffer = Encoding.UTF8.GetBytes(json);
                await response.OutputStream.WriteAsync(buffer);
                response.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error writing JSON response");
                throw;
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;

                Logger.Information("Disposing LocalStorageServer");
                _disposed = true;

                try
                {
                    Stop();
                    
                    _listener?.Close();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    
                    Logger.Information("LocalStorageServer disposed successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error during LocalStorageServer dispose");
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
