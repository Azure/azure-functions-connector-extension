// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingConnectionTests
{
    private const string ResourceId = "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway";

    [Fact]
    public void Create_UsesFullNamedSectionAndSelectedFactory()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:resourceId"] = ResourceId,
            ["ConnectorNamespace:credential"] = "managedidentity",
            ["ConnectorNamespace:clientId"] = "22222222-2222-2222-2222-222222222222",
        });
        var defaultFactory = new TestAzureComponentFactory(new TestTokenCredential("default"));
        var selectedCredential = new TestTokenCredential("selected");
        var selectedFactory = new TestAzureComponentFactory(selectedCredential);
        var factory = new ConnectorPollingConnectionFactory(configuration, defaultFactory);

        ConnectorPollingConnection connection = factory.Create("ConnectorNamespace", selectedFactory);

        Assert.Equal(ResourceId, connection.ResourceId.ToString());
        Assert.Same(selectedCredential, connection.Credential);
        IConfigurationSection selectedSection = Assert.IsAssignableFrom<IConfigurationSection>(selectedFactory.LastConfiguration!);
        Assert.Equal("ConnectorNamespace", selectedSection.Path);
        Assert.Equal("managedidentity", selectedSection["credential"]);
        Assert.Equal("22222222-2222-2222-2222-222222222222", selectedSection["clientId"]);
        Assert.Equal(0, defaultFactory.CreateCredentialCalls);
    }

    [Fact]
    public void Create_AllowsLocalDeveloperCredentialWhenCredentialMarkerIsOmitted()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:resourceId"] = ResourceId,
        });
        var defaultFactory = new TestAzureComponentFactory(new TestTokenCredential());

        ConnectorPollingConnection result = new ConnectorPollingConnectionFactory(configuration, defaultFactory)
            .Create("ConnectorNamespace");

        Assert.NotNull(result.Credential);
        Assert.Equal(1, defaultFactory.CreateCredentialCalls);
        Assert.Null(defaultFactory.LastConfiguration!["credential"]);
    }

    [Theory]
    [InlineData("https://management.azure.com/subscriptions/x")]
    [InlineData("/subscriptions/not-a-guid/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway/triggerConfigs/t")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway?x=1")]
    public void Create_RejectsInvalidResourceIds(string resourceId)
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:resourceId"] = resourceId,
        });

        Assert.Throws<InvalidOperationException>(() =>
            new ConnectorPollingConnectionFactory(configuration, new TestAzureComponentFactory(new TestTokenCredential()))
                .Create("ConnectorNamespace"));
    }

    [Fact]
    public void Create_RejectsSelectorsWithoutCredentialMarker()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:resourceId"] = ResourceId,
            ["ConnectorNamespace:clientId"] = "client-id",
        });

        Assert.Throws<InvalidOperationException>(() =>
            new ConnectorPollingConnectionFactory(configuration, new TestAzureComponentFactory(new TestTokenCredential()))
                .Create("ConnectorNamespace"));
    }

    [Fact]
    public void Create_RejectsBothManagedIdentitySelectors()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectorNamespace:resourceId"] = ResourceId,
            ["ConnectorNamespace:credential"] = "managedidentity",
            ["ConnectorNamespace:clientId"] = "client-id",
            ["ConnectorNamespace:managedIdentityResourceId"] = "/subscriptions/x/resourceGroups/y/providers/Microsoft.ManagedIdentity/userAssignedIdentities/z",
        });

        Assert.Throws<InvalidOperationException>(() =>
            new ConnectorPollingConnectionFactory(configuration, new TestAzureComponentFactory(new TestTokenCredential()))
                .Create("ConnectorNamespace"));
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
}
