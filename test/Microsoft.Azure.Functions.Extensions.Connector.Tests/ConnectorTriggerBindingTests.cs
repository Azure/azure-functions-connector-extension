// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorTriggerAttribute _attribute;
    private readonly ConnectorOptions _options;
    private readonly IConnectorConnectionOptionsProvider _connectionOptionsProvider;

    public ConnectorTriggerBindingTests()
    {
        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _options = new ConnectorOptions();
        _configProvider = new ConnectorExtensionConfigProvider(
            httpRequestProcessor,
            loggerFactory,
            Options.Create(_options),
            new StubConnectorConnectionOptionsProvider());
        var connectionOptions = new ConnectorConnectionOptions(
            new ResourceIdentifier(
                "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns"),
            Mock.Of<TokenCredential>());
        _connectionOptionsProvider =
            new StubConnectorConnectionOptionsProvider(connectionOptions);
        _attribute = new ConnectorTriggerAttribute();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenParameterIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                null!,
                _configProvider,
                _attribute,
                _options,
                _connectionOptionsProvider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                null!,
                _attribute,
                _options,
                _connectionOptionsProvider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAttributeIsNull()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                null!,
                _options,
                _connectionOptionsProvider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                _attribute,
                null!,
                _connectionOptionsProvider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectionOptionsProviderIsNull()
    {
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                _attribute,
                _options,
                null!));
    }

    [Fact]
    public void TriggerValueType_ReturnsStringType()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        // Assert: string as trigger value (raw JSON for worker)
        Assert.Equal(typeof(string), binding.TriggerValueType);
    }

    [Fact]
    public void BindingDataContract_IsEmpty()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        // Assert: empty binding contract
        var contract = binding.BindingDataContract;
        Assert.Empty(contract);
    }

    [Fact]
    public async Task BindAsync_ReturnsTriggerDataWithEmptyBindingData()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        var jsonBody = "{\"test\": 123}";

        var mockBindingContext = new Mock<Microsoft.Azure.WebJobs.Host.Bindings.ValueBindingContext>(
            null!, CancellationToken.None);

        // Act
        var result = await binding.BindAsync(jsonBody, mockBindingContext.Object);

        // Assert: ValueProvider returns string, empty BindingData
        Assert.NotNull(result);
        Assert.NotNull(result.BindingData);
        Assert.Empty(result.BindingData);
    }

    [Fact]
    public void ToParameterDescriptor_ReturnsDescriptorWithCorrectName()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        // Act
        var descriptor = binding.ToParameterDescriptor();

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("ctx", descriptor.Name);
    }

    [Fact]
    public async Task CreateListenerAsync_ReturnsConnectorListener_ForWebhook()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        var mockExecutor = new Mock<Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor>();

        // Use a real FunctionDescriptor since ShortName is not virtual
        var descriptor = new TestFunctionDescriptor { ShortName = "TestFunction" };

        var listenerContext = new Microsoft.Azure.WebJobs.Host.Listeners.ListenerFactoryContext(
            descriptor,
            mockExecutor.Object,
            CancellationToken.None);

        // Act
        var listener = await binding.CreateListenerAsync(listenerContext);

        // Assert
        Assert.NotNull(listener);
        Assert.IsType<ConnectorListener>(listener);
    }

    [Fact]
    public async Task CreateListenerAsync_ReturnsConnectorPollingListener_ForPoll()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var attribute = new ConnectorTriggerAttribute
        {
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            TriggerConfigName = "OnNewEmail",
            MaxBatchSize = 4,
            Concurrency = 8,
        };
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            attribute,
            _options,
            _connectionOptionsProvider);
        var mockExecutor = new Mock<Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor>();
        var descriptor = new TestFunctionDescriptor { ShortName = "TestFunction" };
        var listenerContext = new Microsoft.Azure.WebJobs.Host.Listeners.ListenerFactoryContext(
            descriptor,
            mockExecutor.Object,
            CancellationToken.None);

        // Act
        var listener = await binding.CreateListenerAsync(listenerContext);

        // Assert
        var pollingListener = Assert.IsType<ConnectorPollingListener>(listener);
        Assert.Equal(attribute.Connection, pollingListener.Options.Connection);
        Assert.Equal(attribute.TriggerConfigName, pollingListener.Options.TriggerConfigName);
        Assert.Equal(attribute.MaxBatchSize, pollingListener.Options.MaxBatchSize);
        Assert.Equal(attribute.Concurrency, pollingListener.Options.Concurrency);
        Assert.Equal(
            "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns",
            pollingListener.ConnectionOptions.ResourceId.ToString());
    }

    [Fact]
    public async Task CreateListenerAsync_UsesHostDefaults_ForUnsetPollOptions()
    {
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var attribute = new ConnectorTriggerAttribute
        {
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            TriggerConfigName = "OnNewEmail",
        };
        var options = new ConnectorOptions
        {
            DefaultMaxBatchSize = 2,
            DefaultConcurrency = 6,
        };
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            attribute,
            options,
            _connectionOptionsProvider);
        var mockExecutor = new Mock<Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor>();
        var descriptor = new TestFunctionDescriptor { ShortName = "TestFunction" };
        var listenerContext = new Microsoft.Azure.WebJobs.Host.Listeners.ListenerFactoryContext(
            descriptor,
            mockExecutor.Object,
            CancellationToken.None);

        var listener = await binding.CreateListenerAsync(listenerContext);

        var pollingListener = Assert.IsType<ConnectorPollingListener>(listener);
        Assert.Equal(2, pollingListener.Options.MaxBatchSize);
        Assert.Equal(6, pollingListener.Options.Concurrency);
    }

    [Fact]
    public async Task CreateListenerAsync_ThrowsForUnsupportedDeliveryMode()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var attribute = new ConnectorTriggerAttribute
        {
            DeliveryMode = (ConnectorTriggerDeliveryMode)42,
        };
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            attribute,
            _options,
            _connectionOptionsProvider);
        var mockExecutor = new Mock<Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor>();
        var descriptor = new TestFunctionDescriptor { ShortName = "TestFunction" };
        var listenerContext = new Microsoft.Azure.WebJobs.Host.Listeners.ListenerFactoryContext(
            descriptor,
            mockExecutor.Object,
            CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            binding.CreateListenerAsync(listenerContext));
    }

    [Fact]
    public async Task CreateListenerAsync_ThrowsForNullContext()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(
            parameter,
            _configProvider,
            _attribute,
            _options,
            _connectionOptionsProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            binding.CreateListenerAsync(null!));
    }

    // Test class for reflection
    private static class TestFunctions
    {
        public static void SampleFunction([ConnectorTrigger] string ctx)
        {
        }
    }

    // Test implementation of FunctionDescriptor
    private sealed class TestFunctionDescriptor : Microsoft.Azure.WebJobs.Host.Protocols.FunctionDescriptor
    {
    }
}
