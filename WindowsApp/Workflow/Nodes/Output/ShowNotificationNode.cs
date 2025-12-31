using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Output
{
    /// <summary>
    /// Node that shows a notification to the user.
    /// Uses the NotifyAction delegate to display toast messages.
    /// </summary>
    public class ShowNotificationNode : OutputNodeBase
    {
        public override string Name => "Show Notification";

        private string _messageKey = "message";
        private string _defaultMessage = "";

        public ShowNotificationNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("messageKey", out var key))
            {
                _messageKey = key.ToString() ?? "message";
            }
            if (config.TryGetValue("defaultMessage", out var msg))
            {
                _defaultMessage = msg.ToString() ?? "";
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            var message = context.GetOrDefault<string>(_messageKey, _defaultMessage);
            
            if (string.IsNullOrEmpty(message))
            {
                // Build message from last action
                var lastAction = context.GetOrDefault<string>("lastAction", "");
                switch (lastAction)
                {
                    case "copied":
                        message = "Copied to clipboard!";
                        break;
                    case "pasted":
                        message = "Pasted to active window!";
                        break;
                    default:
                        message = "Action completed!";
                        break;
                }
            }

            var notifyAction = context.GetOrDefault<Action<string>>("NotifyAction", null);
            notifyAction?.Invoke(message);

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }
}
