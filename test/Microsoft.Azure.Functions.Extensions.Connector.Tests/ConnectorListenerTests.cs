// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorListenerTests
{
    private readonly Mock<ITriggeredFunctionExecutor> _mockExecutor;
    private readonly ConnectorExtensionConfigProvider _configProvider;

    public ConnectorListenerTests()
    {
        _mockExecutor = new Mock<ITriggeredFunctionExecutor>();

        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(httpRequestProcessor, loggerFactory);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", "test-trigger");
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorListener(null!, registration));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenRegistrationIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorListener(_configProvider, null!));
    }

    [Fact]
    public async Task Constructor_RegistersFunctionWithConfigProvider()
    {
        // Arrange
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        var registration = new ConnectorFunctionRegistration("RegisteredFunction", _mockExecutor.Object, "test-namespace", "test-trigger");

        // Act - constructor should register the function
        var listener = new ConnectorListener(_configProvider, registration);

        // Assert - function should be callable via ConvertAsync
        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            "http://localhost/api/connector?functionName=RegisteredFunction")
        {
            Content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-ms-trigger-name", "test-trigger");
        request.Headers.Add("x-ms-gateway-resource-name", "test-namespace");

        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", "test-trigger");
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        await listener.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", "test-trigger");
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        await listener.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Cancel_DoesNotThrow()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", "test-trigger");
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        listener.Cancel();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", "test-trigger");
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        listener.Dispose();
        listener.Dispose();
    }

    [Fact]
    public void Registration_ThrowsArgumentNullException_WhenConnectorNamespaceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, null!, "test-trigger"));
    }

    [Fact]
    public void Registration_ThrowsArgumentException_WhenConnectorNamespaceIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "", "test-trigger"));
    }

    [Fact]
    public void Registration_ThrowsArgumentNullException_WhenTriggerNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", null!));
    }

    [Fact]
    public void Registration_ThrowsArgumentException_WhenTriggerNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object, "test-namespace", ""));
    }
}
