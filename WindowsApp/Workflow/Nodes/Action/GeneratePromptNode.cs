using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that generates the combined prompt from system prompt and user input.
    /// </summary>
    public class GeneratePromptNode : ActionNodeBase
    {
        public override string Name => "Generate Prompt";

        public GeneratePromptNode(IServiceProvider services) : base(services)
        {
        }

        protected override Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            var systemPrompt = context.GetOrDefault<SystemPromptInfo>("selectedSystemPrompt", null);
            var userPrompt = context.GetOrDefault<string>("userPrompt", "");

            if (systemPrompt == null)
            {
                return Task.FromResult(WorkflowResult.CreateError(context, "No system prompt selected"));
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                return Task.FromResult(WorkflowResult.CreateError(context, "No user prompt entered"));
            }

            // Combine system prompt with user prompt
            var combinedPrompt = $"{systemPrompt.Content}\n\n{userPrompt}";
            context.Set("generatedPrompt", combinedPrompt);

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }
}
