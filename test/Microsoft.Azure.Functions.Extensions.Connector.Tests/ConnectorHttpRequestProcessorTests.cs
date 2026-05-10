// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorHttpRequestProcessorTests
{
    private readonly ILogger<ConnectorHttpRequestProcessor> _logger;
    private readonly ConnectorHttpRequestProcessor _processor;
    private readonly ConnectorFunctionRegistration _defaultRegistration;

    public ConnectorHttpRequestProcessorTests()
    {
        _logger = NullLogger<ConnectorHttpRequestProcessor>.Instance;
        _processor = new ConnectorHttpRequestProcessor(_logger);
        _defaultRegistration = new ConnectorFunctionRegistration("TestFunction", new Moq.Mock<Microsoft.Azure.WebJobs.Host.Executors.ITriggeredFunctionExecutor>().Object, "test-namespace", "test-trigger");
    }

    private static void AddGatewayHeaders(HttpRequestMessage request, string triggerName = "test-trigger", string namespaceName = "test-namespace")
    {
        request.Headers.Add("x-ms-trigger-name", triggerName);
        request.Headers.Add("x-ms-gateway-resource-name", namespaceName);
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
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_AcceptsPostRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{\"test\": \"data\"}", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
            Content = new StringContent("{\"test\": \"data\"}", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
            Content = new StringContent("", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        string? capturedJson = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (json, _, _) =>
            {
                capturedJson = json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        string? capturedFunctionName = null;
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, functionName, _) =>
            {
                capturedFunctionName = functionName;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            };

        // Act
        await _processor.ProcessAsync(request, "MyTestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal("MyTestFunction", capturedFunctionName);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsCallbackResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Callback error")
            });

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

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
            Content = new StringContent("not valid json {{{", System.Text.Encoding.UTF8, "application/json")
        };
        AddGatewayHeaders(request);
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("valid JSON", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsUnsupportedMediaType_ForNonJsonContentType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("<xml/>", System.Text.Encoding.UTF8, "application/xml")
        };
        AddGatewayHeaders(request);
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("application/json", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsUnsupportedMediaType_WhenContentTypeIsNull()
    {
        // Arrange - Content with no explicit Content-Type (null media type)
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}")
        };
        request.Content.Headers.ContentType = null;
        AddGatewayHeaders(request);
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert - missing Content-Type should be rejected
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBadRequest_WhenTriggerNameHeaderMissing()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        // Only add gateway header, not trigger name
        request.Headers.Add("x-ms-gateway-resource-name", "test-namespace");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("x-ms-trigger-name", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBadRequest_WhenTriggerNameHeaderMismatch()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-ms-trigger-name", "wrong-trigger");
        request.Headers.Add("x-ms-gateway-resource-name", "test-namespace");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Trigger name mismatch", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBadRequest_WhenConnectorNamespaceHeaderMissing()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        // Only add trigger name, not gateway
        request.Headers.Add("x-ms-trigger-name", "test-trigger");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("x-ms-gateway-resource-name", content);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBadRequest_WhenConnectorNamespaceHeaderMismatch()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-ms-trigger-name", "test-trigger");
        request.Headers.Add("x-ms-gateway-resource-name", "wrong-namespace");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Gateway resource mismatch", content);
    }

    [Fact]
    public async Task ProcessAsync_HeaderMatchingIsCaseInsensitive()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/connector")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        // Use different casing than registration
        request.Headers.Add("x-ms-trigger-name", "TEST-TRIGGER");
        request.Headers.Add("x-ms-gateway-resource-name", "TEST-NAMESPACE");
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback =
            (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));

        // Act
        var response = await _processor.ProcessAsync(request, "TestFunction", _defaultRegistration, callback, CancellationToken.None);

        // Assert - case-insensitive match should pass
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
