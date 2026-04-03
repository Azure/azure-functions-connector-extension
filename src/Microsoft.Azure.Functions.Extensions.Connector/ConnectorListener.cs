// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Listener for the Connector trigger.
/// Registers the function with the config provider for HTTP routing.
/// </summary>
internal sealed class ConnectorListener : IListener
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorFunctionRegistration _registration;

    public ConnectorListener(
        ConnectorExtensionConfigProvider configProvider,
        ConnectorFunctionRegistration registration)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));

        // Register as part of create time initialization
        _configProvider.RegisterFunction(_registration);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Cancel() { }
    public void Dispose() { }
}
