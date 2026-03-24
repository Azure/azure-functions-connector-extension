// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger attribute for AI Gateway connector webhooks.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class ConnectorTriggerAttribute : Attribute
{
    /// <summary>
    /// The connector name (e.g., "office365", "sharepointonline", "teams").
    /// </summary>
    public string? Connector { get; set; }

    internal string? ConnectorName => Connector;

    /// <summary>
    /// The trigger operation ID (e.g., "OnNewEmailV3", "OnNewChannelMessage").
    /// </summary>
    public string? Operation { get; set; }

    internal string? OperationName => Operation;

    /// <summary>
    /// The AI Gateway connection name. Can use %AppSettingName% syntax.
    /// </summary>
    public string? Connection { get; set; }

    internal string? ConnectionName => Connection;
}
