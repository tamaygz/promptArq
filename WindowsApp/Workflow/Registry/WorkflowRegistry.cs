using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Core.Workflows;
using Serilog;

namespace PromptArqApp.Workflow.Registry
{
    /// <summary>
    /// Default implementation of IWorkflowRegistry.
    /// Manages registration and retrieval of workflows, nodes, and plugins.
    /// </summary>
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly Dictionary<string, Core.Workflow> _workflows;
        private readonly Dictionary<string, Type> _nodeTypes;
        private readonly List<IWorkflowPlugin> _plugins;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly WorkflowValidator _validator;

        public WorkflowRegistry(IServiceProvider services)
        {
            _workflows = new Dictionary<string, Core.Workflow>();
            _nodeTypes = new Dictionary<string, Type>();
            _plugins = new List<IWorkflowPlugin>();
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = Log.ForContext<WorkflowRegistry>();
            _validator = new WorkflowValidator();
        }

        /// <inheritdoc/>
        public void RegisterWorkflow(Core.Workflow workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            if (string.IsNullOrWhiteSpace(workflow.Id))
                throw new ArgumentException("Workflow ID cannot be empty.", nameof(workflow));

            // Validate workflow structure
            var validationErrors = _validator.Validate(workflow);
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Workflow '{workflow.Id}' validation failed:\n" + 
                                 string.Join("\n", validationErrors.Select(e => $"  - {e}"));
                _logger.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            if (_workflows.ContainsKey(workflow.Id))
            {
                _logger.Warning("Workflow {WorkflowId} is already registered, overwriting", workflow.Id);
            }

            _workflows[workflow.Id] = workflow;
            _logger.Information("Registered workflow: {WorkflowId} - {WorkflowName}", workflow.Id, workflow.Name);
        }

        /// <inheritdoc/>
        public void RegisterNode(string nodeType, Type nodeClass)
        {
            if (string.IsNullOrWhiteSpace(nodeType))
                throw new ArgumentException("Node type cannot be empty.", nameof(nodeType));

            if (nodeClass == null)
                throw new ArgumentNullException(nameof(nodeClass));

            if (!typeof(IWorkflowNode).IsAssignableFrom(nodeClass))
                throw new ArgumentException($"Node class {nodeClass.Name} must implement IWorkflowNode.", nameof(nodeClass));

            if (_nodeTypes.ContainsKey(nodeType))
            {
                _logger.Warning("Node type {NodeType} is already registered, overwriting", nodeType);
            }

            _nodeTypes[nodeType] = nodeClass;
            _logger.Information("Registered node type: {NodeType} -> {NodeClass}", nodeType, nodeClass.Name);
        }

        /// <inheritdoc/>
        public void RegisterPlugin(IWorkflowPlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            _logger.Information("Registering plugin: {PluginId} - {PluginName} v{PluginVersion}", 
                plugin.PluginId, plugin.Name, plugin.Version);

            // Register all workflows from the plugin
            foreach (var workflow in plugin.GetWorkflows())
            {
                RegisterWorkflow(workflow);
            }

            // Register all nodes from the plugin
            foreach (var (nodeType, nodeClass) in plugin.GetNodes())
            {
                RegisterNode(nodeType, nodeClass);
            }

            _plugins.Add(plugin);
            _logger.Information("Plugin {PluginId} registered successfully", plugin.PluginId);
        }

        /// <inheritdoc/>
        public Core.Workflow? GetWorkflow(string workflowId)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                return null;

