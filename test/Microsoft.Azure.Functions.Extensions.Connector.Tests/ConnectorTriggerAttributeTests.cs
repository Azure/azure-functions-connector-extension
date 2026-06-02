// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerAttributeTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var attribute = new ConnectorTriggerAttribute("MyConnectorNamespace", "OnNewEmail-config1");

        Assert.Equal("MyConnectorNamespace", attribute.ConnectorNamespace);
        Assert.Equal("OnNewEmail-config1", attribute.TriggerName);
    }

    [Fact]
    public void Attribute_HasCorrectBindingType()
    {
        var attribute = new ConnectorTriggerAttribute("ns", "trigger");

        Assert.IsType<ConnectorTriggerAttribute>(attribute);
    }

    [Theory]
    [InlineData(null, "trigger")]
    [InlineData("", "trigger")]
    [InlineData("   ", "trigger")]
    public void Constructor_RejectsInvalidConnectorNamespace(string? ns, string trigger)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ConnectorTriggerAttribute(ns!, trigger));
    }

    [Theory]
    [InlineData("ns", null)]
    [InlineData("ns", "")]
    [InlineData("ns", "   ")]
    public void Constructor_RejectsInvalidTriggerName(string ns, string? trigger)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ConnectorTriggerAttribute(ns, trigger!));
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        var type = typeof(ConnectorTriggerAttribute);
        var nsProperty = type.GetProperty(nameof(ConnectorTriggerAttribute.ConnectorNamespace));
        var triggerProperty = type.GetProperty(nameof(ConnectorTriggerAttribute.TriggerName));

        Assert.NotNull(nsProperty);
        Assert.NotNull(triggerProperty);
        Assert.False(nsProperty!.CanWrite, "ConnectorNamespace should be get-only");
        Assert.False(triggerProperty!.CanWrite, "TriggerName should be get-only");
    }

    

    [Fact]
    public void Concurrency_DefaultsToZero()
    {
        var attribute = new ConnectorTriggerAttribute("ns", "trigger");

        Assert.Equal(0, attribute.Concurrency);
    }

    [Fact]
    public void Concurrency_IsSettable()
    {
        var attribute = new ConnectorTriggerAttribute("ns", "trigger")
        {
            Concurrency = 32
        };

        Assert.Equal(32, attribute.Concurrency);
    }

    [Fact]
    public void Concurrency_Property_IsReadWrite()
    {        
        var property = typeof(ConnectorTriggerAttribute).GetProperty(
            nameof(ConnectorTriggerAttribute.Concurrency));

        Assert.NotNull(property);
        Assert.True(property!.CanRead);
        Assert.True(property.CanWrite);
    }
}
