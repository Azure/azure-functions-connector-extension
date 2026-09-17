// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorScalerProvider : ITargetScalerProvider
{
    private readonly ConnectorTargetScaler _targetScaler;

    public ConnectorScalerProvider(IServiceProvider serviceProvider, TriggerMetadata triggerMetadata)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(triggerMetadata);
        string functionName = triggerMetadata.FunctionName;
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);

        string connectionName = GetRequiredMetadata(triggerMetadata, "connection");
        string triggerConfigName = ResolveTriggerConfigName(
            GetRequiredMetadata(triggerMetadata, "triggerConfigName"),
            serviceProvider.GetService<INameResolver>());
        int attributeConcurrency = GetNonNegativeIntMetadata(triggerMetadata, "concurrency");

        AzureComponentFactory? injectedComponentFactory = null;
        if (triggerMetadata.Properties?.TryGetValue(nameof(AzureComponentFactory), out object? value) == true)
        {
            injectedComponentFactory = value as AzureComponentFactory
                ?? throw new InvalidOperationException($"TriggerMetadata.Properties['{nameof(AzureComponentFactory)}'] must be an AzureComponentFactory.");
        }

        ConnectorPollingConnection connection = serviceProvider
            .GetRequiredService<IConnectorPollingConnectionFactory>()
            .Create(connectionName, injectedComponentFactory);
        IConnectorPollingEndpointResolver endpointResolver = serviceProvider
            .GetRequiredService<IConnectorPollingEndpointResolverFactory>()
            .Create(connection, triggerConfigName);
        IConnectorQueueDepthClient depthClient = serviceProvider
            .GetRequiredService<IConnectorQueueDepthClientFactory>()
            .Create(endpointResolver, connection.Credential, functionName, triggerConfigName);

        ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        var metricsProvider = new ConnectorMetricsProvider(
            depthClient,
            functionName,
            triggerConfigName,
            loggerFactory.CreateLogger<ConnectorMetricsProvider>());
        ConnectorOptions options = serviceProvider.GetService<IOptions<ConnectorOptions>>()?.Value ?? new ConnectorOptions();
        _targetScaler = new ConnectorTargetScaler(
            functionName,
            metricsProvider,
            attributeConcurrency,
            options,
            loggerFactory.CreateLogger<ConnectorTargetScaler>());
    }

    public ITargetScaler GetTargetScaler() => _targetScaler;

    private static string GetRequiredMetadata(TriggerMetadata triggerMetadata, string propertyName)
    {
        string? value = triggerMetadata.Metadata?[propertyName]?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Connector Poll trigger metadata must define '{propertyName}'.");
        }

        return value;
    }

    private static int GetNonNegativeIntMetadata(TriggerMetadata triggerMetadata, string propertyName)
    {
        string? value = triggerMetadata.Metadata?[propertyName]?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int result) || result < 0)
        {
            throw new InvalidOperationException($"Connector Poll trigger metadata '{propertyName}' must be a non-negative integer.");
        }

        return result;
    }

    private static string ResolveTriggerConfigName(string triggerConfigName, INameResolver? nameResolver)
    {
        string? resolved = nameResolver?.ResolveWholeString(triggerConfigName) ?? triggerConfigName;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException("Connector Poll TriggerConfigName resolved to an empty value.");
        }

        return resolved;
    }
}
