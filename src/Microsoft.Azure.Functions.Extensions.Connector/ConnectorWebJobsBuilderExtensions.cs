// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
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

        // Register the extension config provider
        builder.AddExtension<ConnectorExtensionConfigProvider>()
            .BindOptions<ConnectorOptions>();

        builder.Services.PostConfigure(configure);

        return builder;
    }
}
