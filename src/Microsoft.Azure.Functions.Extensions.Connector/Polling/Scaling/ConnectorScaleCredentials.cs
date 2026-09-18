// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed record ConnectorScaleConnectionOptions(
    ResourceIdentifier ResourceId,
    TokenCredential Credential,
    bool HasDebugTokenOverride);

internal interface IConnectorScaleConnectionOptionsProvider
{
    ConnectorScaleConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null);
}

internal sealed class ConnectorScaleConnectionOptionsProvider(
    IConfiguration configuration,
    ConnectorConnectionOptionsProvider connectionOptionsProvider) :
    IConnectorScaleConnectionOptionsProvider
{
    public ConnectorScaleConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        IConfigurationSection section =
            connectionOptionsProvider.GetConnectionSection(connectionName);
        string? resourceIdValue = section["resourceId"];
        if (string.IsNullOrWhiteSpace(resourceIdValue))
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must define a non-empty resourceId.");
        }

        ResourceIdentifier resourceId =
            ConnectorConnectionOptionsProvider.ParseResourceId(
                connectionName,
                resourceIdValue);
        if (TryCreateDebugTokenCredential(
            configuration,
            connectionName,
            section,
            out TokenCredential? debugCredential))
        {
            return new ConnectorScaleConnectionOptions(
                resourceId,
                debugCredential!,
                HasDebugTokenOverride: true);
        }

        ConnectorConnectionOptions connection =
            connectionOptionsProvider.Get(connectionName, componentFactory);
        TokenCredential credential = connection.Credential;
        if (string.Equals(
            section["credential"],
            "managedidentity",
            StringComparison.OrdinalIgnoreCase))
        {
            credential = new ManagedIdentityFallbackCredential(
                credential,
                ManagedIdentityFallbackCredential.CreateDefaultAzureCredential);
        }

        return new ConnectorScaleConnectionOptions(
            connection.ResourceId,
            credential,
            HasDebugTokenOverride: false);
    }

    private static bool TryCreateDebugTokenCredential(
        IConfiguration configuration,
        string connectionName,
        IConfigurationSection section,
        out TokenCredential? credential)
    {
        string? token = GetDebugSetting(
            configuration,
            connectionName,
            section,
            "token");
        string? managementToken = GetDebugSetting(
            configuration,
            connectionName,
            section,
            "managementToken");
        string? apiHubToken = GetDebugSetting(
            configuration,
            connectionName,
            section,
            "apiHubToken");

        if (string.IsNullOrWhiteSpace(token) &&
            string.IsNullOrWhiteSpace(managementToken) &&
            string.IsNullOrWhiteSpace(apiHubToken))
        {
            credential = null;
            return false;
        }

        credential = new DebugBearerTokenCredential(
            token,
            managementToken,
            apiHubToken);
        return true;
    }

    private static string? GetDebugSetting(
        IConfiguration configuration,
        string connectionName,
        IConfigurationSection section,
        string key) =>
        section[key] ?? configuration[$"{connectionName}_{key}"];
}

internal sealed class DebugBearerTokenCredential(
    string? token,
    string? managementToken,
    string? apiHubToken) : TokenCredential
{
    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromMinutes(5);

    public override AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken) =>
        CreateAccessToken(SelectToken(requestContext));

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken) =>
        new(GetToken(requestContext, cancellationToken));

    private string SelectToken(TokenRequestContext requestContext)
    {
        foreach (string scope in requestContext.Scopes)
        {
            if (IsScope(scope, ConnectorPollingEndpointResolver.ArmScope) &&
                !string.IsNullOrWhiteSpace(managementToken))
            {
                return managementToken;
            }

            if (IsScope(scope, ConnectorQueueDepthClient.ApiHubScope) &&
                !string.IsNullOrWhiteSpace(apiHubToken))
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
        new(
            token,
            TryGetJwtExpiresOn(token) ??
                DateTimeOffset.UtcNow.Add(DefaultTokenLifetime));

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
            if (document.RootElement.TryGetProperty(
                    "exp",
                    out JsonElement expElement) &&
                expElement.TryGetInt64(out long exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp);
            }
        }
        catch (Exception exception) when (
            exception is FormatException or
                JsonException or
                ArgumentException)
        {
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(
            padded.Length + ((4 - (padded.Length % 4)) % 4),
            '=');
        return Convert.FromBase64String(padded);
    }
}

internal sealed class ManagedIdentityFallbackCredential(
    TokenCredential managedIdentityCredential,
    Func<TokenCredential> fallbackCredentialFactory) : TokenCredential
{
    internal static Func<TokenCredential> CreateDefaultAzureCredential { get; set; } =
        () => new DefaultAzureCredential();

    private readonly Lazy<TokenCredential> _fallbackCredential =
        new(fallbackCredentialFactory);

    public override AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return managedIdentityCredential.GetToken(
                requestContext,
                cancellationToken);
        }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            return _fallbackCredential.Value.GetToken(
                requestContext,
                cancellationToken);
        }
    }

    public override async ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            return await managedIdentityCredential
                .GetTokenAsync(requestContext, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            return await _fallbackCredential.Value
                .GetTokenAsync(requestContext, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static bool ShouldFallback(Exception exception) =>
        exception is CredentialUnavailableException or AuthenticationFailedException;
}
