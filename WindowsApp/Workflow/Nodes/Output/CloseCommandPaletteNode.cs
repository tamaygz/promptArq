using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Output
{
    /// <summary>
    /// Node that closes the command palette form.
    /// Signals completion of the workflow.
    /// </summary>
    public class CloseCommandPaletteNode : OutputNodeBase
    {
        public override string Name => "Close Command Palette";

        public CloseCommandPaletteNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Set a flag to close the form
            context.Set("closePalette", true);
            
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }
}
