using System.Collections.Generic;
using Xunit;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Tests
{
    public class WorkflowValidatorTests
    {
        private readonly WorkflowValidator _validator;

        public WorkflowValidatorTests()
        {
            _validator = new WorkflowValidator();
        }

        [Fact]
        public void Validate_NullWorkflow_ReturnsError()
        {
            var errors = _validator.Validate(null!);
            
            Assert.Single(errors);
            Assert.Contains("cannot be null", errors[0]);
        }

        [Fact]
        public void Validate_EmptyId_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("must have an Id"));
        }

        [Fact]
        public void Validate_EmptyName_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("must have a Name"));
        }

        [Fact]
        public void Validate_NoNodes_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>()
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("must have at least one node"));
        }

        [Fact]
        public void Validate_InvalidEntryNodeId_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "nonexistent",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("does not match any node"));
        }

        [Fact]
        public void Validate_DuplicateNodeIds_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" },
                    new() { Id = "node1", NodeType = "TestNode" }
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("Duplicate node Id"));
        }

        [Fact]
        public void Validate_InvalidConnectionSource_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["nonexistent"] = "node1"
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("Connection source") && e.Contains("does not match any node"));
        }

        [Fact]
        public void Validate_InvalidConnectionTarget_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["node1"] = "nonexistent"
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("Connection target") && e.Contains("does not match any node"));
        }

        [Fact]
        public void Validate_CyclicConnections_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" },
                    new() { Id = "node2", NodeType = "TestNode" },
                    new() { Id = "node3", NodeType = "TestNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["node1"] = "node2",
                    ["node2"] = "node3",
                    ["node3"] = "node1" // Creates a cycle
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("contains cycles"));
        }

        [Fact]
        public void Validate_OrphanedNodes_ReturnsError()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" },
                    new() { Id = "node2", NodeType = "TestNode" }, // Orphaned - not connected
                    new() { Id = "node3", NodeType = "TestNode" }  // Orphaned - not connected
                },
                Connections = new Dictionary<string, string>
                {
                    ["node1"] = null! // Node1 doesn't connect to anything
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Contains(errors, e => e.Contains("orphaned nodes"));
        }

        [Fact]
        public void Validate_ValidWorkflow_ReturnsNoErrors()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" },
                    new() { Id = "node2", NodeType = "TestNode" },
                    new() { Id = "node3", NodeType = "TestNode" }
                },
                Connections = new Dictionary<string, string>
                {
                    ["node1"] = "node2",
                    ["node2"] = "node3"
                }
            };

            var errors = _validator.Validate(workflow);
            
            Assert.Empty(errors);
        }

        [Fact]
        public void IsValid_ValidWorkflow_ReturnsTrue()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>
                {
                    new() { Id = "node1", NodeType = "TestNode" }
                }
            };

            Assert.True(_validator.IsValid(workflow));
        }

        [Fact]
        public void IsValid_InvalidWorkflow_ReturnsFalse()
        {
            var workflow = new PromptArqApp.Workflow.Core.Workflow
            {
                Id = "",
                Name = "Test",
                EntryNodeId = "node1",
                Nodes = new List<WorkflowNodeDefinition>()
            };

            Assert.False(_validator.IsValid(workflow));
        }
    }
}
