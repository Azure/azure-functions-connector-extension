// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger binding for the Connector trigger.
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

    public Type TriggerValueType => typeof(string);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var bindingData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var valueProvider = new StringValueProvider(value as string);
        return Task.FromResult<ITriggerData>(new TriggerData(valueProvider, bindingData));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        string functionName = context.Descriptor.ShortName.Split('.').Last();

        var registration = new ConnectorFunctionRegistration(functionName, context.Executor)
        {
            ConnectorNamespace = _attribute.ConnectorNamespace,
            TriggerName = _attribute.TriggerName
        };

        return Task.FromResult<IListener>(new ConnectorListener(_configProvider, registration));
    }

    public ParameterDescriptor ToParameterDescriptor() => new TriggerParameterDescriptor
    {
        Name = _parameter.Name ?? "payload"
    };

    /// <summary>
    /// Value provider for trigger binding.
    /// </summary>
    private sealed class StringValueProvider(string? value) : IValueProvider
    {
        public Type Type => typeof(string);

        public Task<object?> GetValueAsync() => Task.FromResult<object?>(value);

        public string? ToInvokeString() => value;
    }
}
