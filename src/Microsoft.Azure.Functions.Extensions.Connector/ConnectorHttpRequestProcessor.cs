// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Processes incoming HTTP requests for the Connector trigger.
/// Parses body to JToken and invokes function via callback.
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
    /// Processes an HTTP request and invokes the callback with the parsed JToken.
    /// </summary>
    internal async Task<HttpResponseMessage> ProcessAsync(
        HttpRequestMessage request,
        string functionName,
        Func<JToken, string, CancellationToken, Task<HttpResponseMessage>> executeFunc,
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
            // Validate content length header
            if (request.Content?.Headers?.ContentLength > MaxBodySize)
            {
                _logger.LogWarning("Request body too large: {ContentLength} bytes", 
                    request.Content.Headers.ContentLength);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge);
            }

            // Read request body
            string body = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : string.Empty;

            // Validate actual body size
            if (body.Length > MaxBodySize)
            {
                _logger.LogWarning("Request body too large: {Length} bytes", body.Length);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge);
            }

            LogRequestInfo(request, functionName, body.Length);

            // Parse body to JToken
            JToken triggerValue = string.IsNullOrWhiteSpace(body) 
                ? JValue.CreateNull() 
                : JToken.Parse(body);

            // Execute via callback
            return await executeFunc(triggerValue, functionName, cancellationToken).ConfigureAwait(false);
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
