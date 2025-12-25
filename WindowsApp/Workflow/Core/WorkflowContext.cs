using System;
using System.Collections.Generic;
using System.Threading;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Holds data and state that flows through workflow nodes.
    /// Provides a typed data bag with convenient accessors.
    /// </summary>
    public class WorkflowContext
    {
        private readonly Dictionary<string, object> _data;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Gets the data dictionary for this context.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data => _data;

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public IServiceProvider Services => _services;

        /// <summary>
        /// Gets or sets the cancellation token for async operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public WorkflowContext(IServiceProvider services)
        {
            _data = new Dictionary<string, object>();
            _services = services ?? throw new ArgumentNullException(nameof(services));
            CancellationToken = CancellationToken.None;
        }

        private WorkflowContext(Dictionary<string, object> data, IServiceProvider services, CancellationToken cancellationToken)
        {
            _data = data;
            _services = services;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets a value from the context with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value cast to the specified type.</returns>
        /// <exception cref="KeyNotFoundException">The key does not exist in the context.</exception>
        /// <exception cref="InvalidCastException">The value cannot be cast to the specified type.</exception>
        public T Get<T>(string key)
        {
            if (!_data.TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException($"Key '{key}' not found in workflow context.");
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidCastException($"Value for key '{key}' is of type {value.GetType().Name}, cannot cast to {typeof(T).Name}.");
        }

        /// <summary>
        /// Gets a value from the context with the specified key, or returns a default value if the key doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The default value to return if the key doesn't exist.</param>
        /// <returns>The value cast to the specified type, or the default value.</returns>
        public T GetOrDefault<T>(string key, T defaultValue = default!)
        {
            if (!_data.TryGetValue(key, out var value))
            {
                return defaultValue;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a value in the context with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        public void Set<T>(string key, T value)
        {
            _data[key] = value!;
        }

        /// <summary>
        /// Checks if the context contains a value with the specified key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Removes a value from the context with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns>True if the value was removed, false if the key didn't exist.</returns>
        public bool Remove(string key)
        {
            return _data.Remove(key);
        }

        /// <summary>
        /// Creates a shallow clone of this context for navigation history.
        /// </summary>
        /// <returns>A new WorkflowContext with the same data.</returns>
        public WorkflowContext Clone()
        {
            var clonedData = new Dictionary<string, object>(_data);
            return new WorkflowContext(clonedData, _services, CancellationToken);
        }

        /// <summary>
        /// Clears all data from the context.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }
    }
}
