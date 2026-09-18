// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorTargetScaler : ITargetScaler
{
    private readonly ConnectorMetricsProvider _metricsProvider;
    private readonly int _effectiveConcurrency;
    private readonly ILogger _logger;

    public ConnectorTargetScaler(string functionName, ConnectorMetricsProvider metricsProvider, int attributeConcurrency, ConnectorOptions options, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (attributeConcurrency < 0)
        {
            throw new InvalidOperationException("Connector trigger Concurrency must be zero or greater.");
        }

        _effectiveConcurrency = attributeConcurrency > 0 ? attributeConcurrency : options.DefaultConcurrency;
        if (_effectiveConcurrency <= 0)
        {
            throw new InvalidOperationException("Connector DefaultConcurrency must be greater than zero.");
        }

        TargetScalerDescriptor = new TargetScalerDescriptor(functionName);
    }

    public TargetScalerDescriptor TargetScalerDescriptor { get; }

    public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ConnectorTriggerMetrics metrics = await _metricsProvider.GetMetricsAsync().ConfigureAwait(false);
        long targetWorkerCount =
            (metrics.ApproximateQueueDepth / _effectiveConcurrency) +
            (metrics.ApproximateQueueDepth % _effectiveConcurrency == 0 ? 0 : 1);
        int target = (int)Math.Min(targetWorkerCount, int.MaxValue);
        _logger.LogInformation("Connector target scale for function {FunctionName}: approximateDepth={Depth}, effectiveConcurrency={Concurrency}, targetWorkers={TargetWorkers}.", TargetScalerDescriptor.FunctionId, metrics.ApproximateQueueDepth, _effectiveConcurrency, target);
        return new TargetScalerResult { TargetWorkerCount = target };
    }
}
