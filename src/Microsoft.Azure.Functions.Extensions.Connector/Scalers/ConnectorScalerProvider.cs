// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
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

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
        var options = serviceProvider.GetService<IOptions<ConnectorOptions>>()?.Value ?? new ConnectorOptions();
        var nameResolver = serviceProvider.GetService<INameResolver>();

        string functionName = triggerMetadata.FunctionName;
        string? connectorNamespace = Resolve(triggerMetadata.Metadata?["connectorNamespace"]?.ToString(), nameResolver);
        string? triggerName = Resolve(triggerMetadata.Metadata?["triggerName"]?.ToString(), nameResolver);

        int attributeConcurrency = 0;
        var concurrencyToken = triggerMetadata.Metadata?["concurrency"];
        if (concurrencyToken != null && int.TryParse(concurrencyToken.ToString(), out int parsedConcurrency))
        {
            attributeConcurrency = parsedConcurrency;
        }

        int attributeBatchSize = 0;
        var batchSizeToken = triggerMetadata.Metadata?["batchSize"];
        if (batchSizeToken != null && int.TryParse(batchSizeToken.ToString(), out int parsedBatchSize))
        {
            attributeBatchSize = parsedBatchSize;
        }

        ArgumentException.ThrowIfNullOrEmpty(functionName);

        var metricsLogger = loggerFactory.CreateLogger<ConnectorMetricsProvider>();
        var scalerLogger = loggerFactory.CreateLogger<ConnectorTargetScaler>();

        var metricsProvider = new ConnectorMetricsProvider(
            options, functionName, connectorNamespace, triggerName, metricsLogger);

        _targetScaler = new ConnectorTargetScaler(
            functionId: functionName,
            metricsProvider: metricsProvider,
            options: options,
            attributeConcurrency: attributeConcurrency,
            attributeBatchSize: attributeBatchSize,
            logger: scalerLogger);
    }

    public ITargetScaler GetTargetScaler() => _targetScaler;

    // Resolves %AppSettingName% syntax via INameResolver. Returns the input unchanged
    // when it isn't a %...% reference or when no resolver is registered.
    private static string? Resolve(string? value, INameResolver? resolver)
    {
        if (string.IsNullOrEmpty(value) || resolver is null)
        {
            return value;
        }

        return resolver.ResolveWholeString(value);
    }
}
