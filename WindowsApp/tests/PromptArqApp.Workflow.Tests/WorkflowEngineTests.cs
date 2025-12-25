using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using Serilog;
using Xunit;

namespace PromptArqApp.Workflow.Tests
{
    public class WorkflowEngineTests
    {
        private readonly IServiceProvider _services;
        private readonly IWorkflowRegistry _registry;

        public WorkflowEngineTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(new LoggerConfiguration().CreateLogger());
            _services = services.BuildServiceProvider();

            _registry = new WorkflowRegistry(_services);
            
            // Register test nodes
            _registry.RegisterNode("TestInputNode", typeof(TestInputNode));
            _registry.RegisterNode("TestActionNode", typeof(TestActionNode));
            _registry.RegisterNode("TestOutputNode", typeof(TestOutputNode));
        }

        [Fact]
        public async Task Engine_ShouldExecuteSimpleWorkflow()
        {
            // Arrange
            var workflow = new Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                EntryNodeId = "input",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { Id = "input", NodeType = "TestInputNode" },
                    new() { Id = "action", NodeType = "TestActionNode" },
                    new() { Id = "output", NodeType = "TestOutputNode" }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["input"] = "action",
                    ["action"] = "output"
                }
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            // Act
            var result = await engine.StartWorkflowAsync("test-workflow");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Context);
            Assert.True(result.Context.Has("testValue"));
            Assert.Equal("test-value-123", result.Context.Get<string>("testValue"));
        }

        [Fact]
        public async Task Engine_ShouldExecuteMultiStepWorkflow()
        {
            // Arrange
            var workflow = new Core.Workflow
            {
                Id = "multi-step-workflow",
                Name = "Multi-Step Workflow",
                EntryNodeId = "input",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { Id = "input", NodeType = "TestInputNode" },
                    new() { Id = "action", NodeType = "TestActionNode" },
                    new() { Id = "output", NodeType = "TestOutputNode" }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["input"] = "action",
                    ["action"] = "output"
                }
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            // Act - Start workflow
            var result1 = await engine.StartWorkflowAsync("multi-step-workflow");
            Assert.True(result1.IsSuccess);

            // Move to next node
            var nextNodeId = workflow.GetNextNodeId("input");
            Assert.NotNull(nextNodeId);
            var result2 = await engine.MoveToNextNodeAsync(nextNodeId!, result1.Context);
            Assert.True(result2.IsSuccess);
            Assert.True(result2.Context.Has("transformedValue"));
            Assert.Equal("TEST-VALUE-123", result2.Context.Get<string>("transformedValue"));

            // Move to output node
            nextNodeId = workflow.GetNextNodeId("action");
            Assert.NotNull(nextNodeId);
            var result3 = await engine.MoveToNextNodeAsync(nextNodeId!, result2.Context);
            Assert.True(result3.IsSuccess);
            Assert.Equal("TEST-VALUE-123", result3.Output);
        }

        [Fact]
        public async Task Engine_ShouldSupportBackNavigation()
        {
            // Arrange
            var workflow = new Core.Workflow
            {
                Id = "nav-workflow",
                Name = "Navigation Test Workflow",
                EntryNodeId = "input",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { Id = "input", NodeType = "TestInputNode" },
                    new() { Id = "action", NodeType = "TestActionNode" }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["input"] = "action"
                }
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            // Act - Execute first node
            var result1 = await engine.StartWorkflowAsync("nav-workflow");
            Assert.True(result1.IsSuccess);

            // Move to second node
            var nextNodeId = workflow.GetNextNodeId("input");
            var result2 = await engine.MoveToNextNodeAsync(nextNodeId!, result1.Context);
            Assert.True(result2.IsSuccess);

            // Navigate back
            var previousFrame = engine.NavigateBack();

            // Assert
            Assert.NotNull(previousFrame);
            Assert.Equal("input", previousFrame.NodeId);
            Assert.NotNull(previousFrame.Context);
            Assert.True(previousFrame.Context.Has("testValue"));
        }

        [Fact]
        public async Task Engine_ShouldHandleNodeErrors()
        {
            // Arrange - Create a workflow with a node that will fail
            var workflow = new Core.Workflow
            {
                Id = "error-workflow",
                Name = "Error Test Workflow",
                EntryNodeId = "action",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { 
                        Id = "action", 
                        NodeType = "TestActionNode",
                        Configuration = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["inputKey"] = "nonexistent" // This will cause an error
                        }
                    }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>()
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            // Act
            var result = await engine.StartWorkflowAsync("error-workflow");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public void Engine_ShouldRaiseNodeExecutedEvent()
        {
            // Arrange
            var workflow = new Core.Workflow
            {
                Id = "event-workflow",
                Name = "Event Test Workflow",
                EntryNodeId = "input",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { Id = "input", NodeType = "TestInputNode" }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>()
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            bool eventRaised = false;
            engine.NodeExecuted += (sender, args) =>
            {
                eventRaised = true;
                Assert.NotNull(args.Node);
                Assert.NotNull(args.Result);
            };

            // Act
            var result = engine.StartWorkflowAsync("event-workflow").Result;

            // Assert
            Assert.True(eventRaised);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Engine_ShouldResetState()
        {
            // Arrange
            var workflow = new Core.Workflow
            {
                Id = "reset-workflow",
                Name = "Reset Test Workflow",
                EntryNodeId = "input",
                Nodes = new System.Collections.Generic.List<WorkflowNodeDefinition>
                {
                    new() { Id = "input", NodeType = "TestInputNode" }
                },
                Connections = new System.Collections.Generic.Dictionary<string, string>()
            };

            _registry.RegisterWorkflow(workflow);
            var engine = new WorkflowEngine(_registry, _services);

            // Act
            _ = engine.StartWorkflowAsync("reset-workflow").Result;
            Assert.NotNull(engine.CurrentWorkflow);
            Assert.NotNull(engine.CurrentNode);
            Assert.NotNull(engine.CurrentContext);

            engine.Reset();

            // Assert
            Assert.Null(engine.CurrentWorkflow);
            Assert.Null(engine.CurrentNode);
            Assert.Null(engine.CurrentContext);
            Assert.Equal(0, engine.GetNavigationStack().Count);
        }
    }
}
