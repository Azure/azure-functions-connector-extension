// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace events.
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute, ISupportCardinality
{
    private bool _isBatched;

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
    /// Gets or sets whether multiple events are supplied to each function
    /// invocation. The default is <see langword="false"/>.
    /// </summary>
    [DefaultValue(false)]
    public bool IsBatched
    {
        get => _isBatched;
        set => _isBatched = value;
    }

    /// <summary>
    /// Gets or sets the maximum number of events supplied to one function invocation.
    /// Valid batch sizes are one through 32. A value of zero uses the host-level default.
    /// Values greater than one require <see cref="IsBatched"/>.
    /// </summary>
    public int MaxBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent function invocations per worker instance.
    /// The value must be non-negative. A value of zero uses the host-level default.
    /// </summary>
    public int Concurrency { get; set; }

    Cardinality ISupportCardinality.Cardinality
    {
        get => _isBatched ? Cardinality.Many : Cardinality.One;
        set => _isBatched = value == Cardinality.Many;
    }
}
