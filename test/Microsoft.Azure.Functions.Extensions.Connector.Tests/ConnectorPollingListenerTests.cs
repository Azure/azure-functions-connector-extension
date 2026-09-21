// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingListenerTests
{
    private static readonly ConnectorPollingEndpoints Endpoints = new(
        new Uri("https://runtime.example/receive"),
        new Uri("https://runtime.example/acknowledge"),
        new Uri("https://runtime.example/hasMessages"),
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
    public async Task StartAsync_ThrowsWhenMaxBatchSizeIsNotOne()
    {
        ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            new StubConnectorPollingEndpointResolver(),
            new StubConnectorPollDeliveryClient(),
            new StubConnectorLinkedOutputClient(),
            maxBatchSize: 2);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => listener.StartAsync(CancellationToken.None));

        Assert.Contains("MaxBatchSize", exception.Message);
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
        int hasMessagesCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            HasMessagesAsyncHandler = (_, _) =>
            {
                Interlocked.Increment(ref hasMessagesCount);
                return Task.FromResult(false);
            },
        };
        using ConnectorPollingListener listener = CreateListener(
            Mock.Of<ITriggeredFunctionExecutor>(),
            endpointResolver,
            deliveryClient,
            new StubConnectorLinkedOutputClient());

        Task startTask = listener.StartAsync(CancellationToken.None);
        await resolutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await listener.StopAsync(CancellationToken.None);
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, hasMessagesCount);
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
            HasMessagesAsyncHandler = (_, _) => Task.FromResult(true),
            ReceiveAsyncHandler = (_, maxEvents, _) =>
            {
                Assert.Equal(1, maxEvents);
                return Task.FromResult(
                    new ConnectorReceiveResult([first, second], false));
            },
        };
        var executor = new Mock<ITriggeredFunctionExecutor>();

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient(),
            concurrency: 1,
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
    public async Task Listener_ProcessesConcurrentSingleMessageInvocations()
    {
        ConnectorPollMessage first = CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        ConnectorPollMessage second = CreateInlineMessage("message-2", "lock-2", """{"value":2}""");
        int receiveMaxEvents = 0;
        int acknowledgementCount = 0;
        var acknowledgementsCompleted = NewCompletionSource();
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            HasMessagesAsyncHandler = (_, _) => Task.FromResult(true),
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
    public async Task Listener_DoesNotAcknowledgeFailedInvocation()
    {
        ConnectorPollMessage message =
            CreateInlineMessage("message-1", "lock-1", """{"value":1}""");
        int acknowledgementCount = 0;
        StubConnectorPollDeliveryClient deliveryClient =
            CreateSingleReceiveClient(message);
        deliveryClient.AcknowledgeAsyncHandler = (_, locks, _) =>
        {
            Interlocked.Increment(ref acknowledgementCount);
            return Task.FromResult(Acknowledged(locks.Single()));
        };
        var executionCompleted = NewCompletionSource();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor
            .Setup(value => value.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => executionCompleted.TrySetResult())
            .ReturnsAsync(new FunctionResult(
                new InvalidOperationException("Function failed.")));

        using ConnectorPollingListener listener = CreateListener(
            executor.Object,
            CreateEndpointResolver(),
            deliveryClient,
            new StubConnectorLinkedOutputClient());

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
                    data => (string)data.TriggerValue == """{"value":"linked"}"""),
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
    public async Task Listener_DoesNotReceiveWhenQueueIsEmpty()
    {
        int receiveCount = 0;
        var deliveryClient = new StubConnectorPollDeliveryClient
        {
            HasMessagesAsyncHandler = (_, _) => Task.FromResult(false),
            ReceiveAsyncHandler = (_, _, _) =>
            {
                Interlocked.Increment(ref receiveCount);
                throw new InvalidOperationException("Receive must not be called.");
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

        Assert.Equal(0, receiveCount);
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

    private static ConnectorPollingListener CreateListener(
        ITriggeredFunctionExecutor executor,
        IConnectorPollingEndpointResolver endpointResolver,
        IConnectorPollDeliveryClient deliveryClient,
        IConnectorLinkedOutputClient linkedOutputClient,
        int maxBatchSize = 1,
        int concurrency = 1,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        var registration = new ConnectorFunctionRegistration("TestFunction", executor);
        var options = new ConnectorPollingOptions(
            "ConnectorNamespace",
            "OnNewEmail",
            maxBatchSize,
            concurrency);
        var connectionOptions = new ConnectorConnectionOptions(
            new ResourceIdentifier(
                "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns"),
            Mock.Of<TokenCredential>());

        return new ConnectorPollingListener(
            registration,
            options,
            connectionOptions,
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
            HasMessagesAsyncHandler = (_, _) => Task.FromResult(true),
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
        new([
            new ConnectorAcknowledgeItemResult(
                messageLock.MessageId,
                ConnectorAcknowledgeStatus.Acknowledged),
        ]);

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
