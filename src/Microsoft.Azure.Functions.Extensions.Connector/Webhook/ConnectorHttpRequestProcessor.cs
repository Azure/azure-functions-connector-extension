// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.Connector.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Processes incoming HTTP requests for the Connector trigger.
/// Validates Content-Type and JSON body before invoking the function.
/// </summary>
internal sealed class ConnectorHttpRequestProcessor
{
    private const string ExpectedContentType = ConnectorMediaTypes.Json;

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

        // Validate Content-Type
        HttpContent? content = request.Content;
        var contentType = content?.Headers.ContentType?.MediaType;
        if (content == null ||
            !string.Equals(contentType, ExpectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Connector trigger received unsupported Content-Type: {ContentType}", contentType ?? "(none)");
            return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
            {
                Content = new StringContent($"Content-Type must be {ExpectedContentType}")
            };
        }

        try
        {
            string body;
            try
            {
                body = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsRequestBodyTooLarge(ex))
            {
                _logger.LogWarning(ex, "Request body exceeded the host limit for function {FunctionName}", functionName);
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

    private static bool IsRequestBodyTooLarge(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
            {
                return true;
            }
        }

        return false;
    }

    private void LogRequestInfo(HttpRequestMessage request, string functionName, int bodyLength)
    {
        var headerKeys = string.Join(", ", request.Headers.Select(h => h.Key));
        _logger.LogDebug(
            "Processing connector request: Function={FunctionName}, BodyLength={BodyLength}, Headers=[{Headers}]",
            functionName, bodyLength, headerKeys);
    }
}
