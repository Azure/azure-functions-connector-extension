// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingEndpointsTests
{
    [Fact]
    public void Create_AppendsOnlyRuntimeOperationsAndPreservesOpaqueValues()
    {
        ConnectorPollingEndpoints endpoints = ConnectorPollingEndpoints.Create(
            "https://scale.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name?opaque=a%2Fb");

        Assert.Equal(
            "https://scale.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/receive?opaque=a%2Fb",
            endpoints.ReceiveUri.ToString());
        Assert.Equal(
            "https://scale.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/acknowledge?opaque=a%2Fb",
            endpoints.AcknowledgeUri.ToString());
        Assert.Equal(
            "https://scale.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/approximateQueueDepth?opaque=a%2Fb",
            endpoints.ApproximateQueueDepthUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" http://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("http://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://user@app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name#fragment")]
    [InlineData("/relative/trigger")]
    [InlineData("https://evil.example/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://evil.logic.azure.com.evil/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://logic.azure.com/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com:444/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/custom/api/connectorGateways/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/receive")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/acknowledge")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/approximateQueueDepth")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name/extra")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/name%2Fextra")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns%2Fextra/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/api/%7Eunit/ns/triggerConfigs/name")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/na%6De")]
    [InlineData("https://app-12.region.logic.azure.com/api/extra/../connectorGateways/ns/triggerConfigs/name")]
    public void Create_RejectsInvalidPollingEndpoint(string? pollingEndpoint)
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConnectorPollingEndpoints.Create(pollingEndpoint!));

        Assert.Equal(
            "Connector trigger PollingEndpoint must be a valid Trigger Config polling base URL.",
            exception.Message);
    }
}
