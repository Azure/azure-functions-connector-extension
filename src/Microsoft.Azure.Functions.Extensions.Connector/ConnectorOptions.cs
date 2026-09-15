// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Azure.WebJobs.Hosting;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Options for the Connector extension.
/// </summary>
public sealed class ConnectorOptions : IOptionsFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Gets or sets the default number of events supplied to one function invocation.
    /// Valid values are one through 32.
    /// </summary>
    public int DefaultBatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the default maximum number of concurrent function invocations per worker.
    /// The value must be greater than zero.
    /// </summary>
    public int DefaultConcurrency { get; set; } = 16;

    string IOptionsFormatter.Format() => JsonSerializer.Serialize(
        new
        {
            DefaultBatchSize,
            DefaultConcurrency,
        },
        SerializerOptions);
}
