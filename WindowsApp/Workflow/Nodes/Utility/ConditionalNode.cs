using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Nodes.Input;

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

        private Dictionary<string, string>? _branches;

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
            if (config.TryGetValue("branches", out var branches) && branches is Dictionary<string, string> branchDict)
            {
                _branches = branchDict;
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Check if this is a branch-based condition (e.g., promptAction)
            if (_condition == "promptAction" && _branches != null)
            {
                var actionKey = context.GetOrDefault<string>("promptAction", "");
                if (!string.IsNullOrEmpty(actionKey) && _branches.TryGetValue(actionKey, out var targetNodeId))
                {
                    return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: targetNodeId));
                }
                return Task.FromResult(WorkflowResult.CreateError(context, 
                    $"No branch found for action: {actionKey}"));
            }

            // Check if this is a branch-based condition (e.g., promptAction)
            if (_condition == "selectedItem" && _branches != null)
            {
                var actionKey = context.GetOrDefault<string>("selectedItem", "");
                
                if (!string.IsNullOrEmpty(actionKey) && _branches.TryGetValue(actionKey, out var targetNodeId))
                {
                    return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: targetNodeId));
                }
                return Task.FromResult(WorkflowResult.CreateError(context, 
                    $"No branch found for action: {actionKey}"));
            }

             // Check if this is a branch-based condition (e.g., promptAction)
            if (_condition == "selectedAction" && _branches != null)
            {
                var selAction = context.GetOrDefault<PromptAction>("selectedAction", null);
                var actionKey = selAction?.Type.ToString() ?? "";
                if (!string.IsNullOrEmpty(actionKey) && _branches.TryGetValue(actionKey, out var targetNodeId))
                {
                    return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: targetNodeId));
                }
                return Task.FromResult(WorkflowResult.CreateError(context, 
                    $"No branch found for action: {actionKey}"));
            }
            
            // Traditional boolean condition evaluation
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
