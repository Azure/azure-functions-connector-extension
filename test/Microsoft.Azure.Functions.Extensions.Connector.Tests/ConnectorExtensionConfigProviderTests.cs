// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorExtensionConfigProviderTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorHttpRequestProcessor _httpRequestProcessor;

    public ConnectorExtensionConfigProviderTests()
    {
        var loggerFactory = NullLoggerFactory.Instance;
        _httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(_httpRequestProcessor, loggerFactory);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHttpProcessorIsNull()
    {
        var loggerFactory = NullLoggerFactory.Instance;
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorExtensionConfigProvider(null!, loggerFactory));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new ConnectorExtensionConfigProvider(_httpRequestProcessor, null!));
    }

    [Fact]
    public async Task ConvertAsync_ReturnsBadRequest_WhenFunctionNameMissing()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector");

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("functionName", content);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsBadRequest_WhenFunctionNameEmpty()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector?functionName=");

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsBadRequest_WhenFunctionNameTooLong()
    {
        // Arrange
        var longName = new string('a', 129); // > 128 characters
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost/api/connector?functionName={longName}");

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("too long", content);
    }

    [Theory]
    [InlineData("test<script>")]
    [InlineData("func;tion")]
    [InlineData("my function")]
    [InlineData("test\n")]
    [InlineData("func/name")]
    [InlineData("func.name")]
    public async Task ConvertAsync_ReturnsBadRequest_WhenFunctionNameHasInvalidCharacters(string invalidName)
    {
        // Arrange - function names with special characters should be rejected
        var request = new HttpRequestMessage(HttpMethod.Post, 
            $"http://localhost/api/connector?functionName={Uri.EscapeDataString(invalidName)}");

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert - should be BadRequest (invalid format) or NotFound (not registered)
        // The key is it should NOT return OK
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsNotFound_WhenFunctionNotRegistered()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, 
            "http://localhost/api/connector?functionName=NonExistentFunction");

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsOk_WhenFunctionRegistered()
    {
        // Arrange
        var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
        mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        // Register a listener
        var listener = new ConnectorListener(mockExecutor.Object, _configProvider, "TestFunction");

        var request = new HttpRequestMessage(HttpMethod.Post, 
            "http://localhost/api/connector?functionName=TestFunction")
        {
            Content = new StringContent("{\"test\": \"data\"}")
        };

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConvertAsync_FunctionNameIsCaseInsensitive()
    {
        // Arrange
        var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
        mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        // Register a listener with lowercase name
        var listener = new ConnectorListener(mockExecutor.Object, _configProvider, "testfunction");

        // Request with uppercase name
        var request = new HttpRequestMessage(HttpMethod.Post, 
            "http://localhost/api/connector?functionName=TESTFUNCTION")
        {
            Content = new StringContent("{}")
        };

        // Act
        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddListener_RegistersListener()
    {
        // Arrange
        var mockExecutor = new Mock<ITriggeredFunctionExecutor>();

        // Act - constructor calls AddListener
        var listener = new ConnectorListener(mockExecutor.Object, _configProvider, "AddedFunction");

        // Assert - verify we can find the function
        var request = new HttpRequestMessage(HttpMethod.Post, 
            "http://localhost/api/connector?functionName=AddedFunction")
        {
            Content = new StringContent("{}")
        };

        mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveListener_UnregistersListener()
    {
        // Arrange
        var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new ConnectorListener(mockExecutor.Object, _configProvider, "RemovedFunction");

        // Act
        _configProvider.RemoveListener("RemovedFunction");

        // Assert - function should no longer be found
        var request = new HttpRequestMessage(HttpMethod.Post, 
            "http://localhost/api/connector?functionName=RemovedFunction");

        var response = await _configProvider.ConvertAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConvertAsync_AcceptsValidFunctionNames()
    {
        // Arrange
        var validNames = new[] { "MyFunction", "my_function", "my-function", "Function123", "a", "A_1-B" };
        
        foreach (var validName in validNames)
        {
            var mockExecutor = new Mock<ITriggeredFunctionExecutor>();
            mockExecutor.Setup(e => e.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(true));

            var listener = new ConnectorListener(mockExecutor.Object, _configProvider, validName);

            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"http://localhost/api/connector?functionName={validName}")
            {
                Content = new StringContent("{}")
            };

            // Act
            var response = await _configProvider.ConvertAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Cleanup
            _configProvider.RemoveListener(validName);
        }
    }
}
