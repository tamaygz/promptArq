using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace PromptArqApp
{
    /// <summary>
    /// Centralized logging configuration using Serilog
    /// </summary>
    public static class LoggerConfig
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the global Serilog logger with file, console, and debug sinks
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    string logDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "PromptArq",
                        "logs"
                    );

                    Directory.CreateDirectory(logDirectory);

                    string logFilePath = Path.Combine(logDirectory, "promptarq-.log");

                    Log.Logger = new LoggerConfiguration()
#if DEBUG
                        .MinimumLevel.Debug()
#else
                        .MinimumLevel.Information()
#endif
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "PromptArq")
                        .WriteTo.File(
                            logFilePath,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                        )
#if DEBUG
                        .WriteTo.Debug(
                            outputTemplate: "[{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                        )
                        .WriteTo.Console(
                            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        )
#endif
                        .CreateLogger();

                    _isInitialized = true;
                    Log.Information("Logging system initialized. Log directory: {LogDirectory}", logDirectory);
                }
                catch (Exception ex)
                {
                    // Fallback to console if logging setup fails
                    Console.WriteLine($"Failed to initialize logging: {ex.Message}");
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    _isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Closes and flushes the logger - call this on application shutdown
        /// </summary>
        public static void CloseAndFlush()
        {
            try
            {
                Log.Information("Application shutting down - flushing logs");
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing logger: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a logger for a specific type
        /// </summary>
        public static ILogger ForContext<T>()
        {
            return Log.ForContext<T>();
        }

        /// <summary>
        /// Gets a logger for a specific context name
        /// </summary>
        public static ILogger ForContext(string contextName)
        {
            return Log.ForContext("SourceContext", contextName);
        }
    }
}