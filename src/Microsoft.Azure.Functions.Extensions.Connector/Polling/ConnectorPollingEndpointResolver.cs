// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorPollingEndpointResolver
{
    Task<ConnectorPollingEndpoints> ResolveAsync(CancellationToken cancellationToken = default);

    Task<ConnectorPollingEndpoints> RefreshAsync(CancellationToken cancellationToken = default);
}

internal interface IConnectorPollingEndpointResolverFactory
{
    IConnectorPollingEndpointResolver Create(
        ConnectorPollingConnection connection,
        TokenCredential credential,
        string triggerConfigName);
}

internal sealed class ConnectorPollingEndpointResolverFactory : IConnectorPollingEndpointResolverFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ConnectorPollingEndpointResolverFactory(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IConnectorPollingEndpointResolver Create(
        ConnectorPollingConnection connection,
        TokenCredential credential,
        string triggerConfigName) =>
        new ConnectorPollingEndpointResolver(
            connection,
            credential,
            triggerConfigName,
            _httpClientFactory,
            _loggerFactory.CreateLogger<ConnectorPollingEndpointResolver>());
}

internal sealed class ConnectorPollingEndpointResolver : IConnectorPollingEndpointResolver
{
    internal const string HttpClientName = "ConnectorPollingArm";
    internal const string ArmScope = "https://management.azure.com/.default";
    internal const string ApiVersion = "2026-05-01-preview";

    private readonly ConnectorPollingConnection _connection;
    private readonly TokenCredential _credential;
    private readonly string _triggerConfigName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly object _syncLock = new();
    private Task<ConnectorPollingEndpoints>? _resolution;
    private Task<ConnectorPollingEndpoints>? _refreshResolution;

    public ConnectorPollingEndpointResolver(
        ConnectorPollingConnection connection,
        TokenCredential credential,
        string triggerConfigName,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerConfigName);
        _triggerConfigName = triggerConfigName;
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ConnectorPollingEndpoints> ResolveAsync(
        CancellationToken cancellationToken = default) =>
        GetOrCreateResolution().WaitAsync(cancellationToken);

    public Task<ConnectorPollingEndpoints> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        Task<ConnectorPollingEndpoints> refreshResolution;
        bool refreshStarted = false;
        lock (_syncLock)
        {
            if (_refreshResolution is null)
            {
                refreshResolution = CreateResolution();
                _refreshResolution = refreshResolution;
                refreshStarted = true;
            }
            else
            {
                refreshResolution = _refreshResolution;
            }
        }

        if (refreshStarted)
        {
            _logger.LogInformation(
                "Refreshing Connector Poll endpoints for trigger configuration {TriggerConfigName}.",
                _triggerConfigName);
        }

        return refreshResolution.WaitAsync(cancellationToken);
    }

    private Task<ConnectorPollingEndpoints> GetOrCreateResolution()
    {
        lock (_syncLock)
        {
            return _resolution ?? CreateResolution();
        }
    }

    private Task<ConnectorPollingEndpoints> CreateResolution()
    {
        Task<ConnectorPollingEndpoints> coreResolution = ResolveCoreAsync(CancellationToken.None);
        var completion = new TaskCompletionSource<ConnectorPollingEndpoints>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ConnectorPollingEndpoints> resolution = completion.Task;
        _resolution = resolution;
        _ = CompleteResolutionAsync(coreResolution, resolution, completion);
        return resolution;
    }

    private async Task CompleteResolutionAsync(
        Task<ConnectorPollingEndpoints> coreResolution,
        Task<ConnectorPollingEndpoints> resolution,
        TaskCompletionSource<ConnectorPollingEndpoints> completion)
    {
        try
        {
            ConnectorPollingEndpoints endpoints = await coreResolution.ConfigureAwait(false);
            completion.SetResult(endpoints);
        }
        catch (OperationCanceledException exception) when (coreResolution.IsCanceled)
        {
            completion.SetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            lock (_syncLock)
            {
                if (ReferenceEquals(_resolution, resolution))
                {
                    _resolution = null;
                }
            }

            completion.SetException(exception);
        }
    }

    private async Task<ConnectorPollingEndpoints> ResolveCoreAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext([ArmScope]),
                cancellationToken).ConfigureAwait(false);
            string requestUri =
                $"https://management.azure.com{_connection.ResourceId.ToString().TrimEnd('/')}/triggerConfigs/" +
                $"{Uri.EscapeDataString(_triggerConfigName)}?api-version={ApiVersion}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ConnectorPollingEndpointResolutionException(
                    $"ARM endpoint resolution failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                    response.StatusCode);
            }

            await using Stream content = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(
                content,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return ParseResponse(document.RootElement);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to resolve Connector Poll endpoints for connection {Connection} and trigger configuration {TriggerConfigName}.",
                _connection.Name,
                _triggerConfigName);
            throw;
        }
    }

    private static ConnectorPollingEndpoints ParseResponse(JsonElement root)
    {
        if (!root.TryGetProperty("properties", out JsonElement properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            throw InvalidResponse("The ARM response did not contain a properties object.");
        }

        if (!string.Equals(GetString(properties, "state"), "Enabled", StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidResponse("The Connector trigger configuration must be enabled.");
        }

        if (!string.Equals(GetString(properties, "deliveryMode"), "Poll", StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidResponse("The Connector trigger configuration deliveryMode must be Poll.");
        }

        if (!properties.TryGetProperty("pollingEndpoints", out JsonElement endpoints) ||
            endpoints.ValueKind != JsonValueKind.Object)
        {
            throw InvalidResponse("The ARM response did not contain pollingEndpoints.");
        }

        return new ConnectorPollingEndpoints(
            GetHttpsEndpoint(endpoints, "receiveUri"),
            GetHttpsEndpoint(endpoints, "acknowledgeUri"),
            GetHttpsEndpoint(endpoints, "hasMessagesUri"),
            GetHttpsEndpoint(endpoints, "approximateQueueDepthUri"));
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Uri GetHttpsEndpoint(JsonElement endpoints, string propertyName)
    {
        string? value = GetString(endpoints, propertyName);
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidResponse($"Polling endpoint '{propertyName}' must be an absolute HTTPS URI.");
        }

        return uri;
    }

    private static ConnectorPollingEndpointResolutionException InvalidResponse(string message) => new(message);
}

internal sealed class ConnectorPollingEndpointResolutionException : Exception
{
    public ConnectorPollingEndpointResolutionException(
        string message,
        System.Net.HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;

    public System.Net.HttpStatusCode? StatusCode { get; }
}
