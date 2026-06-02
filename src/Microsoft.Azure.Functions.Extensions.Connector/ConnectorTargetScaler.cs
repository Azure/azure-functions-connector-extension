// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

// TargetWorkerCount = ceil(PendingEvents / Concurrency).
// Concurrency precedence: attribute > ConnectorOptions.DefaultConcurrency.
internal sealed class ConnectorTargetScaler : ITargetScaler
{
    private readonly ConnectorMetricsProvider _metricsProvider;
    private readonly ConnectorOptions _options;
    private readonly int _attributeConcurrency;
    private readonly ILogger _logger;

    private DateTime _lastScaleUpUtc = DateTime.MinValue;
    private TargetScalerResult _lastResult = new() { TargetWorkerCount = 0 };

    public ConnectorTargetScaler(
        string functionId,
        ConnectorMetricsProvider metricsProvider,
        ConnectorOptions options,
        int attributeConcurrency,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(functionId);
        _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _attributeConcurrency = attributeConcurrency;
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
            concurrency = _options.DefaultConcurrency;
            concurrencySource = "hostJson";
        }

        int desired = (int)Math.Ceiling(metrics.PendingEvents / (decimal)concurrency);
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
            "ConnectorTargetScaler {FunctionId}: pending={Pending}, concurrency={Concurrency} (source={Source}), target={Target}",
            TargetScalerDescriptor.FunctionId, metrics.PendingEvents, concurrency, concurrencySource, result.TargetWorkerCount);

        return result;
    }
}
