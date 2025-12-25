using System;
using System.Collections.Generic;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Registry
{
    /// <summary>
    /// Interface for workflow plugins that can provide workflows and nodes.
    /// </summary>
    public interface IWorkflowPlugin
    {
        /// <summary>
        /// Gets the unique identifier for this plugin.
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Gets the human-readable name of this plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets the workflows provided by this plugin.
        /// </summary>
        /// <returns>An enumerable of workflows.</returns>
        IEnumerable<Core.Workflow> GetWorkflows();

        /// <summary>
        /// Gets the nodes provided by this plugin.
        /// </summary>
        /// <returns>An enumerable of (NodeType, NodeClass) tuples.</returns>
        IEnumerable<(string NodeType, Type NodeClass)> GetNodes();
    }

    /// <summary>
    /// Registry for workflows, nodes, and plugins.
    /// </summary>
    public interface IWorkflowRegistry
    {
        /// <summary>
        /// Registers a workflow in the registry.
        /// </summary>
        /// <param name="workflow">The workflow to register.</param>
        void RegisterWorkflow(Core.Workflow workflow);

        /// <summary>
        /// Registers a node type with its implementation class.
        /// </summary>
        /// <param name="nodeType">The type name for the node.</param>
        /// <param name="nodeClass">The class that implements the node.</param>
        void RegisterNode(string nodeType, Type nodeClass);

        /// <summary>
        /// Registers a plugin and all its workflows and nodes.
        /// </summary>
        /// <param name="plugin">The plugin to register.</param>
        void RegisterPlugin(IWorkflowPlugin plugin);

        /// <summary>
        /// Gets a workflow by its ID.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow.</param>
        /// <returns>The workflow, or null if not found.</returns>
        Core.Workflow? GetWorkflow(string workflowId);

        /// <summary>
        /// Gets all registered workflows.
        /// </summary>
        /// <returns>An enumerable of all workflows.</returns>
        IEnumerable<Core.Workflow> GetAllWorkflows();

        /// <summary>
        /// Creates a node instance of the specified type.
        /// </summary>
        /// <param name="nodeType">The type name of the node.</param>
        /// <param name="config">Optional configuration dictionary for the node.</param>
        /// <returns>An instance of the node, or null if the type is not registered.</returns>
        IWorkflowNode? CreateNode(string nodeType, Dictionary<string, object>? config = null);

        /// <summary>
        /// Gets all registered node types.
        /// </summary>
        /// <returns>An enumerable of registered node type names.</returns>
        IEnumerable<string> GetRegisteredNodeTypes();

        /// <summary>
        /// Gets all registered plugins.
        /// </summary>
        /// <returns>An enumerable of all plugins.</returns>
        IEnumerable<IWorkflowPlugin> GetAllPlugins();
    }
}
