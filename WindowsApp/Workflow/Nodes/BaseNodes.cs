using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes
{
    /// <summary>
    /// Abstract base class for all workflow nodes.
    /// Provides common functionality for node implementation.
    /// </summary>
    public abstract class WorkflowNodeBase : IWorkflowNode
    {
        /// <inheritdoc/>
        public virtual string Id { get; protected set; } = Guid.NewGuid().ToString();

        /// <inheritdoc/>
        public abstract string Name { get; }

        protected readonly IServiceProvider Services;

        protected WorkflowNodeBase(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc/>
        public abstract Task<WorkflowResult> ExecuteAsync(WorkflowContext context);

        /// <summary>
        /// Virtual method that can be overridden to configure the node from a dictionary.
        /// </summary>
        /// <param name="config">Configuration dictionary.</param>
        public virtual void Configure(Dictionary<string, object> config)
        {
            // Base implementation does nothing
        }
    }

    /// <summary>
    /// Base class for input nodes that display UI and accept user input.
    /// </summary>
    public abstract class InputNodeBase : WorkflowNodeBase, INodeUIProvider, INodeItemRenderer
    {
        /// <inheritdoc/>
        public abstract NodeUIType UIType { get; }

        /// <inheritdoc/>
        public abstract string HintText { get; }

        /// <inheritdoc/>
        public virtual bool ReadOnly => false;

        protected InputNodeBase(IServiceProvider services) : base(services)
        {
        }

        /// <inheritdoc/>
        public virtual IEnumerable<object> GetItems(WorkflowContext context)
        {
            return Array.Empty<object>();
        }

        /// <inheritdoc/>
        public virtual string GetDisplayText(object item)
        {
            return item?.ToString() ?? "";
        }

        /// <inheritdoc/>
        public virtual string GetSecondaryText(object item)
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual string GetIcon(object item)
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual Color? GetItemColor(object item)
        {
            return null;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Gets the render data for an item. Override this to customize rendering using templates.
        /// Default implementation uses Standard template with data from GetDisplayText, GetSecondaryText, etc.
        /// </summary>
        public virtual ItemRenderData GetItemRenderData(object item)
        {
            return new ItemRenderData
            {
                MainText = GetDisplayText(item),
                SecondaryText = GetSecondaryText(item),
                Icon = GetIcon(item),
                ItemColor = GetItemColor(item),
                Template = ItemRenderTemplate.Standard,
                OriginalItem = item
            };
        }

        /// <inheritdoc/>
        /// <summary>
        /// Provides custom rendering for items when Template is Custom.
        /// Override this to provide completely custom rendering logic.
        /// </summary>
        public virtual bool CustomRenderItem(Graphics graphics, Rectangle bounds, object item, bool isSelected)
        {
            return false; // Default: no custom rendering
        }
    }

    /// <summary>
    /// Base class for action nodes that perform operations without UI.
    /// </summary>
    public abstract class ActionNodeBase : WorkflowNodeBase
    {
        protected ActionNodeBase(IServiceProvider services) : base(services)
        {
        }

        /// <summary>
        /// Performs the action with the given context.
        /// </summary>
        /// <param name="context">The workflow context.</param>
        /// <returns>The result of the action.</returns>
        protected abstract Task<WorkflowResult> PerformActionAsync(WorkflowContext context);

        /// <inheritdoc/>
        public override async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            return await PerformActionAsync(context);
        }
    }

    /// <summary>
    /// Base class for UI nodes that display information to the user.
    /// </summary>
    public abstract class UINodeBase : WorkflowNodeBase, INodeUIProvider
    {
        /// <inheritdoc/>
        public abstract NodeUIType UIType { get; }

        /// <inheritdoc/>
        public abstract string HintText { get; }

        /// <inheritdoc/>
        public virtual bool ReadOnly => true;

        protected UINodeBase(IServiceProvider services) : base(services)
        {
        }

        /// <inheritdoc/>
        public virtual IEnumerable<object> GetItems(WorkflowContext context)
        {
            return Array.Empty<object>();
        }

        /// <inheritdoc/>
        public virtual string GetDisplayText(object item)
        {
            return item?.ToString() ?? "";
        }

        /// <inheritdoc/>
        public virtual string GetSecondaryText(object item)
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual string GetIcon(object item)
        {
            return "";
        }

        /// <inheritdoc/>
        public virtual Color? GetItemColor(object item)
        {
            return null;
        }
    }

    /// <summary>
    /// Base class for utility nodes that perform control flow operations.
    /// </summary>
    public abstract class UtilityNodeBase : WorkflowNodeBase
    {
        protected UtilityNodeBase(IServiceProvider services) : base(services)
        {
        }
    }

    /// <summary>
    /// Base class for output nodes that perform final actions and side effects.
    /// </summary>
    public abstract class OutputNodeBase : WorkflowNodeBase
    {
        protected OutputNodeBase(IServiceProvider services) : base(services)
        {
        }
    }
}
