// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorConnectionOptionsProviderTests
{
    private const string ResourceId =
        "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns";

    [Fact]
    public void Constructor_Throws_WhenConfigurationIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorConnectionOptionsProvider(
                null!,
                Mock.Of<AzureComponentFactory>()));
    }

    [Fact]
    public void Constructor_Throws_WhenComponentFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorConnectionOptionsProvider(
                new ConfigurationBuilder().Build(),
                null!));
    }

    [Fact]
    public void Get_ResolvesPrefixedConnectionAndCredential()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["AzureWebJobsConnectorNamespace:resourceId"] = ResourceId,
                ["AzureWebJobsConnectorNamespace:credential"] = "managedidentity",
                ["AzureWebJobsConnectorNamespace:clientId"] = "client-id",
                ["AzureWebJobsConnectorNamespace:managedIdentityResourceId"] = "identity-resource-id",
            });
        TokenCredential credential = Mock.Of<TokenCredential>();
        var componentFactory = new Mock<AzureComponentFactory>();
        IConfiguration? receivedConfiguration = null;
        componentFactory
            .Setup(factory => factory.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Callback<IConfiguration>(value => receivedConfiguration = value)
            .Returns(credential);
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        ConnectorConnectionOptions result = provider.Get("ConnectorNamespace");

        Assert.Equal(ResourceId, result.ResourceId.ToString());
        Assert.Same(credential, result.Credential);
        Assert.NotNull(receivedConfiguration);
        Assert.Equal(
            "AzureWebJobsConnectorNamespace",
            ((IConfigurationSection)receivedConfiguration).Path);
        Assert.Equal("managedidentity", receivedConfiguration["credential"]);
        Assert.Equal("client-id", receivedConfiguration["clientId"]);
        Assert.Equal(
            "identity-resource-id",
            receivedConfiguration["managedIdentityResourceId"]);
    }

    [Fact]
    public void Get_ResolvesUnprefixedConnection()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectorNamespace:resourceId"] = ResourceId,
            });
        var componentFactory = new Mock<AzureComponentFactory>();
        componentFactory
            .Setup(factory => factory.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns(Mock.Of<TokenCredential>());
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        ConnectorConnectionOptions result = provider.Get("ConnectorNamespace");

        Assert.Equal(ResourceId, result.ResourceId.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Get_Throws_WhenConnectionNameIsMissing(string? connectionName)
    {
        var provider = new ConnectorConnectionOptionsProvider(
            new ConfigurationBuilder().Build(),
            Mock.Of<AzureComponentFactory>());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.Get(connectionName!));

        Assert.Contains("Connection", exception.Message);
    }

    [Fact]
    public void Get_Throws_WhenResourceIdIsMissing()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectorNamespace:credential"] = "managedidentity",
            });
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            Mock.Of<AzureComponentFactory>());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.Get("ConnectorNamespace"));

        Assert.Contains("resourceId", exception.Message);
    }

    [Theory]
    [InlineData("https://management.azure.com/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns")]
    [InlineData("/subscriptions/not-a-guid/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns?api-version=1")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns#fragment")]
    [InlineData(" /subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns/")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups//rg/providers/Microsoft.Web/connectorGateways/ns")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/account")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns/triggerConfigs/config")]
    [InlineData("/subscriptions/11111111-1111-1111-1111-111111111111/providers/Microsoft.Web/connectorGateways/ns")]
    public void Get_Throws_WhenResourceIdIsInvalid(string resourceId)
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectorNamespace:resourceId"] = resourceId,
            });
        var componentFactory = new Mock<AzureComponentFactory>();
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.Get("ConnectorNamespace"));

        Assert.Contains("Microsoft.Web/connectorGateways", exception.Message);
        componentFactory.Verify(
            factory => factory.CreateTokenCredential(It.IsAny<IConfiguration>()),
            Times.Never);
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
