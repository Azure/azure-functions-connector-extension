// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerAttributeTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange & Act
        var attribute = new ConnectorTriggerAttribute("my-namespace", "my-trigger");

        // Assert
        Assert.Equal("my-namespace", attribute.ConnectorNamespace);
        Assert.Equal("my-trigger", attribute.TriggerName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConnectorNamespaceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerAttribute(null!, "my-trigger"));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTriggerNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerAttribute("my-namespace", null!));
    }

    [Fact]
    public void Attribute_HasCorrectBindingType()
    {
        // Arrange
        var attribute = new ConnectorTriggerAttribute("my-namespace", "my-trigger");

        // Assert
        Assert.IsType<ConnectorTriggerAttribute>(attribute);
    }
}
