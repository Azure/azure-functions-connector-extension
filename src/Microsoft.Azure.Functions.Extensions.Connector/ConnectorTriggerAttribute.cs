// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace webhooks.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class ConnectorTriggerAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of <see cref="ConnectorTriggerAttribute"/>.
    /// </summary>
    /// <param name="connectorNamespace">The Connector Namespace resource name.</param>
    /// <param name="triggerName">The TriggerConfig resource name.</param>
    public ConnectorTriggerAttribute(string connectorNamespace, string triggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);
        ConnectorNamespace = connectorNamespace;
        TriggerName = triggerName;
    }

    /// <summary>
    /// The Connector Namespace resource name. Can use %AppSettingName% syntax.
    /// Validated against the x-ms-gateway-resource-name header on each callback.
    /// </summary>
    [AutoResolve]
    public string ConnectorNamespace { get; }

    /// <summary>
    /// The TriggerConfig resource name. Can use %AppSettingName% syntax.
    /// Validated against the x-ms-trigger-name header on each callback.
    /// </summary>
    [AutoResolve]
    public string TriggerName { get; }
}
