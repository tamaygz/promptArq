using System.Collections.Generic;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Core.Workflows
{
    /// <summary>
    /// Repository interface for loading and saving workflow definitions.
    /// Supports JSON file storage with validation.
    /// </summary>
    public interface IWorkflowRepository
    {
        /// <summary>
        /// Loads a workflow from storage by ID.
        /// </summary>
        /// <param name="workflowId">Unique workflow identifier</param>
        /// <returns>The workflow if found, null otherwise</returns>
        Task<PromptArqApp.Workflow.Core.Workflow?> LoadAsync(string workflowId);

        /// <summary>
        /// Saves a workflow to storage.
        /// </summary>
        /// <param name="workflow">Workflow to save</param>
        Task SaveAsync(PromptArqApp.Workflow.Core.Workflow workflow);

        /// <summary>
        /// Lists all available workflows.
        /// </summary>
        /// <returns>List of all workflows</returns>
        Task<List<PromptArqApp.Workflow.Core.Workflow>> ListAsync();

        /// <summary>
        /// Deletes a workflow by ID.
        /// </summary>
        /// <param name="workflowId">Workflow ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(string workflowId);

        /// <summary>
        /// Checks if a workflow exists.
        /// </summary>
        /// <param name="workflowId">Workflow ID to check</param>
        /// <returns>True if exists</returns>
        Task<bool> ExistsAsync(string workflowId);

        /// <summary>
        /// Loads all workflows from a specific directory.
        /// </summary>
        /// <param name="directory">Directory path to scan</param>
        /// <returns>List of workflows found</returns>
        Task<List<PromptArqApp.Workflow.Core.Workflow>> LoadFromDirectoryAsync(string directory);
    }
}
