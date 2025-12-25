using System;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using PromptArqApp.Workflow.Plugins;
using Serilog;

namespace PromptArqApp
{
    /// <summary>
    /// Configures and provides the dependency injection container for the application.
    /// </summary>
    public static class ServiceConfiguration
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Gets the configured service provider instance.
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider has not been configured. Call Configure() first.");
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        /// Configures the service container with all application services.
        /// </summary>
        public static void Configure()
        {
            var services = new ServiceCollection();

            // Core services
            ConfigureCoreServices(services);

            // Workflow services
            ConfigureWorkflowServices(services);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            Log.Information("Service container configured successfully");
        }

        private static void ConfigureCoreServices(IServiceCollection services)
        {
            // Add logging (Serilog is already configured in LoggerConfig)
            services.AddSingleton<ILogger>(Log.Logger);

            // Add application settings
            services.AddSingleton<AppSettings>(sp => AppSettings.Load());

            // Add prompt history
            services.AddSingleton<PromptHistory>(sp => PromptHistory.Load());

            // NotificationManager is static, so no need to register it
        }

        private static void ConfigureWorkflowServices(IServiceCollection services)
        {
            // Register workflow registry as singleton
            services.AddSingleton<IWorkflowRegistry>(sp =>
            {
                var registry = new WorkflowRegistry(sp);
                
                // Register built-in workflows plugin
                registry.RegisterPlugin(new BuiltInWorkflowsPlugin());
                
                return registry;
            });

            // Register workflow engine as transient (new instance per resolve)
            services.AddTransient<WorkflowEngine>();

            Log.Information("Workflow services registered");
        }

        /// <summary>
        /// Gets a required service from the container.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public static T GetRequiredService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a service from the container, or null if not registered.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance, or null.</returns>
        public static T? GetService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Disposes the service provider if it implements IDisposable.
        /// </summary>
        public static void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                _serviceProvider = null;
                Log.Information("Service container disposed");
            }
        }
    }
}
