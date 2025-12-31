using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Input
{
    /// <summary>
    /// Node that displays system prompts for selection in One-Time Prompt workflow.
    /// </summary>
    public class SelectSystemPromptNode : InputNodeBase
    {
        public override string Name => "Select System Prompt";
        public override NodeUIType UIType => NodeUIType.ItemList;
        public override string HintText => "Select a system prompt  |  Press ESC to go back";

        private List<SystemPromptInfo> _systemPrompts = new();

        public SelectSystemPromptNode(IServiceProvider services) : base(services)
        {
        }

        public override async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Get system prompts from web app if not already loaded
            if (!context.Has("systemPrompts"))
            {
                var getSystemPromptsFunc = context.GetOrDefault<Func<Task<List<SystemPromptInfo>>>>("GetSystemPromptsFromWebApp", null);
                if (getSystemPromptsFunc != null)
                {
                    try
                    {
                        _systemPrompts = await getSystemPromptsFunc();
                        context.Set("systemPrompts", _systemPrompts);
                    }
                    catch (Exception ex)
                    {
                        return WorkflowResult.CreateError(context, $"Error loading system prompts: {ex.Message}");
                    }
                }
                else
                {
                    return WorkflowResult.CreateError(context, "GetSystemPromptsFromWebApp delegate not available");
                }
            }
            else
            {
                _systemPrompts = context.Get<List<SystemPromptInfo>>("systemPrompts");
            }

            // If a prompt was selected, store it
            if (context.Has("selectedItem"))
            {
                var selectedItem = context.Get<object>("selectedItem");
                if (selectedItem is SystemPromptInfo systemPrompt)
                {
                    context.Set("selectedSystemPrompt", systemPrompt);
                    return WorkflowResult.CreateSuccess(context);
                }
            }

            return WorkflowResult.CreateSuccess(context);
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            _systemPrompts = context.GetOrDefault<List<SystemPromptInfo>>("systemPrompts", new List<SystemPromptInfo>());
            return _systemPrompts;
        }

        public override string GetDisplayText(object item)
        {
            if (item is SystemPromptInfo prompt)
                return prompt.Name;
            return item?.ToString() ?? "";
        }

        public override string GetSecondaryText(object item)
        {
            if (item is SystemPromptInfo prompt)
                return prompt.Description;
            return "";
        }

        public override string GetIcon(object item)
        {
            return "🤖";
        }

        public override Color? GetItemColor(object item)
        {
            return null;
        }
    }
}
