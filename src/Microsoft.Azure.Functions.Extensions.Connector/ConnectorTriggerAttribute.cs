// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace events.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class ConnectorTriggerAttribute : Attribute
{
    /// <summary>
    /// Gets or sets how Connector Namespace delivers trigger events.
    /// </summary>
    public ConnectorTriggerDeliveryMode DeliveryMode { get; set; } = ConnectorTriggerDeliveryMode.Webhook;

    /// <summary>
    /// Gets or sets the app setting name or prefix for the Connector Namespace connection.
    /// </summary>
    public string? Connection { get; set; }

    /// <summary>
    /// Gets or sets the Connector Namespace Trigger Config name.
    /// </summary>
    [AutoResolve]
    public string? TriggerConfigName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of events supplied to one function invocation.
    /// Valid batch sizes are one through 32. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultMaxBatchSize"/>.
    /// Values greater than one require a batched function parameter.
    /// Batched delivery processes one batch at a time per listener, even when the batch size is one.
    /// </summary>
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent single-event processing tasks per listener
    /// on one worker instance. Each task includes preparation, invocation, and acknowledgement.
    /// The value must be non-negative. In single-event mode, a value of zero uses
    /// <see cref="ConnectorOptions.DefaultMaxConcurrentCalls"/>.
    /// Applies only to single-event delivery, as in the Service Bus extension.
    /// Batched bindings ignore positive values with a startup warning and process one batch at a time per listener.
    /// </summary>
    public int MaxConcurrentCalls { get; set; }

    /// <summary>
    /// Gets or sets the desired number of pending Connector events per worker instance for target-based scaling.
    /// The value must be non-negative. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultTargetPendingEventThreshold"/>.
    /// </summary>
    public int TargetPendingEventThreshold { get; set; }
}
