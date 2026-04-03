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

        // Register the HTTP request processor as a singleton
        builder.Services.TryAddSingleton<ConnectorHttpRequestProcessor>();

        // Register the extension config provider
        builder.AddExtension<ConnectorExtensionConfigProvider>();

        return builder;
    }
}
