// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingProviderTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorTriggerBindingProvider _provider;

    public ConnectorTriggerBindingProviderTests()
    {
        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(
            httpRequestProcessor,
            loggerFactory,
            Options.Create(new ConnectorOptions()),
            new StubConnectorConnectionOptionsProvider(),
            new StubConnectorPollingListenerFactory());
        _provider = new ConnectorTriggerBindingProvider(
            _configProvider,
            new ConnectorOptions(),
            new StubConnectorConnectionOptionsProvider(),
            new StubConnectorPollingListenerFactory());
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBindingProvider(
                null!,
                new ConnectorOptions(),
                new StubConnectorConnectionOptionsProvider(),
                new StubConnectorPollingListenerFactory()));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBindingProvider(
                _configProvider,
                null!,
                new StubConnectorConnectionOptionsProvider(),
                new StubConnectorPollingListenerFactory()));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectionOptionsProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBindingProvider(
                _configProvider,
                new ConnectorOptions(),
                null!,
                new StubConnectorPollingListenerFactory()));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPollingListenerFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBindingProvider(
                _configProvider,
                new ConnectorOptions(),
                new StubConnectorConnectionOptionsProvider(),
                null!));
    }

    [Fact]
    public async Task TryCreateAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _provider.TryCreateAsync(null!));
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsNull_WhenParameterHasNoAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithoutAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsBinding_WhenParameterHasAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(ConnectorTriggerInput), result.TriggerValueType);
        Assert.Empty(result.BindingDataContract);
        Assert.Equal("body", result.ToParameterDescriptor().Name);
    }

    [Fact]
    public async Task Binding_ReturnsPayloadJsonForStringParameter()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.FunctionWithAttribute));
        ConnectorTriggerInput input = ConnectorTriggerInput.FromSingle(
            BinaryData.FromString("""{"value":123}"""),
            "message-1",
            ConnectorTriggerDeliveryMode.Poll);

        ITriggerData result = await binding.BindAsync(
            input,
            CreateValueBindingContext());

        Assert.Equal("""{"value":123}""", await result.ValueProvider.GetValueAsync());
        Assert.Equal(
            "Connector trigger event",
            result.ValueProvider.ToInvokeString());
        Assert.Empty(result.BindingData);
    }

    [Fact]
    public async Task Binding_ReturnsDeferredBindingDataForWorkerParameter()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.DeferredBindingFunction));
        ConnectorTriggerInput input = ConnectorTriggerInput.FromSingle(
            BinaryData.FromString("""{"value":123}"""),
            "message-1",
            ConnectorTriggerDeliveryMode.Poll);

        ITriggerData result = await binding.BindAsync(
            input,
            CreateValueBindingContext());

        ParameterBindingData bindingData =
            Assert.IsType<ParameterBindingData>(
                await result.ValueProvider.GetValueAsync());
        Assert.Equal("1.0", bindingData.Version);
        Assert.Equal("AzureConnectorEvent", bindingData.Source);
        using JsonDocument content = JsonDocument.Parse(
            bindingData.Content.ToString());
        Assert.Equal(
            """{"value":123}""",
            content.RootElement.GetProperty("data").GetString());
        Assert.Equal(
            "message-1",
            content.RootElement.GetProperty("messageId").GetString());
        Assert.Equal(
            "Connector trigger event",
            result.ValueProvider.ToInvokeString());
    }

    [Fact]
    public async Task Binding_ReturnsPayloadArrayForBatchedGenericWorker()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.BatchedStringFunction));
        ConnectorTriggerInput input = CreateBatchInput();

        ITriggerData result = await binding.BindAsync(
            input,
            CreateValueBindingContext());

        Assert.Equal(
            ["""{"value":1}""", """{"value":2}"""],
            Assert.IsType<string[]>(
                await result.ValueProvider.GetValueAsync()));
        Assert.Equal(typeof(string[]), result.ValueProvider.Type);
        Assert.Equal(
            "Connector trigger batch (2 events)",
            result.ValueProvider.ToInvokeString());
    }

    [Fact]
    public async Task Binding_ReturnsDeferredBindingDataArrayForBatchedWorker()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.BatchedDeferredBindingFunction));
        ConnectorTriggerInput input = CreateBatchInput();

        ITriggerData result = await binding.BindAsync(
            input,
            CreateValueBindingContext());

        ParameterBindingData[] bindingData =
            Assert.IsType<ParameterBindingData[]>(
                await result.ValueProvider.GetValueAsync());
        Assert.Equal(2, bindingData.Length);
        Assert.Equal(
            ["message-1", "message-2"],
            bindingData.Select(value =>
            {
                using JsonDocument content =
                    JsonDocument.Parse(value.Content.ToString());
                return content.RootElement
                    .GetProperty("messageId")
                    .GetString();
            }));
        Assert.Equal(typeof(ParameterBindingData[]), result.ValueProvider.Type);
        Assert.Equal(
            "Connector trigger batch (2 events)",
            result.ValueProvider.ToInvokeString());
    }

    [Fact]
    public void BatchInput_RejectsNullEvent()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            ConnectorTriggerInput.FromBatch([null!]));

        Assert.Equal("events", exception.ParamName);
    }

    [Fact]
    public async Task Binding_ConvertsDashboardString()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.FunctionWithAttribute));

        ITriggerData result = await binding.BindAsync(
            """{"value":123}""",
            CreateValueBindingContext());

        Assert.Equal("""{"value":123}""", await result.ValueProvider.GetValueAsync());
    }

    [Fact]
    public async Task Binding_CreatesWebhookListener()
    {
        ITriggerBinding binding = await CreateBindingAsync(
            nameof(TestFunctions.FunctionWithAttribute));

        IListener listener = await binding.CreateListenerAsync(
            CreateListenerContext("TestFunction"));

        Assert.IsType<ConnectorListener>(listener);
    }

    [Fact]
    public async Task Binding_CreatesPollingListenerWithResolvedOptions()
    {
        var options = new ConnectorOptions
        {
            DefaultMaxBatchSize = 2,
            DefaultConcurrency = 6,
        };
        var connectionOptions = new ConnectorConnectionOptions(
            Mock.Of<TokenCredential>());
        var provider = new ConnectorTriggerBindingProvider(
            _configProvider,
            options,
            new StubConnectorConnectionOptionsProvider(connectionOptions),
            new StubConnectorPollingListenerFactory());
        ParameterInfo parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.PollFunction))!
            .GetParameters()[0];
        ITriggerBinding binding = (await provider.TryCreateAsync(
            new TriggerBindingProviderContext(
                parameter,
                CancellationToken.None)))!;

        IListener listener = await binding.CreateListenerAsync(
            CreateListenerContext("PollFunction"));

        ConnectorPollingListener pollingListener =
            Assert.IsType<ConnectorPollingListener>(listener);
        Assert.Equal("ConnectorNamespace", pollingListener.Options.Connection);
        Assert.Equal(
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/on-new-email",
            pollingListener.Options.PollingEndpoint);
        Assert.Equal(2, pollingListener.Options.MaxBatchSize);
        Assert.Equal(6, pollingListener.Options.Concurrency);
        Assert.True(pollingListener.Options.IsBatched);
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsNull_ForDifferentTriggerAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithDifferentAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.Null(result);
    }

    private async Task<ITriggerBinding> CreateBindingAsync(string methodName)
    {
        ParameterInfo parameter = typeof(TestFunctions)
            .GetMethod(methodName)!
            .GetParameters()[0];
        return (await _provider.TryCreateAsync(
            new TriggerBindingProviderContext(
                parameter,
                CancellationToken.None)))!;
    }

    private static ValueBindingContext CreateValueBindingContext() =>
        new Mock<ValueBindingContext>(
            null!,
            CancellationToken.None).Object;

    private static ConnectorTriggerInput CreateBatchInput() =>
        ConnectorTriggerInput.FromBatch(
        [
            new ConnectorTriggerEventInput(
                BinaryData.FromString("""{"value":1}"""),
                "message-1",
                ConnectorTriggerDeliveryMode.Poll),
            new ConnectorTriggerEventInput(
                BinaryData.FromString("""{"value":2}"""),
                "message-2",
                ConnectorTriggerDeliveryMode.Poll),
        ]);

    private static ListenerFactoryContext CreateListenerContext(
        string functionName) =>
        new(
            new TestFunctionDescriptor { ShortName = functionName },
            Mock.Of<ITriggeredFunctionExecutor>(),
            CancellationToken.None);

    // Test class with different function signatures
    private static class TestFunctions
    {
        public static void FunctionWithAttribute([ConnectorTrigger] string body)
        {
        }

        public static void DeferredBindingFunction(
            [ConnectorTrigger] ParameterBindingData body)
        {
        }

        public static void BatchedStringFunction(
            [ConnectorTrigger] string[] body)
        {
        }

        public static void BatchedDeferredBindingFunction(
            [ConnectorTrigger] ParameterBindingData[] body)
        {
        }

        public static void PollFunction(
            [ConnectorTrigger(
                DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
                Connection = "ConnectorNamespace",
                PollingEndpoint =
                    "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/on-new-email")]
            string[] body)
        {
        }

        public static void FunctionWithoutAttribute(string body)
        {
        }

        public static void FunctionWithDifferentAttribute([TestAttribute] string data)
        {
        }
    }

    // Dummy attribute for testing
    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class TestAttribute : Attribute
    {
    }

    private sealed class TestFunctionDescriptor : FunctionDescriptor
    {
    }
}
