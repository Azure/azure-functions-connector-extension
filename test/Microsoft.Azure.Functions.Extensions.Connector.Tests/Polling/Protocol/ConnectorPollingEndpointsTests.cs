// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingEndpointsTests
{
    [Fact]
    public void Create_AppendsOnlyRuntimeOperationsAndPreservesOpaqueValues()
    {
        ConnectorPollingEndpoints endpoints = ConnectorPollingEndpoints.Create(
            "https://scale.region.logic.azure.com/api/connectorGateways/ns?opaque=a%2Fb",
            "name");

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

    [Fact]
    public void Create_AcceptsOpaqueHttpsRuntimeHost()
    {
        ConnectorPollingEndpoints endpoints =
            ConnectorPollingEndpoints.Create(
                "https://gateway.contoso.example/api/connectorGateways/gateway",
                "trigger-name");

        Assert.EndsWith(
            "/api/connectorGateways/gateway/triggerConfigs/trigger-name/receive",
            endpoints.ReceiveUri.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" http://app-12.region.logic.azure.com/api/connectorGateways/ns")]
    [InlineData("http://app-12.region.logic.azure.com/api/connectorGateways/ns")]
    [InlineData("https://user@app-12.region.logic.azure.com/api/connectorGateways/ns")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns#fragment")]
    [InlineData("/relative/trigger")]
    [InlineData("https://app-12.region.logic.azure.com:444/api/connectorGateways/ns")]
    [InlineData("https://app-12.region.logic.azure.com/connectorGateways/ns")]
    [InlineData("https://app-12.region.logic.azure.com/custom/api/connectorGateways/ns")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs")]
    [InlineData("https://app-12.region.logic.azure.com/api/connectorGateways/ns%2Fextra")]
    [InlineData("https://app-12.region.logic.azure.com/api/%7Eunit/ns")]
    [InlineData("https://app-12.region.logic.azure.com/api/extra/../connectorGateways/ns")]
    public void Create_RejectsInvalidPollingEndpoint(string? pollingEndpoint)
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConnectorPollingEndpoints.Create(
                    pollingEndpoint!,
                    "name"));

        Assert.Equal(
            "Connector connection pollingEndpoint must be a valid Connector Namespace polling base URL.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" name")]
    [InlineData("name ")]
    [InlineData("name/extra")]
    [InlineData("name\\extra")]
    [InlineData("name%2Fextra")]
    [InlineData("na%6De")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("name\nextra")]
    public void Create_RejectsInvalidTriggerConfigName(
        string? triggerConfigName)
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConnectorPollingEndpoints.Create(
                    "https://app-12.region.logic.azure.com/api/connectorGateways/ns",
                    triggerConfigName!));

        Assert.Equal(
            "Connector trigger TriggerConfigName must be a valid path segment.",
            exception.Message);
    }
}
