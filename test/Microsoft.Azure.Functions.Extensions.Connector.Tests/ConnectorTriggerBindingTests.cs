// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorTriggerAttribute _attribute;

    public ConnectorTriggerBindingTests()
    {
        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(httpRequestProcessor, loggerFactory);
        _attribute = new ConnectorTriggerAttribute();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenParameterIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(null!, _configProvider, _attribute));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(parameter, null!, _attribute));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAttributeIsNull()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(parameter, _configProvider, null!));
    }

    [Fact]
    public void TriggerValueType_ReturnsStringType()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

        // Assert: string as trigger value (raw JSON for worker)
        Assert.Equal(typeof(string), binding.TriggerValueType);
    }

    [Fact]
    public void BindingDataContract_IsEmpty()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

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
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

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
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

        // Act
        var descriptor = binding.ToParameterDescriptor();

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("ctx", descriptor.Name);
    }

    [Fact]
    public async Task CreateListenerAsync_ReturnsConnectorListener()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

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
    public async Task CreateListenerAsync_ThrowsForNullContext()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider, _attribute);

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
