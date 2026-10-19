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
    internal const int BuiltInDefaultMaxBatchSize = 1;
    internal const int BuiltInDefaultTargetPendingEventThreshold = 16;
    internal const int BuiltInDefaultMaxConcurrentCalls = 16;
    internal const int BuiltInDefaultMaxLinkedOutputSizeInBytes = 100 * 1024 * 1024;
    internal static readonly TimeSpan BuiltInDefaultMaxPollingInterval =
        TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Gets or sets the default number of events supplied to one function invocation.
    /// Valid values are one through 32.
    /// </summary>
    public int DefaultMaxBatchSize { get; set; } = BuiltInDefaultMaxBatchSize;

    /// <summary>
    /// Gets or sets the default desired number of pending Connector events per worker instance for target-based scaling.
    /// The value must be greater than zero.
    /// </summary>
    public int DefaultTargetPendingEventThreshold { get; set; } =
        BuiltInDefaultTargetPendingEventThreshold;

    /// <summary>
    /// Gets or sets the default maximum number of concurrent single-event processing tasks
    /// per listener on one worker instance. Each task includes preparation, invocation, and acknowledgement.
    /// The value must be greater than zero. It does not apply to batched delivery.
    /// </summary>
    public int DefaultMaxConcurrentCalls { get; set; } =
        BuiltInDefaultMaxConcurrentCalls;

    /// <summary>
    /// Gets or sets the maximum delay between Receive requests while a Poll
    /// trigger's queue remains empty.
    /// </summary>
    public TimeSpan MaxPollingInterval { get; set; } =
        BuiltInDefaultMaxPollingInterval;

    /// <summary>
    /// Gets or sets the maximum serialized output size downloaded from one Poll outputs link.
    /// The value must be greater than zero and defaults to 100 MiB (104,857,600 bytes).
    /// This does not apply to inline Poll outputs or Webhook requests.
    /// </summary>
    public int MaxLinkedOutputSizeInBytes { get; set; } =
        BuiltInDefaultMaxLinkedOutputSizeInBytes;

    string IOptionsFormatter.Format() => JsonSerializer.Serialize(
        new
        {
            DefaultMaxBatchSize,
            DefaultTargetPendingEventThreshold,
            DefaultMaxConcurrentCalls,
            MaxPollingInterval,
            MaxLinkedOutputSizeInBytes,
        },
        SerializerOptions);
}
