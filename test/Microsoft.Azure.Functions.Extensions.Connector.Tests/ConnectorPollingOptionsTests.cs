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
            Connection = "ConnectorNamespace",
            TriggerConfigName = "OnNewEmail",
            MaxBatchSize = 4,
            Concurrency = 8,
        };

        var result = ConnectorPollingOptions.Create(attribute, new ConnectorOptions());

        Assert.Equal("ConnectorNamespace", result.Connection);
        Assert.Equal("OnNewEmail", result.TriggerConfigName);
        Assert.Equal(4, result.MaxBatchSize);
        Assert.Equal(8, result.Concurrency);
    }

    [Fact]
    public void Create_UsesHostDefaults_WhenAttributeValuesAreZero()
    {
        var defaults = new ConnectorOptions
        {
            DefaultMaxBatchSize = 3,
            DefaultConcurrency = 7,
        };

        var result = ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), defaults);

        Assert.Equal(3, result.MaxBatchSize);
        Assert.Equal(7, result.Concurrency);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void Create_Throws_WhenAttributeMaxBatchSizeIsOutOfRange(int maxBatchSize)
    {
        var attribute = new ConnectorTriggerAttribute { MaxBatchSize = maxBatchSize };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("MaxBatchSize", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Create_Throws_WhenDefaultMaxBatchSizeIsOutOfRange(int maxBatchSize)
    {
        var defaults = new ConnectorOptions { DefaultMaxBatchSize = maxBatchSize };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(new ConnectorTriggerAttribute(), defaults));

        Assert.Contains("DefaultMaxBatchSize", exception.Message);
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
