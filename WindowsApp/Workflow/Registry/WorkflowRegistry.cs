using System;
using System.Collections.Generic;
using System.Linq;
using PromptArqApp.Workflow.Core;
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

        public WorkflowRegistry(IServiceProvider services)
        {
            _workflows = new Dictionary<string, Core.Workflow>();
            _nodeTypes = new Dictionary<string, Type>();
            _plugins = new List<IWorkflowPlugin>();
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = Log.ForContext<WorkflowRegistry>();
        }

        /// <inheritdoc/>
        public void RegisterWorkflow(Core.Workflow workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            if (string.IsNullOrWhiteSpace(workflow.Id))
                throw new ArgumentException("Workflow ID cannot be empty.", nameof(workflow));

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
                    if (constructorWithServices != null)
                    {
                        instance = (IWorkflowNode)constructorWithServices.Invoke(new object[] { _services });
                    }
                    else
                    {
                        // Try parameterless constructor
                        instance = (IWorkflowNode?)Activator.CreateInstance(nodeClass);
                    }
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
    }
}
