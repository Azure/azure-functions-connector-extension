// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorConnectionOptionsProviderTests
{
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
                ["AzureWebJobsConnectorNamespace:credential"] = "managedidentity",
                ["AzureWebJobsConnectorNamespace:clientId"] = "client-id",
                ["AzureWebJobsConnectorNamespace:pollingEndpoint"] =
                    "https://app-12.region.logic.azure.com/api/connectorGateways/ns",
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

        Assert.Same(credential, result.Credential);
        Assert.NotNull(receivedConfiguration);
        Assert.Equal(
            "AzureWebJobsConnectorNamespace",
            ((IConfigurationSection)receivedConfiguration).Path);
        Assert.Equal("managedidentity", receivedConfiguration["credential"]);
        Assert.Equal("client-id", receivedConfiguration["clientId"]);
        Assert.Equal(
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns",
            result.PollingEndpoint);
    }

    [Fact]
    public void Get_PassesServicePrincipalConfigurationToComponentFactory()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectorNamespace:tenantId"] = "tenant-id",
                ["ConnectorNamespace:clientId"] = "client-id",
                ["ConnectorNamespace:clientSecret"] = "client-secret",
                ["ConnectorNamespace:pollingEndpoint"] =
                    "https://app-12.region.logic.azure.com/api/connectorGateways/ns",
            });
        TokenCredential credential = Mock.Of<TokenCredential>();
        var componentFactory = new Mock<AzureComponentFactory>();
        IConfiguration? receivedConfiguration = null;
        componentFactory
            .Setup(factory =>
                factory.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Callback<IConfiguration>(
                value => receivedConfiguration = value)
            .Returns(credential);
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        ConnectorConnectionOptions result =
            provider.Get("ConnectorNamespace");

        Assert.Same(credential, result.Credential);
        Assert.NotNull(receivedConfiguration);
        Assert.Equal("tenant-id", receivedConfiguration["tenantId"]);
        Assert.Equal("client-id", receivedConfiguration["clientId"]);
        Assert.Equal("client-secret", receivedConfiguration["clientSecret"]);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Get_Throws_WhenPollingEndpointIsMissing(
        string? pollingEndpoint)
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectorNamespace:pollingEndpoint"] = pollingEndpoint,
            });
        var componentFactory = new Mock<AzureComponentFactory>();
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                provider.Get("ConnectorNamespace"));

        Assert.Contains("pollingEndpoint", exception.Message);
        Assert.Contains("ConnectorNamespace", exception.Message);
        componentFactory.Verify(
            factory => factory.CreateTokenCredential(
                It.IsAny<IConfiguration>()),
            Times.Never);
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
