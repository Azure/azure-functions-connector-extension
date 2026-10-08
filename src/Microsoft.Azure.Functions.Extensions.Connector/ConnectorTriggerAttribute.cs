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
    /// Gets or sets the name of the Connector Namespace trigger configuration.
    /// </summary>
    [AutoResolve]
    public string? TriggerConfigName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of events supplied to one function invocation.
    /// Valid values are zero through 32. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultMaxBatchSize"/>.
    /// </summary>
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pending events per worker instance.
    /// The value must be non-negative. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultConcurrency"/>.
    /// </summary>
    public int Concurrency { get; set; }
}
