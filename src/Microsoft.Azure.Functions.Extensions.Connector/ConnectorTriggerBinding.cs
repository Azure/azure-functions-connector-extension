// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs;
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
    private readonly ConnectorOptions _options;
    private readonly IConnectorConnectionOptionsProvider _connectionOptionsProvider;
    private readonly IConnectorPollingListenerFactory _pollingListenerFactory;

    public ConnectorTriggerBinding(
        ParameterInfo parameter,
        ConnectorExtensionConfigProvider configProvider,
        ConnectorTriggerAttribute attribute,
        ConnectorOptions options,
        IConnectorConnectionOptionsProvider connectionOptionsProvider,
        IConnectorPollingListenerFactory pollingListenerFactory)
    {
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionOptionsProvider = connectionOptionsProvider
            ?? throw new ArgumentNullException(nameof(connectionOptionsProvider));
        _pollingListenerFactory = pollingListenerFactory
            ?? throw new ArgumentNullException(nameof(pollingListenerFactory));
    }

    public Type TriggerValueType => typeof(ConnectorTriggerInput);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        ConnectorTriggerInput triggerInput = value switch
        {
            ConnectorTriggerInput connectorTriggerInput => connectorTriggerInput,
            string json => ConnectorTriggerInput.FromSingle(
                BinaryData.FromString(json),
                messageId: null,
                _attribute.DeliveryMode),
            _ => throw new InvalidOperationException(
                $"Unsupported Connector trigger value type '{value?.GetType()}'."),
        };
        var bindingData = new Dictionary<string, object?>(
            StringComparer.OrdinalIgnoreCase);
        string payloadJson = triggerInput.ToPayloadJson();
        ConnectorTriggerInputValueProvider valueProvider = CreateValueProvider(
            triggerInput,
            payloadJson);
        return Task.FromResult<ITriggerData>(
            new TriggerData(valueProvider, bindingData));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var registration = new ConnectorFunctionRegistration(
            context.Descriptor.ShortName,
            context.Executor);

        IListener listener = _attribute.DeliveryMode switch
        {
            ConnectorTriggerDeliveryMode.Webhook =>
                new ConnectorListener(_configProvider, registration),
            ConnectorTriggerDeliveryMode.Poll =>
                CreatePollingListener(registration),
            _ => throw new InvalidOperationException(
                $"Unsupported Connector trigger delivery mode '{_attribute.DeliveryMode}'."),
        };

        return Task.FromResult(listener);
    }

    private ConnectorPollingListener CreatePollingListener(
        ConnectorFunctionRegistration registration)
    {
        ConnectorPollingOptions options =
            ConnectorPollingOptions.Create(
                _attribute,
                _options,
                IsBatchedParameter(_parameter.ParameterType));
        ConnectorConnectionOptions connectionOptions =
            _connectionOptionsProvider.Get(options.Connection);

        return _pollingListenerFactory.Create(
            registration,
            options,
            connectionOptions);
    }

    private ConnectorTriggerInputValueProvider CreateValueProvider(
        ConnectorTriggerInput triggerInput,
        string payloadJson)
    {
        bool usesDeferredBinding =
            _parameter.ParameterType == typeof(ParameterBindingData) ||
            _parameter.ParameterType == typeof(ParameterBindingData[]);
        if (usesDeferredBinding)
        {
            if (triggerInput.IsBatched)
            {
                ParameterBindingData[] values = triggerInput.Events
                    .Select(ConnectorExtensionConfigProvider
                        .ConvertTriggerEventToBindingData)
                    .ToArray();
                return new ConnectorTriggerInputValueProvider(
                    values,
                    typeof(ParameterBindingData[]),
                    payloadJson);
            }

            return new ConnectorTriggerInputValueProvider(
                ConnectorExtensionConfigProvider
                    .ConvertTriggerEventToBindingData(
                        triggerInput.Events.Single()),
                typeof(ParameterBindingData),
                payloadJson);
        }

        if (triggerInput.IsBatched)
        {
            string[] values = triggerInput.Events
                .Select(static connectorEvent =>
                    connectorEvent.Outputs.ToString())
                .ToArray();
            return new ConnectorTriggerInputValueProvider(
                values,
                typeof(string[]),
                payloadJson);
        }

        return new ConnectorTriggerInputValueProvider(
            payloadJson,
            typeof(string),
            payloadJson);
    }

    private static bool IsBatchedParameter(Type parameterType) =>
        parameterType.IsArray && parameterType != typeof(byte[]);

    public ParameterDescriptor ToParameterDescriptor() =>
        new TriggerParameterDescriptor
        {
            Name = _parameter.Name ?? "payload",
        };

    private sealed class ConnectorTriggerInputValueProvider : IValueProvider
    {
        private readonly Task<object?> _valueAsTask;
        private readonly string _invokeString;

        internal ConnectorTriggerInputValueProvider(
            object value,
            Type type,
            string invokeString)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(invokeString);
            if (!type.IsInstanceOfType(value))
            {
                throw new ArgumentException(
                    $"Cannot use value of type '{value.GetType()}' as '{type}'.",
                    nameof(value));
            }

            _valueAsTask = Task.FromResult<object?>(value);
            _invokeString = invokeString;
            Type = type;
        }

        public Type Type { get; }

        public Task<object?> GetValueAsync() => _valueAsTask;

        public string? ToInvokeString() => _invokeString;
    }
}
