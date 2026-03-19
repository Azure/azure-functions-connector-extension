// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;

    public ConnectorTriggerBindingTests()
    {
        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(httpRequestProcessor, loggerFactory);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenParameterIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorTriggerBinding(null!, _configProvider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorTriggerBinding(parameter, null!));
    }

    [Fact]
    public void TriggerValueType_ReturnsTupleType()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        // Assert
        Assert.Equal(typeof((ConnectorContext, JToken)), binding.TriggerValueType);
    }

    [Fact]
    public void BindingDataContract_ContainsExpectedKeys()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        // Assert
        var contract = binding.BindingDataContract;
        Assert.Contains("Body", contract.Keys);
        Assert.Contains("Headers", contract.Keys);
        Assert.Contains("Query", contract.Keys);
        Assert.Contains("Method", contract.Keys);
        Assert.Contains("Url", contract.Keys);
        Assert.Contains("ContentType", contract.Keys);
        Assert.Contains("Timestamp", contract.Keys);
    }

    [Fact]
    public void BindingDataContract_HasCorrectTypes()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        // Assert
        var contract = binding.BindingDataContract;
        Assert.Equal(typeof(string), contract["Body"]);
        Assert.Equal(typeof(IDictionary<string, string>), contract["Headers"]);
        Assert.Equal(typeof(IDictionary<string, string>), contract["Query"]);
        Assert.Equal(typeof(string), contract["Method"]);
        Assert.Equal(typeof(string), contract["Url"]);
        Assert.Equal(typeof(string), contract["ContentType"]);
        Assert.Equal(typeof(DateTimeOffset), contract["Timestamp"]);
    }

    [Fact]
    public async Task BindAsync_CreatesCorrectTriggerData()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        var context = new ConnectorContext
        {
            Body = "{\"test\": 123}",
            Method = "POST",
            Url = "http://localhost/api/test"
        };
        var jsonBody = JToken.Parse("{\"test\": 123}");
        var triggerValue = (context, jsonBody);

        var mockBindingContext = new Mock<Microsoft.Azure.WebJobs.Host.Bindings.ValueBindingContext>(
            null!, CancellationToken.None);

        // Act
        var result = await binding.BindAsync(triggerValue, mockBindingContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.BindingData);
        Assert.Equal("{\"test\": 123}", result.BindingData["Body"]);
        Assert.Equal("POST", result.BindingData["Method"]);
        Assert.Equal("http://localhost/api/test", result.BindingData["Url"]);
    }

    [Fact]
    public async Task BindAsync_ThrowsForInvalidTriggerValue()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        var mockBindingContext = new Mock<Microsoft.Azure.WebJobs.Host.Bindings.ValueBindingContext>(
            null!, CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            binding.BindAsync("invalid value", mockBindingContext.Object));
    }

    [Fact]
    public void ToParameterDescriptor_ReturnsDescriptorWithCorrectName()
    {
        // Arrange
        var parameter = typeof(TestFunctions).GetMethod(nameof(TestFunctions.SampleFunction))!
            .GetParameters()[0];
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

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
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

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
        var binding = new ConnectorTriggerBinding(parameter, _configProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            binding.CreateListenerAsync(null!));
    }

    // Test class for reflection
    private static class TestFunctions
    {
        public static void SampleFunction([ConnectorTrigger] ConnectorContext ctx)
        {
        }
    }

    // Test implementation of FunctionDescriptor
    private sealed class TestFunctionDescriptor : Microsoft.Azure.WebJobs.Host.Protocols.FunctionDescriptor
    {
    }
}
