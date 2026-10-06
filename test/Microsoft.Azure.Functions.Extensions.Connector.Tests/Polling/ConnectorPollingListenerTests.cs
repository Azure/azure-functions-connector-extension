// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingListenerTests
{
    private static readonly ConnectorPollingEndpoints Endpoints = new(
        new Uri("https://runtime.example/receive"),
        new Uri("https://runtime.example/acknowledge"),
        new Uri("https://runtime.example/approximateQueueDepth"));

    [Fact]
    public void Factory_ResolvesTriggerConfigNameFromAppSetting()
    {
        var nameResolver = new TestNameResolver(
            name => name == "OnNewEmailTriggerConfigName"
                ? "on-new-email"
                : null);
        var factory = new ConnectorPollingListenerFactory(
            new TestPollDeliveryClientFactory(
                _ => new StubConnectorPollDeliveryClient()),
            new StubConnectorLinkedOutputClient(),
            new ConnectorLinkedOutputInvocationLimiter(),
            nameResolver,
            NullLoggerFactory.Instance,
            Mock.Of<IDrainModeManager>());

        ConnectorPollingListener listener = factory.Create(
            new ConnectorFunctionRegistration(
                "TestFunction",
                Mock.Of<ITriggeredFunctionExecutor>()),
            new ConnectorPollingOptions(
                "ConnectorNamespace",
                "%OnNewEmailTriggerConfigName%",
                1,
                1),
            new ConnectorConnectionOptions(
                Mock.Of<TokenCredential>(),
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns"));

        Assert.Equal(
            "on-new-email",
            listener.Options.TriggerConfigName);
    }

    [Fact]
    public void Factory_ThrowsWhenTriggerConfigNameResolvesToEmpty()
    {
        var nameResolver = new TestNameResolver(_ => string.Empty);
        var factory = new ConnectorPollingListenerFactory(
            new TestPollDeliveryClientFactory(
                _ => new StubConnectorPollDeliveryClient()),
            new StubConnectorLinkedOutputClient(),
            new ConnectorLinkedOutputInvocationLimiter(),
            nameResolver,
            NullLoggerFactory.Instance,
            Mock.Of<IDrainModeManager>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(
                new ConnectorFunctionRegistration(
                    "TestFunction",
                    Mock.Of<ITriggeredFunctionExecutor>()),
                new ConnectorPollingOptions(
                    "ConnectorNamespace",
                    "%OnNewEmailTriggerConfigName%",
                    1,
                    1),
                new ConnectorConnectionOptions(
                    Mock.Of<TokenCredential>(),
                    "https://app-12.region.logic.azure.com/api/connectorGateways/ns")));

        Assert.Contains("resolved to an empty value", exception.Message);
    }

    [Fact]
    public async Task StartAsync_ThrowsWhenCancellationIsRequested()
    {
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            new StubConnectorPollDeliveryClient(),
            new StubConnectorLinkedOutputClient());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            listener.StartAsync(cancellation.Token));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Listener_RestartsAfterStopOrCancel(
        bool cancelBeforeRestart,
        bool stopBeforeRestart)
    {
        int receiveCount = 0;
        var firstReceive = NewCompletionSource();
        var secondReceive = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
            {
                int count = Interlocked.Increment(ref receiveCount);
                (count == 1 ? firstReceive : secondReceive).TrySetResult();
                return Task.FromResult(new ConnectorReceiveResult([], false));
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await listener.StartAsync(CancellationToken.None);
        await firstReceive.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (cancelBeforeRestart)
        {
            listener.Cancel();
        }

        if (stopBeforeRestart)
        {
            await listener.StopAsync(CancellationToken.None);
        }

        await listener.StartAsync(CancellationToken.None);
        await secondReceive.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(2, receiveCount);
    }

    [Fact]
    public async Task Listener_ConcurrentStartsCreateOnlyOnePump()
    {
        int receiveCount = 0;
        var receiveStarted = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
            {
                Interlocked.Increment(ref receiveCount);
                receiveStarted.TrySetResult();
                return Task.FromResult(new ConnectorReceiveResult([], false));
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await Task.WhenAll(
            listener.StartAsync(CancellationToken.None),
            listener.StartAsync(CancellationToken.None));
        await receiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, receiveCount);
    }

    [Fact]
    public async Task Listener_RestartWaitsForAcknowledgementToDrain()
    {
        int receiveCount = 0;
        var acknowledgementStarted = NewCompletionSource();
        var releaseAcknowledgement = NewCompletionSource();
        var restartedReceive = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
            {
                if (Interlocked.Increment(ref receiveCount) == 1)
                {
                    return Task.FromResult(new ConnectorReceiveResult(
                        [CreateInlineMessage("message-1", "lock-1", "{}")],
                        false));
                }

                restartedReceive.TrySetResult();
                return Task.FromResult(new ConnectorReceiveResult([], false));
            },
            AcknowledgeAsyncHandler = async (_, locks, cancellationToken) =>
            {
                acknowledgementStarted.TrySetResult();
                await releaseAcknowledgement.Task.WaitAsync(cancellationToken);
                return Acknowledged(locks);
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));
        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await listener.StartAsync(CancellationToken.None);
        await acknowledgementStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task stop = listener.StopAsync(CancellationToken.None);
        using var startCancellation = new CancellationTokenSource();
        Task cancelledStart = listener.StartAsync(startCancellation.Token);
        startCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledStart);
        Task restart = listener.StartAsync(CancellationToken.None);
        Assert.False(stop.IsCompleted);
        Assert.False(restart.IsCompleted);
        Assert.Equal(1, receiveCount);

        releaseAcknowledgement.TrySetResult();
        await Task.WhenAll(stop, restart).WaitAsync(TimeSpan.FromSeconds(5));
        await restartedReceive.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(2, receiveCount);
    }

    [Fact]
    public async Task Listener_ConcurrentStopsAllowSubsequentRestart()
    {
        var receiveStarted = NewCompletionSource();
        var releaseReceive = NewCompletionSource();
        var restartedReceive = NewCompletionSource();
        int receiveCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = async (_, _, cancellationToken) =>
            {
                if (Interlocked.Increment(ref receiveCount) == 1)
                {
                    receiveStarted.TrySetResult();
                    await releaseReceive.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                restartedReceive.TrySetResult();
                return new ConnectorReceiveResult([], false);
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await listener.StartAsync(CancellationToken.None);
        await receiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task firstStop = listener.StopAsync(CancellationToken.None);
        Task secondStop = listener.StopAsync(CancellationToken.None);
        Assert.False(firstStop.IsCompleted);
        Assert.False(secondStop.IsCompleted);
        releaseReceive.TrySetResult();
        await Task.WhenAll(firstStop, secondStop).WaitAsync(TimeSpan.FromSeconds(5));

        await listener.StartAsync(CancellationToken.None);
        await restartedReceive.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);
        Assert.Equal(2, receiveCount);
    }

    [Fact]
    public async Task Listener_CancelledStopDoesNotAllowOverlappingPump()
    {
        var receiveStarted = NewCompletionSource();
        var releaseReceive = NewCompletionSource();
        var restartedReceive = NewCompletionSource();
        int receiveCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = async (_, _, cancellationToken) =>
            {
                if (Interlocked.Increment(ref receiveCount) == 1)
                {
                    receiveStarted.TrySetResult();
                    await releaseReceive.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                restartedReceive.TrySetResult();
                return new ConnectorReceiveResult([], false);
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());
        using var stopCancellation = new CancellationTokenSource();

        await listener.StartAsync(CancellationToken.None);
        await receiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task stop = listener.StopAsync(stopCancellation.Token);
        stopCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
        Task restart = listener.StartAsync(CancellationToken.None);
        Assert.False(restart.IsCompleted);
        Assert.Equal(1, receiveCount);

        releaseReceive.TrySetResult();
        await restart.WaitAsync(TimeSpan.FromSeconds(5));
        await restartedReceive.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);
        Assert.Equal(2, receiveCount);
    }

    [Fact]
    public async Task Listener_DisposeDuringStopKeepsCancellationSourceAliveUntilPumpCompletes()
    {
        var receiveStarted = NewCompletionSource();
        var releaseReceive = NewCompletionSource();
        var tokenChecked = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = async (_, _, cancellationToken) =>
            {
                receiveStarted.TrySetResult();
                await releaseReceive.Task;
                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() => tokenChecked.TrySetResult());
                cancellationToken.ThrowIfCancellationRequested();
                return new ConnectorReceiveResult([], false);
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await listener.StartAsync(CancellationToken.None);
        await receiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task stop = listener.StopAsync(CancellationToken.None);
        Task restart = listener.StartAsync(CancellationToken.None);
        listener.Dispose();
        listener.Dispose();
        listener.Cancel();
        Assert.False(stop.IsCompleted);
        releaseReceive.TrySetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(5));
        await tokenChecked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => restart);
        await listener.StopAsync(CancellationToken.None);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            listener.StartAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_UsesHostDrainModeForInFlightExecution(bool drainModeEnabled)
    {
        var executionStarted = NewCompletionSource();
        var releaseExecution = NewCompletionSource();
        CancellationToken executionToken = default;
        int receiveCount = 0;
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
            {
                Interlocked.Increment(ref receiveCount);
                return Task.FromResult(new ConnectorReceiveResult(
                    [CreateInlineMessage("message-1", "lock-1", "{}")],
                    false));
            },
            AcknowledgeAsyncHandler = (_, locks, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Interlocked.Increment(ref acknowledgementCount);
                return Task.FromResult(Acknowledged(locks));
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>(async (_, cancellationToken) =>
            {
                executionToken = cancellationToken;
                executionStarted.TrySetResult();
                await releaseExecution.Task.WaitAsync(cancellationToken);
                return new FunctionResult(true);
            });
        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            drainModeEnabled: drainModeEnabled);

        await listener.StartAsync(CancellationToken.None);
        await executionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task stop = listener.StopAsync(CancellationToken.None);

        Assert.Equal(!drainModeEnabled, executionToken.IsCancellationRequested);
        if (drainModeEnabled)
        {
            Assert.False(stop.IsCompleted);
            Assert.Equal(0, acknowledgementCount);
            releaseExecution.TrySetResult();
        }

        await stop.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(drainModeEnabled ? 1 : 0, acknowledgementCount);
        Assert.Equal(1, receiveCount);
    }

    [Fact]
    public async Task StopAsync_CancelledDrainWaitDoesNotCancelProcessing()
    {
        var executionStarted = NewCompletionSource();
        var releaseExecution = NewCompletionSource();
        CancellationToken executionToken = default;
        int receiveCount = 0;
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(Interlocked.Increment(ref receiveCount) == 1
                    ? new ConnectorReceiveResult(
                        [CreateInlineMessage("message-1", "lock-1", "{}")],
                        false)
                    : new ConnectorReceiveResult([], false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                Interlocked.Increment(ref acknowledgementCount);
                return Task.FromResult(Acknowledged(locks));
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>(async (_, cancellationToken) =>
            {
                executionToken = cancellationToken;
                executionStarted.TrySetResult();
                await releaseExecution.Task.WaitAsync(cancellationToken);
                return new FunctionResult(true);
            });
        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            drainModeEnabled: true);
        using var stopCancellation = new CancellationTokenSource();

        await listener.StartAsync(CancellationToken.None);
        await executionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task stop = listener.StopAsync(stopCancellation.Token);
        stopCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
        Assert.False(executionToken.IsCancellationRequested);

        Task restart = listener.StartAsync(CancellationToken.None);
        Assert.False(restart.IsCompleted);
        releaseExecution.TrySetResult();
        await restart.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, acknowledgementCount);
        Assert.Equal(2, receiveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Listener_CancelAndDisposeOverrideDrainMode(bool dispose)
    {
        var executionStarted = NewCompletionSource();
        int acknowledgementCount = 0;
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>(async (_, cancellationToken) =>
            {
                executionStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new FunctionResult(true);
            });
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(new ConnectorReceiveResult(
                    [CreateInlineMessage("message-1", "lock-1", "{}")],
                    false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                Interlocked.Increment(ref acknowledgementCount);
                return Task.FromResult(Acknowledged(locks));
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            drainModeEnabled: true);

        await listener.StartAsync(CancellationToken.None);
        await executionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (dispose)
        {
            listener.Dispose();
        }
        else
        {
            listener.Cancel();
        }

        await listener.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_RejectsReceiveResponseThatExceedsRequestedCapacity()
    {
        ConnectorPollMessage first =
            CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        ConnectorPollMessage second =
            CreateInlineMessage("message-2", "lock-2", """{"value":2}""");
        var invalidResponseHandled = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
                Assert.Equal(2, maxEvents);
                return Task.FromResult(
                    new ConnectorReceiveResult(
                    [
                        first,
                        second,
                        CreateInlineMessage(
                            "message-3",
                            "lock-3",
                            """{"value":3}"""),
                    ],
                    false));
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
            maxConcurrentCalls: 1,
            isBatched: true,
            delayAsync: (_, cancellationToken) =>
            {
                invalidResponseHandled.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });

        await listener.StartAsync(CancellationToken.None);
        await invalidResponseHandled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        executor.Verify(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listener_CapsReceiveCapacityAtProtocolMaximum()
    {
        int receiveCount = 0;
        var delayStarted = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
                Assert.Equal(
                    ConnectorPollingProtocolLimits.MaximumBatchSize,
                    maxEvents);
                Interlocked.Increment(ref receiveCount);
                return Task.FromResult(
                    new ConnectorReceiveResult([], false));
            },
        };

        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: ConnectorPollingProtocolLimits.MaximumBatchSize,
            maxConcurrentCalls: int.MaxValue,
            isBatched: true,
            delayAsync: (_, cancellationToken) =>
            {
                delayStarted.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });

        await listener.StartAsync(CancellationToken.None);
        await delayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, receiveCount);
    }

    [Fact]
    public async Task Listener_BacksOffRepeatedReceiveFailures()
    {
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                throw new ConnectorPollDeliveryException("Receive failed"),
        };
        var delays = new List<TimeSpan>();
        var expectedDelaysObserved = NewCompletionSource();

        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            delayAsync: async (delay, _) =>
            {
                lock (delays)
                {
                    delays.Add(delay);
                    if (delays.Count == 4)
                    {
                        expectedDelaysObserved.TrySetResult();
                    }
                }

                await Task.Yield();
            });

        await listener.StartAsync(CancellationToken.None);
        await expectedDelaysObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Cancel();
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(
            [
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
            ],
            delays.Take(4));
    }

    [Fact]
    public async Task Listener_BacksOffRepeatedEmptyReceivesToConfiguredMaximum()
    {
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(new ConnectorReceiveResult([], false)),
        };
        var delays = new List<TimeSpan>();
        var expectedDelaysObserved = NewCompletionSource();

        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxPollingInterval: TimeSpan.FromSeconds(4),
            delayAsync: async (delay, _) =>
            {
                lock (delays)
                {
                    delays.Add(delay);
                    if (delays.Count == 5)
                    {
                        expectedDelaysObserved.TrySetResult();
                    }
                }

                await Task.Yield();
            });

        await listener.StartAsync(CancellationToken.None);
        await expectedDelaysObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Cancel();
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(
            [
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(4),
            ],
            delays.Take(5));
    }

    [Fact]
    public async Task Listener_ResetsEmptyReceiveBackoffWhenMessagesAreFound()
    {
        ConnectorPollMessage message =
            CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        int receiveCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(Interlocked.Increment(ref receiveCount) switch
                {
                    1 or 2 => new ConnectorReceiveResult([], false),
                    3 => new ConnectorReceiveResult([message], true),
                    _ => new ConnectorReceiveResult([], false),
                }),
            AcknowledgeAsyncHandler = (_, locks, _) =>
                Task.FromResult(Acknowledged(locks)),
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));
        var delays = new List<TimeSpan>();
        var expectedDelaysObserved = NewCompletionSource();

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            delayAsync: async (delay, _) =>
            {
                lock (delays)
                {
                    delays.Add(delay);
                    if (delays.Count == 3)
                    {
                        expectedDelaysObserved.TrySetResult();
                    }
                }

                await Task.Yield();
            });

        await listener.StartAsync(CancellationToken.None);
        await expectedDelaysObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Cancel();
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(
            [
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(1),
            ],
            delays.Take(3));
    }

    [Fact]
    public async Task Listener_UsesMaxConcurrentCallsForBatchedInvocations()
    {
        ConnectorPollMessage[] messages =
        [
            CreateInlineMessage("message-1", "lock-1", """{"value":1}"""),
            CreateInlineMessage("message-2", "lock-2", """{"value":2}"""),
            CreateInlineMessage("message-3", "lock-3", """{"value":3}"""),
        ];
        int receiveMaxEvents = 0;
        int receiveCount = 0;
        var acknowledgementsCompleted = NewCompletionSource();
        var acknowledgedMessageIds = new List<string>();
        var acknowledgementSizes = new List<int>();
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
                Interlocked.Increment(ref receiveCount);
                receiveMaxEvents = maxEvents;
                return Task.FromResult(
                    new ConnectorReceiveResult(messages, false));
            },
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                lock (acknowledgedMessageIds)
                {
                    acknowledgedMessageIds.AddRange(
                        locks.Select(value => value.MessageId));
                    acknowledgementSizes.Add(locks.Count);
                }

                if (Interlocked.Increment(ref acknowledgementCount) == 2)
                {
                    acknowledgementsCompleted.TrySetResult();
                }

                return Task.FromResult(Acknowledged(locks));
            },
        };

        int activeInvocations = 0;
        int maximumActiveInvocations = 0;
        int enteredInvocations = 0;
        var invocationSizes = new List<int>();
        var bothInvocationsEntered = NewCompletionSource();
        var releaseInvocations = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>(
                async (triggerData, _) =>
                {
                    ConnectorTriggerInput input =
                        Assert.IsType<ConnectorTriggerInput>(
                            triggerData.TriggerValue);
                    Assert.True(input.IsBatched);
                    lock (invocationSizes)
                    {
                        invocationSizes.Add(input.Events.Count);
                    }

                    int active =
                        Interlocked.Increment(ref activeInvocations);
                    UpdateMaximum(ref maximumActiveInvocations, active);
                    if (Interlocked.Increment(ref enteredInvocations) == 2)
                    {
                        bothInvocationsEntered.TrySetResult();
                    }

                    await releaseInvocations.Task;
                    Interlocked.Decrement(ref activeInvocations);
                    return new FunctionResult(true);
                });

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
            maxConcurrentCalls: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await bothInvocationsEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, maximumActiveInvocations);
        Assert.Equal(1, Volatile.Read(ref receiveCount));

        releaseInvocations.TrySetResult();
        await acknowledgementsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(4, receiveMaxEvents);
        Assert.Equal([1, 2], invocationSizes.Order());
        Assert.Equal(
            ["message-1", "message-2", "message-3"],
            acknowledgedMessageIds.Order());
        Assert.Equal([1, 2], acknowledgementSizes.Order());
    }

    [Fact]
    public async Task Listener_ProcessesConcurrentSingleMessageInvocations()
    {
        ConnectorPollMessage first = CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        ConnectorPollMessage second = CreateInlineMessage("message-2", "lock-2", """{"value":2}""");
        int receiveMaxEvents = 0;
        int acknowledgementCount = 0;
        var acknowledgementsCompleted = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
                receiveMaxEvents = maxEvents;
                return Task.FromResult(
                    new ConnectorReceiveResult([first, second], false));
            },
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                if (Interlocked.Increment(ref acknowledgementCount) == 2)
                {
                    acknowledgementsCompleted.TrySetResult();
                }

                return Task.FromResult(Acknowledged(locks.Single()));
            },
        };

        int activeInvocations = 0;
        int maximumActiveInvocations = 0;
        int enteredInvocations = 0;
        var bothInvocationsEntered = NewCompletionSource();
        var releaseInvocations = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                int active = Interlocked.Increment(ref activeInvocations);
                UpdateMaximum(ref maximumActiveInvocations, active);
                if (Interlocked.Increment(ref enteredInvocations) == 2)
                {
                    bothInvocationsEntered.TrySetResult();
                }

                await releaseInvocations.Task;
                Interlocked.Decrement(ref activeInvocations);
                return new FunctionResult(true);
            });

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxConcurrentCalls: 2);

        await listener.StartAsync(CancellationToken.None);
        await bothInvocationsEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, maximumActiveInvocations);

        releaseInvocations.TrySetResult();
        await acknowledgementsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(2, receiveMaxEvents);
        Assert.Equal(2, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_DoesNotAcknowledgeFailedBatchInvocation()
    {
        ConnectorPollMessage first =
            CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        ConnectorPollMessage second =
            CreateInlineMessage("message-2", "lock-2", """{"value":2}""");
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(
                    new ConnectorReceiveResult([first, second], false)),
        };
        deliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
        {
            Interlocked.Increment(ref acknowledgementCount);
            return Task.FromResult(Acknowledged(locks));
        };
        var executionCompleted = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.Is<TriggeredFunctionData>(data =>
                    ((ConnectorTriggerInput)data.TriggerValue)
                        .Events.Count == 2),
                It.IsAny<CancellationToken>()))
            .Callback(() => executionCompleted.TrySetResult())
            .ReturnsAsync(new FunctionResult(
                new InvalidOperationException("Function failed.")));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
            maxConcurrentCalls: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await executionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(0, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_HydratesLinkedOutputBeforeInvocation()
    {
        ConnectorPollMessage message = ConnectorPollMessage.FromOutputsLink(
            "message-1",
            "lock-1",
            new ConnectorOutputsLink(new Uri("https://content.example/output?sig=secret")));
        int acknowledgementCount = 0;
        StubConnectorPollDeliveryClient deliveryClient =
            CreateSingleReceiveClient(message);
        deliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
        {
            Interlocked.Increment(ref acknowledgementCount);
            return Task.FromResult(Acknowledged(locks.Single()));
        };
        int downloadCount = 0;
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (outputsLink, maximumPayloadSize, _) =>
            {
                Assert.Same(message.OutputsLink, outputsLink);
                Assert.Equal(
                    ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes,
                    maximumPayloadSize);
                Interlocked.Increment(ref downloadCount);
                return Task.FromResult(
                    BinaryData.FromString("""{"value":"linked"}"""));
            },
        };
        var invocationCompleted = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.Is<TriggeredFunctionData>(
                    data => ((ConnectorTriggerInput)data.TriggerValue).ToSinglePayloadJson()
                        == """{"value":"linked"}"""),
                It.IsAny<CancellationToken>()))
            .Callback(() => invocationCompleted.TrySetResult())
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient);

        await listener.StartAsync(CancellationToken.None);
        await invocationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, downloadCount);
        Assert.Equal(1, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_InvokesLinkedOutputsIndividuallyAndSerially()
    {
        ConnectorPollMessage first = ConnectorPollMessage.FromOutputsLink(
            "message-1",
            "lock-1",
            new ConnectorOutputsLink(
                new Uri("https://content.example/output-1?sig=secret")));
        ConnectorPollMessage second = ConnectorPollMessage.FromOutputsLink(
            "message-2",
            "lock-2",
            new ConnectorOutputsLink(
                new Uri("https://content.example/output-2?sig=secret")));
        var acknowledgementsCompleted = NewCompletionSource();
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(
                    new ConnectorReceiveResult([first, second], false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                Assert.Single(locks);
                if (Interlocked.Increment(ref acknowledgementCount) == 2)
                {
                    acknowledgementsCompleted.TrySetResult();
                }

                return Task.FromResult(Acknowledged(locks));
            },
        };
        int activeDownloads = 0;
        int maximumActiveDownloads = 0;
        int downloadCount = 0;
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = async (_, _, cancellationToken) =>
            {
                int active = Interlocked.Increment(ref activeDownloads);
                UpdateMaximum(ref maximumActiveDownloads, active);
                Interlocked.Increment(ref downloadCount);
                await Task.Delay(
                    TimeSpan.FromMilliseconds(50),
                    cancellationToken);
                Interlocked.Decrement(ref activeDownloads);
                return BinaryData.FromString("""{"value":"linked"}""");
            },
        };
        int activeInvocations = 0;
        int maximumActiveInvocations = 0;
        var invocationSizes = new List<int>();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Returns<TriggeredFunctionData, CancellationToken>(
                async (triggerData, cancellationToken) =>
                {
                    var input =
                        (ConnectorTriggerInput)triggerData.TriggerValue;
                    lock (invocationSizes)
                    {
                        invocationSizes.Add(input.Events.Count);
                    }

                    int active =
                        Interlocked.Increment(ref activeInvocations);
                    UpdateMaximum(ref maximumActiveInvocations, active);
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(50),
                        cancellationToken);
                    Interlocked.Decrement(ref activeInvocations);
                    return new FunctionResult(true);
                });

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 1,
            maxConcurrentCalls: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await acknowledgementsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(2, downloadCount);
        Assert.Equal(1, maximumActiveDownloads);
        Assert.Equal(1, maximumActiveInvocations);
        Assert.Equal([1, 1], invocationSizes);
    }

    [Fact]
    public async Task Listener_SerializesLinkedOutputsAcrossListenersThroughAcknowledgement()
    {
        var limiter = new ConnectorLinkedOutputInvocationLimiter();
        var firstAcknowledgementStarted = NewCompletionSource();
        var releaseFirstAcknowledgement = NewCompletionSource();
        var secondDownloadStarted = NewCompletionSource();
        var secondAcknowledgementCompleted = NewCompletionSource();
        StubConnectorPollDeliveryClient firstDeliveryClient =
            CreateSingleReceiveClient(ConnectorPollMessage.FromOutputsLink(
                "message-1",
                "lock-1",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-1?sig=secret"))));
        firstDeliveryClient.AcknowledgeAsyncHandler =
            async (_, locks, cancellationToken) =>
            {
                firstAcknowledgementStarted.TrySetResult();
                await releaseFirstAcknowledgement.Task.WaitAsync(
                    cancellationToken);
                return Acknowledged(locks);
            };
        StubConnectorPollDeliveryClient secondDeliveryClient =
            CreateSingleReceiveClient(ConnectorPollMessage.FromOutputsLink(
                "message-2",
                "lock-2",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-2?sig=secret"))));
        secondDeliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
        {
            secondAcknowledgementCompleted.TrySetResult();
            return Task.FromResult(Acknowledged(locks));
        };
        var firstLinkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (_, _, _) =>
                Task.FromResult(BinaryData.FromString("""{"value":1}""")),
        };
        var secondLinkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (_, _, _) =>
            {
                secondDownloadStarted.TrySetResult();
                return Task.FromResult(
                    BinaryData.FromString("""{"value":2}"""));
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener firstListener = CreateListener(
            executor.Object,
            Endpoints,
            firstDeliveryClient,
            firstLinkedOutputClient,
            linkedOutputInvocationLimiter: limiter);
        using ConnectorPollingListener secondListener = CreateListener(
            executor.Object,
            Endpoints,
            secondDeliveryClient,
            secondLinkedOutputClient,
            linkedOutputInvocationLimiter: limiter);

        await firstListener.StartAsync(CancellationToken.None);
        await firstAcknowledgementStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        await secondListener.StartAsync(CancellationToken.None);

        Assert.False(secondDownloadStarted.Task.IsCompleted);

        releaseFirstAcknowledgement.TrySetResult();
        await secondAcknowledgementCompleted.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        await Task.WhenAll(
            firstListener.StopAsync(CancellationToken.None),
            secondListener.StopAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Listener_CancellationStopsRemainingLinkedInvocations()
    {
        ConnectorPollMessage[] messages =
        [
            ConnectorPollMessage.FromOutputsLink(
                "message-1",
                "lock-1",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-1?sig=secret"))),
            ConnectorPollMessage.FromOutputsLink(
                "message-2",
                "lock-2",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-2?sig=secret"))),
        ];
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(new ConnectorReceiveResult(messages, false)),
        };
        var downloadStarted = NewCompletionSource();
        int downloadCount = 0;
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = async (_, _, cancellationToken) =>
            {
                Interlocked.Increment(ref downloadCount);
                downloadStarted.TrySetResult();
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);
                return BinaryData.FromString("""{"value":"linked"}""");
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 2,
            maxConcurrentCalls: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await downloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        listener.Cancel();
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, downloadCount);
        executor.Verify(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listener_SeparatesLinkedOutputsFromInlineBatch()
    {
        ConnectorPollMessage[] messages =
        [
            CreateInlineMessage("inline-1", "lock-1", """{"value":1}"""),
            ConnectorPollMessage.FromOutputsLink(
                "linked-1",
                "lock-2",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-1?sig=secret"))),
            CreateInlineMessage("inline-2", "lock-3", """{"value":2}"""),
            ConnectorPollMessage.FromOutputsLink(
                "linked-2",
                "lock-4",
                new ConnectorOutputsLink(
                    new Uri("https://content.example/output-2?sig=secret"))),
        ];
        var acknowledgementsCompleted = NewCompletionSource();
        int acknowledgementCount = 0;
        var acknowledgementSizes = new List<int>();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(new ConnectorReceiveResult(messages, false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                lock (acknowledgementSizes)
                {
                    acknowledgementSizes.Add(locks.Count);
                }

                if (Interlocked.Increment(ref acknowledgementCount) == 3)
                {
                    acknowledgementsCompleted.TrySetResult();
                }

                return Task.FromResult(Acknowledged(locks));
            },
        };
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (_, _, _) =>
                Task.FromResult(
                    BinaryData.FromString("""{"value":"linked"}""")),
        };
        var invocationSizes = new List<int>();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var input = (ConnectorTriggerInput)data.TriggerValue;
                lock (invocationSizes)
                {
                    invocationSizes.Add(input.Events.Count);
                }
            })
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 4,
            maxConcurrentCalls: 4,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await acknowledgementsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal([1, 1, 2], invocationSizes.Order());
        Assert.Equal([1, 1, 2], acknowledgementSizes.Order());
    }

    [Fact]
    public async Task Listener_PreservesMessageIdButNotLockTokenInBindingData()
    {
        ConnectorPollMessage message =
            CreateInlineMessage("message-1", "lock-secret", """{"value":1}""");
        StubConnectorPollDeliveryClient deliveryClient =
            CreateSingleReceiveClient(message);
        deliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
            Task.FromResult(Acknowledged(locks.Single()));
        string? bindingContent = null;
        var invocationCompleted = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Callback<TriggeredFunctionData, CancellationToken>((data, _) =>
            {
                var triggerInput = (ConnectorTriggerInput)data.TriggerValue;
                bindingContent = ConnectorExtensionConfigProvider
                    .ConvertTriggerEventToBindingData(
                        triggerInput.Events.Single())
                    .Content
                    .ToString();
                invocationCompleted.TrySetResult();
            })
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        await listener.StartAsync(CancellationToken.None);
        await invocationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Contains("message-1", bindingContent);
        Assert.DoesNotContain("lock-secret", bindingContent);
    }

    [Fact]
    public async Task Listener_ReceivesWithoutUsingApproximateHasMessages()
    {
        int receiveCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
            {
                Interlocked.Increment(ref receiveCount);
                return Task.FromResult(
                    new ConnectorReceiveResult([], false));
            },
        };
        var delayStarted = NewCompletionSource();

        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            Endpoints,
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            delayAsync: (_, cancellationToken) =>
            {
                delayStarted.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });

        await listener.StartAsync(CancellationToken.None);
        await delayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, receiveCount);
    }

    [Fact]
    public async Task Listener_DoesNotInvokeOrAcknowledgeWhenLinkedOutputFails()
    {
        ConnectorPollMessage message = ConnectorPollMessage.FromOutputsLink(
            "message-1",
            "lock-1",
            new ConnectorOutputsLink(new Uri("https://content.example/output?sig=secret")));
        int acknowledgementCount = 0;
        StubConnectorPollDeliveryClient deliveryClient =
            CreateSingleReceiveClient(message);
        deliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
        {
            Interlocked.Increment(ref acknowledgementCount);
            return Task.FromResult(Acknowledged(locks.Single()));
        };
        var hydrationAttempted = NewCompletionSource();
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (_, _, _) =>
            {
                hydrationAttempted.TrySetResult();
                throw new ConnectorLinkedOutputException("Download failed.");
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient);

        await listener.StartAsync(CancellationToken.None);
        await hydrationAttempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        executor.Verify(value => value.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_ExcludesFailedHydrationFromBatch()
    {
        ConnectorPollMessage failedMessage = ConnectorPollMessage.FromOutputsLink(
            "message-1",
            "lock-1",
            new ConnectorOutputsLink(
                new Uri("https://content.example/output?sig=secret")));
        ConnectorPollMessage successfulMessage = CreateInlineMessage(
            "message-2",
            "lock-2",
            """{"value":2}""");
        var invocationCompleted = NewCompletionSource();
        IReadOnlyList<ConnectorMessageLock>? acknowledgedLocks = null;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(
                    new ConnectorReceiveResult(
                        [failedMessage, successfulMessage],
                        false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                acknowledgedLocks = locks.ToArray();
                return Task.FromResult(Acknowledged(locks));
            },
        };
        var linkedOutputClient = new StubConnectorLinkedOutputClient
        {
            DownloadAsyncHandler = (_, _, _) =>
                throw new ConnectorLinkedOutputException("Download failed."),
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.Is<TriggeredFunctionData>(data =>
                    ((ConnectorTriggerInput)data.TriggerValue)
                        .Events.Single().MessageId == "message-2"),
                It.IsAny<CancellationToken>()))
            .Callback(() => invocationCompleted.TrySetResult())
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            Endpoints,
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 2,
            maxConcurrentCalls: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await invocationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        ConnectorMessageLock acknowledgedLock =
            Assert.Single(acknowledgedLocks!);
        Assert.Equal("message-2", acknowledgedLock.MessageId);
    }

    private static ConnectorPollingListener CreateListener(
        ITriggeredFunctionExecutor executor,
        ConnectorPollingEndpoints endpoints,
        IConnectorPollDeliveryClient deliveryClient,
        IConnectorLinkedOutputClient linkedOutputClient,
        int maxBatchSize = 1,
        int maxConcurrentCalls = 1,
        bool isBatched = false,
        TimeSpan? maxPollingInterval = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        ConnectorLinkedOutputInvocationLimiter? linkedOutputInvocationLimiter = null,
        bool drainModeEnabled = true)
    {
        var registration = new ConnectorFunctionRegistration("TestFunction", executor);
        var options = new ConnectorPollingOptions(
            "ConnectorNamespace",
            "OnNewEmail",
            maxBatchSize,
            maxConcurrentCalls,
            isBatched)
        {
            MaxPollingInterval =
                maxPollingInterval ?? TimeSpan.FromSeconds(30),
        };
        return new ConnectorPollingListener(
            registration,
            options,
            endpoints,
            deliveryClient,
            linkedOutputClient,
            linkedOutputInvocationLimiter
                ?? new ConnectorLinkedOutputInvocationLimiter(),
            NullLogger<ConnectorPollingListener>.Instance,
            Mock.Of<IDrainModeManager>(
                manager => manager.IsDrainModeEnabled == drainModeEnabled),
            delayAsync ?? WaitUntilCancelledAsync);
    }

    private static StubConnectorPollDeliveryClient CreateSingleReceiveClient(
        ConnectorPollMessage message) =>
        new()
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(new ConnectorReceiveResult([message], false)),
        };

    private static ConnectorPollMessage CreateInlineMessage(
        string messageId,
        string lockToken,
        string json) =>
        ConnectorPollMessage.FromInlineOutputs(
            messageId,
            lockToken,
            BinaryData.FromString(json));

    private static ConnectorAcknowledgeResult Acknowledged(
        ConnectorMessageLock messageLock) =>
        Acknowledged([messageLock]);

    private static ConnectorAcknowledgeResult Acknowledged(
        IReadOnlyList<ConnectorMessageLock> messageLocks) =>
        new(messageLocks
            .Select(messageLock =>
                new ConnectorAcknowledgeItemResult(
                    messageLock.MessageId,
                    ConnectorAcknowledgeStatus.Acknowledged))
            .ToArray());

    private static Task WaitUntilCancelledAsync(
        TimeSpan _,
        CancellationToken cancellationToken) =>
        Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int value)
    {
        int current;
        do
        {
            current = maximum;
            if (current >= value)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref maximum, value, current) != current);
    }

}
