using System.Collections.Generic;
using System.Threading.Tasks;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Defines the type of UI to display for a workflow node.
    /// </summary>
    public enum NodeUIType
    {
        /// <summary>
        /// Show a list of items (prompts, actions, etc.)
        /// </summary>
        ItemList,

        /// <summary>
        /// Show a text input box
        /// </summary>
        TextInput,

        /// <summary>
        /// Show TextDisplayPanel component
        /// </summary>
        TextDisplay,

        /// <summary>
        /// Show multiple input fields
        /// </summary>
        MultiStepInput,

        /// <summary>
        /// Show Yes/No confirmation dialog
        /// </summary>
        Confirmation,

        /// <summary>
        /// Use a custom UI component
        /// </summary>
        Custom,

        /// <summary>
        /// No UI required (background processing)
        /// </summary>
        None
    }

    /// <summary>
    /// Base interface for all workflow nodes.
    /// Nodes are the building blocks of workflows, each representing a single step or operation.
    /// </summary>
    public interface IWorkflowNode
    {
        /// <summary>
        /// Gets the unique identifier for this node instance.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the human-readable name of this node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the node's logic with the given context.
        /// </summary>
        /// <param name="context">The workflow context containing data and services.</param>
        /// <returns>A WorkflowResult indicating success/failure and any output.</returns>
        Task<WorkflowResult> ExecuteAsync(WorkflowContext context);
    }

    /// <summary>
    /// Interface for nodes that provide UI rendering information.
    /// Nodes implementing this interface can control how they are displayed in the command palette.
    /// </summary>
    public interface INodeUIProvider
    {
        /// <summary>
        /// Gets the type of UI to display for this node.
        /// </summary>
        NodeUIType UIType { get; }

        /// <summary>
        /// Gets the hint text to display to the user.
        /// </summary>
        string HintText { get; }

        /// <summary>
        /// Gets whether the search box should be read-only.
        /// </summary>
        bool ReadOnly { get; }

        /// <summary>
        /// Gets the items to display in the UI (for ItemList type).
        /// </summary>
        /// <param name="context">The current workflow context.</param>
        /// <returns>An enumerable of items to display.</returns>
        IEnumerable<object> GetItems(WorkflowContext context);

        /// <summary>
        /// Gets the display text for an item (primary text).
        /// </summary>
        /// <param name="item">The item to get display text for.</param>
        /// <returns>The display text.</returns>
        string GetDisplayText(object item);

        /// <summary>
        /// Gets the secondary text for an item (description/subtitle).
        /// </summary>
        /// <param name="item">The item to get secondary text for.</param>
        /// <returns>The secondary text.</returns>
        string GetSecondaryText(object item);

        /// <summary>
        /// Gets the icon for an item.
        /// </summary>
        /// <param name="item">The item to get icon for.</param>
        /// <returns>The icon string (emoji or icon name).</returns>
        string GetIcon(object item);

        /// <summary>
        /// Gets the color for an item (optional).
        /// </summary>
        /// <param name="item">The item to get color for.</param>
        /// <returns>The color, or null for default.</returns>
        System.Drawing.Color? GetItemColor(object item);
    }
}
