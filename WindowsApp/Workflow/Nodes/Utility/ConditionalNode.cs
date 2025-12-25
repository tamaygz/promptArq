using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Utility
{
    /// <summary>
    /// Node that branches to different next nodes based on a condition.
    /// Evaluates a simple expression or checks for key existence in context.
    /// </summary>
    public class ConditionalNode : UtilityNodeBase
    {
        public override string Name => "Conditional";

        private string _condition = "";
        private string _trueNodeId = "";
        private string _falseNodeId = "";

        public ConditionalNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("condition", out var condition))
            {
                _condition = condition.ToString() ?? "";
            }
            if (config.TryGetValue("trueNodeId", out var trueId))
            {
                _trueNodeId = trueId.ToString() ?? "";
            }
            if (config.TryGetValue("falseNodeId", out var falseId))
            {
                _falseNodeId = falseId.ToString() ?? "";
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            bool conditionResult = EvaluateCondition(context);
            
            var nextNodeId = conditionResult ? _trueNodeId : _falseNodeId;
            
            if (string.IsNullOrEmpty(nextNodeId))
            {
                return Task.FromResult(WorkflowResult.CreateError(context, 
                    $"No next node specified for condition result: {conditionResult}"));
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: nextNodeId));
        }

        private bool EvaluateCondition(WorkflowContext context)
        {
            // Simple condition evaluations
            switch (_condition.ToLowerInvariant())
            {
                case "hasplaceholders":
                    return context.GetOrDefault<bool>("hasPlaceholders", false);
                
                case "executellm":
                    var prompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
                    return prompt?.ExecuteLLM ?? false;
                
                case "isfillplaceholders":
                    var action = context.GetOrDefault<PromptAction>("selectedAction", null);
                    return action?.Type == PromptActionType.FillPlaceholders;
                
                case "ispaste":
                    action = context.GetOrDefault<PromptAction>("selectedAction", null);
                    return action?.Type == PromptActionType.Paste;
                
                case "iscopy":
                    action = context.GetOrDefault<PromptAction>("selectedAction", null);
                    return action?.Type == PromptActionType.Copy;
                
                case "executeonetimeprompt":
                    action = context.GetOrDefault<PromptAction>("selectedAction", null);
                    return action?.Type == PromptActionType.ExecuteOneTimePrompt;
                
                case "editgeneratedprompt":
                    action = context.GetOrDefault<PromptAction>("selectedAction", null);
                    return action?.Type == PromptActionType.EditGeneratedPrompt;
                
                case "selectedaction":
                    return context.Has("selectedAction");
                
                default:
                    // Check if it's a simple key existence check
                    return context.Has(_condition);
            }
        }
    }
}