            return _workflows.TryGetValue(workflowId, out var workflow) ? workflow : null;
        }

        /// <inheritdoc/>
        public IEnumerable<Core.Workflow> GetAllWorkflows()
        {
            return _workflows.Values;
        }

        /// <inheritdoc/>
        public IWorkflowNode? CreateNode(string nodeType, Dictionary<string, object>? config = null)
        {
            if (string.IsNullOrWhiteSpace(nodeType))
            {
                _logger.Error("Cannot create node: nodeType is null or empty");
                return null;
            }

            if (!_nodeTypes.TryGetValue(nodeType, out var nodeClass))
            {
                _logger.Error("Node type {NodeType} not found in registry", nodeType);
                return null;
            }

            try
            {
                // Try to create instance with service provider and config
                IWorkflowNode? instance = null;

                // First, try constructor with (IServiceProvider, Dictionary<string, object>)
                var constructorWithConfig = nodeClass.GetConstructor(new[] { typeof(IServiceProvider), typeof(Dictionary<string, object>) });
                if (constructorWithConfig != null)
                {
                    instance = (IWorkflowNode)constructorWithConfig.Invoke(new object?[] { _services, config ?? new Dictionary<string, object>() });
                }
                else
                {
                    // Try constructor with just IServiceProvider
                    var constructorWithServices = nodeClass.GetConstructor(new[] { typeof(IServiceProvider) });
                    instance = constructorWithServices != null
                        ? (IWorkflowNode)constructorWithServices.Invoke(new object[] { _services })
                        : (IWorkflowNode?)Activator.CreateInstance(nodeClass);
                }

                if (instance == null)
                {
                    _logger.Error("Failed to create instance of node type {NodeType}", nodeType);
                    return null;
                }

                // If the instance has a Configure method and we have config, call it
                if (config != null && config.Count > 0)
                {
                    var configureMethod = nodeClass.GetMethod("Configure");
                    if (configureMethod != null)
                    {
                        configureMethod.Invoke(instance, new object[] { config });
                    }
                }

                _logger.Debug("Created node instance: {NodeType} -> {NodeClass}", nodeType, nodeClass.Name);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating node of type {NodeType}", nodeType);
                return null;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetRegisteredNodeTypes()
        {
            return _nodeTypes.Keys;
        }

        /// <inheritdoc/>
        public IEnumerable<IWorkflowPlugin> GetAllPlugins()
        {
            return _plugins;
        }

        /// <summary>
        /// Loads workflows from JSON files in the specified repository.
        /// </summary>
        /// <param name="repository">The workflow repository to load from</param>
        /// <returns>Number of workflows loaded</returns>
        public async Task<int> LoadFromJsonAsync(IWorkflowRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            try
            {
                var workflows = await repository.ListAsync();
                var loadedCount = 0;

                foreach (var workflow in workflows)
                {
                    try
                    {
                        RegisterWorkflow(workflow);
                        loadedCount++;
                        _logger.Information($"Loaded workflow '{workflow.Id}' from JSON");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, $"Failed to register workflow '{workflow.Id}' from JSON");
                    }
                }

                _logger.Information($"Loaded {loadedCount} workflows from JSON repository");
                return loadedCount;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load workflows from JSON repository");
                throw;
            }
        }

        /// <summary>
        /// Loads workflows from JSON files in the default Workflows directory.
        /// </summary>
        /// <param name="workflowsDirectory">Base workflows directory path</param>
        /// <returns>Number of workflows loaded</returns>
        public async Task<int> LoadFromJsonDirectoryAsync(string workflowsDirectory)
        {
            if (string.IsNullOrWhiteSpace(workflowsDirectory))
                throw new ArgumentException("Workflows directory cannot be null or empty", nameof(workflowsDirectory));

            if (!Directory.Exists(workflowsDirectory))
            {
                _logger.Warning($"Workflows directory does not exist: {workflowsDirectory}");
                return 0;
            }

            var repository = new Data.JsonWorkflowRepository(workflowsDirectory);
            return await LoadFromJsonAsync(repository);
        }

        /// <summary>
        /// Loads workflows from JSON files in the specified directory (synchronous version for DI initialization).
        /// </summary>
        /// <param name="workflowsDirectory">Base workflows directory path</param>
        /// <returns>Number of workflows loaded</returns>
        public int LoadFromJsonDirectorySync(string workflowsDirectory)
        {
            if (string.IsNullOrWhiteSpace(workflowsDirectory))
                throw new ArgumentException("Workflows directory cannot be null or empty", nameof(workflowsDirectory));

            if (!Directory.Exists(workflowsDirectory))
            {
                _logger.Warning($"Workflows directory does not exist: {workflowsDirectory}");
                return 0;
            }

            int loadedCount = 0;
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Load from BuiltIn subdirectory
            var builtInDir = Path.Combine(workflowsDirectory, "BuiltIn");
            if (Directory.Exists(builtInDir))
            {
                loadedCount += LoadWorkflowsFromDirectory(builtInDir, jsonOptions);
            }

            // Load from User subdirectory
            var userDir = Path.Combine(workflowsDirectory, "User");
            if (Directory.Exists(userDir))
            {
                loadedCount += LoadWorkflowsFromDirectory(userDir, jsonOptions);
            }

            _logger.Information($"Loaded {loadedCount} workflows from {workflowsDirectory}");
            return loadedCount;
        }

        private int LoadWorkflowsFromDirectory(string directory, System.Text.Json.JsonSerializerOptions jsonOptions)
        {
            int count = 0;
            var workflowFiles = Directory.GetFiles(directory, "*.workflow.json", SearchOption.TopDirectoryOnly);

            foreach (var file in workflowFiles)
            {
                try
                {
                    _logger.Debug($"Loading workflow from: {file}");
                    var json = File.ReadAllText(file); // Synchronous file read
                    var workflow = System.Text.Json.JsonSerializer.Deserialize<Core.Workflow>(json, jsonOptions);

                    if (workflow != null)
                    {
                        RegisterWorkflow(workflow);
                        count++;
                        _logger.Information($"Loaded workflow '{workflow.Id}' from {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to load workflow from {file}");
                }
            }

            return count;
        }
    }
}
