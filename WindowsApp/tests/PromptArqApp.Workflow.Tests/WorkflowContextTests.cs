using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using Serilog;
using Xunit;

namespace PromptArqApp.Workflow.Tests
{
    public class WorkflowContextTests
    {
        private readonly IServiceProvider _services;

        public WorkflowContextTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(new LoggerConfiguration().CreateLogger());
            _services = services.BuildServiceProvider();
        }

        [Fact]
        public void Context_ShouldStoreAndRetrieveValues()
        {
            // Arrange
            var context = new WorkflowContext(_services);

            // Act
            context.Set("key1", "value1");
            context.Set("key2", 42);

            // Assert
            Assert.Equal("value1", context.Get<string>("key1"));
            Assert.Equal(42, context.Get<int>("key2"));
        }

        [Fact]
        public void Context_ShouldReturnDefaultValueForMissingKey()
        {
            // Arrange
            var context = new WorkflowContext(_services);

            // Act
            var result = context.GetOrDefault("missing", "default");

            // Assert
            Assert.Equal("default", result);
        }

        [Fact]
        public void Context_ShouldThrowForMissingKeyWithGet()
        {
            // Arrange
            var context = new WorkflowContext(_services);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => context.Get<string>("missing"));
        }

        [Fact]
        public void Context_ShouldCheckKeyExistence()
        {
            // Arrange
            var context = new WorkflowContext(_services);
            context.Set("existing", "value");

            // Act & Assert
            Assert.True(context.Has("existing"));
            Assert.False(context.Has("missing"));
        }

        [Fact]
        public void Context_ShouldRemoveKeys()
        {
            // Arrange
            var context = new WorkflowContext(_services);
            context.Set("key", "value");

            // Act
            var removed = context.Remove("key");

            // Assert
            Assert.True(removed);
            Assert.False(context.Has("key"));
        }

        [Fact]
        public void Context_ShouldCloneSuccessfully()
        {
            // Arrange
            var context = new WorkflowContext(_services);
            context.Set("key", "value");

            // Act
            var cloned = context.Clone();
            cloned.Set("key2", "value2");

            // Assert
            Assert.Equal("value", cloned.Get<string>("key"));
            Assert.True(cloned.Has("key2"));
            Assert.False(context.Has("key2")); // Original should not have key2
        }

        [Fact]
        public void Context_ShouldClearAllData()
        {
            // Arrange
            var context = new WorkflowContext(_services);
            context.Set("key1", "value1");
            context.Set("key2", "value2");

            // Act
            context.Clear();

            // Assert
            Assert.False(context.Has("key1"));
            Assert.False(context.Has("key2"));
        }

        [Fact]
        public void Context_ShouldStoreCancellationToken()
        {
            // Arrange
            var context = new WorkflowContext(_services);
            using var cts = new CancellationTokenSource();

            // Act
            context.CancellationToken = cts.Token;

            // Assert
            Assert.Equal(cts.Token, context.CancellationToken);
        }
    }
}
