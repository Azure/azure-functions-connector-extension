// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace webhooks (isolated worker).
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
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
    public string ConnectorNamespace { get; }

    /// <summary>
    /// TriggerConfig resource name within the Namespace. Supports <c>%AppSettingName%</c>.
    /// </summary>
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

