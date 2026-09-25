// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorTriggerInput
{
    private readonly ReadOnlyCollection<ConnectorTriggerEventInput> _events;

    private ConnectorTriggerInput(
        IReadOnlyList<ConnectorTriggerEventInput> events,
        bool isBatched)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            throw new ArgumentException(
                "Connector trigger input must contain at least one event.",
                nameof(events));
        }

        if (events.Any(static connectorEvent => connectorEvent is null))
        {
            throw new ArgumentException(
                "Connector trigger input must not contain null events.",
                nameof(events));
        }

        _events = Array.AsReadOnly(events.ToArray());
        IsBatched = isBatched;
    }

    internal IReadOnlyList<ConnectorTriggerEventInput> Events => _events;

    internal bool IsBatched { get; }

    internal static ConnectorTriggerInput FromSingle(
        BinaryData outputs,
        string? messageId,
        ConnectorTriggerDeliveryMode deliveryMode) =>
        new(
            [new ConnectorTriggerEventInput(outputs, messageId, deliveryMode)],
            isBatched: false);

    internal static ConnectorTriggerInput FromBatch(
        IReadOnlyList<ConnectorTriggerEventInput> events) =>
        new(events, isBatched: true);

    internal string ToSinglePayloadJson()
    {
        if (IsBatched)
        {
            throw new InvalidOperationException(
                "A batched Connector trigger input does not have one payload JSON value.");
        }

        return _events[0].Outputs.ToString();
    }
}

internal sealed class ConnectorTriggerEventInput
{
    internal ConnectorTriggerEventInput(
        BinaryData outputs,
        string? messageId,
        ConnectorTriggerDeliveryMode deliveryMode)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        Outputs = outputs;
        MessageId = messageId;
        DeliveryMode = deliveryMode;
    }

    internal BinaryData Outputs { get; }

    internal string? MessageId { get; }

    internal ConnectorTriggerDeliveryMode DeliveryMode { get; }
}
