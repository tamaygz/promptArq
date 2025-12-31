using System;
using System.Collections.Generic;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Represents a frame in the workflow navigation history.
    /// </summary>
    public class NavigationFrame
    {
        /// <summary>
        /// Gets or sets the ID of the node that was executing.
        /// </summary>
        public string NodeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the context state at this point in navigation.
        /// </summary>
        public WorkflowContext Context { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timestamp when this frame was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Manages the navigation history for workflows, enabling back navigation.
    /// </summary>
    public class WorkflowNavigationStack
    {
        private readonly Stack<NavigationFrame> _history;

        /// <summary>
        /// Gets the number of frames in the navigation history.
        /// </summary>
        public int Count => _history.Count;

        public WorkflowNavigationStack()
        {
            _history = new Stack<NavigationFrame>();
        }

        /// <summary>
        /// Pushes a new navigation frame onto the stack.
        /// </summary>
        /// <param name="nodeId">The ID of the current node.</param>
        /// <param name="context">The current workflow context.</param>
        public void Push(string nodeId, WorkflowContext context)
        {
            var frame = new NavigationFrame
            {
                NodeId = nodeId,
                Context = context.Clone(), // Clone to preserve state
                Timestamp = DateTime.Now
            };
            _history.Push(frame);
        }

        /// <summary>
        /// Pops the most recent navigation frame from the stack.
        /// </summary>
        /// <returns>The navigation frame, or null if the stack is empty.</returns>
        public NavigationFrame? Pop()
        {
            return _history.Count > 0 ? _history.Pop() : null;
        }

        /// <summary>
        /// Peeks at the most recent navigation frame without removing it.
        /// </summary>
        /// <returns>The navigation frame, or null if the stack is empty.</returns>
        public NavigationFrame? Peek()
        {
            return _history.Count > 0 ? _history.Peek() : null;
        }

        /// <summary>
        /// Clears all navigation history.
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }
    }
}
