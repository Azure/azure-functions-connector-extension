// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace webhooks.
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Creates a new instance of <see cref="ConnectorTriggerAttribute"/>.
    /// </summary>
    /// <param name="connectorNamespace">The Connector Namespace resource name.</param>
    /// <param name="triggerName">The TriggerConfig resource name.</param>
    public ConnectorTriggerAttribute(string connectorNamespace, string triggerName)
    {
        ConnectorNamespace = connectorNamespace ?? throw new ArgumentNullException(nameof(connectorNamespace));
        TriggerName = triggerName ?? throw new ArgumentNullException(nameof(triggerName));
    }

    /// <summary>
    /// The Connector Namespace resource name. Can use %AppSettingName% syntax.
    /// Validated against the x-ms-gateway-resource-name header on each callback.
    /// </summary>
    public string ConnectorNamespace { get; }

    /// <summary>
    /// The TriggerConfig resource name. Can use %AppSettingName% syntax.
    /// Validated against the x-ms-trigger-name header on each callback.
    /// </summary>
    public string TriggerName { get; }
}
