// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTargetScalerTests
{
    [Theory]
    [InlineData(0, 16, 0)]
    [InlineData(1, 16, 1)]
    [InlineData(16, 16, 1)]
    [InlineData(17, 16, 2)]
    [InlineData(50, 16, 4)]
    public async Task GetScaleResultAsync_UsesCeilingDepthOverTargetPendingEventThreshold(int depth, int targetPendingEventThreshold, int expected)
    {
        ConnectorTargetScaler scaler = CreateScaler(new SequenceDepthClient(depth), targetPendingEventThreshold, new ConnectorOptions { DefaultTargetPendingEventThreshold = 99 });

        TargetScalerResult result = await scaler.GetScaleResultAsync(new TargetScalerContext());

        Assert.Equal(expected, result.TargetWorkerCount);
    }

    [Fact]
    public async Task GetScaleResultAsync_InstanceConcurrencyTakesPrecedence()
    {
        ConnectorTargetScaler scaler = CreateScaler(
            new SequenceDepthClient(17),
            8,
            new ConnectorOptions { DefaultTargetPendingEventThreshold = 16 });

        TargetScalerResult result = await scaler.GetScaleResultAsync(
            new TargetScalerContext { InstanceConcurrency = 4 });

        Assert.Equal(5, result.TargetWorkerCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetScaleResultAsync_ThrowsWhenInstanceConcurrencyIsNotPositive(
        int instanceConcurrency)
    {
        ConnectorTargetScaler scaler = CreateScaler(
            new SequenceDepthClient(17),
            8,
            new ConnectorOptions());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            scaler.GetScaleResultAsync(
                new TargetScalerContext
                {
                    InstanceConcurrency = instanceConcurrency,
                }));
    }
    [Fact]
    public async Task GetScaleResultAsync_UsesDefaultTargetPendingEventThresholdWhenAttributeValueIsZero()
    {
        ConnectorTargetScaler scaler = CreateScaler(
            new SequenceDepthClient(17),
            0,
            new ConnectorOptions { DefaultTargetPendingEventThreshold = 8 });

        TargetScalerResult result = await scaler.GetScaleResultAsync(
            new TargetScalerContext());

        Assert.Equal(3, result.TargetWorkerCount);
    }

    [Fact]
    public void Constructor_Throws_WhenTargetPendingEventThresholdIsNegative()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CreateScaler(
                new SequenceDepthClient(0),
                -1,
                new ConnectorOptions()));

        Assert.Contains("TargetPendingEventThreshold", exception.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenDefaultTargetPendingEventThresholdIsNotPositive()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CreateScaler(
                new SequenceDepthClient(0),
                0,
                new ConnectorOptions { DefaultTargetPendingEventThreshold = 0 }));

        Assert.Contains("DefaultTargetPendingEventThreshold", exception.Message);
    }

    [Fact]
    public async Task GetScaleResultAsync_MaxBatchSizeDoesNotAffectTarget()
    {
        ConnectorTargetScaler smallBatch = CreateScaler(new SequenceDepthClient(65), 8, new ConnectorOptions { DefaultTargetPendingEventThreshold = 8, DefaultMaxBatchSize = 1 });
        ConnectorTargetScaler largeBatch = CreateScaler(new SequenceDepthClient(65), 8, new ConnectorOptions { DefaultTargetPendingEventThreshold = 8, DefaultMaxBatchSize = 32 });

        TargetScalerResult first = await smallBatch.GetScaleResultAsync(new TargetScalerContext());
        TargetScalerResult second = await largeBatch.GetScaleResultAsync(new TargetScalerContext());

        Assert.Equal(9, first.TargetWorkerCount);
        Assert.Equal(first.TargetWorkerCount, second.TargetWorkerCount);
    }

    [Fact]
    public async Task GetScaleResultAsync_CapsInt64DepthAtMaximumWorkerCount()
    {
        ConnectorTargetScaler scaler = CreateScaler(
            new SequenceDepthClient(long.MaxValue),
            1,
            new ConnectorOptions());

        TargetScalerResult result =
            await scaler.GetScaleResultAsync(new TargetScalerContext());

        Assert.Equal(int.MaxValue, result.TargetWorkerCount);
    }

    [Fact]
    public async Task MetricsProvider_ReturnsZeroAfterSuccessfulSampleWhenQueryFails()
    {
        var depthClient = new SequenceDepthClient(12, new HttpRequestException("transient"));
        var provider = new ConnectorMetricsProvider(
            depthClient,
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);

        ConnectorTriggerMetrics first = await provider.GetMetricsAsync();
        ConnectorTriggerMetrics second = await provider.GetMetricsAsync();

        Assert.Equal(12, first.ApproximateQueueDepth);
        Assert.Equal(0, second.ApproximateQueueDepth);
        Assert.NotSame(first, second);
        Assert.True(second.SampledAtUtc >= first.SampledAtUtc);
    }

    [Fact]
    public async Task MetricsProvider_InitialFailureReturnsZero()
    {
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(new HttpRequestException("initial")),
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);

        ConnectorTriggerMetrics metrics = await provider.GetMetricsAsync();

        Assert.Equal(0, metrics.ApproximateQueueDepth);
    }

    [Fact]
    public async Task MetricsProvider_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(
                new OperationCanceledException(cancellation.Token)),
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetMetricsAsync(cancellation.Token));
    }

    [Fact]
    public void Descriptor_UsesTriggerMetadataFunctionName()
    {
        ConnectorTargetScaler scaler = CreateScaler(new SequenceDepthClient(0), 1, new ConnectorOptions());

        Assert.Equal("Function", scaler.TargetScalerDescriptor.FunctionId);
    }

    private static ConnectorTargetScaler CreateScaler(IConnectorQueueDepthClient depthClient, int attributeTargetPendingEventThreshold, ConnectorOptions options)
    {
        var metrics = new ConnectorMetricsProvider(
            depthClient,
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);
        return new ConnectorTargetScaler("Function", metrics, attributeTargetPendingEventThreshold, options, NullLogger<ConnectorTargetScaler>.Instance);
    }
}
