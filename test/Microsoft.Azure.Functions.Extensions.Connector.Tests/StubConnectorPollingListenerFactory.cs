// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

internal sealed class StubConnectorPollingListenerFactory :
    IConnectorPollingListenerFactory
{
    public ConnectorPollingListener Create(
        ConnectorFunctionRegistration registration,
        ConnectorPollingOptions options,
        ConnectorConnectionOptions connectionOptions) =>
        new(
            registration,
            options,
            new StubConnectorPollingEndpointResolver(),
            new StubConnectorPollDeliveryClient(),
            new StubConnectorLinkedOutputClient(),
            new ConnectorLinkedOutputInvocationLimiter(),
            NullLogger<ConnectorPollingListener>.Instance);
}

internal sealed class StubConnectorPollingEndpointResolver :
    IConnectorPollingEndpointResolver
{
    internal Func<CancellationToken, Task<ConnectorPollingEndpoints>> ResolveAsyncHandler { get; set; } =
        _ => throw new NotSupportedException();

    public Task<ConnectorPollingEndpoints> ResolveAsync(
        CancellationToken cancellationToken = default) =>
        ResolveAsyncHandler(cancellationToken);
}

internal sealed class StubConnectorPollDeliveryClient :
    IConnectorPollDeliveryClient
{
    internal Func<
        ConnectorPollingEndpoints,
        int,
        CancellationToken,
        Task<ConnectorReceiveResult>> ReceiveAsyncHandler
    { get; set; } =
        (_, _, _) => throw new NotSupportedException();

    internal Func<
        ConnectorPollingEndpoints,
        IReadOnlyList<ConnectorMessageLock>,
        CancellationToken,
        Task<ConnectorAcknowledgeResult>> AcknowledgeAsyncHandler
    { get; set; } =
        (_, _, _) => throw new NotSupportedException();

    public Task<ConnectorReceiveResult> ReceiveAsync(
        ConnectorPollingEndpoints endpoints,
        int maxEvents,
        CancellationToken cancellationToken) =>
        ReceiveAsyncHandler(endpoints, maxEvents, cancellationToken);

    public Task<ConnectorAcknowledgeResult> AcknowledgeAsync(
        ConnectorPollingEndpoints endpoints,
        IReadOnlyList<ConnectorMessageLock> messages,
        CancellationToken cancellationToken) =>
        AcknowledgeAsyncHandler(endpoints, messages, cancellationToken);

}

internal sealed class StubConnectorLinkedOutputClient :
    IConnectorLinkedOutputClient
{
    internal Func<
        ConnectorOutputsLink,
        int,
        CancellationToken,
        Task<BinaryData>> DownloadAsyncHandler
    { get; set; } =
        (_, _, _) => throw new NotSupportedException();

    public Task<BinaryData> DownloadAsync(
        ConnectorOutputsLink outputsLink,
        int maximumPayloadSizeInBytes,
        CancellationToken cancellationToken) =>
        DownloadAsyncHandler(
            outputsLink,
            maximumPayloadSizeInBytes,
            cancellationToken);
}
