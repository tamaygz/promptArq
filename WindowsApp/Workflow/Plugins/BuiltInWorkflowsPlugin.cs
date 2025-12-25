using System;
using System.Collections.Generic;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using PromptArqApp.Workflow.Nodes.Input;
using PromptArqApp.Workflow.Nodes.Action;
using PromptArqApp.Workflow.Nodes.Utility;
using PromptArqApp.Workflow.Nodes.Output;

namespace PromptArqApp.Workflow.Plugins
{
    /// <summary>
    /// Built-in workflows plugin providing standard command palette workflows.
    /// Includes: Quick Copy, Quick Paste, Fill Placeholders, etc.
    /// </summary>
    public class BuiltInWorkflowsPlugin : IWorkflowPlugin
    {
        public string PluginId => "promptarq.builtin";
        public string Name => "PromptArq Built-in Workflows";
        public Version Version => new Version(1, 0, 0);

        public IEnumerable<Core.Workflow> GetWorkflows()
        {
            yield return CreateQuickCopyWorkflow();
            yield return CreateQuickPasteWorkflow();
            yield return CreateFillPlaceholdersWorkflow();
        }

        public IEnumerable<(string NodeType, Type NodeClass)> GetNodes()
        {
            // Input nodes
            yield return ("SearchPromptsNode", typeof(SearchPromptsNode));
            yield return ("SelectActionNode", typeof(SelectActionNode));
            yield return ("FillPlaceholderNode", typeof(FillPlaceholderNode));

            // Action nodes
            yield return ("GetPlaceholdersNode", typeof(GetPlaceholdersNode));
            yield return ("FillContentNode", typeof(FillContentNode));
            yield return ("ExecuteLLMNode", typeof(ExecuteLLMNode));
            yield return ("CopyToClipboardNode", typeof(CopyToClipboardNode));
            yield return ("PasteToActiveWindowNode", typeof(PasteToActiveWindowNode));

            // Utility nodes
            yield return ("ConditionalNode", typeof(ConditionalNode));
            yield return ("LoopNode", typeof(LoopNode));

            // Output nodes
            yield return ("CloseCommandPaletteNode", typeof(CloseCommandPaletteNode));
            yield return ("RecordHistoryNode", typeof(RecordHistoryNode));
            yield return ("ShowNotificationNode", typeof(ShowNotificationNode));
        }

