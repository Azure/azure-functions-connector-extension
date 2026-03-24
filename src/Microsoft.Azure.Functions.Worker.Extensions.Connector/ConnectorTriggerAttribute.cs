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
    /// The connector name (e.g., "office365", "sharepointonline", "teams").
    /// </summary>
    public string? Connector { get; set; }

    /// <summary>
    /// The trigger operation ID (e.g., "OnNewEmailV3", "OnNewChannelMessage").
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// The AI Gateway connection name. Can use %AppSettingName% syntax.
    /// </summary>
    public string? Connection { get; set; }
}
