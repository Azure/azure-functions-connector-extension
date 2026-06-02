// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTargetScalerTests
{
    private static ConnectorTargetScaler CreateScaler(
        int mockPendingEvents = 50,
        TimeSpan? throttle = null,
        int defaultConcurrency = 16,
        int attributeConcurrency = 0,
        int defaultBatchSize = 1,
        int attributeBatchSize = 0)
    {
        var options = new ConnectorOptions
        {
            MockPendingEvents = mockPendingEvents,
            DefaultConcurrency = defaultConcurrency,
            DefaultBatchSize = defaultBatchSize,
            ThrottleScaleDownInterval = throttle ?? TimeSpan.FromMinutes(3)
        };

        var metrics = new ConnectorMetricsProvider(
            options,
            functionName: "TestFn",
            connectorNamespace: null,
            triggerName: null,
            logger: NullLogger<ConnectorMetricsProvider>.Instance);

        return new ConnectorTargetScaler(
            functionId: "TestFn",
            metricsProvider: metrics,
            options: options,
            attributeConcurrency: attributeConcurrency,
            attributeBatchSize: attributeBatchSize,
            logger: NullLogger<ConnectorTargetScaler>.Instance);
    }

    private static TargetScalerContext CtxWithConcurrency(int? concurrency = null)
        => new() { InstanceConcurrency = concurrency };

    [Theory]
    [InlineData(0, 16, 0)]
    [InlineData(1, 16, 1)]
    [InlineData(16, 16, 1)]
    [InlineData(17, 16, 2)]
    [InlineData(50, 16, 4)]
    [InlineData(1400, 16, 88)]
    public async Task GetScaleResult_ComputesCeilDivisionOfPendingOverConcurrency(
        int pending, int concurrency, int expected)
    {
        var scaler = CreateScaler(mockPendingEvents: pending, defaultConcurrency: concurrency);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(concurrency));

        Assert.Equal(expected, result.TargetWorkerCount);
    }

    [Fact]
    public async Task GetScaleResult_UsesOptionDefault_WhenContextConcurrencyIsNull()
    {
        var scaler = CreateScaler(mockPendingEvents: 32, defaultConcurrency: 16);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(concurrency: null));

        
        Assert.Equal(2, result.TargetWorkerCount);
    }

    [Fact]
    public async Task ScaleDown_IsSuppressed_WithinThrottleWindow()
    {
        var prior = Environment.GetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar);
        try
        {
            
            var scaler = CreateScaler(mockPendingEvents: 100, throttle: TimeSpan.FromMinutes(3));

            
            var up = await scaler.GetScaleResultAsync(CtxWithConcurrency(16));
            Assert.Equal(7, up.TargetWorkerCount);

            
            SetMockPending(scaler, 0);
            var down = await scaler.GetScaleResultAsync(CtxWithConcurrency(16));

            Assert.Equal(7, down.TargetWorkerCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar, prior);
        }
    }

    [Fact]
    public async Task ScaleDown_IsAllowed_OnceThrottleHasExpired()
    {
        var prior = Environment.GetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar);
        try
        {
            
            var scaler = CreateScaler(mockPendingEvents: 100, throttle: TimeSpan.Zero);

            var up = await scaler.GetScaleResultAsync(CtxWithConcurrency(16));
            Assert.Equal(7, up.TargetWorkerCount);

            SetMockPending(scaler, 0);
            var down = await scaler.GetScaleResultAsync(CtxWithConcurrency(16));

            Assert.Equal(0, down.TargetWorkerCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar, prior);
        }
    }

    [Fact]
    public async Task EnvironmentVariable_OverridesMockPendingEvents()
    {
        var prior = Environment.GetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar, "320");

            var scaler = CreateScaler(mockPendingEvents: 50, defaultConcurrency: 16);
            var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(16));

            
            Assert.Equal(20, result.TargetWorkerCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectorMetricsProvider.MockEnvVar, prior);
        }
    }

    [Fact]
    public void TargetScalerDescriptor_CarriesFunctionId()
    {
        var scaler = CreateScaler();

        Assert.NotNull(scaler.TargetScalerDescriptor);
        Assert.Equal("TestFn", scaler.TargetScalerDescriptor.FunctionId);
    }

    // ==== Precedence chain (Phase 2-#2) ====

    [Fact]
    public async Task AttributeConcurrency_Wins_OverHostJsonDefault()
    {
       
        var scaler = CreateScaler(
            mockPendingEvents: 64,
            defaultConcurrency: 16,
            attributeConcurrency: 8);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        Assert.Equal(8, result.TargetWorkerCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task AttributeConcurrency_ZeroOrNegative_TreatedAsUnset(int attrConcurrency)
    {
        var scaler = CreateScaler(
            mockPendingEvents: 64,
            defaultConcurrency: 16,
            attributeConcurrency: attrConcurrency);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        Assert.Equal(4, result.TargetWorkerCount);
    }

    // ==== Batching math (Phase 2 attribute) ====

    [Theory]
    // pending, concurrency, batchSize, expectedWorkers
    [InlineData(0,    16, 10, 0)]    // empty backlog
    [InlineData(160,  16, 10, 1)]    // exactly one worker's capacity (16*10)
    [InlineData(161,  16, 10, 2)]    // one over -> two workers
    [InlineData(320,  16, 10, 2)]    // two workers' capacity
    [InlineData(1400, 16, 10, 9)]    // ceil(1400 / 160)
    [InlineData(50,    1,  1, 50)]   // degenerate: no batching, low concurrency
    [InlineData(50,   16,  1, 4)]    // batchSize=1 collapses to Phase 1 math
    public async Task GetScaleResult_DividesPendingByConcurrencyTimesBatchSize(
        int pending, int concurrency, int batchSize, int expected)
    {
        var scaler = CreateScaler(
            mockPendingEvents: pending,
            defaultConcurrency: concurrency,
            defaultBatchSize: batchSize);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(concurrency));

        Assert.Equal(expected, result.TargetWorkerCount);
    }

    [Fact]
    public async Task AttributeBatchSize_Wins_OverHostJsonDefault()
    {
        // pending=320, concurrency=16, attr batchSize=10 -> ceil(320 / 160) = 2
        // (host.json defaultBatchSize=1 would give ceil(320/16)=20)
        var scaler = CreateScaler(
            mockPendingEvents: 320,
            defaultConcurrency: 16,
            defaultBatchSize: 1,
            attributeBatchSize: 10);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        Assert.Equal(2, result.TargetWorkerCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task AttributeBatchSize_ZeroOrNegative_TreatedAsUnset(int attrBatchSize)
    {
        // Falls back to defaultBatchSize=5 -> ceil(50 / (16*5)) = ceil(50/80) = 1
        var scaler = CreateScaler(
            mockPendingEvents: 50,
            defaultConcurrency: 16,
            defaultBatchSize: 5,
            attributeBatchSize: attrBatchSize);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        Assert.Equal(1, result.TargetWorkerCount);
    }

    [Fact]
    public async Task DefaultBatchSize_ZeroOrNegative_TreatedAsOne()
    {
        // Guard against misconfigured host.json (e.g. defaultBatchSize=0).
        // Math must NOT divide by zero; should collapse to batchSize=1.
        var scaler = CreateScaler(
            mockPendingEvents: 64,
            defaultConcurrency: 16,
            defaultBatchSize: 0);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        // ceil(64 / (16*1)) = 4
        Assert.Equal(4, result.TargetWorkerCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task DefaultConcurrency_ZeroOrNegative_TreatedAsOne(int badDefault)
    {
        // Guard against misconfigured host.json defaultConcurrency.
        // Must NOT divide by zero or produce negative worker counts.
        var scaler = CreateScaler(
            mockPendingEvents: 50,
            defaultConcurrency: badDefault,
            defaultBatchSize: 1);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        // ceil(50 / (1*1)) = 50
        Assert.Equal(50, result.TargetWorkerCount);
    }

    [Fact]
    public async Task AttributeConcurrency_And_AttributeBatchSize_BothApplied()
    {
        // pending=1000, attr concurrency=4, attr batchSize=25 -> ceil(1000 / 100) = 10
        var scaler = CreateScaler(
            mockPendingEvents: 1000,
            defaultConcurrency: 16,
            defaultBatchSize: 1,
            attributeConcurrency: 4,
            attributeBatchSize: 25);

        var result = await scaler.GetScaleResultAsync(CtxWithConcurrency(null));

        Assert.Equal(10, result.TargetWorkerCount);
    }

    private static void SetMockPending(ConnectorTargetScaler _, int value)
    {
        Environment.SetEnvironmentVariable(
            ConnectorMetricsProvider.MockEnvVar,
            value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
