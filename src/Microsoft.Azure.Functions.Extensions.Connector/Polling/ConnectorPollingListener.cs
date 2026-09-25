// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorPollingListenerFactory
{
    ConnectorPollingListener Create(
        ConnectorFunctionRegistration registration,
        ConnectorPollingOptions options,
        ConnectorConnectionOptions connectionOptions);
}

internal sealed class ConnectorPollingListenerFactory(
    IConnectorPollDeliveryClientFactory deliveryClientFactory,
    IConnectorLinkedOutputClient linkedOutputClient,
    ConnectorLinkedOutputInvocationLimiter linkedOutputInvocationLimiter,
    INameResolver nameResolver,
    ILoggerFactory loggerFactory) : IConnectorPollingListenerFactory
{
    private readonly IConnectorPollDeliveryClientFactory _deliveryClientFactory =
        deliveryClientFactory ?? throw new ArgumentNullException(nameof(deliveryClientFactory));
    private readonly IConnectorLinkedOutputClient _linkedOutputClient =
        linkedOutputClient ?? throw new ArgumentNullException(nameof(linkedOutputClient));
    private readonly ConnectorLinkedOutputInvocationLimiter _linkedOutputInvocationLimiter =
        linkedOutputInvocationLimiter
        ?? throw new ArgumentNullException(nameof(linkedOutputInvocationLimiter));
    private readonly INameResolver _nameResolver =
        nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public ConnectorPollingListener Create(
        ConnectorFunctionRegistration registration,
        ConnectorPollingOptions options,
        ConnectorConnectionOptions connectionOptions)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionOptions);

        string? pollingEndpoint =
            _nameResolver.ResolveWholeString(options.PollingEndpoint) ??
            options.PollingEndpoint;
        if (string.IsNullOrWhiteSpace(pollingEndpoint))
        {
            throw new InvalidOperationException(
                "Connector Poll PollingEndpoint resolved to an empty value.");
        }

        ConnectorPollingOptions resolvedOptions =
            options with { PollingEndpoint = pollingEndpoint };
        return new ConnectorPollingListener(
            registration,
            resolvedOptions,
            ConnectorPollingEndpoints.Create(
                resolvedOptions.PollingEndpoint),
            _deliveryClientFactory.Create(connectionOptions.Credential),
            _linkedOutputClient,
            _linkedOutputInvocationLimiter,
            _loggerFactory.CreateLogger<ConnectorPollingListener>());
    }
}

/// <summary>
/// Connector Namespace Poll listener supporting bounded concurrent
/// single-message or batched function invocations.
/// </summary>
internal sealed class ConnectorPollingListener : IListener
{
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan InitialFailureDelay =
        TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumFailureDelay =
        TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaximumJitter = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    private readonly ConnectorPollingEndpoints _endpoints;
    private readonly IConnectorPollDeliveryClient _deliveryClient;
    private readonly IConnectorLinkedOutputClient _linkedOutputClient;
    private readonly ConnectorLinkedOutputInvocationLimiter _linkedOutputInvocationLimiter;
    private readonly ILogger<ConnectorPollingListener> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly object _lifecycleLock = new();

    private CancellationTokenSource? _receiveCancellation;
    private CancellationTokenSource? _processingCancellation;
    private Task? _messagePump;
    private bool _disposed;

