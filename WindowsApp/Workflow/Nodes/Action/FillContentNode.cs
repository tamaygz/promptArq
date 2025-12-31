using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that fills placeholder values into prompt content.
    /// Uses the web app API for accurate placeholder replacement.
    /// </summary>
    public class FillContentNode : ActionNodeBase
    {
        public override string Name => "Fill Content";

        public FillContentNode(IServiceProvider services) : base(services)
        {
        }

        protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            var selectedPrompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
            if (selectedPrompt == null)
            {
                return WorkflowResult.CreateError(context, "No prompt selected");
            }

            var placeholderValues = context.GetOrDefault<Dictionary<string, string>>("placeholderValues", null);
            if (placeholderValues == null || placeholderValues.Count == 0)
            {
                return WorkflowResult.CreateError(context, "No placeholder values provided");
            }

            // Get the delegate from context
            var fillContentFunc = context.GetOrDefault<Func<string, Dictionary<string, string>, Task<string>>>("FillContentInWebApp", null);
            if (fillContentFunc == null)
            {
                return WorkflowResult.CreateError(context, "FillContentInWebApp delegate not available");
            }

            try
            {
                var filledContent = await fillContentFunc(selectedPrompt.Id, placeholderValues);
                context.Set("filledContent", filledContent);
                
                return WorkflowResult.CreateSuccess(context);
            }
            catch (Exception ex)
            {
                return WorkflowResult.CreateError(context, $"Error filling content: {ex.Message}");
            }
        }
    }
}
