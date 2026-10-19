// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
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

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 16)]
    [InlineData(0, int.MaxValue)]
    [InlineData(4, 1)]
    [InlineData(4, 16)]
    public async Task GetScaleResultAsync_MaxConcurrentCallsDoesNotAffectTarget(
        int instanceConcurrency, int defaultMaxConcurrentCalls)
    {
        ConnectorTargetScaler scaler = CreateScaler(
            new SequenceDepthClient(65),
            8,
            new ConnectorOptions
            {
                DefaultMaxConcurrentCalls = defaultMaxConcurrentCalls,
            });

        TargetScalerResult result = await scaler.GetScaleResultAsync(
            new TargetScalerContext
            {
                InstanceConcurrency = instanceConcurrency == 0 ? null : instanceConcurrency,
            });

        Assert.Equal(instanceConcurrency == 0 ? 9 : 17, result.TargetWorkerCount);
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

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task MetricsProvider_PropagatesFailureAfterSuccessAndRecovers(long recoveryDepth)
    {
        var failure = new ConnectorQueueDepthException("query failed");
        var depthClient = new SequenceDepthClient(12, failure, recoveryDepth);
        var provider = new ConnectorMetricsProvider(
            depthClient,
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);

        ConnectorTriggerMetrics first = await provider.GetMetricsAsync();
        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                provider.GetMetricsAsync());
        DateTime recoveryStartedAtUtc = DateTime.UtcNow;
        ConnectorTriggerMetrics recovered = await provider.GetMetricsAsync();

        Assert.Same(failure, exception);
        Assert.Equal(12, first.ApproximateQueueDepth);
        Assert.Equal(recoveryDepth, recovered.ApproximateQueueDepth);
        Assert.NotSame(first, recovered);
        Assert.InRange(recovered.SampledAtUtc, recoveryStartedAtUtc, DateTime.UtcNow);
        Assert.Equal(3, depthClient.CallCount);
    }

    [Fact]
    public async Task MetricsProvider_InitialFailurePropagates()
    {
        var failure = new ConnectorQueueDepthException("initial");
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(failure),
            "Function",
            NullLogger<ConnectorMetricsProvider>.Instance);

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                provider.GetMetricsAsync());

        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task MetricsProvider_LogsWarningAndPropagatesNonCallerCancellation()
    {
        var failure = new OperationCanceledException("request timeout");
        var logger = new WarningLogger();
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(failure), "Function", logger);

        OperationCanceledException exception =
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                provider.GetMetricsAsync());

        Assert.Same(failure, exception);
        string warning = Assert.Single(logger.Warnings);
        Assert.Contains("no scale metrics are available for this observation", warning);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(17, 2)]
    public async Task GetScaleResultAsync_PropagatesFailureAndRecovers(
        long recoveryDepth, int expectedTarget)
    {
        var failure = new ConnectorQueueDepthException("query failed");
        var depthClient = new SequenceDepthClient(failure, recoveryDepth);
        ConnectorTargetScaler scaler = CreateScaler(
            depthClient, 16, new ConnectorOptions());

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                scaler.GetScaleResultAsync(new TargetScalerContext()));
        TargetScalerResult recovered =
            await scaler.GetScaleResultAsync(new TargetScalerContext());

        Assert.Same(failure, exception);
        Assert.Equal(expectedTarget, recovered.TargetWorkerCount);
        Assert.Equal(2, depthClient.CallCount);
    }

    [Fact]
    public async Task MetricsProvider_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = new OperationCanceledException(cancellation.Token);
        var logger = new WarningLogger();
        var provider = new ConnectorMetricsProvider(
            new SequenceDepthClient(failure),
            "Function",
            logger);

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                provider.GetMetricsAsync(cancellation.Token));

        Assert.Same(failure, exception);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public async Task MetricsProvider_WarningAndPropagatedFailureDoNotExposeCredentialDetails()
    {
        const string sensitiveDetails =
            "https://runtime.test/depth?signature=secret-value credential=secret-token payload=private";
        var logger = new WarningLogger();
        var depthClient = new ConnectorQueueDepthClient(
            new ConnectorPollingEndpoints(
                new Uri("https://runtime.test/receive"),
                new Uri("https://runtime.test/ack"),
                new Uri("https://runtime.test/depth")),
            new ThrowingTokenCredential(new InvalidOperationException(sensitiveDetails)),
            new TestHttpClientFactory(new SequenceHttpMessageHandler()),
            "Function",
            NullLogger<ConnectorQueueDepthClient>.Instance);
        var provider = new ConnectorMetricsProvider(depthClient, "Function", logger);

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                provider.GetMetricsAsync());

        Assert.Equal("Connector approximate queue depth request failed.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(sensitiveDetails, exception.ToString(), StringComparison.Ordinal);
        string warning = Assert.Single(logger.Warnings);
        Assert.Contains("Function", warning);
        Assert.Contains("no scale metrics are available for this observation", warning);
        Assert.DoesNotContain(sensitiveDetails, warning, StringComparison.Ordinal);
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

    private sealed class WarningLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add($"{formatter(state, exception)} {exception}");
            }
        }
    }
}
