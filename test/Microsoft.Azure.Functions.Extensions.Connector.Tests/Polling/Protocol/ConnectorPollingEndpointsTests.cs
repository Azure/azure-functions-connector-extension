// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingEndpointsTests
{
    [Theory]
    [InlineData("https://gateway.example", "https://gateway.example/api")]
    [InlineData("https://gateway.example/", "https://gateway.example/api")]
    [InlineData("https://gateway.example:443", "https://gateway.example/api")]
    [InlineData("https://gateway.example:8443", "https://gateway.example:8443/api")]
    [InlineData("https://gateway.example:8443/", "https://gateway.example:8443/api")]
    [InlineData("https://regional.example/api/connectorGateways/gateway", "https://regional.example/api/connectorGateways/gateway")]
    [InlineData("https://regional.example/api/connectorGateways/gateway/", "https://regional.example/api/connectorGateways/gateway")]
    [InlineData("https://regional.example:443/api/connectorGateways/gateway", "https://regional.example/api/connectorGateways/gateway")]
    [InlineData("https://regional.example:8443/api/connectorGateways/gateway", "https://regional.example:8443/api/connectorGateways/gateway")]
    [InlineData("https://regional.example:8443/api/connectorGateways/gateway/", "https://regional.example:8443/api/connectorGateways/gateway")]
    public void Create_BuildsAllOperations(
        string pollingEndpoint,
        string runtimeBase)
    {
        ConnectorPollingEndpoints endpoints = ConnectorPollingEndpoints.Create(
            pollingEndpoint,
            "trigger name");

        Assert.Equal(
            $"{runtimeBase}/triggerConfigs/trigger%20name/receive",
            endpoints.ReceiveUri.AbsoluteUri);
        Assert.Equal(
            $"{runtimeBase}/triggerConfigs/trigger%20name/acknowledge",
            endpoints.AcknowledgeUri.AbsoluteUri);
        Assert.Equal(
            $"{runtimeBase}/triggerConfigs/trigger%20name/approximateQueueDepth",
            endpoints.ApproximateQueueDepthUri.AbsoluteUri);

        Assert.Empty(endpoints.ReceiveUri.Query);
        Assert.Empty(endpoints.AcknowledgeUri.Query);
        Assert.Empty(endpoints.ApproximateQueueDepthUri.Query);
    }

    [Theory]
    [InlineData("https://gateway.example")]
    [InlineData("https://regional.example/api/connectorGateways/gateway")]
    public void Create_RejectsQueryStrings(string pollingEndpoint)
    {
        foreach (string query in new[]
        {
            "?",
            "?maxEvents=4",
            "?opaque=a%2Fb&version=1"
        })
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                ConnectorPollingEndpoints.Create(pollingEndpoint + query, "trigger"));

            Assert.Contains("without a query string", exception.Message);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("http://gateway.example")]
    [InlineData("http://gateway.example:8443")]
    [InlineData("https://user@gateway.example")]
    [InlineData("https://gateway.example#fragment")]
    [InlineData("https://gateway.example:65536")]
    [InlineData("https://gateway.example:not-a-port")]
    [InlineData("https://gateway.example/api")]
    [InlineData("https://gateway.example/api/triggerconfigs/trigger/receive")]
    [InlineData("https://regional.example/api/connectorGateways")]
    [InlineData("https://regional.example/api/connectorGateways/gateway/extra")]
    [InlineData("https://regional.example/api/connectorGateways/gateway%2Fextra")]
    [InlineData("https://regional.example/api/connectorGateways/..")]
    [InlineData("https://gateway.example/extra/..")]
    [InlineData("https://regional.example/api/extra/../connectorGateways/gateway")]
    [InlineData("https://regional.example/api/connectorGateways/%2e%2e")]
    [InlineData("https://regional.example/api/connectorGateways/gateway%5Cextra")]
    [InlineData("https://regional.example/api/connectorGateways/gateway//")]
    [InlineData("https://gateway.example//")]
    [InlineData("https://gateway.example\\")]
    [InlineData(" https://gateway.example")]
    public void Create_RejectsInvalidEndpoint(string? pollingEndpoint)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingEndpoints.Create(pollingEndpoint!, "trigger"));

        Assert.Contains("pollingEndpoint", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" name")]
    [InlineData("name/extra")]
    [InlineData("name\\extra")]
    [InlineData("name%2Fextra")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("name\nextra")]
    public void Create_RejectsInvalidTriggerConfigName(string? triggerConfigName)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingEndpoints.Create("https://gateway.example", triggerConfigName!));

        Assert.Contains("TriggerConfigName", exception.Message);
    }
}
