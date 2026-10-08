// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorQueueDepthClient
{
    Task<long> GetApproximateQueueDepthAsync(CancellationToken cancellationToken = default);
}

internal interface IConnectorQueueDepthClientFactory
{
    IConnectorQueueDepthClient Create(
        ConnectorPollingEndpoints endpoints,
        TokenCredential credential,
        string functionName);
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
        ConnectorPollingEndpoints endpoints,
        TokenCredential credential,
        string functionName) =>
        new ConnectorQueueDepthClient(
            endpoints,
            credential,
            _httpClientFactory,
            functionName,
            _loggerFactory.CreateLogger<ConnectorQueueDepthClient>());
}

internal sealed class ConnectorQueueDepthClient : IConnectorQueueDepthClient
{
    internal const string HttpClientName =
        ConnectorPollDeliveryClient.HttpClientName;
    internal const string ApiHubScope =
        ConnectorPollDeliveryClient.ApiHubScope;

    private readonly ConnectorPollingEndpoints _endpoints;
    private readonly TokenCredential _credential;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _functionName;
    private readonly ILogger _logger;

    public ConnectorQueueDepthClient(
        ConnectorPollingEndpoints endpoints,
        TokenCredential credential,
        IHttpClientFactory httpClientFactory,
        string functionName,
        ILogger logger)
    {
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _functionName = functionName;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<long> GetApproximateQueueDepthAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetDepthCoreAsync(
            _endpoints.ApproximateQueueDepthUri,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> GetDepthCoreAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext([ApiHubScope]),
                cancellationToken).ConfigureAwait(false);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                ConnectorPollingHttpConstants.BearerAuthenticationScheme,
                token.Token);

            HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
            using CancellationTokenSource timeoutSource =
                ConnectorPollingTimeout.CreateCancellationTokenSource(
                    client,
                    cancellationToken);
            CancellationToken requestCancellationToken = timeoutSource.Token;
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestCancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ConnectorQueueDepthException(
                    $"Connector approximate queue depth request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            BinaryData responseContent =
                await ConnectorPollingContentReader.ReadAsync(
                    response.Content,
                    ConnectorPollingProtocolLimits
                        .MaximumQueueDepthResponseSizeInBytes,
                    reason => new ConnectorQueueDepthException(
                        $"Connector approximate queue depth {reason}."),
                    requestCancellationToken).ConfigureAwait(false);
            long depth =
                ConnectorPollingProtocol.DeserializeApproximateQueueDepth(
                    responseContent);

            _logger.LogDebug(
                "Connector approximate queue depth for function {FunctionName} is {Depth}.",
                _functionName,
                depth);
            return depth;
        }
        catch (ConnectorQueueDepthException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new ConnectorQueueDepthException(
                "Connector approximate queue depth HTTP request failed.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConnectorQueueDepthException(
                "Connector approximate queue depth HTTP request timed out.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
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
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
