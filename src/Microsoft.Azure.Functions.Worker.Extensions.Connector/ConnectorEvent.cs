// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Represents a Connector trigger event and its application-safe delivery metadata.
/// </summary>
/// <typeparam name="T">The Connector trigger payload type.</typeparam>
public sealed class ConnectorEvent<T>
{
    /// <summary>
    /// Gets the Connector trigger payload.
    /// </summary>
    public required T Data { get; init; }

    /// <summary>
    /// Gets the stable Connector delivery identifier for Poll delivery.
    /// The value is <see langword="null"/> for Webhook delivery.
    /// </summary>
    public string? MessageId { get; init; }
}
