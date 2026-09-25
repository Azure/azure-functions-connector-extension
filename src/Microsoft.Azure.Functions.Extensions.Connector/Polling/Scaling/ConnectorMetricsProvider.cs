// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorMetricsProvider
{
    private readonly IConnectorQueueDepthClient _depthClient;
    private readonly string _functionName;
    private readonly ILogger _logger;
    private long _lastKnownGoodDepth = -1;

    public ConnectorMetricsProvider(
        IConnectorQueueDepthClient depthClient,
        string functionName,
        ILogger logger)
    {
        _depthClient = depthClient ?? throw new ArgumentNullException(nameof(depthClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _functionName = functionName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConnectorTriggerMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            long depth = await _depthClient.GetApproximateQueueDepthAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Exchange(ref _lastKnownGoodDepth, depth);
            return new ConnectorTriggerMetrics(depth, DateTime.UtcNow);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            long lastKnownGoodDepth = Volatile.Read(ref _lastKnownGoodDepth);
            if (lastKnownGoodDepth >= 0)
            {
                _logger.LogWarning(exception, "Failed to query Connector queue depth for function {FunctionName}; preserving last known good depth {Depth}.", _functionName, lastKnownGoodDepth);
                return new ConnectorTriggerMetrics(lastKnownGoodDepth, DateTime.UtcNow);
            }

            _logger.LogError(exception, "Initial Connector queue depth query failed for function {FunctionName}; no successful zero-depth result is available.", _functionName);
            throw new InvalidOperationException($"Initial Connector queue depth query failed for function '{_functionName}'.", exception);
        }
    }
}
