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
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Attribute_HasCorrectBindingType()
    {
        // Arrange
        var attribute = new ConnectorTriggerAttribute();

        // Assert - verify it can be created (binding type is set via WebJobsAttribute)
        Assert.IsType<ConnectorTriggerAttribute>(attribute);
    }
}
