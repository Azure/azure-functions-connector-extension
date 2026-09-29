// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorMetricsProvider
{
    private readonly IConnectorQueueDepthClient _depthClient;
    private readonly string _functionName;
    private readonly string _triggerConfigName;
    private readonly ILogger _logger;
    private ConnectorTriggerMetrics? _lastKnownGoodMetrics;

    public ConnectorMetricsProvider(IConnectorQueueDepthClient depthClient, string functionName, string triggerConfigName, ILogger logger)
    {
        _depthClient = depthClient ?? throw new ArgumentNullException(nameof(depthClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerConfigName);
        _functionName = functionName;
        _triggerConfigName = triggerConfigName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConnectorTriggerMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            long depth = await _depthClient.GetApproximateQueueDepthAsync(cancellationToken).ConfigureAwait(false);
            var metrics = new ConnectorTriggerMetrics(depth, DateTime.UtcNow);
            Interlocked.Exchange(ref _lastKnownGoodMetrics, metrics);
            return metrics;
        }
        catch (Exception exception)
        {
            ConnectorTriggerMetrics? lastKnownGoodMetrics =
                Volatile.Read(ref _lastKnownGoodMetrics);
            if (lastKnownGoodMetrics is not null)
            {
                _logger.LogWarning(exception, "Failed to query Connector queue depth for function {FunctionName} and trigger configuration {TriggerConfigName}; preserving last known good depth {Depth}.", _functionName, _triggerConfigName, lastKnownGoodMetrics.ApproximateQueueDepth);
                return lastKnownGoodMetrics;
            }

            _logger.LogError(exception, "Initial Connector queue depth query failed for function {FunctionName} and trigger configuration {TriggerConfigName}; no successful zero-depth result is available.", _functionName, _triggerConfigName);
            throw new InvalidOperationException($"Initial Connector queue depth query failed for function '{_functionName}'.", exception);
        }
    }
}
