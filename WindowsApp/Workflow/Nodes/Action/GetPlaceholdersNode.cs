using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that extracts placeholders from a prompt's content.
    /// Uses the web app API to get accurate placeholder extraction.
    /// </summary>
    public class GetPlaceholdersNode : ActionNodeBase
    {
        public override string Name => "Get Placeholders";

        public GetPlaceholdersNode(IServiceProvider services) : base(services)
        {
        }

        protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            var selectedPrompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
            if (selectedPrompt == null)
            {
                return WorkflowResult.CreateError(context, "No prompt selected");
            }

            // Get the delegate from context (set by CommandPaletteForm)
            var getPlaceholdersFunc = context.GetOrDefault<Func<string, Task<string[]>>>("GetPlaceholdersFromWebApp", null);
            if (getPlaceholdersFunc == null)
            {
                return WorkflowResult.CreateError(context, "GetPlaceholdersFromWebApp delegate not available");
            }

            try
            {
                var placeholders = await getPlaceholdersFunc(selectedPrompt.Id);
                context.Set("placeholders", placeholders.ToList());
                context.Set("hasPlaceholders", placeholders.Length > 0);
                
                return WorkflowResult.CreateSuccess(context);
            }
            catch (Exception ex)
            {
                return WorkflowResult.CreateError(context, $"Error getting placeholders: {ex.Message}");
            }
        }
    }
}
