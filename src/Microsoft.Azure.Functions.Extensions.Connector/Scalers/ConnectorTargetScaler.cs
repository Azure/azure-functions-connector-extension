// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

// TargetWorkerCount = ceil(PendingEvents / (Concurrency * BatchSize)).
// Concurrency precedence: attribute > ConnectorOptions.DefaultConcurrency.
// BatchSize   precedence: attribute > ConnectorOptions.DefaultBatchSize.
internal sealed class ConnectorTargetScaler : ITargetScaler
{
    private readonly ConnectorMetricsProvider _metricsProvider;
    private readonly ConnectorOptions _options;
    private readonly int _attributeConcurrency;
    private readonly int _attributeBatchSize;
    private readonly ILogger _logger;

    private DateTime _lastScaleUpUtc = DateTime.MinValue;
    private TargetScalerResult _lastResult = new() { TargetWorkerCount = 0 };

    public ConnectorTargetScaler(
        string functionId,
        ConnectorMetricsProvider metricsProvider,
        ConnectorOptions options,
        int attributeConcurrency,
        int attributeBatchSize,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(functionId);
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _attributeConcurrency = attributeConcurrency;
        _attributeBatchSize = attributeBatchSize;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        TargetScalerDescriptor = new TargetScalerDescriptor(functionId);
    }

    public TargetScalerDescriptor TargetScalerDescriptor { get; }

    public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var metrics = await _metricsProvider.GetMetricsAsync().ConfigureAwait(false);

        int concurrency;
        string concurrencySource;
        if (_attributeConcurrency > 0)
        {
            concurrency = _attributeConcurrency;
            concurrencySource = "attribute";
        }
        else
        {
            // Guard: host.json with 0/negative defaultConcurrency falls back to 1
            // to avoid divide-by-zero / negative worker counts.
            concurrency = _options.DefaultConcurrency > 0 ? _options.DefaultConcurrency : 1;
            concurrencySource = "hostJson";
        }

        int batchSize;
        string batchSizeSource;
        if (_attributeBatchSize > 0)
        {
            batchSize = _attributeBatchSize;
            batchSizeSource = "attribute";
        }
        else
        {
            batchSize = _options.DefaultBatchSize > 0 ? _options.DefaultBatchSize : 1;
            batchSizeSource = "hostJson";
        }

        long perWorkerCapacity = (long)concurrency * batchSize;
        int desired = (int)Math.Ceiling(metrics.PendingEvents / (decimal)perWorkerCapacity);
        var result = new TargetScalerResult { TargetWorkerCount = desired };

        if (result.TargetWorkerCount < _lastResult.TargetWorkerCount &&
            (DateTime.UtcNow - _lastScaleUpUtc) < _options.ThrottleScaleDownInterval)
        {
            _logger.LogDebug(
                "ConnectorTargetScaler suppressing scale-down: desired={Desired}, last={Last}, sinceScaleUp={Elapsed}",
                desired, _lastResult.TargetWorkerCount, DateTime.UtcNow - _lastScaleUpUtc);

            result = _lastResult;
        }
        else if (result.TargetWorkerCount > _lastResult.TargetWorkerCount)
        {
            _lastScaleUpUtc = DateTime.UtcNow;
        }

        _lastResult = result;

        _logger.LogInformation(
            "ConnectorTargetScaler {FunctionId}: pending={Pending}, concurrency={Concurrency} (source={ConcurrencySource}), batchSize={BatchSize} (source={BatchSizeSource}), target={Target}",
            TargetScalerDescriptor.FunctionId, metrics.PendingEvents, concurrency, concurrencySource, batchSize, batchSizeSource, result.TargetWorkerCount);

        return result;
    }
}
