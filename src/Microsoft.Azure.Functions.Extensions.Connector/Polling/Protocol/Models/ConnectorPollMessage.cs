// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorPollMessage
{
    private ConnectorPollMessage(
        ConnectorMessageLock messageLock,
        BinaryData? outputs,
        ConnectorOutputsLink? outputsLink)
    {
        MessageLock = messageLock;
        Outputs = outputs;
        OutputsLink = outputsLink;
    }

    internal string MessageId => MessageLock.MessageId;

    internal string LockToken => MessageLock.LockToken;

    internal BinaryData? Outputs { get; }

    internal ConnectorOutputsLink? OutputsLink { get; }

    internal ConnectorMessageLock MessageLock { get; }

    internal static ConnectorPollMessage FromInlineOutputs(
        string messageId,
        string lockToken,
        BinaryData outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        return new ConnectorPollMessage(
            new ConnectorMessageLock(messageId, lockToken),
            new BinaryData(outputs.ToArray()),
            outputsLink: null);
    }

    internal static ConnectorPollMessage FromOutputsLink(
        string messageId,
        string lockToken,
        ConnectorOutputsLink outputsLink)
    {
        ArgumentNullException.ThrowIfNull(outputsLink);
        return new ConnectorPollMessage(
            new ConnectorMessageLock(messageId, lockToken),
            outputs: null,
            outputsLink);
    }

    public override string ToString() =>
        $"{nameof(ConnectorPollMessage)} {{ " +
        $"{nameof(MessageId)} = [REDACTED], " +
        $"{nameof(LockToken)} = [REDACTED], " +
        $"OutputSource = {(Outputs is null ? nameof(OutputsLink) : nameof(Outputs))} }}";
}
