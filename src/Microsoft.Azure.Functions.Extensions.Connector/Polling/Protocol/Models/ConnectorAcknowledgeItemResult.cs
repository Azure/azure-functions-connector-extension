// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorAcknowledgeItemResult
{
    internal ConnectorAcknowledgeItemResult(
        string messageId,
        ConnectorAcknowledgeStatus status)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException(
                "Connector acknowledgement message ID must not be empty.",
                nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(status.ToString()))
        {
            throw new ArgumentException(
                "Connector acknowledgement status must not be empty.",
                nameof(status));
        }

        MessageId = messageId;
        Status = status;
    }

    internal string MessageId { get; }

    internal ConnectorAcknowledgeStatus Status { get; }

    internal bool IsAcknowledged => Status.IsAcknowledged;

    public override string ToString() =>
        $"{nameof(ConnectorAcknowledgeItemResult)} {{ " +
        $"{nameof(MessageId)} = [REDACTED], " +
        $"{nameof(Status)} = {(Status.IsKnown ? Status.ToString() : "[UNKNOWN]")} }}";
}
