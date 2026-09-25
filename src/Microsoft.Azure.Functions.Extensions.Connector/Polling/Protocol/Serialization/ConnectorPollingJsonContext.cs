// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Connector;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = false)]
[JsonSerializable(typeof(ConnectorReceiveResponseWireDto))]
[JsonSerializable(typeof(ConnectorOutputsLinkWireDto))]
[JsonSerializable(typeof(ConnectorAcknowledgeRequestWireDto))]
[JsonSerializable(typeof(ConnectorAcknowledgeResponseWireDto))]
[JsonSerializable(typeof(ConnectorApproximateQueueDepthWireDto))]
internal partial class ConnectorPollingJsonContext : JsonSerializerContext
{
}
