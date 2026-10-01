// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorConnectionOptionsProviderScalingTests
{
    [Fact]
    public void Get_UsesFullNamedSectionAndSelectedFactory()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:credential"] = "managedidentity",
            ["ConnectorNamespace:clientId"] = "22222222-2222-2222-2222-222222222222",
        });
        var defaultFactory = new TestAzureComponentFactory(new TestTokenCredential("default"));
        var selectedCredential = new TestTokenCredential("selected");
        var selectedFactory = new TestAzureComponentFactory(selectedCredential);
        ConnectorConnectionOptionsProvider provider =
            CreateProvider(configuration, defaultFactory);

        ConnectorConnectionOptions connection =
            provider.Get("ConnectorNamespace", selectedFactory);

        Assert.Same(selectedCredential, connection.Credential);
        IConfigurationSection selectedSection = Assert.IsAssignableFrom<IConfigurationSection>(selectedFactory.LastConfiguration!);
        Assert.Equal("ConnectorNamespace", selectedSection.Path);
        Assert.Equal("managedidentity", selectedSection["credential"]);
        Assert.Equal("22222222-2222-2222-2222-222222222222", selectedSection["clientId"]);
        Assert.Equal(0, defaultFactory.CreateCredentialCalls);
    }

    [Fact]
    public void Get_AllowsLocalDeveloperCredentialWhenCredentialMarkerIsOmitted()
    {
        IConfiguration configuration =
            BuildConfiguration(new Dictionary<string, string?>());
        var defaultFactory = new TestAzureComponentFactory(new TestTokenCredential());

        ConnectorConnectionOptions result =
            CreateProvider(configuration, defaultFactory)
            .Get("ConnectorNamespace");

        Assert.NotNull(result.Credential);
        Assert.Equal(1, defaultFactory.CreateCredentialCalls);
        Assert.Null(defaultFactory.LastConfiguration!["credential"]);
    }

    [Fact]
    public void Get_DelegatesSelectorsWithoutCredentialMarker()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:clientId"] = "client-id",
        });
        var credential = new TestTokenCredential();
        var componentFactory = new TestAzureComponentFactory(credential);

        ConnectorConnectionOptions connection =
            CreateProvider(configuration, componentFactory)
                .Get("ConnectorNamespace");

        Assert.Same(credential, connection.Credential);
        Assert.Equal("client-id", componentFactory.LastConfiguration!["clientId"]);
    }

    [Fact]
    public void Get_DelegatesBothManagedIdentitySelectors()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:credential"] = "managedidentity",
            ["ConnectorNamespace:clientId"] = "client-id",
            ["ConnectorNamespace:managedIdentityResourceId"] = "/subscriptions/x/resourceGroups/y/providers/Microsoft.ManagedIdentity/userAssignedIdentities/z",
        });
        var credential = new TestTokenCredential();
        var componentFactory = new TestAzureComponentFactory(credential);

        ConnectorConnectionOptions connection =
            CreateProvider(configuration, componentFactory)
                .Get("ConnectorNamespace");

        Assert.NotNull(connection.Credential);
        Assert.Equal(
            "managedidentity",
            componentFactory.LastConfiguration!["credential"]);
        Assert.Equal(
            "client-id",
            componentFactory.LastConfiguration["clientId"]);
        Assert.NotNull(
            componentFactory.LastConfiguration["managedIdentityResourceId"]);
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> settings)
    {
        var values = new Dictionary<string, string?>(settings)
        {
            ["ConnectorNamespace:pollingEndpoint"] =
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns",
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ConnectorConnectionOptionsProvider CreateProvider(
        IConfiguration configuration,
        TestAzureComponentFactory componentFactory) =>
        new(configuration, componentFactory);
}
