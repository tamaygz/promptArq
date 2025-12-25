using System;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Represents the result of a workflow node execution.
    /// </summary>
    public class WorkflowResult
    {
        /// <summary>
        /// Gets whether the node execution was successful.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the updated workflow context after node execution.
        /// </summary>
        public WorkflowContext Context { get; init; } = null!;

        /// <summary>
        /// Gets the ID of the next node to execute (for branching).
        /// If null, the workflow will follow the default connection.
        /// </summary>
        public string? NextNodeId { get; init; }

        /// <summary>
        /// Gets the output of the node execution.
        /// </summary>
        public object? Output { get; init; }

        /// <summary>
        /// Gets the error message if the execution failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Creates a successful workflow result.
        /// </summary>
        /// <param name="context">The updated workflow context.</param>
        /// <param name="nextNodeId">Optional ID of the next node to execute.</param>
        /// <param name="output">Optional output from the node.</param>
        /// <returns>A successful WorkflowResult.</returns>
        public static WorkflowResult CreateSuccess(WorkflowContext context, string? nextNodeId = null, object? output = null)
        {
            return new WorkflowResult
            {
                IsSuccess = true,
                Context = context ?? throw new ArgumentNullException(nameof(context)),
                NextNodeId = nextNodeId,
                Output = output
            };
        }

        /// <summary>
        /// Creates a failed workflow result.
        /// </summary>
        /// <param name="context">The workflow context at the time of failure.</param>
        /// <param name="errorMessage">A description of the error.</param>
        /// <returns>A failed WorkflowResult.</returns>
        public static WorkflowResult CreateError(WorkflowContext context, string errorMessage)
        {
            return new WorkflowResult
            {
                IsSuccess = false,
                Context = context ?? throw new ArgumentNullException(nameof(context)),
                ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage))
            };
        }
    }
}
