// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.WebJobs.Hosting;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Options for the Connector extension.
/// </summary>
public sealed class ConnectorOptions : IOptionsFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Gets or sets the default number of events supplied to one function invocation.
    /// Valid values are one through 32.
    /// </summary>
    public int DefaultMaxBatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default desired number of pending Connector events per worker instance for target-based scaling.
    /// The value must be greater than zero.
    /// </summary>
    public int DefaultTargetPendingEventThreshold { get; set; } = 16;

    /// <summary>
    /// Gets or sets the default maximum number of concurrent function invocations per worker instance.
    /// The value must be greater than zero.
    /// </summary>
    public int DefaultMaxConcurrentCalls { get; set; } = 16;

    /// <summary>
    /// Gets or sets the maximum delay between Receive requests while a Poll
    /// trigger's queue remains empty.
    /// </summary>
    public TimeSpan MaxPollingInterval { get; set; } =
        TimeSpan.FromSeconds(30);

    string IOptionsFormatter.Format() => JsonSerializer.Serialize(
        new
        {
            DefaultMaxBatchSize,
            DefaultTargetPendingEventThreshold,
            DefaultMaxConcurrentCalls,
            MaxPollingInterval,
        },
        SerializerOptions);
}
