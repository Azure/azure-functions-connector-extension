// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Azure.WebJobs;
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
        string? resolverTriggerConfigName = null;
        var endpointResolverFactory = new TestResolverFactory(
            (_, _, triggerConfigName) =>
            {
                resolverTriggerConfigName = triggerConfigName;
                return new StubConnectorPollingEndpointResolver();
            });
        var nameResolver = new TestNameResolver(
            name => name == "ConnectorTriggerConfigName" ? "OnNewEmail" : null);
        var factory = new ConnectorPollingListenerFactory(
            endpointResolverFactory,
            new TestPollDeliveryClientFactory(
                _ => new StubConnectorPollDeliveryClient()),
            new StubConnectorLinkedOutputClient(),
            nameResolver,
            NullLoggerFactory.Instance);

        ConnectorPollingListener listener = factory.Create(
            new ConnectorFunctionRegistration(
                "TestFunction",
                Mock.Of<ITriggeredFunctionExecutor>()),
            new ConnectorPollingOptions(
                "ConnectorNamespace",
                "%ConnectorTriggerConfigName%",
                1,
                1),
            new ConnectorConnectionOptions(
                new ResourceIdentifier(
                    "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Web/connectorGateways/test"),
                Mock.Of<TokenCredential>()));

        Assert.Equal("OnNewEmail", listener.Options.TriggerConfigName);
        Assert.Equal("OnNewEmail", resolverTriggerConfigName);
    }

    [Fact]
    public void Factory_ThrowsWhenTriggerConfigNameResolvesToEmpty()
    {
        var nameResolver = new TestNameResolver(_ => string.Empty);
        var factory = new ConnectorPollingListenerFactory(
            new TestResolverFactory(
                (_, _, _) => new StubConnectorPollingEndpointResolver()),
            new TestPollDeliveryClientFactory(
                _ => new StubConnectorPollDeliveryClient()),
            new StubConnectorLinkedOutputClient(),
            nameResolver,
            NullLoggerFactory.Instance);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(
                new ConnectorFunctionRegistration(
                    "TestFunction",
                    Mock.Of<ITriggeredFunctionExecutor>()),
                new ConnectorPollingOptions(
                    "ConnectorNamespace",
                    "%ConnectorTriggerConfigName%",
                    1,
                    1),
                new ConnectorConnectionOptions(
                    new ResourceIdentifier(
                        "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Web/connectorGateways/test"),
                    Mock.Of<TokenCredential>())));

        Assert.Contains("resolved to an empty value", exception.Message);
    }

    [Fact]
    public async Task StopAsync_DuringEndpointResolutionPreventsMessagePumpStartup()
    {
        var resolutionStarted = NewCompletionSource();
        var endpointResolver = new StubConnectorPollingEndpointResolver
        {
            ResolveAsyncHandler = async cancellationToken =>
            {
                resolutionStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Unreachable.");
            },
        };
        var deliveryClient = new StubConnectorPollDeliveryClient();
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            endpointResolver,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        Task startTask = listener.StartAsync(CancellationToken.None);
        await resolutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));
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
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
            concurrency: 1,
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
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: ConnectorPollingProtocolLimits.MaximumBatchSize,
            concurrency: int.MaxValue,
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
    public async Task Listener_GroupsMessagesIntoConcurrentBatchedInvocations()
    {
        ConnectorPollMessage[] messages =
        [
            CreateInlineMessage("message-1", "lock-1", """{"value":1}"""),
            CreateInlineMessage("message-2", "lock-2", """{"value":2}"""),
            CreateInlineMessage("message-3", "lock-3", """{"value":3}"""),
        ];
        int receiveMaxEvents = 0;
        var acknowledgementsCompleted = NewCompletionSource();
        var acknowledgedMessageIds = new List<string>();
        var acknowledgementSizes = new List<int>();
        int acknowledgementCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
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
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
            concurrency: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await bothInvocationsEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, maximumActiveInvocations);

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
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            concurrency: 2);

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
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2,
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
                    data => ((ConnectorTriggerInput)data.TriggerValue).ToPayloadJson()
                        == """{"value":"linked"}"""),
                It.IsAny<CancellationToken>()))
            .Callback(() => invocationCompleted.TrySetResult())
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            CreateEndpointResolver(),
            deliveryClient,
            linkedOutputClient);

        await listener.StartAsync(CancellationToken.None);
        await invocationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(1, downloadCount);
        Assert.Equal(1, acknowledgementCount);
    }

    [Fact]
    public async Task Listener_HydratesBatchLinkedOutputsSequentially()
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
        var acknowledgementCompleted = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            ReceiveAsyncHandler = (_, _, _) =>
                Task.FromResult(
                    new ConnectorReceiveResult([first, second], false)),
            AcknowledgeAsyncHandler = (_, locks, _) =>
            {
                acknowledgementCompleted.TrySetResult();
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
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.Is<TriggeredFunctionData>(data =>
                    ((ConnectorTriggerInput)data.TriggerValue)
                        .Events.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FunctionResult(true));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            CreateEndpointResolver(),
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 2,
            isBatched: true);

        await listener.StartAsync(CancellationToken.None);
        await acknowledgementCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);

        Assert.Equal(2, downloadCount);
        Assert.Equal(1, maximumActiveDownloads);
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
            CreateEndpointResolver(),
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
            CreateEndpointResolver(),
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
            CreateEndpointResolver(),
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
            CreateEndpointResolver(),
            deliveryClient,
            linkedOutputClient,
            maxBatchSize: 2,
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
        IConnectorPollingEndpointResolver endpointResolver,
        IConnectorPollDeliveryClient deliveryClient,
        IConnectorLinkedOutputClient linkedOutputClient,
        int maxBatchSize = 1,
        int concurrency = 1,
        bool isBatched = false,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        var registration = new ConnectorFunctionRegistration("TestFunction", executor);
        var options = new ConnectorPollingOptions(
            "ConnectorNamespace",
            "OnNewEmail",
            maxBatchSize,
            concurrency,
            isBatched);
        return new ConnectorPollingListener(
            registration,
            options,
            endpointResolver,
            deliveryClient,
            linkedOutputClient,
            NullLogger<ConnectorPollingListener>.Instance,
            delayAsync ?? WaitUntilCancelledAsync);
    }

    private static StubConnectorPollingEndpointResolver CreateEndpointResolver() =>
        new()
        {
            ResolveAsyncHandler = _ => Task.FromResult(Endpoints),
        };

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
