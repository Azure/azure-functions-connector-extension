// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Immutable connection configuration for Connector Namespace Poll delivery.
/// </summary>
internal sealed record ConnectorConnectionOptions(
    ResourceIdentifier ResourceId,
    TokenCredential Credential);
