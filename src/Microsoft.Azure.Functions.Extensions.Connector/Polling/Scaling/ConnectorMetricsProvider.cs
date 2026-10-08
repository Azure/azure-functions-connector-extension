// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorMetricsProvider
{
    private readonly IConnectorQueueDepthClient _depthClient;
    private readonly string _functionName;
    private readonly ILogger _logger;

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
            return new ConnectorTriggerMetrics(depth, DateTime.UtcNow);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogFunctionScaleWarning(
                "Failed to query Connector queue depth; returning zero depth.",
                _functionName,
                exception);
            return new ConnectorTriggerMetrics(0, DateTime.UtcNow);
        }
    }
}
