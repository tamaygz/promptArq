using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Registry;
using Serilog;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Event args for node execution events.
    /// </summary>
    public class NodeExecutedEventArgs : EventArgs
    {
        public IWorkflowNode Node { get; }
        public WorkflowResult Result { get; }

        public NodeExecutedEventArgs(IWorkflowNode node, WorkflowResult result)
        {
            Node = node;
            Result = result;
        }
    }

    /// <summary>
    /// Event args for node error events.
    /// </summary>
    public class NodeErrorEventArgs : EventArgs
    {
        public IWorkflowNode Node { get; }
        public Exception Exception { get; }

        public NodeErrorEventArgs(IWorkflowNode node, Exception exception)
        {
            Node = node;
            Exception = exception;
        }
    }

    /// <summary>
    /// Executes workflows and manages their state and navigation.
    /// </summary>
    public class WorkflowEngine
    {
        private readonly IWorkflowRegistry _registry;
        private readonly IServiceProvider _services;
        private readonly WorkflowNavigationStack _navigationStack;
        private readonly ILogger _logger;

        private Workflow? _currentWorkflow;
        private IWorkflowNode? _currentNode;
        private WorkflowContext? _currentContext;

        /// <summary>
        /// Raised when a node completes execution successfully.
        /// </summary>
        public event EventHandler<NodeExecutedEventArgs>? NodeExecuted;

        /// <summary>
        /// Raised when a node encounters an error during execution.
        /// </summary>
        public event EventHandler<NodeErrorEventArgs>? NodeError;

        /// <summary>
        /// Gets the current workflow being executed.
        /// </summary>
        public Workflow? CurrentWorkflow => _currentWorkflow;

        /// <summary>
        /// Gets the current node being executed.
        /// </summary>
        public IWorkflowNode? CurrentNode => _currentNode;

        /// <summary>
        /// Gets the current workflow context.
        /// </summary>
        public WorkflowContext? CurrentContext => _currentContext;

        public WorkflowEngine(IWorkflowRegistry registry, IServiceProvider services)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _navigationStack = new WorkflowNavigationStack();
            _logger = Log.ForContext<WorkflowEngine>();
        }

        /// <summary>
        /// Starts executing a workflow from its entry node.
        /// </summary>
        /// <param name="workflowId">The ID of the workflow to start.</param>
        /// <param name="initialContext">The initial workflow context, or null to create a new one.</param>
        /// <returns>The result of executing the entry node.</returns>
        public async Task<WorkflowResult> StartWorkflowAsync(string workflowId, WorkflowContext? initialContext = null)
        {
            _logger.Information("Starting workflow: {WorkflowId}", workflowId);

            _currentWorkflow = _registry.GetWorkflow(workflowId);
            if (_currentWorkflow == null)
            {
                var errorMsg = $"Workflow '{workflowId}' not found in registry.";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _currentContext = initialContext ?? new WorkflowContext(_services);
            _navigationStack.Clear();

            // Get the entry node
            var entryNodeDef = _currentWorkflow.GetNodeById(_currentWorkflow.EntryNodeId);
            if (entryNodeDef == null)
            {
                var errorMsg = $"Entry node '{_currentWorkflow.EntryNodeId}' not found in workflow '{workflowId}'.";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _currentNode = _registry.CreateNode(entryNodeDef.NodeType, entryNodeDef.Configuration);
            if (_currentNode == null)
            {
                var errorMsg = $"Failed to create node of type '{entryNodeDef.NodeType}'.";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            return await ExecuteNodeAsync(_currentNode, _currentContext);
        }

        /// <summary>
        /// Executes a specific node with the given context.
        /// </summary>
        /// <param name="node">The node to execute.</param>
        /// <param name="context">The workflow context.</param>
        /// <returns>The result of node execution.</returns>
        public async Task<WorkflowResult> ExecuteNodeAsync(IWorkflowNode node, WorkflowContext context)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (context == null) throw new ArgumentNullException(nameof(context));

            _logger.Information("Executing node: {NodeId} ({NodeName})", node.Id, node.Name);

            try
            {
                var result = await node.ExecuteAsync(context);

                if (result.IsSuccess)
                {
                    _logger.Information("Node {NodeId} executed successfully", node.Id);
                    NodeExecuted?.Invoke(this, new NodeExecutedEventArgs(node, result));
                }
                else
                {
                    _logger.Warning("Node {NodeId} failed: {ErrorMessage}", node.Id, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error executing node {NodeId}", node.Id);
                NodeError?.Invoke(this, new NodeErrorEventArgs(node, ex));
                return WorkflowResult.CreateError(context, $"Node execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Moves to the next node in the workflow based on the result and connections.
        /// </summary>
        /// <param name="nodeId">The ID of the node to move to.</param>
        /// <param name="context">The current workflow context.</param>
        /// <returns>The result of executing the next node.</returns>
        public async Task<WorkflowResult> MoveToNextNodeAsync(string nodeId, WorkflowContext context)
        {
            if (_currentWorkflow == null)
            {
                throw new InvalidOperationException("No workflow is currently active.");
            }

            // Save current state to navigation stack before moving
            if (_currentNode != null)
            {
                var nodeDef = _currentWorkflow.GetNodeById(_currentNode.Id);
                if (nodeDef?.AllowBackNavigation == true)
                {
                    _navigationStack.Push(_currentNode.Id, context);
                }
            }

            _logger.Information("Moving to next node: {NodeId}", nodeId);

            var nextNodeDef = _currentWorkflow.GetNodeById(nodeId);
            if (nextNodeDef == null)
            {
                var errorMsg = $"Node '{nodeId}' not found in workflow.";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _currentNode = _registry.CreateNode(nextNodeDef.NodeType, nextNodeDef.Configuration);
            if (_currentNode == null)
            {
                var errorMsg = $"Failed to create node of type '{nextNodeDef.NodeType}'.";
                _logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _currentContext = context;
            return await ExecuteNodeAsync(_currentNode, context);
        }

        /// <summary>
        /// Navigates back to the previous node in the workflow.
        /// </summary>
        /// <returns>The previous navigation frame, or null if at the start.</returns>
        public NavigationFrame? NavigateBack()
        {
            var previousFrame = _navigationStack.Pop();
            
            if (previousFrame != null)
            {
                _logger.Information("Navigating back to node: {NodeId}", previousFrame.NodeId);
                _currentContext = previousFrame.Context;
                
                // Restore the node
                if (_currentWorkflow != null)
                {
                    var nodeDef = _currentWorkflow.GetNodeById(previousFrame.NodeId);
                    if (nodeDef != null)
                    {
                        _currentNode = _registry.CreateNode(nodeDef.NodeType, nodeDef.Configuration);
                    }
                }
            }
            else
            {
                _logger.Information("Already at the start of workflow, cannot navigate back");
            }

            return previousFrame;
        }

        /// <summary>
        /// Resets the workflow engine state.
        /// </summary>
        public void Reset()
        {
            _logger.Information("Resetting workflow engine");
            _currentWorkflow = null;
            _currentNode = null;
            _currentContext = null;
            _navigationStack.Clear();
        }

        /// <summary>
        /// Gets the navigation stack for inspection.
        /// </summary>
        public WorkflowNavigationStack GetNavigationStack() => _navigationStack;
    }
}
