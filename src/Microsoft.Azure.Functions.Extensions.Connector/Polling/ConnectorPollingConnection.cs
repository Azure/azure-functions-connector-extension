// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

    private static InvalidOperationException InvalidResourceId(
        string connectionName,
        Exception? innerException = null) =>
        new(
            $"Connector Poll connection '{connectionName}' must define a resource-group-scoped Microsoft.Web/connectorGateways resource ID with a valid subscription GUID.",
            innerException);
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
