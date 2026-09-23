// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Extension configuration provider for the Connector trigger.
/// </summary>
[Extension("Connector", "connector")]
internal sealed class ConnectorExtensionConfigProvider : IExtensionConfigProvider,
    IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    internal const string BindingDataVersion = "1.0";
    internal const string BindingDataSource = "AzureConnectorEvent";
    internal const string BindingDataContentType = "application/json";

    private static readonly Regex FunctionNamePattern = new(@"^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled);

    private readonly ILogger<ConnectorExtensionConfigProvider> _logger;
    private readonly ILogger _consoleLogger;
    private readonly ConnectorHttpRequestProcessor _httpRequestProcessor;
    private readonly ConnectorOptions _options;
    private readonly IConnectorConnectionOptionsProvider _connectionOptionsProvider;
    private readonly IConnectorPollingListenerFactory _pollingListenerFactory;
    private readonly ConcurrentDictionary<string, ConnectorFunctionRegistration> _functions = new(StringComparer.OrdinalIgnoreCase);

    public ConnectorExtensionConfigProvider(
        ConnectorHttpRequestProcessor httpRequestProcessor,
        ILoggerFactory loggerFactory,
        IOptions<ConnectorOptions> options,
        IConnectorConnectionOptionsProvider connectionOptionsProvider,
        IConnectorPollingListenerFactory pollingListenerFactory)
    {
        _httpRequestProcessor = httpRequestProcessor ?? throw new ArgumentNullException(nameof(httpRequestProcessor));
        _logger = loggerFactory?.CreateLogger<ConnectorExtensionConfigProvider>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _consoleLogger = loggerFactory?.CreateLogger("Host.Function.Console")
            ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionOptionsProvider = connectionOptionsProvider
            ?? throw new ArgumentNullException(nameof(connectionOptionsProvider));
        _pollingListenerFactory = pollingListenerFactory
            ?? throw new ArgumentNullException(nameof(pollingListenerFactory));
    }

    internal void RegisterFunction(ConnectorFunctionRegistration registration)
    {
        _functions[registration.FunctionName] = registration;
        _logger.LogDebug("Registered function {Function}", registration.FunctionName);
    }

    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

#pragma warning disable CS0618 // GetWebhookHandler remains the WebJobs webhook registration API.
        var webhookUrl = context.GetWebhookHandler();
#pragma warning restore CS0618

        var extensionUri = webhookUrl?.GetLeftPart(UriPartial.Path) ?? string.Empty;
        _consoleLogger.LogInformation("Connector endpoint: {uri}", extensionUri);

        var rule = context.AddBindingRule<ConnectorTriggerAttribute>();
        rule.BindToTrigger(new ConnectorTriggerBindingProvider(
                this,
                _options,
                _connectionOptionsProvider,
                _pollingListenerFactory));
    }

    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        var queryString = HttpUtility.ParseQueryString(input.RequestUri?.Query ?? string.Empty);
        string? functionName = queryString["functionName"];

        if (string.IsNullOrEmpty(functionName) || !FunctionNamePattern.IsMatch(functionName))
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid or missing functionName parameter")
            };
        }

        if (!_functions.TryGetValue(functionName, out var registration))
        {
            _logger.LogInformation("Function '{FunctionName}' not found, available: [{Available}]",
                functionName, string.Join(", ", _functions.Keys));
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent($"Function '{functionName}' not found")
            };
        }

        return await _httpRequestProcessor.ProcessAsync(
            input,
            functionName,
            ExecuteAsync,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> ExecuteAsync(string triggerValue, string functionName, CancellationToken cancellationToken)
    {
        if (!_functions.TryGetValue(functionName, out var registration))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var triggerData = new TriggeredFunctionData
        {
            TriggerValue = ConnectorTriggerInput.FromSingle(
                BinaryData.FromString(triggerValue),
                messageId: null,
                ConnectorTriggerDeliveryMode.Webhook),
        };
        var result = await registration.Executor.TryExecuteAsync(triggerData, cancellationToken).ConfigureAwait(false);

        if (result.Succeeded)
        {
            _logger.LogDebug("Function {FunctionName} executed successfully", functionName);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        _logger.LogError(result.Exception, "Function {FunctionName} failed", functionName);
        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(result.Exception?.Message ?? "Function execution failed")
        };
    }

    internal static ParameterBindingData ConvertTriggerEventToBindingData(
        ConnectorTriggerEventInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("deliveryMode", input.DeliveryMode.ToString());
            writer.WriteString("data", input.Outputs.ToString());
            if (input.MessageId is null)
            {
                writer.WriteNull("messageId");
            }
            else
            {
                writer.WriteString("messageId", input.MessageId);
            }

            writer.WriteEndObject();
        }

        return new ParameterBindingData(
            BindingDataVersion,
            BindingDataSource,
            BinaryData.FromBytes(stream.ToArray()),
            BindingDataContentType);
    }
}
