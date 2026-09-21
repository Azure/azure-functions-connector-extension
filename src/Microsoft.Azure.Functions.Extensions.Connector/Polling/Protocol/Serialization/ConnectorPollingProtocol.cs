// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingProtocol
{
    internal static ConnectorReceiveResult DeserializeReceive(
        BinaryData content,
        bool moreMessagesAvailable)
    {
        ArgumentNullException.ThrowIfNull(content);

        ConnectorReceiveResponseWireDto response = Deserialize(
            content,
            ConnectorPollingJsonContext.Default.ConnectorReceiveResponseWireDto,
            "receive");
        List<ConnectorPollMessageWireDto?> wireMessages =
            response.Messages ??
            throw ProtocolError("Receive response must contain a messages array.");

        if (wireMessages.Count > ConnectorPollingProtocolLimits.MaximumBatchSize)
        {
            throw ProtocolError(
                $"Receive response must not contain more than {ConnectorPollingProtocolLimits.MaximumBatchSize} messages.");
        }

        var messages = new List<ConnectorPollMessage>(wireMessages.Count);
        for (int index = 0; index < wireMessages.Count; index++)
        {
            ConnectorPollMessageWireDto wireMessage =
                wireMessages[index] ??
                throw ProtocolError(
                    $"Receive response message at index {index} must be a JSON object.");
            messages.Add(ParseMessage(wireMessage, index));
        }

        return new ConnectorReceiveResult(messages, moreMessagesAvailable);
    }

    internal static BinaryData SerializeAcknowledgeRequest(
        IReadOnlyList<ConnectorMessageLock> messageLocks)
    {
        ValidateMessageLocks(messageLocks);

        var messages = new List<ConnectorMessageLockWireDto>(messageLocks.Count);
        foreach (ConnectorMessageLock messageLock in messageLocks)
        {
            ArgumentNullException.ThrowIfNull(messageLock);
            messages.Add(new ConnectorMessageLockWireDto
            {
                MessageId = messageLock.MessageId,
                LockToken = messageLock.LockToken,
            });
        }

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(
            new ConnectorAcknowledgeRequestWireDto { Messages = messages },
            ConnectorPollingJsonContext.Default.ConnectorAcknowledgeRequestWireDto);
        return new BinaryData(content);
    }

    internal static ConnectorAcknowledgeResult DeserializeAcknowledge(
        BinaryData content,
        IReadOnlyList<ConnectorMessageLock> submittedLocks)
    {
        ArgumentNullException.ThrowIfNull(content);
        ValidateMessageLocks(submittedLocks);

        ConnectorAcknowledgeResponseWireDto response = Deserialize(
            content,
            ConnectorPollingJsonContext.Default.ConnectorAcknowledgeResponseWireDto,
            "acknowledgement");
        List<ConnectorAcknowledgeItemWireDto?> wireResults =
            response.Results ??
            throw ProtocolError("Acknowledgement response must contain a results array.");

        if (wireResults.Count != submittedLocks.Count)
        {
            throw ProtocolError(
                "Acknowledgement response result count must match the submitted message count.");
        }

        var results = new List<ConnectorAcknowledgeItemResult>(wireResults.Count);
        for (int index = 0; index < wireResults.Count; index++)
        {
            ConnectorAcknowledgeItemWireDto wireResult =
                wireResults[index] ??
                throw ProtocolError(
                    $"Acknowledgement response result at index {index} must be a JSON object.");
            string messageId = RequireValue(
                wireResult.MessageId,
                $"results[{index}].messageId");
            string statusValue = RequireValue(
                wireResult.Status,
                $"results[{index}].status");
            ConnectorMessageLock submittedLock =
                submittedLocks[index] ??
                throw new ArgumentException(
                    $"Submitted message lock at index {index} must not be null.",
                    nameof(submittedLocks));

            if (!messageId.Equals(submittedLock.MessageId, StringComparison.Ordinal))
            {
                throw ProtocolError(
                    $"Acknowledgement response message ID at index {index} does not match the submitted message.");
            }

            results.Add(new ConnectorAcknowledgeItemResult(
                messageId,
                ConnectorAcknowledgeStatus.FromWireValue(statusValue)));
        }

        return new ConnectorAcknowledgeResult(results);
    }

    internal static ConnectorQueueStatus DeserializeQueueStatus(
        BinaryData hasMessagesContent,
        BinaryData approximateQueueDepthContent)
    {
        ArgumentNullException.ThrowIfNull(hasMessagesContent);
        ArgumentNullException.ThrowIfNull(approximateQueueDepthContent);

        bool hasMessages = DeserializeHasMessages(hasMessagesContent);
        long approximateQueueDepth =
            DeserializeApproximateQueueDepth(approximateQueueDepthContent);

        return new ConnectorQueueStatus(hasMessages, approximateQueueDepth);
    }

    internal static bool DeserializeHasMessages(BinaryData content)
    {
        ArgumentNullException.ThrowIfNull(content);

        ConnectorHasMessagesWireDto response = Deserialize(
            content,
            ConnectorPollingJsonContext.Default.ConnectorHasMessagesWireDto,
            "has-messages");
        return response.HasMessages ??
            throw ProtocolError(
                "Has-messages response must contain a boolean hasMessages property.");
    }

    internal static long DeserializeApproximateQueueDepth(BinaryData content)
    {
        ArgumentNullException.ThrowIfNull(content);

        ConnectorApproximateQueueDepthWireDto response = Deserialize(
            content,
            ConnectorPollingJsonContext.Default.ConnectorApproximateQueueDepthWireDto,
            "approximate-queue-depth");
        long approximateQueueDepth =
            response.ApproximateQueueDepth ??
            throw ProtocolError(
                "Queue-depth response must contain an integer approximateQueueDepth property.");

        if (approximateQueueDepth < 0)
        {
            throw ProtocolError(
                "Queue-depth response approximateQueueDepth must not be negative.");
        }

        return approximateQueueDepth;
    }

    private static ConnectorPollMessage ParseMessage(
        ConnectorPollMessageWireDto wireMessage,
        int index)
    {
        string messageId = RequireValue(
            wireMessage.MessageId,
            $"messages[{index}].messageId");
        string lockToken = RequireValue(
            wireMessage.LockToken,
            $"messages[{index}].lockToken");
        bool hasInlineOutputs =
            wireMessage.Outputs.ValueKind is not JsonValueKind.Undefined and
                not JsonValueKind.Null;
        bool hasOutputsLink =
            wireMessage.OutputsLink.ValueKind is not JsonValueKind.Undefined and
                not JsonValueKind.Null;

        if (hasInlineOutputs == hasOutputsLink)
        {
            throw ProtocolError(
                $"Receive response message at index {index} must contain exactly one of outputs and outputsLink.");
        }

        if (hasInlineOutputs)
        {
            if (wireMessage.Outputs.ValueKind != JsonValueKind.Object)
            {
                throw ProtocolError(
                    $"Receive response property 'messages[{index}].outputs' must be a JSON object.");
            }

            return ConnectorPollMessage.FromInlineOutputs(
                messageId,
                lockToken,
                BinaryData.FromString(wireMessage.Outputs.GetRawText()));
        }

        if (wireMessage.OutputsLink.ValueKind != JsonValueKind.Object)
        {
            throw ProtocolError(
                $"Receive response property 'messages[{index}].outputsLink' must be a JSON object.");
        }

        ConnectorOutputsLinkWireDto? outputsLink =
            wireMessage.OutputsLink.Deserialize(
                ConnectorPollingJsonContext.Default.ConnectorOutputsLinkWireDto);
        if (outputsLink is null)
        {
            throw ProtocolError(
                $"Receive response property 'messages[{index}].outputsLink' must be a JSON object.");
        }

        Uri uri = ConnectorPollingUri.Parse(
            outputsLink.Uri,
            $"messages[{index}].outputsLink.uri");

        return ConnectorPollMessage.FromOutputsLink(
            messageId,
            lockToken,
            new ConnectorOutputsLink(uri));
    }

    private static T Deserialize<T>(
        BinaryData content,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
        string operation)
    {
        T? result;
        try
        {
            using Stream stream = content.ToStream();
            result = JsonSerializer.Deserialize(stream, typeInfo);
        }
        catch (JsonException exception)
        {
            throw ProtocolError(
                $"Connector Poll {operation} response contains invalid JSON.",
                exception);
        }

        return result ??
            throw ProtocolError(
                $"Connector Poll {operation} response must contain a JSON object.");
    }

    private static string RequireValue(string? value, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ProtocolError(
                $"Connector Poll response property '{propertyPath}' must not be empty.");
        }

        return value;
    }

    private static void ValidateMessageLocks(
        IReadOnlyList<ConnectorMessageLock> messageLocks)
    {
        ArgumentNullException.ThrowIfNull(messageLocks);

        if (messageLocks.Count is < 1 or > ConnectorPollingProtocolLimits.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(messageLocks),
                $"Connector acknowledgement requests must contain between 1 and {ConnectorPollingProtocolLimits.MaximumBatchSize} messages.");
        }

        for (int index = 0; index < messageLocks.Count; index++)
        {
            if (messageLocks[index] is null)
            {
                throw new ArgumentException(
                    $"Submitted message lock at index {index} must not be null.",
                    nameof(messageLocks));
            }
        }
    }

    private static JsonException ProtocolError(
        string message,
        Exception? innerException = null) =>
        new(message, innerException);
}
