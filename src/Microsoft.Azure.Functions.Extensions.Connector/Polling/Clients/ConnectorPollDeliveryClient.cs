// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Net.Http.Headers;
using Azure.Core;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorPollDeliveryClient
{
    Task<ConnectorReceiveResult> ReceiveAsync(
        ConnectorPollingEndpoints endpoints,
        int maxEvents,
        CancellationToken cancellationToken);

    Task<ConnectorAcknowledgeResult> AcknowledgeAsync(
        ConnectorPollingEndpoints endpoints,
        IReadOnlyList<ConnectorMessageLock> messages,
        CancellationToken cancellationToken);
}

internal interface IConnectorPollDeliveryClientFactory
{
    IConnectorPollDeliveryClient Create(TokenCredential credential);
}

internal sealed class ConnectorPollDeliveryClientFactory :
    IConnectorPollDeliveryClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ConnectorPollDeliveryClientFactory(IHttpClientFactory httpClientFactory) =>
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));

    public IConnectorPollDeliveryClient Create(TokenCredential credential) =>
        new ConnectorPollDeliveryClient(
            credential ?? throw new ArgumentNullException(nameof(credential)),
            _httpClientFactory);
}

internal sealed class ConnectorPollDeliveryClient : IConnectorPollDeliveryClient
{
    internal const string HttpClientName =
        ConnectorPollingHttpConstants.RuntimeClientName;
    internal const string ApiHubScope =
        ConnectorPollingHttpConstants.ApiHubScope;
    internal const string MoreMessagesAvailableHeader =
        ConnectorPollingHttpConstants.MoreMessagesAvailableHeader;

    private readonly TokenCredential _credential;
    private readonly IHttpClientFactory _httpClientFactory;

    internal ConnectorPollDeliveryClient(
        TokenCredential credential,
        IHttpClientFactory httpClientFactory)
    {
        _credential =
            credential ?? throw new ArgumentNullException(nameof(credential));
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<ConnectorReceiveResult> ReceiveAsync(
        ConnectorPollingEndpoints endpoints,
        int maxEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (maxEvents is < 1 or > ConnectorPollingProtocolLimits.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxEvents),
                $"Connector Receive maxEvents must be between 1 and {ConnectorPollingProtocolLimits.MaximumBatchSize}.");
        }

        Uri endpoint = AddMaxEvents(endpoints.ReceiveUri, maxEvents);
        HttpResponseMessage response;
        try
        {
            response = await SendAuthenticatedAsync(
                HttpMethod.Get,
                endpoint,
                content: null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new ConnectorPollDeliveryException(
                "Connector Receive HTTP request failed.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConnectorPollDeliveryException(
                "Connector Receive HTTP request timed out.");
        }
        using (response)
        {
            EnsureSuccess(response, "Receive");

            bool moreMessagesAvailable =
                ParseMoreMessagesAvailable(response.Headers);
            BinaryData content = await ReadContentAsync(
                response,
                cancellationToken).ConfigureAwait(false);
            return ConnectorPollingProtocol.DeserializeReceive(
                content,
                moreMessagesAvailable);
        }
    }

    public async Task<ConnectorAcknowledgeResult> AcknowledgeAsync(
        ConnectorPollingEndpoints endpoints,
        IReadOnlyList<ConnectorMessageLock> messages,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(messages);
        BinaryData requestContent =
            ConnectorPollingProtocol.SerializeAcknowledgeRequest(messages);
        using var content = new ByteArrayContent(requestContent.ToArray());
        content.Headers.ContentType =
            new MediaTypeHeaderValue(
                ConnectorPollingHttpConstants.JsonMediaType)
            {
                CharSet = ConnectorPollingHttpConstants.Utf8CharacterSet,
            };

        HttpResponseMessage response;
        try
        {
            response = await SendAuthenticatedAsync(
                HttpMethod.Post,
                endpoints.AcknowledgeUri,
                content,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new ConnectorPollDeliveryException(
                "Connector Acknowledge HTTP request failed.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new ConnectorPollDeliveryException(
                "Connector Acknowledge HTTP request timed out.");
        }
        using (response)
        {
            EnsureSuccess(response, "Acknowledge");

            BinaryData responseContent = await ReadContentAsync(
                response,
                cancellationToken).ConfigureAwait(false);
            return ConnectorPollingProtocol.DeserializeAcknowledge(
                responseContent,
                messages);
        }
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method,
        Uri endpoint,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        AccessToken token = await _credential.GetTokenAsync(
            new TokenRequestContext([ApiHubScope]),
            cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(method, endpoint)
        {
            Content = content,
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);
        return await _httpClientFactory.CreateClient(HttpClientName)
            .SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);
    }

    // Polling endpoints are service-generated and may contain opaque query
    // parameters that must remain unchanged. Remove any existing maxEvents
    // parameter, then append the listener's current capacity as the single
    // authoritative value so the request never contains ambiguous duplicates.
    private static Uri AddMaxEvents(Uri endpoint, int maxEvents)
    {
        string[] existingParameters = endpoint.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> parameters = existingParameters.Where(
            static parameter =>
            {
                int separatorIndex = parameter.IndexOf('=');
                ReadOnlySpan<char> name = separatorIndex < 0
                    ? parameter.AsSpan()
                    : parameter.AsSpan(0, separatorIndex);
                return !name.Equals(
                    ConnectorPollingHttpConstants.MaxEventsQueryParameter,
                    StringComparison.OrdinalIgnoreCase);
            });
        var builder = new UriBuilder(endpoint)
        {
            Query = string.Join(
                "&",
                parameters.Append(
                    $"{ConnectorPollingHttpConstants.MaxEventsQueryParameter}={maxEvents.ToString(CultureInfo.InvariantCulture)}")),
        };
        return builder.Uri;
    }

    private static bool ParseMoreMessagesAvailable(
        HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues(
            MoreMessagesAvailableHeader,
            out IEnumerable<string>? values))
        {
            return false;
        }

        string[] headerValues = values.ToArray();
        if (headerValues.Length != 1 ||
            !bool.TryParse(headerValues[0], out bool result))
        {
            throw new ConnectorPollDeliveryException(
                $"Connector Receive response header '{MoreMessagesAvailableHeader}' must contain one boolean value.");
        }

        return result;
    }

    private static async Task<BinaryData> ReadContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using Stream stream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await BinaryData.FromStreamAsync(stream, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ConnectorPollDeliveryException(
                $"Connector {operation} request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }
    }

}

internal sealed class ConnectorPollDeliveryException : Exception
{
    internal ConnectorPollDeliveryException(string message)
        : base(message)
    {
    }
}
