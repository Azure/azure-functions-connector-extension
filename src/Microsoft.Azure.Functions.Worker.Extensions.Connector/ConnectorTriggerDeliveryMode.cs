// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Specifies how Connector Namespace delivers trigger events.
/// </summary>
public enum ConnectorTriggerDeliveryMode
{
    /// <summary>
    /// Connector Namespace pushes events to the extension webhook.
    /// </summary>
    Webhook,

    /// <summary>
    /// The extension polls Connector Namespace for events.
    /// </summary>
    Poll,
}
