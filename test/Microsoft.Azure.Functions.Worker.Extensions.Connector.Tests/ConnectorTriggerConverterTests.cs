// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Tests;

public class ConnectorTriggerConverterTests
{
    private readonly ConnectorTriggerConverter _converter = new(
        Options.Create(new WorkerOptions
        {
            Serializer = new JsonObjectSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        }));

    [Fact]
    public async Task ConvertAsync_ConvertsScalarPayload()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload),
            BindingData("""{"subject":"hello"}""", "message-1"));

        Assert.Equal(ConversionStatus.Succeeded, result.Status);
        Assert.Equal("hello", Assert.IsType<TestPayload>(result.Value).Subject);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsPayloadBatch()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload[]),
            Collection(
                BindingData("""{"subject":"first"}""", "message-1"),
                BindingData("""{"subject":"second"}""", "message-2")));

        TestPayload[] payloads = Assert.IsType<TestPayload[]>(result.Value);
        Assert.Equal(["first", "second"], payloads.Select(payload => payload.Subject));
    }

    [Fact]
    public async Task ConvertAsync_AllowsArrayTargetForSinglePollEvent()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload[]),
            BindingData("""{"subject":"hello"}""", "message-1"));

        TestPayload[] payloads = Assert.IsType<TestPayload[]>(result.Value);
        Assert.Equal("hello", Assert.Single(payloads).Subject);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsMetadataEnvelope()
    {
        ConversionResult result = await ConvertAsync(
            typeof(ConnectorEvent<TestPayload>),
            BindingData("""{"subject":"hello"}""", "message-1"));

        var connectorEvent = Assert.IsType<ConnectorEvent<TestPayload>>(result.Value);
        Assert.Equal("hello", connectorEvent.Data.Subject);
        Assert.Equal("message-1", connectorEvent.MessageId);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsMetadataEnvelopeBatch()
    {
        ConversionResult result = await ConvertAsync(
            typeof(ConnectorEvent<TestPayload>[]),
            Collection(
                BindingData("""{"subject":"first"}""", "message-1"),
                BindingData("""{"subject":"second"}""", "message-2")));

        ConnectorEvent<TestPayload>[] events =
            Assert.IsType<ConnectorEvent<TestPayload>[]>(result.Value);
        Assert.Equal(["first", "second"], events.Select(value => value.Data.Subject));
        Assert.Equal(["message-1", "message-2"], events.Select(value => value.MessageId));
    }

    [Fact]
    public async Task ConvertAsync_UsesNullMessageIdForWebhook()
    {
        ConversionResult result = await ConvertAsync(
            typeof(ConnectorEvent<TestPayload>),
            BindingData(
                """{"subject":"hello"}""",
                messageId: null,
                ConnectorTriggerDeliveryMode.Webhook));

        Assert.Null(Assert.IsType<ConnectorEvent<TestPayload>>(result.Value).MessageId);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsRawJsonForStringTarget()
    {
        ConversionResult result = await ConvertAsync(
            typeof(string),
            BindingData("""{"subject":"hello"}""", messageId: null));

        Assert.Equal("""{"subject":"hello"}""", Assert.IsType<string>(result.Value));
    }

    [Fact]
    public async Task ConvertAsync_ReturnsEmptyWebhookBodyForStringTarget()
    {
        ConversionResult result = await ConvertAsync(
            typeof(string),
            BindingData(
                string.Empty,
                messageId: null,
                ConnectorTriggerDeliveryMode.Webhook));

        Assert.Equal(string.Empty, Assert.IsType<string>(result.Value));
    }

    [Fact]
    public async Task ConvertAsync_PreservesWebhookArrayPayload()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload[]),
            BindingData(
                """[{"subject":"first"},{"subject":"second"}]""",
                messageId: null,
                ConnectorTriggerDeliveryMode.Webhook));

        TestPayload[] payloads = Assert.IsType<TestPayload[]>(result.Value);
        Assert.Equal(["first", "second"], payloads.Select(payload => payload.Subject));
    }

    [Fact]
    public async Task ConvertAsync_ConvertsJsonElementAndObjectTargets()
    {
        ModelBindingData bindingData =
            BindingData("""{"subject":"hello"}""", messageId: null);

        ConversionResult jsonElementResult =
            await ConvertAsync(typeof(JsonElement), bindingData);
        ConversionResult objectResult =
            await ConvertAsync(typeof(object), bindingData);

        Assert.Equal(
            "hello",
            Assert.IsType<JsonElement>(jsonElementResult.Value)
                .GetProperty("subject")
                .GetString());
        Assert.Equal(
            "hello",
            Assert.IsType<JsonElement>(objectResult.Value)
                .GetProperty("subject")
                .GetString());
    }

    [Fact]
    public async Task ConvertAsync_RejectsScalarTargetForBatch()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload),
            Collection(
                BindingData("""{"subject":"first"}""", "message-1"),
                BindingData("""{"subject":"second"}""", "message-2")));

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("must be an array", result.Error!.Message);
    }

    [Fact]
    public async Task ConvertAsync_RejectsEmptyBatch()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload[]),
            Collection());

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("at least one event", result.Error!.Message);
    }

    [Fact]
    public async Task ConvertAsync_RejectsOpenGenericTarget()
    {
        ConversionResult result = await ConvertAsync(
            typeof(ConnectorEvent<>),
            BindingData("""{"subject":"hello"}""", "message-1"));

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("Open generic", result.Error!.Message);
    }

    [Fact]
    public async Task ConvertAsync_RejectsUnexpectedBindingSource()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload),
            new TestModelBindingData(
                "1.0",
                "UnexpectedSource",
                BinaryData.FromString(
                    """{"deliveryMode":"Poll","data":"{}","messageId":null}"""),
                "application/json"));

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("binding source", result.Error!.Message);
    }

    [Fact]
    public async Task ConvertAsync_RejectsMissingDeliveryMode()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload),
            new TestModelBindingData(
                "1.0",
                "AzureConnectorEvent",
                BinaryData.FromString(
                    """{"data":"{}","messageId":"message-1"}"""),
                "application/json"));

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.IsType<JsonException>(result.Error);
    }

    [Fact]
    public async Task ConvertAsync_RejectsUnknownDeliveryMode()
    {
        ConversionResult result = await ConvertAsync(
            typeof(TestPayload),
            new TestModelBindingData(
                "1.0",
                "AzureConnectorEvent",
                BinaryData.FromString(
                    """{"deliveryMode":42,"data":"{}","messageId":"message-1"}"""),
                "application/json"));

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Contains("delivery mode", result.Error!.Message);
    }

    private ValueTask<ConversionResult> ConvertAsync(
        Type targetType,
        object source) =>
        _converter.ConvertAsync(new TestConverterContext(targetType, source));

    private static ModelBindingData BindingData(
        string data,
        string? messageId,
        ConnectorTriggerDeliveryMode deliveryMode =
            ConnectorTriggerDeliveryMode.Poll) =>
        new TestModelBindingData(
            "1.0",
            "AzureConnectorEvent",
            BinaryData.FromString(
                new JsonObject
                {
                    ["deliveryMode"] = deliveryMode.ToString(),
                    ["data"] = data,
                    ["messageId"] = messageId,
                }.ToJsonString()),
            "application/json");

    private static CollectionModelBindingData Collection(
        params ModelBindingData[] bindingData) =>
        new TestCollectionModelBindingData(bindingData);

    private sealed class TestPayload
    {
        public string? Subject { get; init; }
    }

    private sealed class TestConverterContext(
        Type targetType,
        object source) : ConverterContext
    {
        public override Type TargetType { get; } = targetType;

        public override object Source { get; } = source;

        public override FunctionContext FunctionContext => null!;

        public override IReadOnlyDictionary<string, object> Properties { get; } =
            new Dictionary<string, object>();
    }

    private sealed class TestModelBindingData(
        string version,
        string source,
        BinaryData content,
        string contentType) : ModelBindingData
    {
        public override string Version { get; } = version;

        public override string Source { get; } = source;

        public override BinaryData Content { get; } = content;

        public override string ContentType { get; } = contentType;
    }

    private sealed class TestCollectionModelBindingData(
        ModelBindingData[] modelBindingData) : CollectionModelBindingData
    {
        public override ModelBindingData[] ModelBindingData { get; } =
            modelBindingData;
    }
}
