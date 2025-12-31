using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Output
{
    /// <summary>
    /// Node that records prompt usage in history.
    /// Tracks which prompts are used most frequently.
    /// </summary>
    public class RecordHistoryNode : OutputNodeBase
    {
        public override string Name => "Record History";

        public RecordHistoryNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            var selectedPrompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
            if (selectedPrompt == null)
            {
                // Not an error, just skip recording
                return Task.FromResult(WorkflowResult.CreateSuccess(context));
            }

            var history = context.Services.GetService(typeof(PromptHistory)) as PromptHistory;
            if (history != null)
            {
                history.RecordPromptUsage(selectedPrompt.Id, selectedPrompt.Title);
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }
}
