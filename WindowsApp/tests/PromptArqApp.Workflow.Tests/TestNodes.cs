using System;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Nodes;

namespace PromptArqApp.Workflow.Tests
{
    /// <summary>
    /// Simple test node that sets a value in the context.
    /// </summary>
    public class TestInputNode : InputNodeBase
    {
        public override string Name => "Test Input Node";
        public override NodeUIType UIType => NodeUIType.TextInput;
        public override string HintText => "Enter test value";

        private string _outputKey = "testValue";

        public TestInputNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("outputKey", out var key))
            {
                _outputKey = key.ToString() ?? "testValue";
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Simulate getting input from UI
            context.Set(_outputKey, "test-value-123");
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }

    /// <summary>
    /// Simple test node that transforms a value.
    /// </summary>
    public class TestActionNode : ActionNodeBase
    {
        public override string Name => "Test Action Node";

        private string _inputKey = "testValue";
        private string _outputKey = "transformedValue";

        public TestActionNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("inputKey", out var inKey))
            {
                _inputKey = inKey.ToString() ?? "testValue";
            }
            if (config.TryGetValue("outputKey", out var outKey))
            {
                _outputKey = outKey.ToString() ?? "transformedValue";
            }
        }

        protected override Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            try
            {
                var input = context.Get<string>(_inputKey);
                var transformed = input.ToUpperInvariant();
                context.Set(_outputKey, transformed);
                return Task.FromResult(WorkflowResult.CreateSuccess(context));
            }
            catch (Exception ex)
            {
                return Task.FromResult(WorkflowResult.CreateError(context, ex.Message));
            }
        }
    }

    /// <summary>
    /// Simple test node that outputs a result.
    /// </summary>
    public class TestOutputNode : OutputNodeBase
    {
        public override string Name => "Test Output Node";

        private string _inputKey = "transformedValue";

        public TestOutputNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("inputKey", out var key))
            {
                _inputKey = key.ToString() ?? "transformedValue";
            }
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            try
            {
                var value = context.Get<string>(_inputKey);
                return Task.FromResult(WorkflowResult.CreateSuccess(context, output: value));
            }
            catch (Exception ex)
            {
                return Task.FromResult(WorkflowResult.CreateError(context, ex.Message));
            }
        }
    }
}
