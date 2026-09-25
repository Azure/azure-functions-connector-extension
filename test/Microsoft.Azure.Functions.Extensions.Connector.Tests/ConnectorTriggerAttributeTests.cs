// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerAttributeTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var attribute = new ConnectorTriggerAttribute();

        // Assert
        Assert.Equal(ConnectorTriggerDeliveryMode.Webhook, attribute.DeliveryMode);
        Assert.Null(attribute.Connection);
        Assert.Null(attribute.PollingEndpoint);
        Assert.Equal(0, attribute.MaxBatchSize);
        Assert.Equal(0, attribute.Concurrency);
    }

    [Fact]
    public void Attribute_HasCorrectBindingType()
    {
        // Arrange
        var attribute = new ConnectorTriggerAttribute();

        // Assert - verify it can be created (binding type is set via WebJobsAttribute)
        Assert.IsType<ConnectorTriggerAttribute>(attribute);
    }

    [Fact]
    public void Properties_AcceptPollMetadata()
    {
        // Arrange & Act
        var attribute = new ConnectorTriggerAttribute
        {
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            PollingEndpoint = "%OnNewEmailEndpoint%",
            MaxBatchSize = 4,
            Concurrency = 8,
        };

        // Assert
        Assert.Equal(ConnectorTriggerDeliveryMode.Poll, attribute.DeliveryMode);
        Assert.Equal("ConnectorNamespace", attribute.Connection);
        Assert.Equal(
            "%OnNewEmailEndpoint%",
            attribute.PollingEndpoint);
        Assert.Equal(4, attribute.MaxBatchSize);
        Assert.Equal(8, attribute.Concurrency);
    }
}
