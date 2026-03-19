// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Extension configuration provider for the Connector extension.
/// 
/// This class serves two critical purposes:
/// 1. Initializes the extension and registers bindings with the Azure Functions host
/// 2. Implements IAsyncConverter to handle incoming HTTP requests for connector-triggered functions
/// 
/// The [Extension] attribute defines:
/// - "Connector": The extension name used in logs and diagnostics
/// - "connector": The configuration section name in host.json (extensions.connector)
/// </summary>
[Extension("Connector", "connector")]
internal sealed class ConnectorExtensionConfigProvider : IExtensionConfigProvider,
    IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private const int MaxFunctionNameLength = 128;
    private static readonly Regex FunctionNamePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ConnectorExtensionConfigProvider> _logger;
    private readonly ConnectorHttpRequestProcessor _httpRequestProcessor;
    private readonly ConcurrentDictionary<string, ConnectorListener> _listeners = new(StringComparer.OrdinalIgnoreCase);

    public ConnectorExtensionConfigProvider(
        ConnectorHttpRequestProcessor httpRequestProcessor,
        ILoggerFactory loggerFactory)
    {
        _httpRequestProcessor = httpRequestProcessor ?? throw new ArgumentNullException(nameof(httpRequestProcessor));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<ConnectorExtensionConfigProvider>();
    }

    /// <summary>
    /// Registers a listener for a function. Called by ConnectorListener during construction.
    /// Thread-safe via ConcurrentDictionary.
    /// </summary>
    internal void AddListener(string functionName, ConnectorListener listener)
    {
        _listeners[functionName] = listener;
        _logger.LogDebug("Registered connector listener for function: {FunctionName}", functionName);
    }

    /// <summary>
    /// Removes a listener for a function. Called during listener disposal.
    /// </summary>
    internal void RemoveListener(string functionName)
    {
        _listeners.TryRemove(functionName, out _);
        _logger.LogDebug("Removed connector listener for function: {FunctionName}", functionName);
    }

    /// <summary>
    /// Initializes the extension. This is called by the Azure Functions host during startup.
    /// </summary>
    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Get the webhook URL that external systems will use to invoke connector functions
#pragma warning disable 618 // GetWebhookHandler is obsolete but still the way to register webhook endpoints
        Uri? webhookUrl = context.GetWebhookHandler();
#pragma warning restore 618

        // Log endpoint to console (visible in Functions host output)
        var endpointPath = webhookUrl?.GetLeftPart(UriPartial.Path) ?? string.Empty;
        _logger.LogInformation("Connector trigger endpoint: {Endpoint}", endpointPath);

        // Register the trigger binding with converters for various parameter types
        var bindingRule = context.AddBindingRule<ConnectorTriggerAttribute>();

        // Add converters to support different parameter types
        // JToken is our internal trigger value type - these converters transform it to what the function expects

        // ConnectorContext converter - provides full request context
        bindingRule.AddConverter<(ConnectorContext, JToken), ConnectorContext>(tuple => tuple.Item1);

        // String converter - provides raw body
        bindingRule.AddConverter<(ConnectorContext, JToken), string>(tuple => tuple.Item1.Body);

        // JToken/JObject/JArray converters - provides parsed JSON
        bindingRule.AddConverter<(ConnectorContext, JToken), JToken>(tuple => tuple.Item2);
        bindingRule.AddConverter<(ConnectorContext, JToken), JObject>(tuple => tuple.Item2 as JObject ?? new JObject());
        bindingRule.AddConverter<(ConnectorContext, JToken), JArray>(tuple => tuple.Item2 as JArray ?? new JArray());

        // Open converter for POCO types - deserializes JSON to any user-defined type
        bindingRule.AddOpenConverter<(ConnectorContext, JToken), OpenType.Poco>(typeof(ConnectorContextToPocoConverter<>));

        // Bind to trigger with our binding provider
        bindingRule.BindToTrigger(new ConnectorTriggerBindingProvider(this));
    }

    /// <summary>
    /// Handles incoming HTTP requests for connector-triggered functions.
    /// This is called by the Azure Functions host when a request arrives at the connector webhook endpoint.
    /// </summary>
    public async Task<HttpResponseMessage> ConvertAsync(
        HttpRequestMessage input,
        CancellationToken cancellationToken)
    {
        // Extract the function name from the query string
        var queryString = HttpUtility.ParseQueryString(input.RequestUri?.Query ?? string.Empty);
        string? functionName = queryString["functionName"];

        if (string.IsNullOrEmpty(functionName))
        {
            _logger.LogWarning("Connector request missing functionName query parameter");
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Missing required query parameter: functionName")
            };
        }

        // Validate functionName length and format
        if (functionName.Length > MaxFunctionNameLength)
        {
            _logger.LogWarning("functionName too long: {Length}", functionName.Length);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("functionName parameter too long")
            };
        }

        if (!FunctionNamePattern.IsMatch(functionName))
        {
            _logger.LogWarning("Invalid functionName format: {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid functionName format")
            };
        }

        if (!_listeners.TryGetValue(functionName, out ConnectorListener? listener))
        {
            _logger.LogWarning("No connector function found with name: {FunctionName}", functionName);
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Function not found")
            };
        }

        return await _httpRequestProcessor.ProcessAsync(
            input,
            functionName,
            listener.Executor,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Open converter that deserializes JSON to any POCO type.
    /// This enables users to use custom types as their function parameter.
    /// Uses secure serialization settings to prevent deserialization attacks.
    /// </summary>
    private sealed class ConnectorContextToPocoConverter<T> : IConverter<(ConnectorContext, JToken), T>
    {
        private static readonly JsonSerializerSettings SecureSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,  // Prevents type confusion attacks
            MaxDepth = 32,  // Prevent deep nesting DoS
            DateParseHandling = DateParseHandling.DateTime
        };

        public T Convert((ConnectorContext, JToken) input)
        {
            var (context, jsonBody) = input;
            var result = jsonBody.ToObject<T>(JsonSerializer.Create(SecureSettings));
            return result ?? throw new InvalidOperationException($"Failed to deserialize JSON body to type {typeof(T).Name}");
        }
    }
}
