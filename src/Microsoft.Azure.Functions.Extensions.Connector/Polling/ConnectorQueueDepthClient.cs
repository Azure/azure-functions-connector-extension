// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorQueueDepthClient
{
    Task<int> GetApproximateQueueDepthAsync(CancellationToken cancellationToken = default);
}

internal interface IConnectorQueueDepthClientFactory
{
    IConnectorQueueDepthClient Create(
        IConnectorPollingEndpointResolver endpointResolver,
        TokenCredential credential,
        string functionName,
        string triggerConfigName);
}

internal sealed class ConnectorQueueDepthClientFactory : IConnectorQueueDepthClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ConnectorQueueDepthClientFactory(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IConnectorQueueDepthClient Create(
        IConnectorPollingEndpointResolver endpointResolver,
        TokenCredential credential,
        string functionName,
        string triggerConfigName) =>
        new ConnectorQueueDepthClient(
            endpointResolver,
            credential,
            _httpClientFactory,
            functionName,
            triggerConfigName,
            _loggerFactory.CreateLogger<ConnectorQueueDepthClient>());
}

internal sealed class ConnectorQueueDepthClient : IConnectorQueueDepthClient
{
    internal const string HttpClientName = "ConnectorPollingRuntime";
    internal const string ApiHubScope = "https://apihub.azure.com/.default";

    private readonly IConnectorPollingEndpointResolver _endpointResolver;
    private readonly TokenCredential _credential;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _functionName;
    private readonly string _triggerConfigName;
    private readonly ILogger _logger;

    public ConnectorQueueDepthClient(
        IConnectorPollingEndpointResolver endpointResolver,
        TokenCredential credential,
        IHttpClientFactory httpClientFactory,
        string functionName,
        string triggerConfigName,
        ILogger logger)
    {
        _endpointResolver = endpointResolver ?? throw new ArgumentNullException(nameof(endpointResolver));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerConfigName);
        _functionName = functionName;
        _triggerConfigName = triggerConfigName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> GetApproximateQueueDepthAsync(
        CancellationToken cancellationToken = default)
    {
        ConnectorPollingEndpoints endpoints = await _endpointResolver
            .ResolveAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetDepthCoreAsync(
                endpoints.ApproximateQueueDepthUri,
                cancellationToken).ConfigureAwait(false);
        }
        catch (ConnectorQueueDepthException exception) when (exception.EndpointMayBeStale)
        {
            _logger.LogWarning(
                exception,
                "Connector queue depth endpoint was stale for function {FunctionName} and trigger configuration {TriggerConfigName}; refreshing once.",
                _functionName,
                _triggerConfigName);
            endpoints = await _endpointResolver
                .RefreshAsync(cancellationToken).ConfigureAwait(false);
            return await GetDepthCoreAsync(
                endpoints.ApproximateQueueDepthUri,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> GetDepthCoreAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        try
        {
            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext([ApiHubScope]),
                cancellationToken).ConfigureAwait(false);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ConnectorQueueDepthException(
                    $"Connector approximate queue depth request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                    response.StatusCode);
            }

            await using Stream content = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(
                content,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("approximateQueueDepth", out JsonElement depthElement) ||
                !depthElement.TryGetInt32(out int depth) ||
                depth < 0)
            {
                throw new ConnectorQueueDepthException(
                    "Connector approximate queue depth response did not contain a non-negative integer.");
            }

            _logger.LogDebug(
                "Connector approximate queue depth for function {FunctionName} and trigger configuration {TriggerConfigName} is {Depth}.",
                _functionName,
                _triggerConfigName,
                depth);
            return depth;
        }
        catch (ConnectorQueueDepthException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to query Connector approximate queue depth for function {FunctionName} and trigger configuration {TriggerConfigName}.",
                _functionName,
                _triggerConfigName);
            throw new ConnectorQueueDepthException(
                "Connector approximate queue depth request failed.",
                innerException: exception);
        }
    }
}

internal sealed class ConnectorQueueDepthException : Exception
{
    public ConnectorQueueDepthException(
        string message,
        HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;

    public HttpStatusCode? StatusCode { get; }

    public bool EndpointMayBeStale => StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
}
