// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingWirePropertyNames
{
    internal const string ApproximateQueueDepth = "approximateQueueDepth";
    internal const string LockToken = "lockToken";
    internal const string MessageId = "messageId";
    internal const string Messages = "messages";
    internal const string Outputs = "outputs";
    internal const string OutputsLink = "outputsLink";
    internal const string Results = "results";
    internal const string Status = "status";
    internal const string Uri = "uri";
}

internal sealed class ConnectorReceiveResponseWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.Messages)]
    public List<ConnectorPollMessageWireDto?>? Messages { get; init; }
}

internal sealed class ConnectorPollMessageWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.MessageId)]
    public string? MessageId { get; init; }

    [JsonPropertyName(ConnectorPollingWirePropertyNames.LockToken)]
    public string? LockToken { get; init; }

    [JsonPropertyName(ConnectorPollingWirePropertyNames.Outputs)]
    public JsonElement Outputs { get; init; }

    [JsonPropertyName(ConnectorPollingWirePropertyNames.OutputsLink)]
    public JsonElement OutputsLink { get; init; }
}

internal sealed class ConnectorOutputsLinkWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.Uri)]
    public string? Uri { get; init; }
}

internal sealed class ConnectorAcknowledgeRequestWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.Messages)]
    public required List<ConnectorMessageLockWireDto> Messages { get; init; }
}

internal sealed class ConnectorMessageLockWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.MessageId)]
    public required string MessageId { get; init; }

    [JsonPropertyName(ConnectorPollingWirePropertyNames.LockToken)]
    public required string LockToken { get; init; }
}

internal sealed class ConnectorAcknowledgeResponseWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.Results)]
    public List<ConnectorAcknowledgeItemWireDto?>? Results { get; init; }
}

internal sealed class ConnectorAcknowledgeItemWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.MessageId)]
    public string? MessageId { get; init; }

    [JsonPropertyName(ConnectorPollingWirePropertyNames.Status)]
    public string? Status { get; init; }
}

internal sealed class ConnectorApproximateQueueDepthWireDto
{
    [JsonPropertyName(ConnectorPollingWirePropertyNames.ApproximateQueueDepth)]
    public long? ApproximateQueueDepth { get; init; }
}
