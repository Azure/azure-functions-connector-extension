// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorReceiveResponseWireDto
{
    [JsonPropertyName("messages")]
    public List<ConnectorPollMessageWireDto?>? Messages { get; init; }
}

internal sealed class ConnectorPollMessageWireDto
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("lockToken")]
    public string? LockToken { get; init; }

    [JsonPropertyName("outputs")]
    public JsonElement Outputs { get; init; }

    [JsonPropertyName("outputsLink")]
    public JsonElement OutputsLink { get; init; }
}

internal sealed class ConnectorOutputsLinkWireDto
{
    [JsonPropertyName("uri")]
    public string? Uri { get; init; }
}

internal sealed class ConnectorAcknowledgeRequestWireDto
{
    [JsonPropertyName("messages")]
    public required List<ConnectorMessageLockWireDto> Messages { get; init; }
}

internal sealed class ConnectorMessageLockWireDto
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    [JsonPropertyName("lockToken")]
    public required string LockToken { get; init; }
}

internal sealed class ConnectorAcknowledgeResponseWireDto
{
    [JsonPropertyName("results")]
    public List<ConnectorAcknowledgeItemWireDto?>? Results { get; init; }
}

internal sealed class ConnectorAcknowledgeItemWireDto
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

internal sealed class ConnectorHasMessagesWireDto
{
    [JsonPropertyName("hasMessages")]
    public bool? HasMessages { get; init; }
}

internal sealed class ConnectorApproximateQueueDepthWireDto
{
    [JsonPropertyName("approximateQueueDepth")]
    public long? ApproximateQueueDepth { get; init; }
}
