// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorConnectionOptionsProvider : IConnectorConnectionOptionsProvider
{
    private const string ResourceIdPropertyName = "resourceId";
    private const string SubscriptionsSegment = "subscriptions";
    private const string ResourceGroupsSegment = "resourceGroups";
    private const string ProvidersSegment = "providers";
    private const string ConnectorNamespaceProvider = "Microsoft.Web";
    private const string ConnectorNamespaceResourceName = "connectorGateways";
    private const string ConnectorNamespaceResourceType =
        ConnectorNamespaceProvider + "/" + ConnectorNamespaceResourceName;
    private const int ExpectedSegmentCount = 8;

    private readonly IConfiguration _configuration;
    private readonly AzureComponentFactory _componentFactory;

    public ConnectorConnectionOptionsProvider(
        IConfiguration configuration,
        AzureComponentFactory componentFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
    }

    public ConnectorConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            throw new InvalidOperationException(
                "Connector trigger Connection is required for Poll delivery.");
        }

        IConfigurationSection connectionSection =
            GetConnectionSection(connectionName);
        string? resourceIdValue = connectionSection[ResourceIdPropertyName];

        if (string.IsNullOrWhiteSpace(resourceIdValue))
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must configure {ResourceIdPropertyName}.");
        }

        ResourceIdentifier resourceId = ParseResourceId(connectionName, resourceIdValue);
        ValidateIdentitySelectors(connectionSection, connectionName);
        TokenCredential credential =
            (componentFactory ?? _componentFactory)
            .CreateTokenCredential(connectionSection);

        return new ConnectorConnectionOptions(resourceId, credential);
    }

    internal IConfigurationSection GetConnectionSection(string connectionName) =>
        _configuration.GetWebJobsConnectionSection(connectionName);

    internal static void ValidateIdentitySelectors(
        IConfigurationSection section,
        string connectionName)
    {
        bool hasCredential = !string.IsNullOrWhiteSpace(section["credential"]);
        bool hasClientId = !string.IsNullOrWhiteSpace(section["clientId"]);
        bool hasResourceId =
            !string.IsNullOrWhiteSpace(section["managedIdentityResourceId"]);

        if (hasClientId && hasResourceId)
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must specify only one managed identity selector.");
        }

        if ((hasClientId || hasResourceId) && !hasCredential)
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must configure credential when selecting a managed identity.");
        }
    }

    internal static ResourceIdentifier ParseResourceId(
        string connectionName,
        string resourceIdValue)
    {
        if (!resourceIdValue.Equals(resourceIdValue.Trim(), StringComparison.Ordinal) ||
            !resourceIdValue.StartsWith("/", StringComparison.Ordinal) ||
            resourceIdValue.EndsWith("/", StringComparison.Ordinal) ||
            resourceIdValue.Count(character => character == '/') != ExpectedSegmentCount ||
            resourceIdValue.Contains('?', StringComparison.Ordinal) ||
            resourceIdValue.Contains('#', StringComparison.Ordinal))
        {
            throw InvalidResourceId(connectionName);
        }

        ResourceIdentifier resourceId;
        try
        {
            resourceId = new ResourceIdentifier(resourceIdValue);
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException)
        {
            throw InvalidResourceId(connectionName, exception);
        }

        string[] segments = resourceIdValue.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        bool hasExpectedShape =
            segments.Length == ExpectedSegmentCount &&
            segments[0].Equals(SubscriptionsSegment, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(segments[1], out _) &&
            segments[2].Equals(ResourceGroupsSegment, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(segments[3]) &&
            segments[4].Equals(ProvidersSegment, StringComparison.OrdinalIgnoreCase) &&
            segments[5].Equals(ConnectorNamespaceProvider, StringComparison.OrdinalIgnoreCase) &&
            segments[6].Equals(ConnectorNamespaceResourceName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(segments[7]) &&
            resourceId.ResourceType.ToString().Equals(
                ConnectorNamespaceResourceType,
                StringComparison.OrdinalIgnoreCase);

        if (!hasExpectedShape)
        {
            throw InvalidResourceId(connectionName);
        }

        return resourceId;
    }

    private static InvalidOperationException InvalidResourceId(
        string connectionName,
        Exception? innerException = null) =>
        new(
            $"Connector connection '{connectionName}' {ResourceIdPropertyName} must identify a resource-group-scoped {ConnectorNamespaceResourceType} resource with a valid subscription ID and no child path.",
            innerException);
}
