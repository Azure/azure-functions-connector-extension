// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector.Tests;

public class ConnectorTriggerAttributeTests
{
    [Fact]
    public void Constructor_UsesSingleCardinalityByDefault()
    {
        var attribute = new ConnectorTriggerAttribute();

        Assert.False(attribute.IsBatched);
        Assert.Equal(
            Cardinality.One,
            ((ISupportCardinality)attribute).Cardinality);
    }

    [Fact]
    public void IsBatched_UsesManyCardinality()
    {
        var attribute = new ConnectorTriggerAttribute
        {
            IsBatched = true,
        };

        Assert.Equal(
            Cardinality.Many,
            ((ISupportCardinality)attribute).Cardinality);
    }

    [Fact]
    public void Cardinality_UpdatesIsBatched()
    {
        var attribute = new ConnectorTriggerAttribute();
        var cardinality = (ISupportCardinality)attribute;

        cardinality.Cardinality = Cardinality.Many;
        Assert.True(attribute.IsBatched);

        cardinality.Cardinality = Cardinality.One;
        Assert.False(attribute.IsBatched);
    }
}
