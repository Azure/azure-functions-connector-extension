// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed record ConnectorPollingConnection(
    string Name,
    ResourceIdentifier ResourceId,
    TokenCredential Credential);

internal interface IConnectorPollingConnectionFactory
{
    ConnectorPollingConnection Create(
        string connectionName,
        AzureComponentFactory? componentFactory = null);
}

internal sealed class ConnectorPollingConnectionFactory : IConnectorPollingConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly AzureComponentFactory _componentFactory;

    public ConnectorPollingConnectionFactory(
        IConfiguration configuration,
        AzureComponentFactory componentFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
    }

    public ConnectorPollingConnection Create(
        string connectionName,
        AzureComponentFactory? componentFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        IConfigurationSection section = _configuration.GetSection(connectionName);
        string? resourceIdValue = section["resourceId"];
        if (string.IsNullOrWhiteSpace(resourceIdValue))
        {
            throw new InvalidOperationException(
                $"Connector Poll connection '{connectionName}' must define '{connectionName}__resourceId'.");
        }

        ResourceIdentifier resourceId = ParseResourceId(resourceIdValue, connectionName);
        if (TryCreateDebugTokenCredential(connectionName, section, out TokenCredential? debugCredential))
        {
            return new ConnectorPollingConnection(connectionName, resourceId, debugCredential!);
        }

        ValidateIdentitySelectors(section, connectionName);
        TokenCredential credential = (componentFactory ?? _componentFactory)
            .CreateTokenCredential(section);
        if (string.Equals(section["credential"], "managedidentity", StringComparison.OrdinalIgnoreCase))
        {
            credential = new ManagedIdentityFallbackCredential(credential, ManagedIdentityFallbackCredential.CreateDefaultAzureCredential);
        }

        return new ConnectorPollingConnection(connectionName, resourceId, credential);
    }

    private static ResourceIdentifier ParseResourceId(string value, string connectionName)
    {
        if (value.Contains('?', StringComparison.Ordinal) ||
            value.Contains('#', StringComparison.Ordinal) ||
            Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw InvalidResourceId(connectionName);
        }

        ResourceIdentifier resourceId;
        try
        {
            resourceId = new ResourceIdentifier(value);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw InvalidResourceId(connectionName, exception);
        }

        string[] segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        bool valid = segments.Length == 8 &&
            segments[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParseExact(segments[1], "D", out _) &&
            segments[2].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(segments[3]) &&
            segments[4].Equals("providers", StringComparison.OrdinalIgnoreCase) &&
            segments[5].Equals("Microsoft.Web", StringComparison.OrdinalIgnoreCase) &&
            segments[6].Equals("connectorGateways", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(segments[7]);
        if (!valid)
        {
            throw InvalidResourceId(connectionName);
        }

        return resourceId;
    }

    private static void ValidateIdentitySelectors(
        IConfigurationSection section,
        string connectionName)
    {
        bool hasCredential = !string.IsNullOrWhiteSpace(section["credential"]);
        bool hasClientId = !string.IsNullOrWhiteSpace(section["clientId"]);
        bool hasResourceId = !string.IsNullOrWhiteSpace(section["managedIdentityResourceId"]);
        if (hasClientId && hasResourceId)
        {
            throw new InvalidOperationException(
                $"Connector Poll connection '{connectionName}' must specify only one managed identity selector.");
        }

        if ((hasClientId || hasResourceId) && !hasCredential)
        {
            throw new InvalidOperationException(
                $"Connector Poll connection '{connectionName}' must define '{connectionName}__credential' when selecting a managed identity.");
        }
    }

    private bool TryCreateDebugTokenCredential(
        string connectionName,
        IConfigurationSection section,
        out TokenCredential? credential)
    {
        string? token = GetDebugSetting(connectionName, section, "token");
        string? managementToken = GetDebugSetting(connectionName, section, "managementToken");
        string? apiHubToken = GetDebugSetting(connectionName, section, "apiHubToken");
        if (string.IsNullOrWhiteSpace(token) &&
            string.IsNullOrWhiteSpace(managementToken) &&
            string.IsNullOrWhiteSpace(apiHubToken))
        {
            credential = null;
            return false;
        }

        credential = new DebugBearerTokenCredential(token, managementToken, apiHubToken);
        return true;
    }

    private string? GetDebugSetting(
        string connectionName,
        IConfigurationSection section,
        string key) =>
        section[key] ?? _configuration[$"{connectionName}_{key}"];

    private static InvalidOperationException InvalidResourceId(
        string connectionName,
        Exception? innerException = null) =>
        new(
            $"Connector Poll connection '{connectionName}' must define a resource-group-scoped Microsoft.Web/connectorGateways resource ID with a valid subscription GUID.",
            innerException);
}

internal sealed class DebugBearerTokenCredential(
    string? token,
    string? managementToken,
    string? apiHubToken) : TokenCredential
{
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(5);

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        CreateAccessToken(SelectToken(requestContext));

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new(GetToken(requestContext, cancellationToken));

    private string SelectToken(TokenRequestContext requestContext)
    {
        foreach (string scope in requestContext.Scopes)
        {
            if (IsScope(scope, ConnectorPollingEndpointResolver.ArmScope) && !string.IsNullOrWhiteSpace(managementToken))
            {
                return managementToken;
            }

            if (IsScope(scope, ConnectorQueueDepthClient.ApiHubScope) && !string.IsNullOrWhiteSpace(apiHubToken))
            {
                return apiHubToken;
            }
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        throw new CredentialUnavailableException(
            "Connector debug token settings did not include a token for the requested audience.");
    }

    private static bool IsScope(string scope, string expectedScope) =>
        string.Equals(scope, expectedScope, StringComparison.OrdinalIgnoreCase);

    private static AccessToken CreateAccessToken(string token) =>
        new(token, TryGetJwtExpiresOn(token) ?? DateTimeOffset.UtcNow.Add(DefaultTokenLifetime));

    private static DateTimeOffset? TryGetJwtExpiresOn(string token)
    {
        string[] parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            byte[] payload = Base64UrlDecode(parts[1]);
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("exp", out JsonElement expElement) &&
                expElement.TryGetInt64(out long exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp);
            }
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentException)
        {
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}

internal sealed class ManagedIdentityFallbackCredential(
    TokenCredential managedIdentityCredential,
    Func<TokenCredential> fallbackCredentialFactory) : TokenCredential
{
    internal static Func<TokenCredential> CreateDefaultAzureCredential { get; set; } = () => new DefaultAzureCredential();

    private readonly Lazy<TokenCredential> _fallbackCredential = new(fallbackCredentialFactory);

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        try
        {
            return managedIdentityCredential.GetToken(requestContext, cancellationToken);
        }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            return _fallbackCredential.Value.GetToken(requestContext, cancellationToken);
        }
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        try
        {
            return await managedIdentityCredential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            return await _fallbackCredential.Value.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ShouldFallback(Exception exception) =>
        exception is CredentialUnavailableException or AuthenticationFailedException;
}
