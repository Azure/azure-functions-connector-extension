// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Extension configuration provider for the Connector trigger.
/// </summary>
[Extension("Connector", "connector")]
internal sealed class ConnectorExtensionConfigProvider : IExtensionConfigProvider,
    IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private static readonly Regex FunctionNamePattern = new(@"^[a-zA-Z0-9_-]{1,128}$", RegexOptions.Compiled);

    private readonly ILogger<ConnectorExtensionConfigProvider> _logger;
    private readonly ConnectorHttpRequestProcessor _httpRequestProcessor;
    private readonly ConcurrentDictionary<string, ConnectorFunctionRegistration> _functions = new(StringComparer.OrdinalIgnoreCase);

    public ConnectorExtensionConfigProvider(
        ConnectorHttpRequestProcessor httpRequestProcessor,
        ILoggerFactory loggerFactory)
    {
        _httpRequestProcessor = httpRequestProcessor ?? throw new ArgumentNullException(nameof(httpRequestProcessor));
        _logger = loggerFactory?.CreateLogger<ConnectorExtensionConfigProvider>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    internal void RegisterFunction(ConnectorFunctionRegistration registration)
    {
        _functions[registration.FunctionName] = registration;
        _logger.LogDebug("Registered function {Function}", registration.FunctionName);
    }

    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

#pragma warning disable 618
        var webhookUrl = context.GetWebhookHandler();
#pragma warning restore 618

        _logger.LogInformation("Connector endpoint: {Endpoint}", webhookUrl?.GetLeftPart(UriPartial.Path));

        context
            .AddBindingRule<ConnectorTriggerAttribute>()
            .BindToTrigger(new ConnectorTriggerBindingProvider(this));
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
            registration,
            (triggerValue, _, ct) => ExecuteAsync(triggerValue, registration, ct),
            cancellationToken);
    }

    private async Task<HttpResponseMessage> ExecuteAsync(string triggerValue, ConnectorFunctionRegistration registration, CancellationToken cancellationToken)
    {
        var triggerData = new TriggeredFunctionData { TriggerValue = triggerValue };
        var result = await registration.Executor.TryExecuteAsync(triggerData, cancellationToken).ConfigureAwait(false);

        if (result.Succeeded)
        {
            _logger.LogDebug("Function {FunctionName} executed successfully", registration.FunctionName);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        _logger.LogError(result.Exception, "Function {FunctionName} failed", registration.FunctionName);
        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(result.Exception?.Message ?? "Function execution failed")
        };
    }
}
