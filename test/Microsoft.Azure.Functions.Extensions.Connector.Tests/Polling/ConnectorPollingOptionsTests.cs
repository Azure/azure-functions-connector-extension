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
            TriggerConfigName = "%OnNewEmailTriggerConfigName%",
            MaxBatchSize = 1,
            MaxConcurrentCalls = 8,
        };

        var result = ConnectorPollingOptions.Create(
            attribute,
            new ConnectorOptions(),
            isBatched: false);

        Assert.Equal("ConnectorNamespace", result.Connection);
        Assert.Equal(
            "%OnNewEmailTriggerConfigName%",
            result.TriggerConfigName);
        Assert.Equal(1, result.MaxBatchSize);
        Assert.Equal(8, result.MaxConcurrentCalls);
        Assert.Equal(TimeSpan.FromSeconds(30), result.MaxPollingInterval);
        Assert.False(result.IsBatched);
    }

    [Fact]
    public void Create_UsesHostDefaults_WhenAttributeValuesAreZero()
    {
        var defaults = new ConnectorOptions
        {
            DefaultMaxBatchSize = 1,
            DefaultMaxConcurrentCalls = 7,
            MaxPollingInterval = TimeSpan.FromSeconds(45),
        };

        var result = ConnectorPollingOptions.Create(
            CreateValidAttribute(),
            defaults,
            isBatched: false);

        Assert.Equal(1, result.MaxBatchSize);
        Assert.Equal(7, result.MaxConcurrentCalls);
        Assert.Equal(TimeSpan.FromSeconds(45), result.MaxPollingInterval);
        Assert.False(result.IsBatched);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void Create_BatchedDeliveryIgnoresHostConcurrency(
        int defaultMaxConcurrentCalls)
    {
        var defaults = new ConnectorOptions
        {
            DefaultMaxBatchSize = 3,
            DefaultMaxConcurrentCalls = defaultMaxConcurrentCalls,
        };

        var result = ConnectorPollingOptions.Create(
            CreateValidAttribute(), defaults, isBatched: true);

        Assert.Equal(3, result.MaxBatchSize);
        Assert.Equal(0, result.MaxConcurrentCalls);
        Assert.True(result.IsBatched);
    }

    [Fact]
    public void Create_BatchedDeliveryUsesBatchSizeOverride()
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.MaxBatchSize = 4;

        var result = ConnectorPollingOptions.Create(
            attribute, new ConnectorOptions(), isBatched: true);

        Assert.Equal(4, result.MaxBatchSize);
        Assert.Equal(0, result.MaxConcurrentCalls);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 16)]
    [InlineData(4, 1)]
    [InlineData(4, 16)]
    public void Create_BatchedBindingPreservesIgnoredMaxConcurrentCallsForWarning(
        int maxBatchSize, int maxConcurrentCalls)
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.MaxBatchSize = maxBatchSize;
        attribute.MaxConcurrentCalls = maxConcurrentCalls;

        var result = ConnectorPollingOptions.Create(
            attribute, new ConnectorOptions(), isBatched: true);

        Assert.Equal(maxConcurrentCalls, result.MaxConcurrentCalls);
        Assert.True(result.IsBatched);
    }

    [Fact]
    public void Create_SingleDeliveryUsesBuiltInConcurrencyDefault()
    {
        var result = ConnectorPollingOptions.Create(
            CreateValidAttribute(), new ConnectorOptions());

        Assert.Equal(16, result.MaxConcurrentCalls);
    }

    [Fact]
    public void Create_Throws_WhenCardinalityOneUsesBatchSizeGreaterThanOne()
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.MaxBatchSize = 2;

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() =>
                ConnectorPollingOptions.Create(
                    attribute,
                    new ConnectorOptions(),
                    isBatched: false));

        Assert.Contains("cardinality", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void Create_Throws_WhenAttributeMaxBatchSizeIsOutOfRange(int maxBatchSize)
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.MaxBatchSize = maxBatchSize;

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
            ConnectorPollingOptions.Create(CreateValidAttribute(), defaults));

        Assert.Contains("DefaultMaxBatchSize", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_Throws_WhenAttributeMaxConcurrentCallsIsNegative(bool isBatched)
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.MaxConcurrentCalls = -1;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions(), isBatched));

        Assert.Contains("MaxConcurrentCalls", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenDefaultMaxConcurrentCallsIsNotPositive()
    {
        var defaults = new ConnectorOptions { DefaultMaxConcurrentCalls = 0 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(CreateValidAttribute(), defaults));

        Assert.Contains("DefaultMaxConcurrentCalls", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999)]
    public void Create_Throws_WhenMaxPollingIntervalIsLessThanOneSecond(
        int milliseconds)
    {
        var defaults = new ConnectorOptions
        {
            MaxPollingInterval =
                TimeSpan.FromMilliseconds(milliseconds),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(CreateValidAttribute(), defaults));

        Assert.Contains("MaxPollingInterval", exception.Message);
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
            ConnectorPollingOptions.Create(CreateValidAttribute(), null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_Throws_WhenConnectionIsMissing(string? connection)
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.Connection = connection;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("Connection", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_Throws_WhenTriggerConfigNameIsMissing(
        string? triggerConfigName)
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.TriggerConfigName = triggerConfigName;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("TriggerConfigName", exception.Message);
    }

    private static ConnectorTriggerAttribute CreateValidAttribute() => new()
    {
        Connection = "ConnectorNamespace",
        TriggerConfigName = "%OnNewEmailTriggerConfigName%",
    };
}
