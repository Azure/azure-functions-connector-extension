// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger binding for the Connector trigger.
/// Uses JObject as trigger value with IValueProvider for isolated worker support.
/// </summary>
internal sealed class ConnectorTriggerBinding : ITriggerBinding
{
    private readonly ParameterInfo _parameter;
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorTriggerAttribute _attribute;

    public ConnectorTriggerBinding(
        ParameterInfo parameter, 
        ConnectorExtensionConfigProvider configProvider,
        ConnectorTriggerAttribute attribute)
    {
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
    }

    public Type TriggerValueType => typeof(JObject);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = 
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        // IValueProvider is required for isolated worker - provides JSON string for gRPC
        var bindingData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var valueProvider = new JsonStringValueProvider(value as JObject);
        return Task.FromResult<ITriggerData>(new TriggerData(valueProvider, bindingData));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        string functionName = context.Descriptor.ShortName.Split('.').Last();
        
        var registration = new ConnectorFunctionRegistration(functionName, context.Executor)
        {
            Connector = _attribute.ConnectorName,
            Operation = _attribute.OperationName,
            Connection = _attribute.ConnectionName
        };
        
        return Task.FromResult<IListener>(new ConnectorListener(_configProvider, registration));
    }

    public ParameterDescriptor ToParameterDescriptor() => new TriggerParameterDescriptor
    {
        Name = _parameter.Name ?? "payload"
    };

    /// <summary>
    /// Value provider that serializes JObject to JSON string for isolated worker.
    /// Required because null IValueProvider doesn't work with isolated worker gRPC.
    /// </summary>
    private sealed class JsonStringValueProvider : IValueProvider
    {
        private readonly JObject? _value;

        public JsonStringValueProvider(JObject? value) => _value = value;

        public Type Type => typeof(string);

        public Task<object?> GetValueAsync() => 
            Task.FromResult<object?>(_value?.ToString(Newtonsoft.Json.Formatting.None));

        public string? ToInvokeString() => _value?.ToString(Newtonsoft.Json.Formatting.None);
    }
}
