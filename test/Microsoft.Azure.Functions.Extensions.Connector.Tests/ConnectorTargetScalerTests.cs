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
    public async Task GetScaleResultAsync_UsesCeilingDepthOverEffectiveConcurrency(int depth, int concurrency, int expected)
    {
        ConnectorTargetScaler scaler = CreateScaler(new SequenceDepthClient(depth), concurrency, new ConnectorOptions { DefaultConcurrency = 99 });

        TargetScalerResult result = await scaler.GetScaleResultAsync(new TargetScalerContext { InstanceConcurrency = 3 });

        Assert.Equal(expected, result.TargetWorkerCount);
    }

    [Fact]
    public async Task GetScaleResultAsync_UsesHostDefaultWhenAttributeConcurrencyIsZero()
    {
        ConnectorTargetScaler scaler = CreateScaler(new SequenceDepthClient(17), 0, new ConnectorOptions { DefaultConcurrency = 8 });

        TargetScalerResult result = await scaler.GetScaleResultAsync(new TargetScalerContext { InstanceConcurrency = 2 });

        Assert.Equal(3, result.TargetWorkerCount);
    }

    [Fact]
    public async Task GetScaleResultAsync_MaxBatchSizeDoesNotAffectTarget()
    {
        ConnectorTargetScaler smallBatch = CreateScaler(new SequenceDepthClient(65), 8, new ConnectorOptions { DefaultConcurrency = 8, DefaultMaxBatchSize = 1 });
        ConnectorTargetScaler largeBatch = CreateScaler(new SequenceDepthClient(65), 8, new ConnectorOptions { DefaultConcurrency = 8, DefaultMaxBatchSize = 32 });

        TargetScalerResult first = await smallBatch.GetScaleResultAsync(new TargetScalerContext());
        TargetScalerResult second = await largeBatch.GetScaleResultAsync(new TargetScalerContext());

        Assert.Equal(9, first.TargetWorkerCount);
        Assert.Equal(first.TargetWorkerCount, second.TargetWorkerCount);
    }

    [Fact]
    public async Task MetricsProvider_PreservesLastKnownGoodDepthOnTransientFailure()
    {
        var depthClient = new SequenceDepthClient(12, new HttpRequestException("transient"));
        var provider = new ConnectorMetricsProvider(depthClient, "Function", "Trigger", NullLogger<ConnectorMetricsProvider>.Instance);

        ConnectorTriggerMetrics first = await provider.GetMetricsAsync();
        ConnectorTriggerMetrics second = await provider.GetMetricsAsync();

        Assert.Equal(12, first.PendingEvents);
        Assert.Equal(12, second.PendingEvents);
    }

    [Fact]
    public async Task MetricsProvider_InitialFailureDoesNotReportSuccessfulZero()
    {
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(new HttpRequestException("initial")),
            "Function",
            "Trigger",
            NullLogger<ConnectorMetricsProvider>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetMetricsAsync());
    }

    [Fact]
    public void Descriptor_UsesTriggerMetadataFunctionName()
    {
        ConnectorTargetScaler scaler = CreateScaler(new SequenceDepthClient(0), 1, new ConnectorOptions());

        Assert.Equal("Function", scaler.TargetScalerDescriptor.FunctionId);
    }

    private static ConnectorTargetScaler CreateScaler(IConnectorQueueDepthClient depthClient, int attributeConcurrency, ConnectorOptions options)
    {
        var metrics = new ConnectorMetricsProvider(depthClient, "Function", "Trigger", NullLogger<ConnectorMetricsProvider>.Instance);
        return new ConnectorTargetScaler("Function", metrics, attributeConcurrency, options, NullLogger<ConnectorTargetScaler>.Instance);
    }
}
