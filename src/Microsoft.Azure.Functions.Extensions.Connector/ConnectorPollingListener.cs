// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Placeholder listener for Connector Namespace Poll delivery.
/// </summary>
internal sealed class ConnectorPollingListener : IListener
{
    internal ConnectorPollingListener(
        ConnectorFunctionRegistration registration,
        ConnectorPollingOptions options)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    internal ConnectorFunctionRegistration Registration { get; }

    internal ConnectorPollingOptions Options { get; }

    public Task StartAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Connector trigger Poll delivery is not implemented yet.");

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Cancel()
    {
    }

    public void Dispose()
    {
    }
}
