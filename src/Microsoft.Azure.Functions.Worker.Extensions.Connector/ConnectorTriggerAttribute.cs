// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for AI Gateway connector webhooks.
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// The AI Gateway endpoint name. Resolved from app settings using the
    /// {name}__endpoint convention. Can use %AppSettingName% syntax.
    /// Allows different functions to target different gateway instances.
    /// </summary>
    public string? AIGateway { get; set; }

    /// <summary>
    /// The connector type name (e.g., "office365", "sharepointonline", "teams").
    /// </summary>
    public string? ConnectorType { get; set; }

    /// <summary>
    /// The trigger operation ID (e.g., "OnNewEmailV3", "OnNewChannelMessage").
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// The AI Gateway connection name. Can use %AppSettingName% syntax.
    /// </summary>
    public string? Connection { get; set; }
}
