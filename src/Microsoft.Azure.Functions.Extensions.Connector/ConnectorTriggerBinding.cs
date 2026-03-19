// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Defines the trigger binding for the Connector trigger.
/// 
/// A trigger binding is responsible for:
/// 1. Defining what data is available to the function (BindingDataContract)
/// 2. Binding trigger values when the function is invoked (BindAsync)
/// 3. Creating the listener that watches for trigger events (CreateListenerAsync)
/// </summary>
internal sealed class ConnectorTriggerBinding : ITriggerBinding
{
    private readonly ParameterInfo _parameter;
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly IReadOnlyDictionary<string, Type> _bindingContract;

    public ConnectorTriggerBinding(
        ParameterInfo parameter,
        ConnectorExtensionConfigProvider configProvider)
    {
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));

        // Define the binding data contract - what data is available to the function
        _bindingContract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "Body", typeof(string) },
            { "Headers", typeof(IDictionary<string, string>) },
            { "Query", typeof(IDictionary<string, string>) },
            { "Method", typeof(string) },
            { "Url", typeof(string) },
            { "ContentType", typeof(string) },
            { "Timestamp", typeof(DateTimeOffset) }
        };
    }

    /// <summary>
    /// The type of the trigger value. This is what the listener will provide.
    /// We use a tuple of (ConnectorContext, JToken) as the internal trigger value type,
    /// which can be converted to various types via the registered converters.
    /// </summary>
    public Type TriggerValueType => typeof((ConnectorContext, JToken));

    /// <summary>
    /// Defines the binding data that functions can access via binding expressions.
    /// </summary>
    public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingContract;

    /// <summary>
    /// Binds the trigger value when a function is invoked.
    /// </summary>
    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        // The value comes as a tuple of (ConnectorContext, JToken)
        var (connectorContext, jsonBody) = value switch
        {
            ValueTuple<ConnectorContext, JToken> tuple => tuple,
            _ => throw new InvalidOperationException($"Expected (ConnectorContext, JToken) tuple but got {value?.GetType()}")
        };

        // Create binding data from the connector context
        var bindingData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            { "Body", connectorContext.Body },
            { "Headers", connectorContext.Headers },
            { "Query", connectorContext.Query },
            { "Method", connectorContext.Method },
            { "Url", connectorContext.Url },
            { "ContentType", connectorContext.ContentType },
            { "Timestamp", connectorContext.Timestamp },
            // Also provide the JSON body for converters
            { "data", jsonBody }
        };

        // Create a value provider that returns the tuple for converter chain processing
        var valueProvider = new ConnectorValueProvider(value, TriggerValueType);
        var triggerData = new TriggerData(valueProvider, bindingData);
        return Task.FromResult<ITriggerData>(triggerData);
    }

    /// <summary>
    /// Creates the listener that will register this function for HTTP routing.
    /// </summary>
    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Get the function name (strip any prefix like "Functions.")
        string functionName = context.Descriptor.ShortName.Split('.').Last();

        var listener = new ConnectorListener(
            context.Executor,
            _configProvider,
            functionName);

        return Task.FromResult<IListener>(listener);
    }

    /// <summary>
    /// Returns a description of this parameter for diagnostic purposes.
    /// </summary>
    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ConnectorTriggerParameterDescriptor
        {
            Name = _parameter.Name ?? "connector",
            DisplayHints = new ParameterDisplayHints
            {
                Description = "Connector trigger fired",
                Prompt = "Connector"
            }
        };
    }

    private sealed class ConnectorTriggerParameterDescriptor : TriggerParameterDescriptor
    {
        public override string GetTriggerReason(IDictionary<string, string> arguments)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Connector trigger fired at {0:o}",
                DateTime.UtcNow);
        }
    }
}
