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
    public void Constructor_ThrowsArgumentNullException_WhenExecutorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorListener(null!, _configProvider, "TestFunction"));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorListener(_mockExecutor.Object, null!, "TestFunction"));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFunctionNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorListener(_mockExecutor.Object, _configProvider, null!));
    }

    [Fact]
    public void Executor_ReturnsProvidedExecutor()
    {
        // Arrange
        var listener = new ConnectorListener(
            _mockExecutor.Object, 
            _configProvider, 
            "TestFunction");

        // Assert
        Assert.Same(_mockExecutor.Object, listener.Executor);
    }

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        // Arrange
        var listener = new ConnectorListener(
            _mockExecutor.Object, 
            _configProvider, 
            "TestFunction");

        // Act & Assert (should not throw)
        await listener.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var listener = new ConnectorListener(
            _mockExecutor.Object, 
            _configProvider, 
            "TestFunction");

        // Act & Assert (should not throw)
        await listener.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Cancel_DoesNotThrow()
    {
        // Arrange
        var listener = new ConnectorListener(
            _mockExecutor.Object, 
            _configProvider, 
            "TestFunction");

        // Act & Assert (should not throw)
        listener.Cancel();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var listener = new ConnectorListener(
            _mockExecutor.Object, 
            _configProvider, 
            "TestFunction");

        // Act & Assert (should not throw)
        listener.Dispose();
        listener.Dispose();
    }
}
