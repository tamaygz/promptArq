using System;

namespace PromptArqApp
{
    public enum PromptActionType
    {
        Execute,
        Paste,
        Copy,
        FillPlaceholders,
        Export,
        Share,
        OpenInEditor,
        Archive,
        Restore,
        Improve,
        CoAuthorOneTimePrompt,
        ExecuteOneTimePrompt,
        EditGeneratedPrompt
    }

    public class PromptAction
    {
        public PromptActionType Type { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        override public string ToString()
        {
            return $"PromptAction: Name: {Name} (Type: {Type}) - Descr: {Description} [Icon: {Icon}, Enabled: {IsEnabled}]";
        }
    }

    public class PromptInfo
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Content { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string[] Placeholders { get; set; } = Array.Empty<string>();
        public bool IsArchived { get; set; }
        public bool HasPlaceholders { get; set; }
        public bool ExecuteLLM { get; set; }
    }

    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? Error { get; set; }
    }

    public class SystemPromptInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Content { get; set; } = "";
        public string ScopeType { get; set; } = "";
        public string? ScopeId { get; set; }
        public int Priority { get; set; }
        public string CreatedBy { get; set; } = "";
    }
}
