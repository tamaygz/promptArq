using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Validates workflow definitions to detect structural issues.
    /// </summary>
    public class WorkflowValidator
    {
        /// <summary>
        /// Validates a workflow and returns a list of validation errors.
        /// </summary>
        /// <param name="workflow">The workflow to validate</param>
        /// <returns>List of validation error messages. Empty list if workflow is valid.</returns>
        public List<string> Validate(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            var errors = new List<string>();

            if (workflow == null)
            {
                errors.Add("Workflow cannot be null");
                return errors;
            }

            // Validate basic properties
            if (string.IsNullOrWhiteSpace(workflow.Id))
                errors.Add("Workflow must have an Id");

            if (string.IsNullOrWhiteSpace(workflow.Name))
                errors.Add("Workflow must have a Name");

            if (workflow.Nodes == null || workflow.Nodes.Count == 0)
            {
                errors.Add("Workflow must have at least one node");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(workflow.EntryNodeId))
            {
                errors.Add("Workflow must have an EntryNodeId");
            }
            else if (!workflow.Nodes.Any(n => n.Id == workflow.EntryNodeId))
            {
                errors.Add($"EntryNodeId '{workflow.EntryNodeId}' does not match any node in the workflow");
            }

            // Validate nodes
            var nodeIds = new HashSet<string>();
            foreach (var node in workflow.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    errors.Add("All nodes must have an Id");
                    continue;
                }

                if (nodeIds.Contains(node.Id))
                {
                    errors.Add($"Duplicate node Id: '{node.Id}'");
                }
                else
                {
                    nodeIds.Add(node.Id);
                }

                if (string.IsNullOrWhiteSpace(node.NodeType))
                {
                    errors.Add($"Node '{node.Id}' must have a NodeType");
                }
            }

            // Validate connections
            if (workflow.Connections != null)
            {
                foreach (var connection in workflow.Connections)
                {
                    if (!nodeIds.Contains(connection.Key))
                    {
                        errors.Add($"Connection source '{connection.Key}' does not match any node");
                    }

                    if (!string.IsNullOrWhiteSpace(connection.Value) && !nodeIds.Contains(connection.Value))
                    {
                        errors.Add($"Connection target '{connection.Value}' does not match any node");
                    }
                }
            }

            // Detect cycles
            var cycles = DetectCycles(workflow);
            if (cycles.Count > 0)
            {
                errors.Add($"Workflow contains cycles: {string.Join(", ", cycles)}");
            }

            // Detect orphaned nodes (nodes with no incoming connections and not the entry node)
            var orphanedNodes = DetectOrphanedNodes(workflow);
            if (orphanedNodes.Count > 0)
            {
                errors.Add($"Workflow contains orphaned nodes (unreachable): {string.Join(", ", orphanedNodes)}");
            }

            return errors;
        }

        /// <summary>
        /// Detects cycles in the workflow connections.
        /// </summary>
        private List<string> DetectCycles(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            var cycles = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in workflow.Nodes.Where(n => !visited.Contains(n.Id)))
            {
                if (DetectCycleDFS(node.Id, workflow, visited, recursionStack, new List<string>(), cycles))
                {
                    break; // Found a cycle
                }
            }

            return cycles;
        }

        private bool DetectCycleDFS(string nodeId, PromptArqApp.Workflow.Core.Workflow workflow, 
            HashSet<string> visited, HashSet<string> recursionStack, List<string> path, List<string> cycles)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);
            path.Add(nodeId);

            // Get next node from connections
            if (workflow.Connections != null && workflow.Connections.TryGetValue(nodeId, out var nextNodeId) && !string.IsNullOrWhiteSpace(nextNodeId))
            {
                if (!visited.Contains(nextNodeId))
                {
                    if (DetectCycleDFS(nextNodeId, workflow, visited, recursionStack, path, cycles))
                        return true;
                }
                else if (recursionStack.Contains(nextNodeId))
                {
                    // Found a cycle
                    var cycleStart = path.IndexOf(nextNodeId);
                    var cycle = string.Join(" → ", path.Skip(cycleStart).Concat(new[] { nextNodeId }));
                    cycles.Add(cycle);
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            recursionStack.Remove(nodeId);
            return false;
        }

        /// <summary>
        /// Detects orphaned nodes that cannot be reached from the entry node.
        /// </summary>
        private List<string> DetectOrphanedNodes(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            var reachable = new HashSet<string>();
            var toVisit = new Queue<string>();

            // Start from entry node
            if (!string.IsNullOrWhiteSpace(workflow.EntryNodeId))
            {
                toVisit.Enqueue(workflow.EntryNodeId);
                reachable.Add(workflow.EntryNodeId);
            }

            // BFS to find all reachable nodes
            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();

                if (workflow.Connections != null
                    && workflow.Connections.TryGetValue(current, out var nextNodeId)
                    && !string.IsNullOrWhiteSpace(nextNodeId)
                    && !reachable.Contains(nextNodeId))
                {
                    reachable.Add(nextNodeId);
                    toVisit.Enqueue(nextNodeId);
                }
            }

            // Find orphaned nodes
            var orphaned = new List<string>();
            foreach (var node in workflow.Nodes.Where(n => !reachable.Contains(n.Id)))
            {
                orphaned.Add(node.Id);
            }

            return orphaned;
        }

        /// <summary>
        /// Checks if a workflow is valid.
        /// </summary>
        /// <param name="workflow">The workflow to check</param>
        /// <returns>True if workflow is valid, false otherwise</returns>
        public bool IsValid(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            return Validate(workflow).Count == 0;
        }

        /// <summary>
        /// Validates a workflow and throws an exception if invalid.
        /// </summary>
        /// <param name="workflow">The workflow to validate</param>
        /// <exception cref="InvalidOperationException">Thrown if workflow is invalid</exception>
        public void ValidateAndThrow(PromptArqApp.Workflow.Core.Workflow workflow)
        {
            var errors = Validate(workflow);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Workflow validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}")));
            }
        }
    }
}
