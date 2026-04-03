// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Processes incoming HTTP requests for the Connector trigger.
/// </summary>
internal sealed class ConnectorHttpRequestProcessor
{
    private const long MaxBodySize = 10 * 1024 * 1024; // 10 MB

    private readonly ILogger<ConnectorHttpRequestProcessor> _logger;

    public ConnectorHttpRequestProcessor(ILogger<ConnectorHttpRequestProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an HTTP request and invokes the callback with the raw JSON string.
    /// </summary>
    internal async Task<HttpResponseMessage> ProcessAsync(
        HttpRequestMessage request,
        string functionName,
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> executeFunc,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post && request.Method != HttpMethod.Put)
        {
            _logger.LogWarning("Connector trigger received unsupported method: {Method}", request.Method);
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
            {
                Content = new StringContent("Only POST and PUT methods are supported.")
            };
        }

        try
        {
            if (request.Content?.Headers?.ContentLength > MaxBodySize)
            {
                _logger.LogWarning("Request body too large: {ContentLength} bytes",
                    request.Content.Headers.ContentLength);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge);
            }

            string body = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : string.Empty;

            if (body.Length > MaxBodySize)
            {
                _logger.LogWarning("Request body too large: {Length} bytes", body.Length);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge);
            }

            LogRequestInfo(request, functionName, body.Length);

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON in request body for function {FunctionName}", functionName);
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Request body must be valid JSON")
                    };
                }
            }

            return await executeFunc(body, functionName, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request cancelled for function {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for function {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    private void LogRequestInfo(HttpRequestMessage request, string functionName, int bodyLength)
    {
        var headerKeys = string.Join(", ", request.Headers.Select(h => h.Key));
        _logger.LogDebug(
            "Processing connector request: Function={FunctionName}, BodyLength={BodyLength}, Headers=[{Headers}]",
            functionName, bodyLength, headerKeys);
    }
}
