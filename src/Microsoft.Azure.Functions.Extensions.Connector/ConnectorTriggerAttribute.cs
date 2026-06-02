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
    public ConnectorTriggerAttribute(string connectorNamespace, string triggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);
        ConnectorNamespace = connectorNamespace;
        TriggerName = triggerName;
    }

    /// <summary>
    /// Connector Namespace resource name. Supports <c>%AppSettingName%</c>.
    /// </summary>
    [AutoResolve]
    public string ConnectorNamespace { get; }

    /// <summary>
    /// TriggerConfig resource name within the Namespace. Supports <c>%AppSettingName%</c>.
    /// </summary>
    [AutoResolve]
    public string TriggerName { get; }

    /// <summary>
    /// Per-function concurrency override. Positive values override <c>host.json</c>'s
    /// <c>extensions.connector.defaultConcurrency</c>; <c>0</c> means unset.
    /// </summary>
    public int Concurrency { get; set; }

    /// <summary>
    /// Per-function batch size override (events delivered per invocation). Positive
    /// values override <c>host.json</c>'s <c>extensions.connector.defaultBatchSize</c>;
    /// <c>0</c> means unset.
    /// </summary>
    public int BatchSize { get; set; }
}
