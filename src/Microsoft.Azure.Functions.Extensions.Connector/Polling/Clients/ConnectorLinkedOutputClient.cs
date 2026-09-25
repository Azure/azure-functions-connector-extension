// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Azure.Functions.Extensions.Connector.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorLinkedOutputClient
{
    Task<BinaryData> DownloadAsync(
        ConnectorOutputsLink outputsLink,
        int maximumPayloadSizeInBytes,
        CancellationToken cancellationToken);
}

internal sealed class ConnectorLinkedOutputClient :
    IConnectorLinkedOutputClient
{
    internal const string HttpClientName =
        ConnectorPollingHttpConstants.LinkedOutputClientName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConnectorLinkedOutputClient> _logger;
    private readonly Func<int, CancellationToken, Task> _retryDelayAsync =
        DelayForRetryAsync;

    public ConnectorLinkedOutputClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ConnectorLinkedOutputClient> logger)
    {
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal ConnectorLinkedOutputClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ConnectorLinkedOutputClient> logger,
        Func<int, CancellationToken, Task> retryDelayAsync)
        : this(httpClientFactory, logger) =>
        _retryDelayAsync = retryDelayAsync ??
            throw new ArgumentNullException(nameof(retryDelayAsync));

    public async Task<BinaryData> DownloadAsync(
        ConnectorOutputsLink outputsLink,
        int maximumPayloadSizeInBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputsLink);
        if (maximumPayloadSizeInBytes is < 1 or
            > ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPayloadSizeInBytes),
                $"Connector linked-output size limit must be between 1 and {ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes} bytes.");
        }

        for (int attempt = 1;
            attempt <= ConnectorPollingHttpConstants.MaximumSafeOperationAttempts;
            attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    outputsLink.Uri);
                HttpClient client =
                    _httpClientFactory.CreateClient(HttpClientName);
                using CancellationTokenSource timeoutSource =
                    ConnectorPollingTimeout.CreateCancellationTokenSource(
                        client,
                        cancellationToken);
                CancellationToken requestCancellationToken =
                    timeoutSource.Token;
                using HttpResponseMessage response =
                    await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        requestCancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    if (attempt <
                            ConnectorPollingHttpConstants.MaximumSafeOperationAttempts &&
                        IsTransient(response.StatusCode))
                    {
                        await _retryDelayAsync(attempt, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    throw Failure(
                        outputsLink.Uri,
                        $"request failed with HTTP {(int)response.StatusCode} ({response.StatusCode})");
                }

                ValidateResponseHeaders(
                    response,
                    outputsLink.Uri,
                    maximumPayloadSizeInBytes);
                BinaryData content =
                    await ConnectorPollingContentReader.ReadAsync(
                    response.Content,
                    maximumPayloadSizeInBytes,
                    reason => Failure(outputsLink.Uri, reason),
                    requestCancellationToken).ConfigureAwait(false);
                ValidateJsonObject(content, outputsLink.Uri);
                return content;
            }
            catch (HttpRequestException)
            {
                if (attempt ==
                    ConnectorPollingHttpConstants.MaximumSafeOperationAttempts)
                {
                    throw Failure(
                        outputsLink.Uri,
                        "HTTP request failed");
                }

                _logger.LogWarning(
                    "Transient Connector linked-output request failure; retrying attempt {NextAttempt} of {MaximumAttempts}.",
                    attempt + 1,
                    ConnectorPollingHttpConstants.MaximumSafeOperationAttempts);
                await _retryDelayAsync(attempt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt ==
                    ConnectorPollingHttpConstants.MaximumSafeOperationAttempts)
                {
                    throw Failure(
                        outputsLink.Uri,
                        "HTTP request timed out");
                }

                _logger.LogWarning(
                    "Connector linked-output request timed out; retrying attempt {NextAttempt} of {MaximumAttempts}.",
                    attempt + 1,
                    ConnectorPollingHttpConstants.MaximumSafeOperationAttempts);
                await _retryDelayAsync(attempt, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            "Connector linked-output retry loop completed unexpectedly.");
    }

    private static void ValidateResponseHeaders(
        HttpResponseMessage response,
        Uri endpoint,
        int maximumPayloadSizeInBytes)
    {
        MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
        if (contentType is null ||
            !string.Equals(
                contentType.MediaType,
        ConnectorMediaTypes.Json,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                contentType.CharSet,
                ConnectorPollingHttpConstants.Utf8CharacterSet,
                StringComparison.OrdinalIgnoreCase))
        {
            throw Failure(
                endpoint,
                "response Content-Type must be application/json; charset=utf-8");
        }

        if (response.Content.Headers.ContentEncoding.Count > 0)
        {
            throw Failure(
                endpoint,
                "response must not use content encoding");
        }

        if (response.Content.Headers.ContentLength is long contentLength &&
            contentLength > maximumPayloadSizeInBytes)
        {
            throw Failure(
                endpoint,
                $"response exceeded the {maximumPayloadSizeInBytes}-byte limit");
        }
    }

    private static void ValidateJsonObject(BinaryData content, Uri endpoint)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw Failure(
                    endpoint,
                    "response body must be a complete JSON object");
            }
        }
        catch (JsonException exception)
        {
            throw Failure(
                endpoint,
                "response body must be a complete JSON object",
                innerException: exception);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static ConnectorLinkedOutputException Failure(
        Uri endpoint,
        string reason,
        Exception? innerException = null) =>
        new(
            $"Connector linked-output {reason} at {ConnectorPollingUri.Redact(endpoint)}.",
            innerException);

    private static Task DelayForRetryAsync(
        int attempt,
        CancellationToken cancellationToken) =>
        Task.Delay(
            ConnectorPollingHttpConstants.BaseRetryDelay * attempt,
            cancellationToken);
}

internal sealed class ConnectorLinkedOutputException : Exception
{
    internal ConnectorLinkedOutputException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
