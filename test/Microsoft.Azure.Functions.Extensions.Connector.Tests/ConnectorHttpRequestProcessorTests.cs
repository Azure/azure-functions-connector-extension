// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorHttpRequestProcessorTests
{
    private readonly ILogger<ConnectorHttpRequestProcessor> _logger;
    private readonly Mock<ITriggeredFunctionExecutor> _mockExecutor;
    private readonly ConnectorHttpRequestProcessor _processor;

    public ConnectorHttpRequestProcessorTests()
    {
        _logger = NullLogger<ConnectorHttpRequestProcessor>.Instance;
        _mockExecutor = new Mock<ITriggeredFunctionExecutor>();
        _processor = new ConnectorHttpRequestProcessor(_logger);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConnectorHttpRequestProcessor(null!));
    }

    [Fact]
    public async Task ProcessAsync_ReturnsMethodNotAllowed_ForGetRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/connector");

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("POST and PUT", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsMethodNotAllowed_ForDeleteRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Delete, "http://localhost/api/connector");

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPostRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{\"test\": \"data\"}")
        };

        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPutRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost/api/connector")
        {
            Content = new StringContent("{\"test\": \"data\"}")
        };

        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsRequestEntityTooLarge_WhenContentLengthExceedsMax()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("test")
        };
        // Set content length header to exceed max (10MB)
        request.Content.Headers.ContentLength = 11 * 1024 * 1024;

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_ParsesJsonBody_Correctly()
    {
        // Arrange
        var jsonBody = "{\"email\": \"test@example.com\", \"subject\": \"Hello\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedData);
        
        var (context, jtoken) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData.TriggerValue;
        Assert.Equal(jsonBody, context.Body);
        Assert.Equal("test@example.com", jtoken["email"]?.ToString());
    }

    [Fact]
    public async Task ProcessAsync_ParsesHeaders_Correctly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };
        request.Headers.Add("X-Custom-Header", "CustomValue");
        request.Headers.Add("X-Request-Id", "12345");

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        var (context, _) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal("CustomValue", context.Headers["X-Custom-Header"]);
        Assert.Equal("12345", context.Headers["X-Request-Id"]);
    }

    [Fact]
    public async Task ProcessAsync_ParsesQueryParameters_Correctly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector?functionName=Test&filter=active")
        {
            Content = new StringContent("{}")
        };

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        var (context, _) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal("Test", context.Query["functionName"]);
        Assert.Equal("active", context.Query["filter"]);
    }

    [Fact]
    public async Task ProcessAsync_HandlesNonJsonBody_Gracefully()
    {
        // Arrange
        var plainTextBody = "This is plain text, not JSON";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent(plainTextBody, System.Text.Encoding.UTF8, "text/plain")
        };

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (context, jtoken) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal(plainTextBody, context.Body);
        Assert.Equal(plainTextBody, jtoken.ToString()); // Non-JSON wrapped as string value
    }

    [Fact]
    public async Task ProcessAsync_HandlesEmptyBody_Gracefully()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("")
        };

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (context, jtoken) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal(string.Empty, context.Body);
        Assert.Equal(Newtonsoft.Json.Linq.JTokenType.Null, jtoken.Type);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsInternalServerError_WhenFunctionExecutionFails()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };

        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(false, new Exception("Function failed")));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("failed", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsRequestTimeout_WhenCancelled()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };

        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_SetsConnectorContext_Properties()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:7071/api/connector?code=secret")
        {
            Content = new StringContent("{\"data\": 123}", System.Text.Encoding.UTF8, "application/json")
        };

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        var (context, _) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal("POST", context.Method);
        Assert.Equal("http://localhost:7071/api/connector?code=secret", context.Url);
        Assert.Equal("application/json", context.ContentType);
        Assert.NotEqual(default, context.Timestamp);
    }

    [Fact]
    public async Task ProcessAsync_HeadersAreCaseInsensitive()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };
        request.Headers.Add("X-Custom-Header", "Value");

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        var (context, _) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.True(context.Headers.ContainsKey("x-custom-header"));
        Assert.True(context.Headers.ContainsKey("X-CUSTOM-HEADER"));
    }

    [Fact]
    public async Task ProcessAsync_HandlesNullRequestContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector");
        // Content is null

        TriggeredFunctionData? capturedData = null;
        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, ct) => capturedData = data)
            .ReturnsAsync(new FunctionResult(true));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (context, _) = ((ConnectorContext, Newtonsoft.Json.Linq.JToken))capturedData!.TriggerValue;
        Assert.Equal(string.Empty, context.Body);
    }

    [Fact]
    public async Task ProcessAsync_HandlesExecutorException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };

        _mockExecutor.Setup(e => e.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _mockExecutor.Object, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
