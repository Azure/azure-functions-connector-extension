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
        Assert.Null(attribute.TriggerConfigName);
        Assert.Equal(0, attribute.MaxBatchSize);
        Assert.Equal(0, attribute.MaxConcurrentCalls);
        Assert.Equal(0, attribute.TargetPendingEventThreshold);
    }

    [Fact]
    public void Properties_AcceptPollMetadata()
    {
        // Arrange & Act
        var attribute = new ConnectorTriggerAttribute
        {
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            TriggerConfigName = "%OnNewEmailTriggerConfigName%",
            MaxBatchSize = 4,
            MaxConcurrentCalls = 8,
            TargetPendingEventThreshold = 12,
        };

        // Assert
        Assert.Equal(ConnectorTriggerDeliveryMode.Poll, attribute.DeliveryMode);
        Assert.Equal("ConnectorNamespace", attribute.Connection);
        Assert.Equal(
            "%OnNewEmailTriggerConfigName%",
            attribute.TriggerConfigName);
        Assert.Equal(4, attribute.MaxBatchSize);
        Assert.Equal(8, attribute.MaxConcurrentCalls);
        Assert.Equal(12, attribute.TargetPendingEventThreshold);
    }
}
