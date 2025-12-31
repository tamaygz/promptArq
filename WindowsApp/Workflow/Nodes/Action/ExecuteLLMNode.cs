using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that executes a prompt through the LLM API.
    /// Uses the web app's execution endpoint.
    /// </summary>
    public class ExecuteLLMNode : ActionNodeBase
    {
        public override string Name => "Execute LLM";

        public ExecuteLLMNode(IServiceProvider services) : base(services)
        {
        }

        protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            var selectedPrompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
            if (selectedPrompt == null)
            {
                return WorkflowResult.CreateError(context, "No prompt selected");
            }

            // Get the content to execute (filled or original)
            var content = context.GetOrDefault<string>("filledContent", selectedPrompt.Content);

            // Get the delegate from context
            var executePromptFunc = context.GetOrDefault<Func<string, string?, Task<ExecutionResult>>>("ExecutePromptInWebApp", null);
            if (executePromptFunc == null)
            {
                return WorkflowResult.CreateError(context, "ExecutePromptInWebApp delegate not available");
            }

            try
            {
                // Notify that execution is starting
                var notifyAction = context.GetOrDefault<Action<string>>("NotifyAction", null);
                notifyAction?.Invoke("Executing through LLM...");

                var result = await executePromptFunc(selectedPrompt.Id, content);

                if (result.Success && result.Result != null)
                {
                    // Store the execution result
                    context.Set("executionResult", result.Result);
                    context.Set("filledContent", result.Result); // Update content with result
                    
                    return WorkflowResult.CreateSuccess(context);
                }
                else
                {
                    return WorkflowResult.CreateError(context, $"LLM execution failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                return WorkflowResult.CreateError(context, $"Error executing LLM: {ex.Message}");
            }
        }
    }
}
