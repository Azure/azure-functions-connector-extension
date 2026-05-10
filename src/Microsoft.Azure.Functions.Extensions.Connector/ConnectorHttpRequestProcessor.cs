// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Processes incoming HTTP requests for the Connector trigger.
/// Validates Content-Type, gateway headers, and JSON body before invoking the function.
/// </summary>
internal sealed class ConnectorHttpRequestProcessor
{
    private const long MaxBodySize = 10 * 1024 * 1024; // 10 MB
    private const string TriggerNameHeader = "x-ms-trigger-name";
    private const string ConnectorNamespaceHeader = "x-ms-gateway-resource-name";

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
        ConnectorFunctionRegistration registration,
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
        var contentType = request.Content?.Headers?.ContentType?.MediaType;
        if (!string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Connector trigger received unsupported Content-Type: {ContentType}", contentType);
            return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType)
            {
                Content = new StringContent("Content-Type must be application/json")
            };
        }

        // Validate x-ms-trigger-name header
        var headerValidationResult = ValidateHeaders(request, registration);
        if (headerValidationResult != null)
        {
            return headerValidationResult;
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
        var triggerName = request.Headers.TryGetValues(TriggerNameHeader, out var tn) ? tn.First() : "(not set)";
        var gatewayName = request.Headers.TryGetValues(ConnectorNamespaceHeader, out var gn) ? gn.First() : "(not set)";
        _logger.LogDebug(
            "Processing connector request: Function={FunctionName}, TriggerName={TriggerName}, Gateway={Gateway}, BodyLength={BodyLength}",
            functionName, triggerName, gatewayName, bodyLength);
    }

    private HttpResponseMessage? ValidateHeaders(HttpRequestMessage request, ConnectorFunctionRegistration registration)
    {
        // Validate x-ms-trigger-name
        if (!request.Headers.TryGetValues(TriggerNameHeader, out var triggerValues))
        {
            _logger.LogWarning("Missing required header {Header} for function {FunctionName}", TriggerNameHeader, registration.FunctionName);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"Missing required header: {TriggerNameHeader}")
            };
        }

        var triggerName = triggerValues.First();
        if (!string.Equals(triggerName, registration.TriggerName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Trigger name mismatch for function {FunctionName}: expected '{Expected}', received '{Received}'",
                registration.FunctionName, registration.TriggerName, triggerName);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Trigger name mismatch")
            };
        }

        // Validate x-ms-gateway-resource-name
        if (!request.Headers.TryGetValues(ConnectorNamespaceHeader, out var gatewayValues))
        {
            _logger.LogWarning("Missing required header {Header} for function {FunctionName}", ConnectorNamespaceHeader, registration.FunctionName);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"Missing required header: {ConnectorNamespaceHeader}")
            };
        }

        var gatewayName = gatewayValues.First();
        if (!string.Equals(gatewayName, registration.ConnectorNamespace, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Gateway resource mismatch for function {FunctionName}: expected '{Expected}', received '{Received}'",
                registration.FunctionName, registration.ConnectorNamespace, gatewayName);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Gateway resource mismatch")
            };
        }

        return null; // validation passed
    }
}
