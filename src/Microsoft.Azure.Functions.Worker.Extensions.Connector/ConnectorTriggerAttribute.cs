// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Connector.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for the Connector extension (isolated worker model).
/// Apply this to a function parameter to trigger the function via HTTP webhook.
/// 
/// Functions with this trigger will be invoked via HTTP POST requests to:
/// /runtime/webhooks/connector?functionName={FunctionName}
/// </summary>
/// <remarks>
/// The InputConverter attribute specifies which converter to use for deserializing
/// the trigger data from the host process into the worker process.
/// 
/// Supported parameter types:
/// - <see cref="ConnectorContext"/>: Full context with body, headers, query params
/// - <see cref="string"/>: Raw request body as string
/// - Any POCO: Automatically deserialized from JSON body
/// </remarks>
[InputConverter(typeof(ConnectorContextConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Creates a new ConnectorTriggerAttribute.
    /// </summary>
    public ConnectorTriggerAttribute()
    {
    }
}
