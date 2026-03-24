// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
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
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
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
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
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
            Content = new StringContent("{\"test\": \"data\"}")
        };
        JToken? capturedToken = null;
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
            (token, _, _) => 
            {
                capturedToken = token;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(capturedToken);
        Assert.Equal("data", capturedToken!["test"]?.ToString());
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPutRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost/api/connector")
        {
            Content = new StringContent("{\"test\": \"data\"}")
        };
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_ParsesJsonBodyToJToken()
    {
        // Arrange
        var jsonBody = "{\"subject\": \"Test Email\", \"from\": \"test@example.com\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent(jsonBody)
        };
        JToken? capturedToken = null;
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
            (token, _, _) => 
            {
                capturedToken = token;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedToken);
        Assert.Equal("Test Email", capturedToken!["subject"]?.ToString());
        Assert.Equal("test@example.com", capturedToken["from"]?.ToString());
    }

    [Fact]
    public async Task ProcessAsync_HandlesEmptyBody()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("")
        };
        JToken? capturedToken = null;
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
            (token, _, _) => 
            {
                capturedToken = token;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(capturedToken);
        Assert.Equal(JTokenType.Null, capturedToken!.Type);
    }

    [Fact]
    public async Task ProcessAsync_PassesFunctionName()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };
        string? capturedFunctionName = null;
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
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
            Content = new StringContent("{}")
        };
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> callback = 
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
}
