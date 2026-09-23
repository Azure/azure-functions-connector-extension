// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed record ConnectorScaleConnectionOptions(
    ResourceIdentifier ResourceId,
    TokenCredential Credential);

internal interface IConnectorScaleConnectionOptionsProvider
{
    ConnectorScaleConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null);
}

internal sealed class ConnectorScaleConnectionOptionsProvider(
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
            credential);
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
