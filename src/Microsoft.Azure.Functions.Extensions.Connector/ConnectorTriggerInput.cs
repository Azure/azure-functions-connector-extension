// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Text.Json;

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

    internal string ToPayloadJson()
    {
        if (!IsBatched)
        {
            return _events[0].Outputs.ToString();
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (ConnectorTriggerEventInput connectorEvent in _events)
            {
                writer.WriteRawValue(
                    connectorEvent.Outputs.ToMemory().Span,
                    skipInputValidation: false);
            }

            writer.WriteEndArray();
        }

        return BinaryData.FromBytes(stream.ToArray()).ToString();
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
