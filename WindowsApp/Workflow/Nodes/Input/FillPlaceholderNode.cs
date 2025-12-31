using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Input
{
    /// <summary>
    /// Node that allows filling a single placeholder with value suggestions from history.
    /// Displays recent values for the placeholder and allows manual entry.
    /// </summary>
    public class FillPlaceholderNode : InputNodeBase
    {
        public override string Name => "Fill Placeholder";
        public override NodeUIType UIType => NodeUIType.TextInput;
        public override string HintText => "Fill placeholder value  |  Use arrow keys to select suggestions or type your value";

        private const string SuggestionPrefix = "💡 ";
        private const string SuggestionSeparator = "─────── Recent Values ───────";

        public FillPlaceholderNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Get current placeholder info
            var currentPlaceholder = context.GetOrDefault<string>("currentPlaceholder", "");
            if (string.IsNullOrEmpty(currentPlaceholder))
            {
                return Task.FromResult(WorkflowResult.CreateError(context, "No placeholder specified"));
            }

            // Get the entered value from UI
            var enteredValue = context.GetOrDefault<string>("userInput", "");
            
            // Store the value
            if (!string.IsNullOrEmpty(enteredValue))
            {
                // Get or create placeholder values dictionary
                var placeholderValues = context.GetOrDefault<Dictionary<string, string>>("placeholderValues", new Dictionary<string, string>());
                placeholderValues[currentPlaceholder] = enteredValue;
                context.Set("placeholderValues", placeholderValues);

                // Record in history
                var history = context.Services.GetService(typeof(PromptHistory)) as PromptHistory;
                history?.RecordPlaceholderValue(currentPlaceholder, enteredValue);

                // Remember for next placeholder (to exclude from suggestions)
                context.Set("lastEnteredPlaceholderValue", enteredValue);
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            var currentPlaceholder = context.GetOrDefault<string>("currentPlaceholder", "");
            var settings = context.Services.GetService(typeof(AppSettings)) as AppSettings;
            var history = context.Services.GetService(typeof(PromptHistory)) as PromptHistory;

            if (settings?.ShowLastUsedPlaceholderValues == true && history != null)
            {
                var lastValue = context.GetOrDefault<string>("lastEnteredPlaceholderValue", "");
                var suggestions = history.GetPlaceholderValueSuggestions(currentPlaceholder, lastValue);
                
                if (suggestions.Count > 0)
                {
                    yield return SuggestionSeparator;
                    foreach (var suggestion in suggestions)
                    {
                        yield return $"{SuggestionPrefix}{suggestion}";
                    }
                }
                else
                {
                    yield return $"Enter value for: {currentPlaceholder}";
                }
            }
            else
            {
                yield return $"Enter value for: {currentPlaceholder}";
            }
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
