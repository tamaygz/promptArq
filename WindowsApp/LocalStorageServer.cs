using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PromptArqApp
{
    /// <summary>
    /// Simple HTTP server that provides a REST API for storage operations
    /// Both WebView2 and external browsers can connect to this
    /// </summary>
    public class LocalStorageServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _dbPath;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _listenerTask;
        private const int Port = 5001; // Different from Vite (5000)

        public LocalStorageServer()
        {
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptArq",
                "promptarq.db"
            );

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

            // Initialize database
            InitializeDatabase();

            // Setup HTTP listener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void InitializeDatabase()
        {
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

            Console.WriteLine($"SQLite database initialized at: {_dbPath}");
        }

        public void Start()
        {
            _listener.Start();
            _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
            Console.WriteLine($"Local storage server started on http://localhost:{Port}/");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _listenerTask?.Wait();
            Console.WriteLine("Local storage server stopped");
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
                {
                    // Listener stopped
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in listener: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

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

                var path = request.Url?.AbsolutePath ?? "/";

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
                        response.StatusCode = 404;
                        await WriteJsonResponse(response, new { error = "Not found" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                context.Response.StatusCode = 500;
                await WriteJsonResponse(context.Response, new { error = ex.Message });
            }
        }

        private async Task HandleKeys(HttpListenerResponse response)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT key FROM kv_store";

            var keys = new List<string>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    keys.Add(reader.GetString(0));
                }
            }

            response.StatusCode = 200;
            await WriteJsonResponse(response, keys);
        }

        private async Task HandleGet(HttpListenerRequest request, HttpListenerResponse response)
        {
            var key = request.QueryString["key"];
            if (string.IsNullOrEmpty(key))
            {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new { error = "Missing key parameter" });
                return;
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM kv_store WHERE key = @key";
            command.Parameters.AddWithValue("@key", key);

            var value = command.ExecuteScalar() as string;

            response.StatusCode = 200;
            if (value != null)
            {
                // Return raw JSON value
                response.ContentType = "application/json";
                var buffer = Encoding.UTF8.GetBytes(value);
                await response.OutputStream.WriteAsync(buffer);
            }
            else
            {
                await WriteJsonResponse(response, null);
            }

            response.Close();
        }

        private async Task HandleSet(HttpListenerRequest request, HttpListenerResponse response)
        {
            var key = request.QueryString["key"];
            if (string.IsNullOrEmpty(key))
            {
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

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO kv_store (key, value, updated_at) 
                VALUES (@key, @value, strftime('%s', 'now'))
                ON CONFLICT(key) 
                DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at
            ";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);

            command.ExecuteNonQuery();

            response.StatusCode = 200;
            await WriteJsonResponse(response, new { success = true });
        }

        private async Task HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            var key = request.QueryString["key"];
            if (string.IsNullOrEmpty(key))
            {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new { error = "Missing key parameter" });
                return;
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM kv_store WHERE key = @key";
            command.Parameters.AddWithValue("@key", key);

            command.ExecuteNonQuery();

            response.StatusCode = 200;
            await WriteJsonResponse(response, new { success = true });
        }

        private async Task HandleHealth(HttpListenerResponse response)
        {
            response.StatusCode = 200;
            await WriteJsonResponse(response, new { status = "ok", database = _dbPath });
        }

        private async Task WriteJsonResponse(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(buffer);
            response.Close();
        }

        public void Dispose()
        {
            Stop();
            _listener.Close();
            _cancellationTokenSource.Dispose();
        }
    }
}
