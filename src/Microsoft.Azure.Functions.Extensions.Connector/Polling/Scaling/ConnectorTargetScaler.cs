// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorTargetScaler : ITargetScaler
{
    private readonly ConnectorMetricsProvider _metricsProvider;
    private readonly int _configuredTargetPendingEventThreshold;
    private readonly ILogger _logger;

    public ConnectorTargetScaler(string functionName, ConnectorMetricsProvider metricsProvider, int attributeTargetPendingEventThreshold, ConnectorOptions options, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (attributeTargetPendingEventThreshold < 0)
        {
            throw new InvalidOperationException("Connector trigger TargetPendingEventThreshold must be zero or greater.");
        }

        _configuredTargetPendingEventThreshold = attributeTargetPendingEventThreshold > 0 ? attributeTargetPendingEventThreshold : options.DefaultTargetPendingEventThreshold;
        if (_configuredTargetPendingEventThreshold <= 0)
        {
            throw new InvalidOperationException("Connector DefaultTargetPendingEventThreshold must be greater than zero.");
        }

        TargetScalerDescriptor = new TargetScalerDescriptor(functionName);
    }

    public TargetScalerDescriptor TargetScalerDescriptor { get; }

    public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        int effectiveTargetPendingEventThreshold = _configuredTargetPendingEventThreshold;

        ConnectorTriggerMetrics metrics = await _metricsProvider.GetMetricsAsync().ConfigureAwait(false);
        long targetWorkerCount =
            (metrics.ApproximateQueueDepth / effectiveTargetPendingEventThreshold) +
            (metrics.ApproximateQueueDepth % effectiveTargetPendingEventThreshold == 0 ? 0 : 1);
        int target = (int)Math.Min(targetWorkerCount, int.MaxValue);
        _logger.LogDebug("Connector target scale for function {FunctionName}: approximateDepth={Depth}, effectiveTargetPendingEventThreshold={TargetPendingEventThreshold}, targetWorkers={TargetWorkers}.", TargetScalerDescriptor.FunctionId, metrics.ApproximateQueueDepth, effectiveTargetPendingEventThreshold, target);
        return new TargetScalerResult { TargetWorkerCount = target };
    }
}
