// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorHttpRequestProcessorTests
{
    private readonly ILogger<ConnectorHttpRequestProcessor> _logger;
    private readonly ConnectorHttpRequestProcessor _processor;

    public ConnectorHttpRequestProcessorTests()
    {
        _logger = NullLogger<ConnectorHttpRequestProcessor>.Instance;
        _processor = new ConnectorHttpRequestProcessor(_logger);
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConnectorHttpRequestProcessor(null!));
    }

    [Fact]
    public async Task ProcessAsync_ReturnsUnsupportedMediaType_WhenContentTypeIsNotJson()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("some text", Encoding.UTF8, "text/plain")
        };
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("application/json", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsUnsupportedMediaType_WhenContentTypeIsMissing()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"))
        };
        request.Content.Headers.ContentType = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_AcceptsApplicationJsonContentType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("{\"test\": true}")
        };
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsMethodNotAllowed_ForGetRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/connector");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

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
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPostRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("{\"test\": \"data\"}")
        };
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(capturedJson);
        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.Equal("data", doc.RootElement.GetProperty("test").GetString());
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPutRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost/api/connector")
        {
            Content = JsonContent("{\"test\": \"data\"}")
        };
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_PassesRawJsonString()
    {
        // Arrange
        var jsonBody = "{\"subject\": \"Test Email\", \"from\": \"test@example.com\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent(jsonBody)
        };
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedJson);
        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.Equal("Test Email", doc.RootElement.GetProperty("subject").GetString());
        Assert.Equal("test@example.com", doc.RootElement.GetProperty("from").GetString());
    }

    [Fact]
    public async Task ProcessAsync_HandlesEmptyBody()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("")
        };
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(string.Empty, capturedJson);
    }

    [Fact]
    public async Task ProcessAsync_PassesFunctionName()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("{}")
        };
        string? capturedFunctionName = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, functionName, _) =>
            {
                capturedFunctionName = functionName;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        await _processor.ProcessAsync(request, "MyTestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal("MyTestFunction", capturedFunctionName);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsCallbackResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("{}")
        };
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Callback error")
            });

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Callback error", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBadRequest_ForInvalidJson()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = JsonContent("not valid json {{{")
        };
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("valid JSON", content);
    }
}
