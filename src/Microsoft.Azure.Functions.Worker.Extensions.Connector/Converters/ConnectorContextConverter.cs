// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Converters;

/// <summary>
/// Converter that deserializes the ConnectorContext from the host process.
/// This converter is used by the isolated worker model to convert the
/// serialized trigger data into a usable ConnectorContext object.
/// </summary>
internal sealed class ConnectorContextConverter : IInputConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 32,  // Prevent deep nesting attacks
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow
    };

    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        try
        {
            // The source should be the serialized ConnectorContext from the host
            string? sourceJson = context.Source as string;
            if (sourceJson == null)
            {
                // Try to get the raw data
                if (context.Source is ReadOnlyMemory<byte> bytes && bytes.Length > 0)
                {
                    sourceJson = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
                }
                else if (context.Source is byte[] byteArray && byteArray.Length > 0)
                {
                    sourceJson = System.Text.Encoding.UTF8.GetString(byteArray);
                }
                else
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
                }
            }

            // If the target type is ConnectorContext, deserialize directly
            if (context.TargetType == typeof(ConnectorContext))
            {
                var connectorContext = JsonSerializer.Deserialize<ConnectorContext>(sourceJson, JsonOptions);
                if (connectorContext != null)
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(connectorContext));
                }
            }

            // If the target type is string, return the body
            if (context.TargetType == typeof(string))
            {
                var connectorContext = JsonSerializer.Deserialize<ConnectorContext>(sourceJson, JsonOptions);
                if (connectorContext != null)
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(connectorContext.Body));
                }
            }

            // For other POCO types, try to deserialize the body as that type
            var ctx = JsonSerializer.Deserialize<ConnectorContext>(sourceJson, JsonOptions);
            if (ctx != null && !string.IsNullOrEmpty(ctx.Body))
            {
                var result = JsonSerializer.Deserialize(ctx.Body, context.TargetType, JsonOptions);
                if (result != null)
                {
                    return new ValueTask<ConversionResult>(ConversionResult.Success(result));
                }
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
        catch (JsonException)
        {
            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
