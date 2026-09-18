// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingEndpointResolverTests
{
    private const string ResourceId = "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway";

    [Fact]
    public async Task ResolveAsync_UsesArmScopeEscapedNameAndOneRequestForConcurrentCalls()
    {
        var credential = new TestTokenCredential();
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(ValidResponse()));
        var resolver = CreateResolver(handler, credential, "folder/name");

        ConnectorPollingEndpoints[] results = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => resolver.ResolveAsync()));

        Assert.Equal(1, handler.CallCount);
        Assert.All(results, result => Assert.Equal("https://runtime.test/depth?opaque=1", result.ApproximateQueueDepthUri.ToString()));
        Assert.Equal(new[] { ConnectorPollingEndpointResolver.ArmScope }, Assert.Single(credential.RequestedScopes));
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal($"https://management.azure.com{ResourceId}/triggerConfigs/folder%2Fname?api-version=2026-05-01-preview", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task ResolveAsync_RetriesAfterTransientArmFailure()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            _ => JsonResponse(ValidResponse()));
        var resolver = CreateResolver(handler, new TestTokenCredential(), "trigger");

        await Assert.ThrowsAsync<ConnectorPollingEndpointResolutionException>(() => resolver.ResolveAsync());
        ConnectorPollingEndpoints resolved = await resolver.ResolveAsync();

        Assert.Equal(2, handler.CallCount);
        Assert.Equal("https://runtime.test/depth?opaque=1", resolved.ApproximateQueueDepthUri.ToString());
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentCallsAwaitOneNewArmRequest()
    {
        var refreshRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidResponse("one"))),
            (_, _) =>
            {
                refreshRequestStarted.SetResult();
                return refreshResponse.Task;
            });
        var resolver = CreateResolver(handler, new TestTokenCredential(), "trigger");
        await resolver.ResolveAsync();

        Task<ConnectorPollingEndpoints> firstRefresh = resolver.RefreshAsync();
        await refreshRequestStarted.Task;
        Task<ConnectorPollingEndpoints>[] concurrentRefreshes = Enumerable.Range(0, 8)
            .Select(_ => resolver.RefreshAsync())
            .ToArray();

        Assert.Equal(2, handler.CallCount);
        Assert.False(firstRefresh.IsCompleted);
        Assert.All(concurrentRefreshes, refresh => Assert.False(refresh.IsCompleted));

        refreshResponse.SetResult(JsonResponse(ValidResponse("two")));
        ConnectorPollingEndpoints[] refreshed = await Task.WhenAll([firstRefresh, .. concurrentRefreshes]);
        ConnectorPollingEndpoints laterRefresh = await resolver.RefreshAsync();

        Assert.Equal(2, handler.CallCount);
        Assert.All(refreshed, value => Assert.Contains("two", value.ApproximateQueueDepthUri.Host));
        Assert.Contains("two", laterRefresh.ApproximateQueueDepthUri.Host);
    }

    [Theory]
    [InlineData("Disabled", "Poll", "https://runtime.test/depth")]
    [InlineData("Enabled", "Webhook", "https://runtime.test/depth")]
    [InlineData("Enabled", "Poll", "http://runtime.test/depth")]
    public async Task ResolveAsync_ValidatesStateModeAndHttps(string state, string mode, string depthUri)
    {
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(ValidResponse("runtime", state, mode, depthUri)));
        var resolver = CreateResolver(handler, new TestTokenCredential(), "trigger");

        await Assert.ThrowsAsync<ConnectorPollingEndpointResolutionException>(() => resolver.ResolveAsync());
    }

    private static ConnectorPollingEndpointResolver CreateResolver(
        HttpMessageHandler handler,
        TokenCredential credential,
        string triggerName) =>
        new(
            new ConnectorPollingConnection("ConnectorNamespace", new ResourceIdentifier(ResourceId), credential),
            credential,
            triggerName,
            new TestHttpClientFactory(handler),
            NullLogger<ConnectorPollingEndpointResolver>.Instance);

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static string ValidResponse(
        string host = "runtime",
        string state = "Enabled",
        string mode = "Poll",
        string? depthUri = null) =>
        $$"""
        {
          "properties": {
            "state": "{{state}}",
            "deliveryMode": "{{mode}}",
            "pollingEndpoints": {
              "receiveUri": "https://{{host}}.test/receive?opaque=1",
              "acknowledgeUri": "https://{{host}}.test/ack?opaque=1",
              "hasMessagesUri": "https://{{host}}.test/has?opaque=1",
              "approximateQueueDepthUri": "{{depthUri ?? $"https://{host}.test/depth?opaque=1"}}"
            }
          }
        }
        """;
}
