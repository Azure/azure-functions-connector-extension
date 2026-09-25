// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

internal sealed class TestTokenCredential(string token = "test-token") : TokenCredential
{
    public List<string[]> RequestedScopes { get; } = [];

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        RequestedScopes.Add(requestContext.Scopes);
        return new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new(GetToken(requestContext, cancellationToken));
}

internal sealed class ThrowingTokenCredential(Exception exception) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => throw exception;

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => throw exception;
}

internal sealed class TestAzureComponentFactory(TokenCredential credential) : AzureComponentFactory
{
    public IConfiguration? LastConfiguration { get; private set; }
    public int CreateCredentialCalls { get; private set; }

    public override TokenCredential CreateTokenCredential(IConfiguration configuration)
    {
        LastConfiguration = configuration;
        CreateCredentialCalls++;
        return credential;
    }

    public override object CreateClientOptions(Type optionsType, object serviceVersion, IConfiguration configuration) =>
        throw new NotSupportedException();

    public override object CreateClient(Type clientType, IConfiguration configuration, TokenCredential credential, object clientOptions) =>
        throw new NotSupportedException();
}

internal sealed class TestHttpClientFactory(
    HttpMessageHandler handler,
    TimeSpan? timeout = null) : IHttpClientFactory
{
    public List<string> Names { get; } = [];

    public HttpClient CreateClient(string name)
    {
        Names.Add(name);
        var client = new HttpClient(handler, disposeHandler: false);
        if (timeout is TimeSpan configuredTimeout)
        {
            client.Timeout = configuredTimeout;
        }

        return client;
    }
}

internal sealed class SequenceHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
{
    private int _index;
    public int CallCount { get; private set; }
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        Requests.Add(request);
        int index = Math.Min(Interlocked.Increment(ref _index) - 1, responses.Length - 1);
        return Task.FromResult(responses[index](request));
    }
}

internal sealed class AsyncSequenceHttpMessageHandler(
    params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responses) : HttpMessageHandler
{
    private int _index;
    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        int index = Math.Min(Interlocked.Increment(ref _index) - 1, responses.Length - 1);
        return responses[index](request, cancellationToken);
    }
}

internal sealed class SequenceDepthClient(params object[] results) : IConnectorQueueDepthClient
{
    private int _index;
    public int CallCount { get; private set; }

    public Task<long> GetApproximateQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        object result = results[Math.Min(_index++, results.Length - 1)];
        return result is Exception exception
            ? Task.FromException<long>(exception)
            : Task.FromResult(Convert.ToInt64(result));
    }
}

internal sealed class TestConnectionOptionsProvider(
    Func<string, AzureComponentFactory?, ConnectorConnectionOptions> get) :
    IConnectorConnectionOptionsProvider
{
    public ConnectorConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null) =>
        get(connectionName, componentFactory);
}

internal sealed class TestPollDeliveryClientFactory(
    Func<TokenCredential, IConnectorPollDeliveryClient> create) :
    IConnectorPollDeliveryClientFactory
{
    public IConnectorPollDeliveryClient Create(TokenCredential credential) =>
        create(credential);
}

internal sealed class TestNameResolver(Func<string, string?> resolve) : INameResolver
{
    public string? Resolve(string name) => resolve(name);
}

internal sealed class TestDepthClientFactory(
    Func<
        ConnectorPollingEndpoints,
        TokenCredential,
        string,
        IConnectorQueueDepthClient> create) :
    IConnectorQueueDepthClientFactory
{
    public IConnectorQueueDepthClient Create(
        ConnectorPollingEndpoints endpoints,
        TokenCredential credential,
        string functionName) =>
        create(endpoints, credential, functionName);
}
