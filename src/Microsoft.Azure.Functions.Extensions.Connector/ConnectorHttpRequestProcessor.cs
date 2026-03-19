// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Web;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Processes incoming HTTP requests for the Connector trigger.
/// Parses the request body, headers, and query parameters into a format
/// suitable for function invocation.
/// </summary>
internal sealed class ConnectorHttpRequestProcessor
{
    /// <summary>
    /// Maximum allowed request body size (10 MB).
    /// </summary>
    private const long MaxBodySize = 10 * 1024 * 1024;

    private readonly ILogger<ConnectorHttpRequestProcessor> _logger;

    public ConnectorHttpRequestProcessor(ILogger<ConnectorHttpRequestProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an HTTP request and returns the appropriate response.
    /// </summary>
    /// <param name="request">The incoming HTTP request.</param>
    /// <param name="functionName">The name of the function to invoke.</param>
    /// <param name="executor">The function executor to invoke with the parsed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response to return to the caller.</returns>
    public async Task<HttpResponseMessage> ProcessAsync(
        HttpRequestMessage request,
        string functionName,
        ITriggeredFunctionExecutor executor,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post && request.Method != HttpMethod.Put)
        {
            _logger.LogWarning("Connector trigger received non-POST/PUT request: {Method}", request.Method);
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
            {
                Content = new StringContent("Only POST and PUT methods are supported.")
            };
        }

        try
        {
            // Validate request body size
            if (request.Content?.Headers?.ContentLength > MaxBodySize)
            {
                _logger.LogWarning("Request body too large: {ContentLength} bytes", 
                    request.Content.Headers.ContentLength);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
                {
                    Content = new StringContent("Request body size exceeds maximum allowed size")
                };
            }

            // Read the request body
            string body = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : string.Empty;

            // Double-check actual body size after reading
            if (body.Length > MaxBodySize)
            {
                _logger.LogWarning("Request body too large after reading: {Length} bytes", body.Length);
                return new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
                {
                    Content = new StringContent("Request body size exceeds maximum allowed size")
                };
            }

            // Parse headers
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            // Parse query parameters
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var queryString = HttpUtility.ParseQueryString(request.RequestUri?.Query ?? string.Empty);
            foreach (string? key in queryString.AllKeys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    query[key] = queryString[key] ?? string.Empty;
                }
            }

            // Create the connector context
            var context = new ConnectorContext
            {
                Body = body,
                Headers = headers,
                Query = query,
                Method = request.Method.Method,
                Url = request.RequestUri?.ToString() ?? string.Empty,
                ContentType = request.Content?.Headers?.ContentType?.MediaType,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Parse body as JToken for type conversion support
            JToken jsonBody;
            try
            {
                jsonBody = string.IsNullOrWhiteSpace(body) 
                    ? JValue.CreateNull() 
                    : JToken.Parse(body);
            }
            catch (JsonException)
            {
                // Body is not valid JSON, wrap it as a string value
                jsonBody = new JValue(body);
            }

            _logger.LogDebug("Processing connector request for function {FunctionName}, body length: {BodyLength}", 
                functionName, body.Length);

            return await ExecuteFunctionAsync(executor, context, jsonBody, functionName, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request processing cancelled for function {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("Request cancelled")
            };
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            _logger.LogError(ex, "Error processing connector request for function {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("An error occurred processing your request")
            };
        }
    }

    /// <summary>
    /// Executes the function with the provided context and JSON body.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteFunctionAsync(
        ITriggeredFunctionExecutor executor,
        ConnectorContext context,
        JToken jsonBody,
        string functionName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create the trigger data with both context and JSON body as a tuple
            var triggerData = new TriggeredFunctionData
            {
                TriggerValue = (context, jsonBody)
            };

            var result = await executor.TryExecuteAsync(triggerData, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                _logger.LogDebug("Connector function {FunctionName} executed successfully", functionName);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Function executed successfully")
                };
            }
            else
            {
                _logger.LogError(result.Exception, "Connector function {FunctionName} failed", functionName);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Function execution failed")
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Function execution cancelled: {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("Request cancelled")
            };
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            _logger.LogError(ex, "Error executing connector function {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Function execution failed")
            };
        }
    }
}