        private Core.Workflow CreateQuickCopyWorkflow()
        {
            return new Core.Workflow
            {
                Id = "quick-copy",
                Name = "Quick Copy",
                Description = "Search for a prompt and copy it to clipboard",
                Icon = "📎",
                EntryNodeId = "search",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "search", NodeType = "SearchPromptsNode" },
                    new() { Id = "select-action", NodeType = "SelectActionNode" },
                    new() { 
                        Id = "check-copy", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "iscopy",
                            ["trueNodeId"] = "check-llm",
                            ["falseNodeId"] = "close"
                        }
                    },
                    new() { 
                        Id = "check-llm", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "executellm",
                            ["trueNodeId"] = "execute-llm",
                            ["falseNodeId"] = "copy"
                        }
                    },
                    new() { Id = "execute-llm", NodeType = "ExecuteLLMNode" },
                    new() { Id = "copy", NodeType = "CopyToClipboardNode" },
                    new() { Id = "record", NodeType = "RecordHistoryNode" },
                    new() { Id = "notify", NodeType = "ShowNotificationNode" },
                    new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["search"] = "select-action",
                    ["select-action"] = "check-copy",
                    ["check-copy"] = "check-llm",
                    ["check-llm"] = "copy",
                    ["execute-llm"] = "copy",
                    ["copy"] = "record",
                    ["record"] = "notify",
                    ["notify"] = "close"
                },
                Metadata = new WorkflowMetadata
                {
                    Author = "PromptArq Team",
                    Version = new Version(1, 0, 0),
                    Tags = new[] { "copy", "quick", "clipboard" }
                }
            };
        }

        private Core.Workflow CreateQuickPasteWorkflow()
        {
            return new Core.Workflow
            {
                Id = "quick-paste",
                Name = "Quick Paste",
                Description = "Search for a prompt and paste it to active window",
                Icon = "📋",
                EntryNodeId = "search",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "search", NodeType = "SearchPromptsNode" },
                    new() { Id = "select-action", NodeType = "SelectActionNode" },
                    new() { 
                        Id = "check-paste", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "ispaste",
                            ["trueNodeId"] = "check-llm",
                            ["falseNodeId"] = "close"
                        }
                    },
                    new() { 
                        Id = "check-llm", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "executellm",
                            ["trueNodeId"] = "execute-llm",
                            ["falseNodeId"] = "paste"
                        }
                    },
                    new() { Id = "execute-llm", NodeType = "ExecuteLLMNode" },
                    new() { Id = "paste", NodeType = "PasteToActiveWindowNode" },
                    new() { Id = "record", NodeType = "RecordHistoryNode" },
                    new() { Id = "notify", NodeType = "ShowNotificationNode" },
                    new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["search"] = "select-action",
                    ["select-action"] = "check-paste",
                    ["check-paste"] = "check-llm",
                    ["check-llm"] = "paste",
                    ["execute-llm"] = "paste",
                    ["paste"] = "record",
                    ["record"] = "notify",
                    ["notify"] = "close"
                },
                Metadata = new WorkflowMetadata
                {
                    Author = "PromptArq Team",
                    Version = new Version(1, 0, 0),
                    Tags = new[] { "paste", "quick", "active-window" }
                }
            };
        }

        private Core.Workflow CreateFillPlaceholdersWorkflow()
        {
            return new Core.Workflow
            {
                Id = "fill-placeholders",
                Name = "Fill Placeholders",
                Description = "Fill template variables and execute prompt",
                Icon = "📝",
                EntryNodeId = "search",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "search", NodeType = "SearchPromptsNode" },
                    new() { Id = "select-action", NodeType = "SelectActionNode" },
                    new() { 
                        Id = "check-fill", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "isfillplaceholders",
                            ["trueNodeId"] = "get-placeholders",
                            ["falseNodeId"] = "close"
                        }
                    },
                    new() { Id = "get-placeholders", NodeType = "GetPlaceholdersNode" },
                    new() { 
                        Id = "check-has-placeholders", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "hasplaceholders",
                            ["trueNodeId"] = "placeholder-loop",
                            ["falseNodeId"] = "show-output-options"
                        }
                    },
                    new() { 
                        Id = "placeholder-loop", 
                        NodeType = "LoopNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["itemsKey"] = "placeholders",
                            ["loopBodyNodeId"] = "fill-placeholder",
                            ["exitNodeId"] = "fill-content"
                        }
                    },
                    new() { Id = "fill-placeholder", NodeType = "FillPlaceholderNode" },
                    new() { Id = "fill-content", NodeType = "FillContentNode" },
                    new() { Id = "show-output-options", NodeType = "SelectActionNode" },
                    new() { 
                        Id = "check-output-action", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "ispaste",
                            ["trueNodeId"] = "check-llm-output",
                            ["falseNodeId"] = "check-copy-output"
                        }
                    },
                    new() { 
                        Id = "check-llm-output", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "executellm",
                            ["trueNodeId"] = "execute-llm-paste",
                            ["falseNodeId"] = "paste"
                        }
                    },
                    new() { 
                        Id = "check-copy-output", 
                        NodeType = "ConditionalNode",
                        Configuration = new Dictionary<string, object>
                        {
                            ["condition"] = "iscopy",
                            ["trueNodeId"] = "copy",
                            ["falseNodeId"] = "close"
                        }
                    },
                    new() { Id = "execute-llm-paste", NodeType = "ExecuteLLMNode" },
                    new() { Id = "paste", NodeType = "PasteToActiveWindowNode" },
                    new() { Id = "copy", NodeType = "CopyToClipboardNode" },
                    new() { Id = "record", NodeType = "RecordHistoryNode" },
                    new() { Id = "notify", NodeType = "ShowNotificationNode" },
                    new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["search"] = "select-action",
                    ["select-action"] = "check-fill",
                    ["check-fill"] = "get-placeholders",
                    ["get-placeholders"] = "check-has-placeholders",
                    ["check-has-placeholders"] = "placeholder-loop",
                    ["placeholder-loop"] = "fill-placeholder",
                    ["fill-placeholder"] = "placeholder-loop", // Loop back
                    ["fill-content"] = "show-output-options",
                    ["show-output-options"] = "check-output-action",
                    ["check-output-action"] = "check-llm-output",
                    ["check-llm-output"] = "execute-llm-paste",
                    ["check-copy-output"] = "copy",
                    ["execute-llm-paste"] = "paste",
                    ["paste"] = "record",
                    ["copy"] = "record",
                    ["record"] = "notify",
                    ["notify"] = "close"
                },
                Metadata = new WorkflowMetadata
                {
                    Author = "PromptArq Team",
                    Version = new Version(1, 0, 0),
                    Tags = new[] { "placeholders", "template", "fill" },
                    RequiredServices = new[] { "GetPlaceholdersFromWebApp", "FillContentInWebApp" }
                }
            };
        }
    }
}
