using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that executes a one-time prompt through the LLM API.
    /// </summary>
    public class ExecuteOneTimePromptNode : ActionNodeBase
    {
        public override string Name => "Execute One-Time Prompt";

        public ExecuteOneTimePromptNode(IServiceProvider services) : base(services)
        {
        }

        protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            var systemPrompt = context.GetOrDefault<SystemPromptInfo>("selectedSystemPrompt", null);
            var userPrompt = context.GetOrDefault<string>("userPrompt", "");

            if (systemPrompt == null)
            {
                return WorkflowResult.CreateError(context, "No system prompt selected");
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                return WorkflowResult.CreateError(context, "No user prompt entered");
            }

            // Get the delegate from context
            var executeOneTimeFunc = context.GetOrDefault<Func<string, string, Task<ExecutionResult>>>("ExecuteOneTimePromptFromWebApp", null);
            if (executeOneTimeFunc == null)
            {
                return WorkflowResult.CreateError(context, "ExecuteOneTimePromptFromWebApp delegate not available");
            }

            try
            {
                // Notify that execution is starting
                var notifyAction = context.GetOrDefault<Action<string>>("NotifyAction", null);
                notifyAction?.Invoke("Executing prompt through LLM...");

                var result = await executeOneTimeFunc(systemPrompt.Id, userPrompt);

                if (result.Success && result.Result != null)
                {
                    context.Set("executionResult", result.Result);
                    context.Set("filledContent", result.Result); // For compatibility with copy/paste nodes
                    
                    return WorkflowResult.CreateSuccess(context);
                }
                else
                {
                    return WorkflowResult.CreateError(context, $"Execution failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                return WorkflowResult.CreateError(context, $"Error executing one-time prompt: {ex.Message}");
            }
        }
    }
}
