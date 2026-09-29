// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

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
        _configProvider = new ConnectorExtensionConfigProvider(
            httpRequestProcessor,
            loggerFactory,
            Options.Create(new ConnectorOptions()),
            new StubConnectorConnectionOptionsProvider(),
            new StubConnectorPollingListenerFactory());
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object);
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

        var registration = new ConnectorFunctionRegistration("RegisteredFunction", _mockExecutor.Object);

        // Act - constructor should register the function
        var listener = new ConnectorListener(_configProvider, registration);

        // Assert - function should be callable via ConvertAsync
        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            "http://localhost/api/connector?functionName=RegisteredFunction")
        {
            Content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object);
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        await listener.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_LogsCapturedWebhookEndpoint()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddProvider(loggerProvider));
        var configProvider = CreateConfigProvider(loggerFactory);
        configProvider.CaptureWebhookEndpoint(
            new Uri(
                "http://localhost:7071/runtime/webhooks/connector?code=secret"));
        var registration = new ConnectorFunctionRegistration(
            "TestFunction",
            _mockExecutor.Object);
        var listener = new ConnectorListener(configProvider, registration);

        // Act
        await listener.StartAsync(CancellationToken.None);

        // Assert
        Assert.Contains(
            "Connector endpoint: http://localhost:7071/runtime/webhooks/connector",
            loggerProvider.Messages);
        Assert.DoesNotContain(
            loggerProvider.Messages,
            message => message.Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public void CaptureWebhookEndpoint_DoesNotLogBeforeWebhookListenerStarts()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddProvider(loggerProvider));
        var configProvider = CreateConfigProvider(loggerFactory);

        // Act
        configProvider.CaptureWebhookEndpoint(
            new Uri("http://localhost:7071/runtime/webhooks/connector"));

        // Assert
        Assert.DoesNotContain(
            loggerProvider.Messages,
            message => message.StartsWith(
                "Connector endpoint:",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_LogsWebhookEndpointOnlyOnce()
    {
        // Arrange
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddProvider(loggerProvider));
        var configProvider = CreateConfigProvider(loggerFactory);
        configProvider.CaptureWebhookEndpoint(
            new Uri("http://localhost:7071/runtime/webhooks/connector"));
        var firstListener = new ConnectorListener(
            configProvider,
            new ConnectorFunctionRegistration(
                "FirstFunction",
                _mockExecutor.Object));
        var secondListener = new ConnectorListener(
            configProvider,
            new ConnectorFunctionRegistration(
                "SecondFunction",
                _mockExecutor.Object));

        // Act
        await Task.WhenAll(
            firstListener.StartAsync(CancellationToken.None),
            secondListener.StartAsync(CancellationToken.None));

        // Assert
        Assert.Single(
            loggerProvider.Messages,
            message => message.StartsWith(
                "Connector endpoint:",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object);
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        await listener.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Cancel_DoesNotThrow()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object);
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        listener.Cancel();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var registration = new ConnectorFunctionRegistration("TestFunction", _mockExecutor.Object);
        var listener = new ConnectorListener(_configProvider, registration);

        // Act & Assert (should not throw)
        listener.Dispose();
        listener.Dispose();
    }

    private static ConnectorExtensionConfigProvider CreateConfigProvider(
        ILoggerFactory loggerFactory)
    {
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        return new ConnectorExtensionConfigProvider(
            httpRequestProcessor,
            loggerFactory,
            Options.Create(new ConnectorOptions()),
            new StubConnectorConnectionOptionsProvider(),
            new StubConnectorPollingListenerFactory());
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string>
            _messages = new();

        internal IReadOnlyCollection<string> Messages => _messages.ToArray();

        public ILogger CreateLogger(string categoryName) =>
            new CapturingLogger(_messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(
            System.Collections.Concurrent.ConcurrentQueue<string> messages) :
            ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull =>
                null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                messages.Enqueue(formatter(state, exception));
        }
    }
}
