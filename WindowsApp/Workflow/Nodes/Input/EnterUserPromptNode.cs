using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Input
{
    /// <summary>
    /// Node that prompts user to enter their prompt text for one-time execution.
    /// </summary>
    public class EnterUserPromptNode : InputNodeBase
    {
        public override string Name => "Enter User Prompt";
        public override NodeUIType UIType => NodeUIType.TextInput;
        public override string HintText => "Enter your prompt  |  Press Enter to continue";

        public EnterUserPromptNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Get user input
            var userPrompt = context.GetOrDefault<string>("userInput", "");
            
            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                context.Set("userPrompt", userPrompt.Trim());
                return Task.FromResult(WorkflowResult.CreateSuccess(context));
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            yield return "Type your prompt and press Enter...";
        }

        public override string GetDisplayText(object item)
        {
            return item?.ToString() ?? "";
        }

        public override string GetSecondaryText(object item)
        {
            return "";
        }

        public override string GetIcon(object item)
        {
            return "";
        }

        public override Color? GetItemColor(object item)
        {
            return null;
        }
    }
}
