// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorQueueStatus
{
    internal ConnectorQueueStatus(bool hasMessages, long approximateQueueDepth)
    {
        if (approximateQueueDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approximateQueueDepth),
                "Connector approximate queue depth must not be negative.");
        }

        HasMessages = hasMessages;
        ApproximateQueueDepth = approximateQueueDepth;
    }

    internal bool HasMessages { get; }

    internal long ApproximateQueueDepth { get; }
}
