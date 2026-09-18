// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorQueueDepthClientTests
{
    [Fact]
    public async Task GetApproximateQueueDepthAsync_UsesOpaqueEndpointRuntimeScopeAndParsesDepth()
    {
        ConnectorPollingEndpoints endpoints = Endpoints("https://opaque.runtime.test/custom/depth?server=value");
        var resolver = new StubEndpointResolver(endpoints);
        var credential = new TestTokenCredential();
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"approximateQueueDepth\":37}"));
        var client = CreateClient(resolver, credential, handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(37, depth);
        Assert.Equal(new[] { ConnectorQueueDepthClient.ApiHubScope }, Assert.Single(credential.RequestedScopes));
        Assert.Equal(endpoints.ApproximateQueueDepthUri, Assert.Single(handler.Requests).RequestUri);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_RefreshesOnceForNotFound()
    {
        var resolver = new StubEndpointResolver(
            Endpoints("https://old.test/depth"),
            Endpoints("https://new.test/depth"));
        var handler = new SequenceHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.NotFound, "{}"),
            _ => JsonResponse(HttpStatusCode.OK, "{\"approximateQueueDepth\":9}"));
        var client = CreateClient(resolver, new TestTokenCredential(), handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(9, depth);
        Assert.Equal(1, resolver.RefreshCalls);
        Assert.Equal(new[] { "old.test", "new.test" }, handler.Requests.Select(r => r.RequestUri!.Host));
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_SupportsInt64Depth()
    {
        var resolver = new StubEndpointResolver(
            Endpoints("https://runtime.test/depth"));
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(
                HttpStatusCode.OK,
                "{\"approximateQueueDepth\":2147483648}"));
        var client = CreateClient(
            resolver,
            new TestTokenCredential(),
            handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(2147483648L, depth);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DoesNotRefreshForOtherErrors()
    {
        var resolver = new StubEndpointResolver(Endpoints("https://runtime.test/depth"));
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.InternalServerError, "{}"));
        var client = CreateClient(resolver, new TestTokenCredential(), handler);

        await Assert.ThrowsAsync<ConnectorQueueDepthException>(() => client.GetApproximateQueueDepthAsync());

        Assert.Equal(0, resolver.RefreshCalls);
        Assert.Equal(1, handler.CallCount);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"approximateQueueDepth\":-1}")]
    [InlineData("{\"approximateQueueDepth\":\"4\"}")]
    public async Task GetApproximateQueueDepthAsync_RejectsInvalidDepth(string body)
    {
        var resolver = new StubEndpointResolver(Endpoints("https://runtime.test/depth"));
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, body));
        var client = CreateClient(resolver, new TestTokenCredential(), handler);

        await Assert.ThrowsAsync<ConnectorQueueDepthException>(() => client.GetApproximateQueueDepthAsync());
    }

    private static ConnectorQueueDepthClient CreateClient(
        IConnectorPollingEndpointResolver resolver,
        TestTokenCredential credential,
        HttpMessageHandler handler) =>
        new(
            resolver,
            credential,
            new TestHttpClientFactory(handler),
            "Function",
            "Trigger",
            NullLogger<ConnectorQueueDepthClient>.Instance);

    private static ConnectorPollingEndpoints Endpoints(string depthUri) => new(
        new Uri("https://runtime.test/receive"),
        new Uri("https://runtime.test/ack"),
        new Uri("https://runtime.test/has"),
        new Uri(depthUri));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };
}
