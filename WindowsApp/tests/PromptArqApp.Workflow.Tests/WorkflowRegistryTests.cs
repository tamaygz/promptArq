using System;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using Serilog;
using Xunit;

namespace PromptArqApp.Workflow.Tests
{
    public class WorkflowRegistryTests
    {
        private readonly IServiceProvider _services;

        public WorkflowRegistryTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(new LoggerConfiguration().CreateLogger());
            _services = services.BuildServiceProvider();
        }

        [Fact]
        public void Registry_ShouldRegisterAndRetrieveWorkflow()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            var workflow = new Core.Workflow
            {
                Id = "test-workflow",
                Name = "Test Workflow",
                Description = "A test workflow"
            };

            // Act
            registry.RegisterWorkflow(workflow);
            var retrieved = registry.GetWorkflow("test-workflow");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("test-workflow", retrieved!.Id);
            Assert.Equal("Test Workflow", retrieved.Name);
        }

        [Fact]
        public void Registry_ShouldReturnNullForMissingWorkflow()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);

            // Act
            var result = registry.GetWorkflow("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Registry_ShouldRegisterAndCreateNode()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            registry.RegisterNode("TestInputNode", typeof(TestInputNode));

            // Act
            var node = registry.CreateNode("TestInputNode");

            // Assert
            Assert.NotNull(node);
            Assert.IsType<TestInputNode>(node);
        }

        [Fact]
        public void Registry_ShouldReturnNullForMissingNodeType()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);

            // Act
            var node = registry.CreateNode("NonexistentNode");

            // Assert
            Assert.Null(node);
        }

        [Fact]
        public void Registry_ShouldCreateNodeWithConfiguration()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            registry.RegisterNode("TestActionNode", typeof(TestActionNode));

            var config = new System.Collections.Generic.Dictionary<string, object>
            {
                ["inputKey"] = "customInput",
                ["outputKey"] = "customOutput"
            };

            // Act
            var node = registry.CreateNode("TestActionNode", config);

            // Assert
            Assert.NotNull(node);
            Assert.IsType<TestActionNode>(node);
        }

        [Fact]
        public void Registry_ShouldListAllWorkflows()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            registry.RegisterWorkflow(new Core.Workflow { Id = "workflow1", Name = "Workflow 1" });
            registry.RegisterWorkflow(new Core.Workflow { Id = "workflow2", Name = "Workflow 2" });

            // Act
            var workflows = registry.GetAllWorkflows();

            // Assert
            Assert.Equal(2, System.Linq.Enumerable.Count(workflows));
        }

        [Fact]
        public void Registry_ShouldListRegisteredNodeTypes()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            registry.RegisterNode("TestInputNode", typeof(TestInputNode));
            registry.RegisterNode("TestActionNode", typeof(TestActionNode));

            // Act
            var nodeTypes = registry.GetRegisteredNodeTypes();

            // Assert
            Assert.Equal(2, System.Linq.Enumerable.Count(nodeTypes));
            Assert.Contains("TestInputNode", nodeTypes);
            Assert.Contains("TestActionNode", nodeTypes);
        }

        [Fact]
        public void Registry_ShouldRegisterPlugin()
        {
            // Arrange
            var registry = new WorkflowRegistry(_services);
            var plugin = new TestPlugin();

            // Act
            registry.RegisterPlugin(plugin);

            // Assert
            var workflow = registry.GetWorkflow("test-plugin-workflow");
            Assert.NotNull(workflow);
            Assert.Equal("Test Plugin Workflow", workflow!.Name);

            var node = registry.CreateNode("TestPluginNode");
            Assert.NotNull(node);

            var plugins = registry.GetAllPlugins();
            Assert.Single(plugins);
        }

        // Helper test plugin
        private class TestPlugin : IWorkflowPlugin
        {
            public string PluginId => "test-plugin";
            public string Name => "Test Plugin";
            public Version Version => new Version(1, 0, 0);

            public System.Collections.Generic.IEnumerable<Core.Workflow> GetWorkflows()
            {
                yield return new Core.Workflow
                {
                    Id = "test-plugin-workflow",
                    Name = "Test Plugin Workflow"
                };
            }

            public System.Collections.Generic.IEnumerable<(string NodeType, Type NodeClass)> GetNodes()
            {
                yield return ("TestPluginNode", typeof(TestInputNode));
            }
        }
    }
}
