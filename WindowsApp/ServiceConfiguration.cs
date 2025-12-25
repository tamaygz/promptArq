using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using PromptArqApp.Workflow.Plugins;
using PromptArqApp.Core.Services;
using PromptArqApp.Core.Capabilities;
using PromptArqApp.Core.Actions;
using PromptArqApp.Services;
using PromptArqApp.Capabilities;
using PromptArqApp.Actions;
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

            // Advanced services (Phase 7)
            ConfigureAdvancedServices(services);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            Log.Information("Service container configured successfully with all Phase 7 services");
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
            try
            {
                Log.Debug("[ServiceConfig] ConfigureWorkflowServices() called");
                
                // Register workflow registry as singleton
                services.AddSingleton<IWorkflowRegistry>(sp =>
                {
                    try
                    {
                        Log.Information("[ServiceConfig] IWorkflowRegistry factory method executing...");
                        var registry = new WorkflowRegistry(sp);
                        
                        // Register node types from built-in plugin (needed for JSON workflows)
                        Log.Information("[ServiceConfig] Registering built-in node types...");
                        var plugin = new BuiltInWorkflowsPlugin();
                        foreach (var (nodeType, nodeClass) in plugin.GetNodes())
                        {
                            registry.RegisterNode(nodeType, nodeClass);
                        }
                        Log.Information("[ServiceConfig] Built-in node types registered");
                        
                        // Skip programmatic workflows - use JSON workflows instead
                        Log.Information("[ServiceConfig] Skipping programmatic workflows - using JSON workflows only");
                        
                        // Phase 7.5: Load workflows from JSON files
                        try
                        {
                            var workflowsDirectory = System.IO.Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory, 
                                "Workflows");
                            
                            Log.Information($"[ServiceConfig] Looking for workflows in: {workflowsDirectory}");
                            if (System.IO.Directory.Exists(workflowsDirectory))
                            {
                                Log.Information($"[ServiceConfig] Workflows directory exists, loading JSON files...");
                                // Load JSON workflows synchronously during startup (using dedicated sync method)
                                var workflowCount = registry.LoadFromJsonDirectorySync(workflowsDirectory);
                                Log.Information($"[ServiceConfig] Loaded {workflowCount} workflows from JSON files");
                                
                                var registeredWorkflows = registry.GetAllWorkflows().ToList();
                                Log.Information($"[ServiceConfig] After JSON loading, {registeredWorkflows.Count} workflows total: {string.Join(", ", registeredWorkflows.Select(w => w.Id))}");
                            }
                            else
                            {
                                Log.Warning($"[ServiceConfig] Workflows directory not found: {workflowsDirectory}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "[ServiceConfig] Failed to load JSON workflows during startup");
                            // Don't fail application startup if JSON loading fails
                        }
                        
                        Log.Information("[ServiceConfig] IWorkflowRegistry factory method completed successfully");
                        return registry;
                    }
                    catch (Exception factoryEx)
                    {
                        Log.Fatal(factoryEx, "[ServiceConfig] FATAL: Exception in IWorkflowRegistry factory method!");
                        throw; // Re-throw to let DI container know about the failure
                    }
                });

                // Register workflow engine as transient (new instance per resolve)
                services.AddTransient<WorkflowEngine>();

                Log.Information("Workflow services registered with JSON loading support");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[ServiceConfig] FATAL: Exception in ConfigureWorkflowServices!");
                throw; // Re-throw to prevent silent failures
            }
        }

        private static void ConfigureAdvancedServices(IServiceCollection services)
        {
            // Phase 7.2: Capability Registry System
            services.AddSingleton<CapabilityRegistry>(sp =>
            {
                var registry = new CapabilityRegistry();
                
                // Register built-in capability providers  
                registry.RegisterProvider(new PromptCapabilitiesProvider());
                registry.RegisterProvider(new ClipboardCapabilitiesProvider());
                registry.RegisterProvider(new SystemCapabilitiesProvider());
                
                return registry;
            });

            // Phase 7.3: Universal Actions Framework
            services.AddSingleton<ActionRegistry>(sp =>
            {
                var registry = new ActionRegistry();
                var clipboardService = sp.GetService<IClipboardService>();
                
                // Register built-in actions
                registry.RegisterAction(new CopyAction(clipboardService));
                registry.RegisterAction(new OpenUrlAction());
                registry.RegisterAction(new OpenFileAction());
                
                return registry;
            });

            // Phase 7.4: Service Implementations
            services.AddSingleton<IClipboardService, ClipboardService>();
            // Future: Add ISnippetService, ISystemService implementations

            Log.Information("Phase 7 advanced services registered (Capabilities, Actions, Services)");
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
