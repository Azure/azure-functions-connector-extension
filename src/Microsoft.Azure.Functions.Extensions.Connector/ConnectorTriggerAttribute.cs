// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Attribute that marks a function parameter as a Connector trigger.
/// Functions with this trigger will be invoked via HTTP POST requests to:
/// /runtime/webhooks/connector?functionName={FunctionName}
/// </summary>
/// <remarks>
/// Supported parameter types:
/// - <see cref="ConnectorContext"/>: Full context with body, headers, query params
/// - <see cref="string"/>: Raw request body as string
/// - <see cref="Newtonsoft.Json.Linq.JToken"/>: Parsed JSON body
/// - Any POCO: Automatically deserialized from JSON body
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class ConnectorTriggerAttribute : Attribute
{
    /// <summary>
    /// Creates a new ConnectorTriggerAttribute.
    /// </summary>
    public ConnectorTriggerAttribute()
    {
    }
}
