// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Connector;

public static class ConnectorWebJobsBuilderExtensions
{
    /// <summary>
    /// Adds the Connector extension.
    /// </summary>
    public static IWebJobsBuilder AddConnector(this IWebJobsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConnector(_ => { });
    }

    /// <summary>
    /// Adds the Connector extension and applies <paramref name="configure"/> after host.json binding.
    /// </summary>
    public static IWebJobsBuilder AddConnector(this IWebJobsBuilder builder, Action<ConnectorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.TryAddSingleton<ConnectorHttpRequestProcessor>();

        builder.AddExtension<ConnectorExtensionConfigProvider>()
            .BindOptions<ConnectorOptions>();

        builder.Services.PostConfigure(configure);

        return builder;
    }

    // Called reflectively by Scale Monitor RegisterExtensionHelper.AddTriggerScale.
    // Signature is load-bearing: must be internal static (IWebJobsBuilder, TriggerMetadata).
    internal static IWebJobsBuilder AddConnectorScaleForTrigger(
        this IWebJobsBuilder builder,
        TriggerMetadata triggerMetadata)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(triggerMetadata);

        builder.Services.AddSingleton<ITargetScalerProvider>(sp =>
            new ConnectorScalerProvider(sp, triggerMetadata));

        return builder;
    }
}