    internal ConnectorPollingListener(
        ConnectorFunctionRegistration registration,
        ConnectorPollingOptions options,
        ConnectorPollingEndpoints endpoints,
        IConnectorPollDeliveryClient deliveryClient,
        IConnectorLinkedOutputClient linkedOutputClient,
        ConnectorLinkedOutputInvocationLimiter linkedOutputInvocationLimiter,
        ILogger<ConnectorPollingListener> logger,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _deliveryClient = deliveryClient ?? throw new ArgumentNullException(nameof(deliveryClient));
        _linkedOutputClient = linkedOutputClient ?? throw new ArgumentNullException(nameof(linkedOutputClient));
        _linkedOutputInvocationLimiter = linkedOutputInvocationLimiter
            ?? throw new ArgumentNullException(nameof(linkedOutputInvocationLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delayAsync = delayAsync ?? DelayWithJitterAsync;
    }

    internal ConnectorFunctionRegistration Registration { get; }

    internal ConnectorPollingOptions Options { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lifecycleLock)
        {
            ThrowIfDisposed();
            if (_messagePump is not null || _receiveCancellation is not null)
            {
                return Task.CompletedTask;
            }

            var receiveCancellation = new CancellationTokenSource();
            var processingCancellation = new CancellationTokenSource();
            _receiveCancellation = receiveCancellation;
            _processingCancellation = processingCancellation;
            _messagePump = RunMessagePumpAsync(
                _endpoints,
                receiveCancellation.Token,
                processingCancellation.Token);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task? messagePump;
        CancellationTokenSource? processingCancellation;
        lock (_lifecycleLock)
        {
            _receiveCancellation?.Cancel();
            messagePump = _messagePump;
            processingCancellation = _processingCancellation;
        }

        if (messagePump is null)
        {
            return;
        }

        Task timeout = Task.Delay(ShutdownTimeout, cancellationToken);
        if (await Task.WhenAny(messagePump, timeout).ConfigureAwait(false) != messagePump)
        {
            processingCancellation?.Cancel();
        }

        await messagePump.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Cancel()
    {
        lock (_lifecycleLock)
        {
            _receiveCancellation?.Cancel();
            _processingCancellation?.Cancel();
        }
    }

    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _receiveCancellation?.Cancel();
            _processingCancellation?.Cancel();
            _receiveCancellation?.Dispose();
            _processingCancellation?.Dispose();
        }
    }

    private async Task RunMessagePumpAsync(
        ConnectorPollingEndpoints endpoints,
        CancellationToken receiveCancellationToken,
        CancellationToken processingCancellationToken)
    {
        var activeInvocations = new HashSet<Task>();
        TimeSpan failureDelay = InitialFailureDelay;
        try
        {
            while (!receiveCancellationToken.IsCancellationRequested)
            {
                activeInvocations.RemoveWhere(static task => task.IsCompleted);
                int availableInvocationSlots =
                    Options.Concurrency - activeInvocations.Count;
                if (availableInvocationSlots <= 0)
                {
                    await Task.WhenAny(activeInvocations)
                        .WaitAsync(receiveCancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                try
                {
                    int maxEvents = CalculateMaxEvents(
                        availableInvocationSlots,
                        Options.MaxBatchSize);
                    ConnectorReceiveResult receiveResult =
                        await _deliveryClient.ReceiveAsync(
                            endpoints,
                            maxEvents,
                            receiveCancellationToken).ConfigureAwait(false);
                    failureDelay = InitialFailureDelay;
                    if (receiveResult.Messages.Count > maxEvents)
                    {
                        throw new ConnectorPollDeliveryException(
                            $"Connector Receive returned {receiveResult.Messages.Count} messages when at most {maxEvents} were requested.");
                    }

                    foreach (ConnectorPollMessage[] batch in
                        receiveResult.Messages.Chunk(Options.MaxBatchSize))
                    {
                        activeInvocations.Add(ProcessBatchAsync(
                            endpoints,
                            batch,
                            processingCancellationToken));
                    }

                    if (receiveResult.Messages.Count == 0 ||
                        !receiveResult.MoreMessagesAvailable)
                    {
                        await _delayAsync(
                            EmptyQueueDelay,
                            receiveCancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                    when (receiveCancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Connector Poll listener cycle failed for function {FunctionName}.",
                        Registration.FunctionName);
                    await _delayAsync(
                        failureDelay,
                        receiveCancellationToken).ConfigureAwait(false);
                    failureDelay = TimeSpan.FromTicks(
                        Math.Min(
                            failureDelay.Ticks * 2,
                            MaximumFailureDelay.Ticks));
                }
            }
        }
        catch (OperationCanceledException)
            when (receiveCancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await Task.WhenAll(activeInvocations).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatchAsync(
        ConnectorPollingEndpoints endpoints,
        IReadOnlyList<ConnectorPollMessage> messages,
        CancellationToken cancellationToken)
    {
        ConnectorPollMessage[] inlineMessages = messages
            .Where(static message => message.Outputs is not null)
            .ToArray();
        if (inlineMessages.Length > 0)
        {
            await ProcessInvocationBatchAsync(
                endpoints,
                inlineMessages,
                cancellationToken).ConfigureAwait(false);
        }

        foreach (ConnectorPollMessage linkedMessage in messages.Where(
            static message => message.OutputsLink is not null))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await ProcessInvocationBatchAsync(
                endpoints,
                [linkedMessage],
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessInvocationBatchAsync(
        ConnectorPollingEndpoints endpoints,
        ConnectorPollMessage[] messages,
        CancellationToken cancellationToken)
    {
        IDisposable? linkedOutputLease = null;
        try
        {
            if (messages.Length == 1 && messages[0].OutputsLink is not null)
            {
                linkedOutputLease =
                    await _linkedOutputInvocationLimiter.AcquireAsync(
                        cancellationToken).ConfigureAwait(false);
            }

            var preparedMessages = new List<PreparedMessage>(messages.Length);
            foreach (ConnectorPollMessage message in messages)
            {
                PreparedMessage? preparedMessage =
                    await PrepareMessageAsync(
                        message,
                        cancellationToken).ConfigureAwait(false);
                if (preparedMessage is not null)
                {
                    preparedMessages.Add(preparedMessage);
                }
            }

            if (preparedMessages.Count == 0)
            {
                return;
            }

            var triggerData = new TriggeredFunctionData
            {
                TriggerValue = Options.IsBatched
                    ? ConnectorTriggerInput.FromBatch(
                        preparedMessages
                            .Select(static item => item.Input)
                            .ToArray())
                    : ConnectorTriggerInput.FromSingle(
                        preparedMessages[0].Input.Outputs,
                        preparedMessages[0].Input.MessageId,
                        ConnectorTriggerDeliveryMode.Poll),
            };
            FunctionResult result = await Registration.Executor.TryExecuteAsync(
                triggerData,
                cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _logger.LogError(
                    result.Exception,
                    "Connector Poll function {FunctionName} failed; {MessageCount} messages will not be acknowledged.",
                    Registration.FunctionName,
                    preparedMessages.Count);
                return;
            }

            ConnectorMessageLock[] messageLocks = preparedMessages
                .Select(static item => item.MessageLock)
                .ToArray();
            ConnectorAcknowledgeResult acknowledgeResult =
                await _deliveryClient.AcknowledgeAsync(
                    endpoints,
                    messageLocks,
                    cancellationToken).ConfigureAwait(false);
            foreach (ConnectorAcknowledgeItemResult itemResult in
                acknowledgeResult.Results)
            {
                if (!itemResult.IsAcknowledged)
                {
                    _logger.LogWarning(
                        "Connector Poll acknowledgement returned status {Status} for function {FunctionName}.",
                        itemResult.Status.IsKnown ? itemResult.Status.ToString() : "Unknown",
                        Registration.FunctionName);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Connector Poll batch processing was cancelled for function {FunctionName}.",
                Registration.FunctionName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Connector Poll batch processing failed for function {FunctionName}; the messages will not be acknowledged.",
                Registration.FunctionName);
        }
        finally
        {
            linkedOutputLease?.Dispose();
        }
    }

    private async Task<PreparedMessage?> PrepareMessageAsync(
        ConnectorPollMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            BinaryData outputs = message.Outputs ??
                await _linkedOutputClient.DownloadAsync(
                    message.OutputsLink!,
                    ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes,
                    cancellationToken).ConfigureAwait(false);
            return new PreparedMessage(
                new ConnectorTriggerEventInput(
                    outputs,
                    message.MessageId,
                    ConnectorTriggerDeliveryMode.Poll),
                message.MessageLock);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Connector Poll message hydration failed for function {FunctionName}; the message will not be acknowledged.",
                Registration.FunctionName);
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static Task DelayWithJitterAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        int maximumJitterMilliseconds = checked((int)MaximumJitter.TotalMilliseconds);
        TimeSpan jitter = TimeSpan.FromMilliseconds(
            Random.Shared.Next(maximumJitterMilliseconds + 1));
        return Task.Delay(delay + jitter, cancellationToken);
    }

    private static int CalculateMaxEvents(
        int availableInvocationSlots,
        int maxBatchSize)
    {
        int maximumBatchSize =
            ConnectorPollingProtocolLimits.MaximumBatchSize;
        return availableInvocationSlots >=
            (maximumBatchSize + maxBatchSize - 1) / maxBatchSize
                ? maximumBatchSize
                : availableInvocationSlots * maxBatchSize;
    }

    private sealed record PreparedMessage(
        ConnectorTriggerEventInput Input,
        ConnectorMessageLock MessageLock);
}
