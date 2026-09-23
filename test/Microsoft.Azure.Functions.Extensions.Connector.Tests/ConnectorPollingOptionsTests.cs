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

        var result = ConnectorPollingOptions.Create(
            attribute,
            new ConnectorOptions(),
            isBatched: true);

        Assert.Equal("ConnectorNamespace", result.Connection);
        Assert.Equal("OnNewEmail", result.TriggerConfigName);
        Assert.Equal(4, result.MaxBatchSize);
        Assert.Equal(8, result.Concurrency);
        Assert.True(result.IsBatched);
    }

    [Fact]
    public void Create_UsesHostDefaults_WhenAttributeValuesAreZero()
    {
        var defaults = new ConnectorOptions
        {
            DefaultMaxBatchSize = 3,
            DefaultConcurrency = 7,
        };

        var result = ConnectorPollingOptions.Create(
            CreateValidAttribute(),
            defaults,
            isBatched: true);

        Assert.Equal(3, result.MaxBatchSize);
        Assert.Equal(7, result.Concurrency);
        Assert.True(result.IsBatched);
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

    [Fact]
    public void Create_Throws_WhenAttributeConcurrencyIsNegative()
    {
        ConnectorTriggerAttribute attribute = CreateValidAttribute();
        attribute.Concurrency = -1;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(attribute, new ConnectorOptions()));

        Assert.Contains("Concurrency", exception.Message);
    }

    [Fact]
    public void Create_Throws_WhenDefaultConcurrencyIsNotPositive()
    {
        var defaults = new ConnectorOptions { DefaultConcurrency = 0 };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConnectorPollingOptions.Create(CreateValidAttribute(), defaults));

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
    public void Create_Throws_WhenTriggerConfigNameIsMissing(string? triggerConfigName)
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
        TriggerConfigName = "OnNewEmail",
    };
}
