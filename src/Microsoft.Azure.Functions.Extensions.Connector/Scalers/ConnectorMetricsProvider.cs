// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

// Phase 1 mock. Replaced by a real Namespace HTTP client in Phase 2.
internal sealed class ConnectorMetricsProvider
{
    internal const string MockEnvVar = "CONNECTOR_MOCK_PENDING_EVENTS";

    private readonly ConnectorOptions _options;
    private readonly string _functionName;
    private readonly string? _connectorNamespace;
    private readonly string? _triggerName;
    private readonly ILogger _logger;

    public ConnectorMetricsProvider(
        ConnectorOptions options,
        string functionName,
        string? connectorNamespace,
        string? triggerName,
        ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        _connectorNamespace = connectorNamespace;
        _triggerName = triggerName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ConnectorTriggerMetrics> GetMetricsAsync()
    {
        int pending = ResolveMockPendingEvents();

        _logger.LogDebug(
            "ConnectorMetricsProvider mock metrics for {FunctionName} (connectorNamespace={ConnectorNamespace}, triggerName={TriggerName}): {Pending} pending events",
            _functionName, _connectorNamespace ?? "<none>", _triggerName ?? "<none>", pending);

        return Task.FromResult(new ConnectorTriggerMetrics
        {
            PendingEvents = pending,
            SampledAtUtc = DateTime.UtcNow
        });
    }

    private int ResolveMockPendingEvents()
    {
        var envValue = Environment.GetEnvironmentVariable(MockEnvVar);
        if (!string.IsNullOrWhiteSpace(envValue) && int.TryParse(envValue, out int parsed) && parsed >= 0)
        {
            return parsed;
        }

        return Math.Max(0, _options.MockPendingEvents);
    }
}
