// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorMessageLock
{
    internal ConnectorMessageLock(string messageId, string lockToken)
    {
        MessageId = RequireValue(messageId, nameof(messageId));
        LockToken = RequireValue(lockToken, nameof(lockToken));
    }

    internal string MessageId { get; }

    internal string LockToken { get; }

    public override string ToString() =>
        $"{nameof(ConnectorMessageLock)} {{ " +
        $"{nameof(MessageId)} = [REDACTED], " +
        $"{nameof(LockToken)} = [REDACTED] }}";

    private static string RequireValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"Connector Poll {parameterName} must not be empty.",
                parameterName);
        }

        return value;
    }
}
