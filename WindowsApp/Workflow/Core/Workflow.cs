using System;
using System.Collections.Generic;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Defines a workflow as a collection of connected nodes.
    /// </summary>
    public class Workflow
    {
        /// <summary>
        /// Gets or sets the unique identifier for this workflow.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the human-readable name of this workflow.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the description of what this workflow does.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets the icon for this workflow (emoji or icon name).
        /// </summary>
        public string Icon { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of node definitions in this workflow.
        /// </summary>
        public List<WorkflowNodeDefinition> Nodes { get; set; } = new();

        /// <summary>
        /// Gets or sets the connections between nodes.
        /// Key is the source node ID, value is the target node ID.
        /// </summary>
        public Dictionary<string, string> Connections { get; set; } = new();

        /// <summary>
        /// Gets or sets the conditional branches for nodes (e.g., ConditionalNode).
        /// Key is the source node ID, value is a dictionary of condition -> target node ID.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>>? Branches { get; set; }

        /// <summary>
        /// Gets or sets the ID of the entry node (first node to execute).
        /// </summary>
        public string EntryNodeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the metadata for this workflow.
        /// </summary>
        public WorkflowMetadata Metadata { get; set; } = new();

        /// <summary>
        /// Gets a node definition by its ID.
        /// </summary>
        /// <param name="nodeId">The ID of the node to get.</param>
        /// <returns>The node definition, or null if not found.</returns>
        public WorkflowNodeDefinition? GetNodeById(string nodeId)
        {
            return Nodes.Find(n => n.Id == nodeId);
        }

        /// <summary>
        /// Gets the next node ID after the specified node.
        /// </summary>
        /// <param name="currentNodeId">The current node ID.</param>
        /// <returns>The next node ID, or null if no connection exists.</returns>
        public string? GetNextNodeId(string currentNodeId)
        {
            return Connections.TryGetValue(currentNodeId, out var nextId) ? nextId : null;
        }
    }

    /// <summary>
    /// Defines a node within a workflow.
    /// </summary>
    public class WorkflowNodeDefinition
    {
        /// <summary>
        /// Gets or sets the unique identifier for this node within the workflow.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the type name of the node class to instantiate.
        /// </summary>
        public string NodeType { get; set; } = "";

        /// <summary>
        /// Gets or sets the configuration dictionary for this node.
        /// These values are passed to the node during instantiation.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Gets or sets whether back navigation is allowed from this node.
        /// </summary>
        public bool AllowBackNavigation { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom node ID to navigate to when going back.
        /// If null, uses the default back navigation behavior.
        /// </summary>
        public string? CustomBackNodeId { get; set; }
    }

    /// <summary>
    /// Metadata about a workflow.
    /// </summary>
    public class WorkflowMetadata
    {
        /// <summary>
        /// Gets or sets the author of this workflow.
        /// </summary>
        public string Author { get; set; } = "";

        /// <summary>
        /// Gets or sets the version of this workflow.
        /// </summary>
        public Version Version { get; set; } = new Version(1, 0, 0);

        /// <summary>
        /// Gets or sets the tags for categorizing this workflow.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the required services for this workflow to function.
        /// </summary>
        public string[] RequiredServices { get; set; } = Array.Empty<string>();
    }
}
