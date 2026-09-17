// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Extension methods for Connector integration with Azure Functions.
/// </summary>
public static class ConnectorWebJobsBuilderExtensions
{
    /// <summary>
    /// Adds the Connector extension to the provided <see cref="IWebJobsBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
    /// <returns>The configured builder for chaining.</returns>
    public static IWebJobsBuilder AddConnector(this IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConnector(_ => { });
    }

    /// <summary>
    /// Adds the Connector extension and configures its options.
    /// </summary>
    /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
    /// <param name="configure">An action that configures Connector options.</param>
    /// <returns>The configured builder for chaining.</returns>
    public static IWebJobsBuilder AddConnector(
        this IWebJobsBuilder builder,
        Action<ConnectorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        // Register the HTTP request processor as a singleton
        builder.Services.TryAddSingleton<ConnectorHttpRequestProcessor>();
        builder.Services.AddAzureClientsCore();
        builder.Services.AddHttpClient(
            ConnectorPollingEndpointResolver.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddHttpClient(
            ConnectorQueueDepthClient.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.TryAddSingleton<IConnectorPollingConnectionFactory, ConnectorPollingConnectionFactory>();
        builder.Services.TryAddSingleton<IConnectorPollingEndpointResolverFactory, ConnectorPollingEndpointResolverFactory>();
        builder.Services.TryAddSingleton<IConnectorQueueDepthClientFactory, ConnectorQueueDepthClientFactory>();

        // Register the extension config provider
        builder.AddExtension<ConnectorExtensionConfigProvider>()
            .BindOptions<ConnectorOptions>();

        builder.Services.PostConfigure(configure);

        return builder;
    }

    // Called reflectively by Scale Monitor RegisterExtensionHelper.AddTriggerScale.
    // The internal static (IWebJobsBuilder, TriggerMetadata) signature is load-bearing.
    internal static IWebJobsBuilder AddConnectorScaleForTrigger(
        this IWebJobsBuilder builder,
        TriggerMetadata triggerMetadata)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(triggerMetadata);

        string? deliveryMode = triggerMetadata.Metadata?["deliveryMode"]?.ToString();
        if (!string.Equals(deliveryMode, "Poll", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(deliveryMode, "1", StringComparison.Ordinal))
        {
            return builder;
        }

        builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider =>
            new ConnectorScalerProvider(serviceProvider, triggerMetadata));
        return builder;
    }
}
