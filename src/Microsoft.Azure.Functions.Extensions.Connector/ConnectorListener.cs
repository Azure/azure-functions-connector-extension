// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Listener for the Connector trigger.
/// 
/// Unlike traditional listeners that poll or subscribe to events, this listener
/// is essentially a stub. The actual HTTP handling is done by the
/// <see cref="ConnectorExtensionConfigProvider"/> which implements
/// IAsyncConverter&lt;HttpRequestMessage, HttpResponseMessage&gt;.
/// 
/// This listener's main job is to register itself with the extension config provider
/// so that incoming HTTP requests can be routed to the correct function executor.
/// </summary>
internal sealed class ConnectorListener : IListener
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly string _functionName;

    /// <summary>
    /// Gets the function executor for this listener.
    /// </summary>
    public ITriggeredFunctionExecutor Executor { get; }

    /// <summary>
    /// Creates a new ConnectorListener.
    /// </summary>
    /// <param name="executor">The function executor to invoke when requests arrive.</param>
    /// <param name="configProvider">The extension config provider to register with.</param>
    /// <param name="functionName">The name of the function this listener is for.</param>
    public ConnectorListener(
        ITriggeredFunctionExecutor executor,
        ConnectorExtensionConfigProvider configProvider,
        string functionName)
    {
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));

        // Register this listener with the config provider during construction
        // This allows the config provider to route HTTP requests to this function
        _configProvider.AddListener(_functionName, this);
    }

    /// <summary>
    /// Starts the listener. This is a no-op since HTTP routing is handled by the host.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // No-op: HTTP requests are routed by the Azure Functions host
        // through the IAsyncConverter interface implemented by ConnectorExtensionConfigProvider
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the listener. This is a no-op.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cancels the listener. This is a no-op.
    /// </summary>
    public void Cancel()
    {
    }

    /// <summary>
    /// Disposes the listener and removes it from the config provider.
    /// </summary>
    public void Dispose()
    {
        _configProvider.RemoveListener(_functionName);
    }
}
