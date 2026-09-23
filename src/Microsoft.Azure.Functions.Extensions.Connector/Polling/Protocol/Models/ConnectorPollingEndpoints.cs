// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorPollingEndpoints
{
    internal ConnectorPollingEndpoints(
        Uri receiveUri,
        Uri acknowledgeUri,
        Uri approximateQueueDepthUri)
    {
        ReceiveUri = ConnectorPollingUri.Validate(receiveUri, nameof(receiveUri));
        AcknowledgeUri = ConnectorPollingUri.Validate(acknowledgeUri, nameof(acknowledgeUri));
        ApproximateQueueDepthUri = ConnectorPollingUri.Validate(
            approximateQueueDepthUri,
            nameof(approximateQueueDepthUri));
    }

    internal Uri ReceiveUri { get; }

    internal Uri AcknowledgeUri { get; }

    internal Uri ApproximateQueueDepthUri { get; }

    public override string ToString() =>
        $"{nameof(ConnectorPollingEndpoints)} {{ " +
        $"{nameof(ReceiveUri)} = {ConnectorPollingUri.Redact(ReceiveUri)}, " +
        $"{nameof(AcknowledgeUri)} = {ConnectorPollingUri.Redact(AcknowledgeUri)}, " +
        $"{nameof(ApproximateQueueDepthUri)} = {ConnectorPollingUri.Redact(ApproximateQueueDepthUri)} }}";
}
