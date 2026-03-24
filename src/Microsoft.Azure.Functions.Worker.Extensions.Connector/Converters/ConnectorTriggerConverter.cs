// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Converter for ConnectorTrigger that converts JSON string to POCO types.
/// Registers with the worker to handle deserialization of trigger payloads.
/// </summary>
[SupportedTargetType(typeof(object))]
internal sealed class ConnectorTriggerConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            // Source from host is always a JSON string
            if (context.Source is not string json)
            {
                return new ValueTask<ConversionResult>(
                    ConversionResult.Failed(new InvalidOperationException("Expected string source from host")));
            }

            // If target is string, return as-is
            if (context.TargetType == typeof(string))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(json));
            }

            // Deserialize to target POCO type
            var result = JsonSerializer.Deserialize(json, context.TargetType);
            return new ValueTask<ConversionResult>(ConversionResult.Success(result));
        }
        catch (JsonException ex)
        {
            return new ValueTask<ConversionResult>(
                ConversionResult.Failed(new InvalidOperationException(
                    $"Failed to deserialize connector payload to {context.TargetType.Name}. " +
                    "Ensure the payload matches the expected schema.", ex)));
        }
    }
}
