// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Converts deferred Connector event data to the declared function parameter type.
/// </summary>
[SupportsDeferredBinding]
internal sealed class ConnectorTriggerConverter : IInputConverter
{
    private const string BindingDataVersion = "1.0";
    private const string BindingDataSource = "AzureConnectorEvent";
    private const string BindingDataContentType = "application/json";

    private readonly WorkerOptions _workerOptions;

    public ConnectorTriggerConverter(IOptions<WorkerOptions> workerOptions)
    {
        _workerOptions = workerOptions?.Value
            ?? throw new ArgumentNullException(nameof(workerOptions));
    }

    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            object result;
            if (context.Source is ModelBindingData bindingData)
            {
                result = await ConvertSingleAsync(
                    bindingData,
                    context.TargetType).ConfigureAwait(false);
            }
            else if (context.Source is CollectionModelBindingData collection)
            {
                result = await ConvertCollectionAsync(
                    collection,
                    context.TargetType).ConfigureAwait(false);
            }
            else
            {
                return ConversionResult.Unhandled();
            }

            return ConversionResult.Success(result);
        }
        catch (Exception exception)
        {
            return ConversionResult.Failed(exception);
        }
    }

    private async Task<object> ConvertSingleAsync(
        ModelBindingData bindingData,
        Type targetType)
    {
        ValidateTargetType(targetType);
        ConnectorEventBindingData connectorEvent =
            DeserializeBindingData(bindingData);

        if (targetType.IsArray)
        {
            bool isMetadataArray =
                targetType.GetElementType() is { IsGenericType: true } elementType &&
                elementType.GetGenericTypeDefinition() == typeof(ConnectorEvent<>);
            if (connectorEvent.DeliveryMode == ConnectorTriggerDeliveryMode.Webhook &&
                !isMetadataArray)
            {
                return await DeserializePayloadAsync(
                    connectorEvent.Data!,
                    targetType).ConfigureAwait(false);
            }

            return await ConvertArrayAsync(
                [connectorEvent],
                targetType).ConfigureAwait(false);
        }

        return await ConvertEventAsync(
            connectorEvent,
            targetType).ConfigureAwait(false);
    }

    private async Task<object> ConvertCollectionAsync(
        CollectionModelBindingData collection,
        Type targetType)
    {
        ValidateTargetType(targetType);
        ArgumentNullException.ThrowIfNull(collection.ModelBindingData);
        if (!targetType.IsArray)
        {
            throw new InvalidOperationException(
                $"Connector trigger parameter type '{targetType}' must be an array for a batched invocation.");
        }

        ConnectorEventBindingData[] events = collection.ModelBindingData
            .Select(DeserializeBindingData)
            .ToArray();
        if (events.Length == 0)
        {
            throw new InvalidOperationException(
                "Connector trigger binding data must contain at least one event.");
        }

        if (events.Any(
            static connectorEvent =>
                connectorEvent.DeliveryMode != ConnectorTriggerDeliveryMode.Poll))
        {
            throw new InvalidOperationException(
                "Batched Connector trigger binding data is supported only for Poll delivery.");
        }

        return await ConvertArrayAsync(events, targetType).ConfigureAwait(false);
    }

    private async Task<Array> ConvertArrayAsync(
        IReadOnlyList<ConnectorEventBindingData> events,
        Type targetType)
    {
        Type elementType = targetType.GetElementType()!;
        Array result = Array.CreateInstance(elementType, events.Count);
        for (int index = 0; index < events.Count; index++)
        {
            result.SetValue(
                await ConvertEventAsync(
                    events[index],
                    elementType).ConfigureAwait(false),
                index);
        }

        return result;
    }

    private async Task<object> ConvertEventAsync(
        ConnectorEventBindingData connectorEvent,
        Type targetType)
    {
        if (connectorEvent.Data is null)
        {
            throw new InvalidOperationException(
                "Connector event binding data does not contain payload data.");
        }

        if (targetType.IsGenericType &&
            targetType.GetGenericTypeDefinition() == typeof(ConnectorEvent<>))
        {
            Type payloadType = targetType.GetGenericArguments()[0];
            object payload = await DeserializePayloadAsync(
                connectorEvent.Data,
                payloadType).ConfigureAwait(false);
            object result = Activator.CreateInstance(targetType)
                ?? throw new InvalidOperationException(
                    $"Unable to create Connector trigger target type '{targetType}'.");
            targetType.GetProperty(nameof(ConnectorEvent<object>.Data))!
                .SetValue(result, payload);
            targetType.GetProperty(nameof(ConnectorEvent<object>.MessageId))!
                .SetValue(result, connectorEvent.MessageId);
            return result;
        }

        return await DeserializePayloadAsync(
            connectorEvent.Data,
            targetType).ConfigureAwait(false);
    }

    private async Task<object> DeserializePayloadAsync(
        string data,
        Type targetType)
    {
        if (targetType == typeof(string))
        {
            return data;
        }

        if (targetType == typeof(JsonElement))
        {
            using JsonDocument document = JsonDocument.Parse(data);
            return document.RootElement.Clone();
        }

        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(data));
        if (_workerOptions.Serializer is not { } serializer)
        {
            throw new InvalidOperationException(
                "The Azure Functions worker serializer is not configured.");
        }

        return await serializer.DeserializeAsync(
            stream,
            targetType,
            CancellationToken.None).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Connector trigger payload could not be converted to '{targetType}'.");
    }

    private static ConnectorEventBindingData DeserializeBindingData(
        ModelBindingData bindingData)
    {
        ValidateBindingData(bindingData);
        ConnectorEventBindingData connectorEvent = bindingData.Content
            .ToObjectFromJson<ConnectorEventBindingData>()
            ?? throw new InvalidOperationException(
                "Connector event binding data content could not be read.");
        if (!Enum.IsDefined(connectorEvent.DeliveryMode))
        {
            throw new InvalidOperationException(
                $"Unsupported Connector trigger delivery mode '{connectorEvent.DeliveryMode}'.");
        }

        return connectorEvent;
    }

    private static void ValidateTargetType(Type targetType)
    {
        if (targetType.ContainsGenericParameters)
        {
            throw new InvalidOperationException(
                $"Open generic Connector trigger target type '{targetType}' is not supported.");
        }
    }

    private static void ValidateBindingData(ModelBindingData bindingData)
    {
        ArgumentNullException.ThrowIfNull(bindingData);

        if (!string.Equals(
            bindingData.Version,
            BindingDataVersion,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported Connector event binding data version '{bindingData.Version}'.");
        }

        if (!string.Equals(
            bindingData.Source,
            BindingDataSource,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Invalid Connector event binding source '{bindingData.Source}'.");
        }

        if (!string.Equals(
            bindingData.ContentType,
            BindingDataContentType,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid Connector event binding content type '{bindingData.ContentType}'.");
        }
    }

    private sealed class ConnectorEventBindingData
    {
        [JsonPropertyName("deliveryMode")]
        [JsonRequired]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConnectorTriggerDeliveryMode DeliveryMode { get; init; }

        [JsonPropertyName("data")]
        [JsonRequired]
        public string? Data { get; init; }

        [JsonPropertyName("messageId")]
        public string? MessageId { get; init; }
    }
}
