// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorConnectionOptionsProvider : IConnectorConnectionOptionsProvider
{
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
        ValidateIdentitySelectors(connectionSection, connectionName);
        TokenCredential credential =
            (componentFactory ?? _componentFactory)
            .CreateTokenCredential(connectionSection);

        return new ConnectorConnectionOptions(credential);
    }

    internal IConfigurationSection GetConnectionSection(string connectionName) =>
        _configuration.GetWebJobsConnectionSection(connectionName);

    internal static void ValidateIdentitySelectors(
        IConfigurationSection section,
        string connectionName)
    {
        bool hasCredential = !string.IsNullOrWhiteSpace(section["credential"]);
        bool hasClientId = !string.IsNullOrWhiteSpace(section["clientId"]);
        bool hasManagedIdentityResourceId =
            !string.IsNullOrWhiteSpace(section["managedIdentityResourceId"]);

        if (hasClientId && hasManagedIdentityResourceId)
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must specify only one managed identity selector.");
        }

        if ((hasClientId || hasManagedIdentityResourceId) &&
            !hasCredential)
        {
            throw new InvalidOperationException(
                $"Connector connection '{connectionName}' must configure credential when selecting a managed identity.");
        }
    }
}
