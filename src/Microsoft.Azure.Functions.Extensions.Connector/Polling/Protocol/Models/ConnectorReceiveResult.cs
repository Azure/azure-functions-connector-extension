// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorReceiveResult
{
    internal ConnectorReceiveResult(
        IReadOnlyList<ConnectorPollMessage> messages,
        bool moreMessagesAvailable)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Any(static message => message is null))
        {
            throw new ArgumentException(
                "Connector receive results must not contain null messages.",
                nameof(messages));
        }

        Messages = Array.AsReadOnly(messages.ToArray());
        MoreMessagesAvailable = moreMessagesAvailable;
    }

    internal IReadOnlyList<ConnectorPollMessage> Messages { get; }

    internal bool MoreMessagesAvailable { get; }

    public override string ToString() =>
        $"{nameof(ConnectorReceiveResult)} {{ " +
        $"MessageCount = {Messages.Count}, " +
        $"{nameof(MoreMessagesAvailable)} = {MoreMessagesAvailable} }}";
}
