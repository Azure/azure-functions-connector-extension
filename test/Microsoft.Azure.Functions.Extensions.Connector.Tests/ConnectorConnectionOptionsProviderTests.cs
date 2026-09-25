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
    }

    [Fact]
    public void Get_ResolvesUnprefixedConnectionCredential()
    {
        IConfiguration configuration = BuildConfiguration(
            new Dictionary<string, string?>());
        var componentFactory = new Mock<AzureComponentFactory>();
        componentFactory
            .Setup(factory => factory.CreateTokenCredential(It.IsAny<IConfiguration>()))
            .Returns(Mock.Of<TokenCredential>());
        var provider = new ConnectorConnectionOptionsProvider(
            configuration,
            componentFactory.Object);

        ConnectorConnectionOptions result =
            provider.Get("ConnectorNamespace");

        Assert.NotNull(result.Credential);
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

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
