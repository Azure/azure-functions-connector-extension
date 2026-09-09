// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Immutable configuration for a Connector Namespace Poll trigger.
/// </summary>
internal sealed record ConnectorPollingOptions(
    string? Connection,
    string? TriggerConfigName,
    int MaxEvents);
