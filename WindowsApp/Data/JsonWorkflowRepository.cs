using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PromptArqApp.Core.Workflows;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Data
{
    /// <summary>
    /// JSON file-based workflow repository implementation.
    /// Stores workflows as .workflow.json files in the Workflows directory.
    /// </summary>
    public class JsonWorkflowRepository : IWorkflowRepository
    {
        private readonly string _baseDirectory;
        private readonly WorkflowValidator _validator;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonWorkflowRepository(string baseDirectory)
        {
            _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            _validator = new WorkflowValidator();

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            // Ensure base directory exists
            EnsureDirectoryStructure();
        }

        private void EnsureDirectoryStructure()
        {
            Directory.CreateDirectory(_baseDirectory);
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "BuiltIn"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "User"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "Plugins"));
        }

        public async Task<PromptArqApp.Workflow.Core.Workflow?> LoadAsync(string workflowId)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));

            var filePath = FindWorkflowFile(workflowId);
            if (filePath == null || !File.Exists(filePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var workflow = JsonSerializer.Deserialize<PromptArqApp.Workflow.Core.Workflow>(json, _jsonOptions);

                if (workflow != null)
                {
                    // Validate workflow
                    var errors = _validator.Validate(workflow);
                    if (errors.Any())
                    {
                        throw new InvalidOperationException(
                            $"Workflow '{workflowId}' validation failed: {string.Join(", ", errors)}");
                    }
                }

                return workflow;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load workflow '{workflowId}' from {filePath}", ex);
            }
        }

        public async Task SaveAsync(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            // Validate before saving
            var errors = _validator.Validate(workflow);
            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot save invalid workflow: {string.Join(", ", errors)}");
            }

            var fileName = $"{workflow.Id}.workflow.json";
            var filePath = Path.Combine(_baseDirectory, "User", fileName);

            try
            {
                var json = JsonSerializer.Serialize(workflow, _jsonOptions);
                
                // Atomic write: write to temp file, then move
                var tempPath = filePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json);
                File.Move(tempPath, filePath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save workflow '{workflow.Id}'", ex);
            }
        }

        public async Task<List<PromptArqApp.Workflow.Core.Workflow>> ListAsync()
        {
            var workflows = new List<PromptArqApp.Workflow.Core.Workflow>();

            // Load from all subdirectories
            foreach (var subDir in new[] { "BuiltIn", "User" })
            {
                var dirPath = Path.Combine(_baseDirectory, subDir);
                if (Directory.Exists(dirPath))
                {
                    workflows.AddRange(await LoadFromDirectoryAsync(dirPath));
                }
            }

            // Load from plugin directories
            var pluginsDir = Path.Combine(_baseDirectory, "Plugins");
            if (Directory.Exists(pluginsDir))
            {
                foreach (var pluginDir in Directory.GetDirectories(pluginsDir))
                {
                    workflows.AddRange(await LoadFromDirectoryAsync(pluginDir));
                }
            }

            return workflows;
        }

        public async Task<bool> DeleteAsync(string workflowId)
        {
            var filePath = FindWorkflowFile(workflowId);
            if (filePath == null || !File.Exists(filePath))
                return false;

            // Don't allow deleting built-in workflows
            if (filePath.Contains(Path.Combine(_baseDirectory, "BuiltIn")))
            {
                throw new InvalidOperationException("Cannot delete built-in workflows");
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete workflow '{workflowId}'", ex);
            }
        }

        public Task<bool> ExistsAsync(string workflowId)
        {
            var filePath = FindWorkflowFile(workflowId);
            return Task.FromResult(filePath != null && File.Exists(filePath));
        }

        public async Task<List<PromptArqApp.Workflow.Core.Workflow>> LoadFromDirectoryAsync(string directory)
        {
            var workflows = new List<PromptArqApp.Workflow.Core.Workflow>();

            if (!Directory.Exists(directory))
                return workflows;

            var workflowFiles = Directory.GetFiles(directory, "*.workflow.json", SearchOption.TopDirectoryOnly);

            foreach (var file in workflowFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var workflow = JsonSerializer.Deserialize<PromptArqApp.Workflow.Core.Workflow>(json, _jsonOptions);

                    if (workflow != null)
                    {
                        // Validate
                        var errors = _validator.Validate(workflow);
                        if (errors.Any())
                        {
                            Console.WriteLine($"Workflow validation failed for {file}: {string.Join(", ", errors)}");
                            continue;
                        }

                        workflows.Add(workflow);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load workflow from {file}: {ex.Message}");
                }
            }

            return workflows;
        }

        private string? FindWorkflowFile(string workflowId)
        {
            var fileName = $"{workflowId}.workflow.json";

            // Search in order: BuiltIn, User, Plugins
            var searchPaths = new[]
            {
                Path.Combine(_baseDirectory, "BuiltIn", fileName),
                Path.Combine(_baseDirectory, "User", fileName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Search in plugin directories
            var pluginsDir = Path.Combine(_baseDirectory, "Plugins");
            if (Directory.Exists(pluginsDir))
            {
                foreach (var pluginDir in Directory.GetDirectories(pluginsDir))
                {
                    var path = Path.Combine(pluginDir, fileName);
                    if (File.Exists(path))
                        return path;
                }
            }

            return null;
        }
    }
}
