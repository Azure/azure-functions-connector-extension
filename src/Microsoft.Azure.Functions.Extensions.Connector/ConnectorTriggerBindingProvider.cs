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

    public ConnectorTriggerBindingProvider(
        ConnectorExtensionConfigProvider configProvider,
        ConnectorOptions options)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        var binding = new ConnectorTriggerBinding(parameter, _configProvider, attribute, _options);
        return Task.FromResult<ITriggerBinding?>(binding);
    }
}
