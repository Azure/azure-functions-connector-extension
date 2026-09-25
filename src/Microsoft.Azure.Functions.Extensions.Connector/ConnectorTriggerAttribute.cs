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
    /// Gets or sets the app setting expression containing the complete
    /// trigger-specific Connector Poll endpoint base.
    /// </summary>
    [AutoResolve]
    public string? PollingEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of events supplied to one function invocation.
    /// Valid batch sizes are one through 32. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultMaxBatchSize"/>.
    /// Values greater than one require a batched function parameter.
    /// </summary>
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent function invocations per worker instance.
    /// The value must be non-negative. A value of zero uses
    /// <see cref="ConnectorOptions.DefaultConcurrency"/>.
    /// </summary>
    public int Concurrency { get; set; }
}
