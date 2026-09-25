// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;

namespace Microsoft.Azure.Functions.Extensions.Connector;

public static class ConnectorWebJobsBuilderExtensions
{
    private static readonly string PollDeliveryModeValue =
        ((int)ConnectorTriggerDeliveryMode.Poll).ToString(
            CultureInfo.InvariantCulture);

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
            ConnectorPollDeliveryClient.HttpClientName,
            client =>
                client.Timeout = ConnectorPollingHttpConstants.RuntimeTimeout)
            .RemoveAllLoggers();
        builder.Services.AddHttpClient(
            ConnectorLinkedOutputClient.HttpClientName,
            client =>
                client.Timeout =
                    ConnectorPollingHttpConstants.LinkedOutputTimeout)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
            })
            .RemoveAllLoggers();
        builder.Services.TryAddSingleton<ConnectorConnectionOptionsProvider>();
        builder.Services.TryAddSingleton<IConnectorConnectionOptionsProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<ConnectorConnectionOptionsProvider>());
        builder.Services.TryAddSingleton<IConnectorQueueDepthClientFactory, ConnectorQueueDepthClientFactory>();
        builder.Services.TryAddSingleton<IConnectorPollDeliveryClientFactory, ConnectorPollDeliveryClientFactory>();
        builder.Services.TryAddSingleton<IConnectorLinkedOutputClient, ConnectorLinkedOutputClient>();
        builder.Services.TryAddSingleton<ConnectorLinkedOutputInvocationLimiter>();
        builder.Services.TryAddSingleton<IConnectorPollingListenerFactory, ConnectorPollingListenerFactory>();

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

        string? deliveryMode = triggerMetadata.Metadata?[
            ConnectorTriggerMetadataNames.DeliveryMode]?.ToString();
        if (!string.Equals(
                deliveryMode,
                nameof(ConnectorTriggerDeliveryMode.Poll),
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                deliveryMode,
                PollDeliveryModeValue,
                StringComparison.Ordinal))
        {
            return builder;
        }

        builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider =>
            new ConnectorScalerProvider(serviceProvider, triggerMetadata));
        return builder;
    }
}
