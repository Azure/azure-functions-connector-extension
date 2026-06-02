// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.WebJobs.Hosting;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Options bound from <c>host.json:extensions.connector</c>.
/// </summary>
public class ConnectorOptions : IOptionsFormatter
{
    /// <summary>
    /// Default per-worker concurrency. Used when the trigger attribute doesn't set one.
    /// </summary>
    public int DefaultConcurrency { get; set; } = 16;

    /// <summary>
    /// Default events-per-invocation batch size. Used when the trigger attribute
    /// doesn't set one. A value of 1 means non-batched (one event per invocation).
    /// </summary>
    public int DefaultBatchSize { get; set; } = 1;

    /// <summary>
    /// Minimum time after a scale-up before a scale-down vote is honoured.
    /// </summary>
    public TimeSpan ThrottleScaleDownInterval { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Phase 1 mock backlog size when <c>CONNECTOR_MOCK_PENDING_EVENTS</c> env var is unset. Removed in Phase 2.
    /// </summary>
    public int MockPendingEvents { get; set; } = 50;

    /// <inheritdoc />
    string IOptionsFormatter.Format()
    {
        return JsonSerializer.Serialize(new
        {
            DefaultConcurrency,
            DefaultBatchSize,
            ThrottleScaleDownInterval = ThrottleScaleDownInterval.ToString(),
            MockPendingEvents
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
