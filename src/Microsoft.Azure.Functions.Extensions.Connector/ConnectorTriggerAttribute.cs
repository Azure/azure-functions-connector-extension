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
    /// The AI Gateway endpoint name. Resolved from app settings using the
    /// {name}__endpoint convention. Can use %AppSettingName% syntax.
    /// Allows different functions to target different gateway instances.
    /// </summary>
    public string? AIGateway { get; set; }

    internal string? GatewayName => AIGateway;

    /// <summary>
    /// The connector type name (e.g., "office365", "sharepointonline", "teams").
    /// </summary>
    public string? ConnectorType { get; set; }

    internal string? ConnectorName => ConnectorType;

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
