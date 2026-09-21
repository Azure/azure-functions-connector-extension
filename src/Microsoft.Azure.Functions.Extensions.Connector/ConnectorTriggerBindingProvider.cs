// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Provider that creates trigger bindings for the Connector trigger.
/// </summary>
internal sealed class ConnectorTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorOptions _options;
    private readonly IConnectorConnectionOptionsProvider _connectionOptionsProvider;
    private readonly IConnectorPollingListenerFactory _pollingListenerFactory;

    public ConnectorTriggerBindingProvider(
        ConnectorExtensionConfigProvider configProvider,
        ConnectorOptions options,
        IConnectorConnectionOptionsProvider connectionOptionsProvider,
        IConnectorPollingListenerFactory pollingListenerFactory)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionOptionsProvider = connectionOptionsProvider
            ?? throw new ArgumentNullException(nameof(connectionOptionsProvider));
        _pollingListenerFactory = pollingListenerFactory
            ?? throw new ArgumentNullException(nameof(pollingListenerFactory));
    }

    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ParameterInfo parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<ConnectorTriggerAttribute>(inherit: false);

        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            attribute,
            _options,
            _connectionOptionsProvider,
            _pollingListenerFactory);
        return Task.FromResult<ITriggerBinding?>(binding);
    }
}
