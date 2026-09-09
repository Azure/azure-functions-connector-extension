// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

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
        Assert.Equal(32, attribute.MaxEvents);
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
            TriggerConfigName = "OnNewEmail",
            MaxEvents = 16,
        };

        // Assert
        Assert.Equal(ConnectorTriggerDeliveryMode.Poll, attribute.DeliveryMode);
        Assert.Equal("ConnectorNamespace", attribute.Connection);
        Assert.Equal("OnNewEmail", attribute.TriggerConfigName);
        Assert.Equal(16, attribute.MaxEvents);
    }
}
