// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorTriggerInput
{
    private readonly BinaryData _outputs;

    private ConnectorTriggerInput(
        BinaryData outputs,
        string? messageId,
        ConnectorTriggerDeliveryMode deliveryMode)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        _outputs = outputs;
        MessageId = messageId;
        DeliveryMode = deliveryMode;
    }

    internal BinaryData Outputs => _outputs;

    internal string? MessageId { get; }

    internal ConnectorTriggerDeliveryMode DeliveryMode { get; }

    internal static ConnectorTriggerInput FromSingle(
        BinaryData outputs,
        string? messageId,
        ConnectorTriggerDeliveryMode deliveryMode) =>
        new(outputs, messageId, deliveryMode);

    internal string ToPayloadJson() => _outputs.ToString();
}
