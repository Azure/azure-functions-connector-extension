// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingOptionsTests
{
    [Fact]
    public void Create_UsesAttributeOverrides()
    {
        var attribute = new ConnectorTriggerAttribute
        {
            Connection = "ConnectorGateway",
            TriggerConfigName = "OnNewEmail",
            BatchSize = 4,
            Concurrency = 8,
        };

        var result = ConnectorPollingOptions.Create(attribute, new ConnectorOptions());

        Assert.Equal("ConnectorGateway", result.Connection);
        Assert.Equal("OnNewEmail", result.TriggerConfigName);
        Assert.Equal(4, result.BatchSize);
        Assert.Equal(8, result.Concurrency);
    }

    [Fact]
    public void Create_UsesHostDefaults_WhenAttributeValuesAreZero()
    {
        var defaults = new ConnectorOptions
        {
            DefaultBatchSize = 3,
            DefaultConcurrency = 7,
        };

        var result = ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), defaults);

        Assert.Equal(3, result.BatchSize);
        Assert.Equal(7, result.Concurrency);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void Create_Throws_WhenAttributeBatchSizeIsOutOfRange(int batchSize)
    {
        var attribute = new ConnectorTriggerAttribute { BatchSize = batchSize };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("BatchSize", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Create_Throws_WhenDefaultBatchSizeIsOutOfRange(int batchSize)
    {
        var defaults = new ConnectorOptions { DefaultBatchSize = batchSize };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), defaults));

        Assert.Contains("DefaultBatchSize", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenAttributeConcurrencyIsNegative()
    {
        var attribute = new ConnectorTriggerAttribute { Concurrency = -1 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("Concurrency", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenDefaultConcurrencyIsNotPositive()
    {
        var defaults = new ConnectorOptions { DefaultConcurrency = 0 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), defaults));

        Assert.Contains("DefaultConcurrency", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenAttributeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConnectorPollingOptions.Create(null!, new ConnectorOptions()));
    }

    [Fact]
    public void Create_Throws_WhenDefaultsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), null!));
    }
}
