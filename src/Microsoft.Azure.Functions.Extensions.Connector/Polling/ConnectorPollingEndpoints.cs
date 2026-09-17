// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Opaque runtime endpoints returned by Connector Namespace for a Poll trigger configuration.
/// </summary>
internal sealed record ConnectorPollingEndpoints(
    Uri ReceiveUri,
    Uri AcknowledgeUri,
    Uri HasMessagesUri,
    Uri ApproximateQueueDepthUri);
