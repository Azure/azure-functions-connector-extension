// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        string functionName = triggerMetadata.FunctionName;
        string? connectorNamespace = triggerMetadata.Metadata?["connectorNamespace"]?.ToString();
        string? triggerName = triggerMetadata.Metadata?["triggerName"]?.ToString();

        int attributeConcurrency = 0;
        var concurrencyToken = triggerMetadata.Metadata?["concurrency"];
        if (concurrencyToken != null && int.TryParse(concurrencyToken.ToString(), out int parsed))
        {
            attributeConcurrency = parsed;
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
            logger: scalerLogger);
    }

    public ITargetScaler GetTargetScaler() => _targetScaler;
}
