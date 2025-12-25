using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PromptArqApp.Core.Actions;

/// <summary>
/// Central registry for managing universal actions
/// </summary>
public class ActionRegistry
{
    private readonly Dictionary<string, IUniversalAction> _actions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Register an action
    /// </summary>
    /// <param name="action">Action to register</param>
    public void RegisterAction(IUniversalAction action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            _actions[action.Id] = action;
        }
    }

    /// <summary>
    /// Register multiple actions
    /// </summary>
    /// <param name="actions">Actions to register</param>
    public void RegisterActions(IEnumerable<IUniversalAction> actions)
    {
        foreach (var action in actions)
        {
            RegisterAction(action);
        }
    }

    /// <summary>
    /// Unregister an action
    /// </summary>
    /// <param name="actionId">ID of action to unregister</param>
    public void UnregisterAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return;

        lock (_lock)
        {
            _actions.Remove(actionId);
        }
    }

    /// <summary>
    /// Get all registered actions
    /// </summary>
    /// <returns>Collection of all actions</returns>
    public IReadOnlyList<IUniversalAction> GetAllActions()
    {
        lock (_lock)
        {
            return _actions.Values.ToList();
        }
    }

    /// <summary>
    /// Find an action by ID
    /// </summary>
    /// <param name="actionId">Action ID to find</param>
    /// <returns>Action if found, null otherwise</returns>
    public IUniversalAction? FindAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return null;

        lock (_lock)
        {
            return _actions.TryGetValue(actionId, out var action) ? action : null;
        }
    }

    /// <summary>
    /// Get actions that can handle the given content
    /// </summary>
    /// <param name="content">Content to check</param>
    /// <param name="contentType">Type of content</param>
    /// <returns>Actions that can handle the content</returns>
    public IEnumerable<IUniversalAction> GetActionsForContent(string content, ContentType contentType)
    {
        if (string.IsNullOrEmpty(content))
            return Enumerable.Empty<IUniversalAction>();

        lock (_lock)
        {
            return _actions.Values
                .Where(a => a.SupportedContentTypes.Contains(contentType) &&
                           a.CanHandle(content, contentType))
                .ToList();
        }
    }

    /// <summary>
    /// Execute an action by ID
    /// </summary>
    /// <param name="actionId">ID of action to execute</param>
    /// <param name="context">Action context</param>
    /// <returns>Action result</returns>
    public async Task<ActionResult> ExecuteActionAsync(string actionId, ActionContext context)
    {
        var action = FindAction(actionId);
        if (action == null)
        {
            return ActionResult.Failed($"Action '{actionId}' not found");
        }

        try
        {
            return await action.ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Action execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get actions by content type
    /// </summary>
    /// <param name="contentType">Content type to filter by</param>
    /// <returns>Actions supporting the content type</returns>
    public IEnumerable<IUniversalAction> GetActionsByContentType(ContentType contentType)
    {
        lock (_lock)
        {
            return _actions.Values
                .Where(a => a.SupportedContentTypes.Contains(contentType))
                .ToList();
        }
    }
}
