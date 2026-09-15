// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace events.
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
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
    public string? TriggerConfigName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of events supplied to one function invocation.
    /// Valid values are zero through 32. A value of zero uses the host-level default.
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent function invocations per worker.
    /// The value must be non-negative. A value of zero uses the host-level default.
    /// </summary>
    public int Concurrency { get; set; }
}
