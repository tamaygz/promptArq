using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Utility
{
    /// <summary>
    /// Node that iterates through a list of items, executing a sub-workflow for each item.
    /// Tracks current iteration and provides loop control.
    /// </summary>
    public class LoopNode : UtilityNodeBase
    {
        public override string Name => "Loop";

        private string _itemsKey = "items";
        private string _loopBodyNodeId = "";
        private string _exitNodeId = "";

        public LoopNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("itemsKey", out var key))
            {
                _itemsKey = key.ToString() ?? "items";
            }
            if (config.TryGetValue("loopBodyNodeId", out var bodyId))
            {
                _loopBodyNodeId = bodyId.ToString() ?? "";
            }
            if (config.TryGetValue("exitNodeId", out var exitId))
            {
                _exitNodeId = exitId.ToString() ?? "";
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // For placeholder filling loop specifically
            if (_itemsKey == "placeholders")
            {
                return HandlePlaceholderLoop(context);
            }

            // Generic loop handling
            if (!context.Has(_itemsKey))
            {
                return Task.FromResult(WorkflowResult.CreateError(context, $"Items key '{_itemsKey}' not found in context"));
            }

            var items = context.Get<List<object>>(_itemsKey);
            var currentIndex = context.GetOrDefault<int>("currentLoopIndex", 0);

            if (currentIndex < items.Count)
            {
                // Set current item and continue loop
                context.Set("currentLoopItem", items[currentIndex]);
                context.Set("currentLoopIndex", currentIndex + 1);
                return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: _loopBodyNodeId));
            }
            else
            {
                // Loop complete, exit
                context.Remove("currentLoopIndex");
                context.Remove("currentLoopItem");
                return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: _exitNodeId));
            }
        }

        private Task<WorkflowResult> HandlePlaceholderLoop(WorkflowContext context)
        {
            var placeholders = context.GetOrDefault<List<string>>("placeholders", new List<string>());
            var currentIndex = context.GetOrDefault<int>("currentPlaceholderIndex", 0);

            if (currentIndex < placeholders.Count)
            {
                // Set current placeholder and continue loop
                var currentPlaceholder = placeholders[currentIndex];
                context.Set("currentPlaceholder", currentPlaceholder);
                context.Set("currentPlaceholderIndex", currentIndex + 1);
                
                // Get previous value if going back
                var placeholderValues = context.GetOrDefault<Dictionary<string, string>>("placeholderValues", new Dictionary<string, string>());
                if (placeholderValues.TryGetValue(currentPlaceholder, out var previousValue))
                {
                    context.Set("userInput", previousValue);
                }
                
                return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: _loopBodyNodeId));
            }
            else
            {
                // All placeholders filled
                context.Remove("currentPlaceholderIndex");
                context.Remove("currentPlaceholder");
                return Task.FromResult(WorkflowResult.CreateSuccess(context, nextNodeId: _exitNodeId));
            }
        }
    }
}
