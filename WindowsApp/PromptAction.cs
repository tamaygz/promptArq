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
        Improve
    }

    public class PromptAction
    {
        public PromptActionType Type { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }

    public class PromptInfo
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Content { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool IsArchived { get; set; }
        public bool HasPlaceholders { get; set; }
    }
}
