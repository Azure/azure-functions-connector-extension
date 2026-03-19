// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// =============================================================================
// IMPORTANT: This is the WORKER-SIDE version of ConnectorContext.
// 
// In the isolated worker model, context types must exist in BOTH packages:
// - WebJobs extension version: Used by the host process to create and
//   populate the context before serializing it to the worker process.
// - This version (Worker extension): Used by the worker process to deserialize
//   and provide the context to user functions.
//
// The two versions should have matching properties for proper serialization.
// =============================================================================

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Context object passed to functions using the ConnectorTrigger.
/// Contains the full HTTP request information including body, headers, and query parameters.
/// </summary>
public sealed class ConnectorContext
{
    /// <summary>
    /// Gets or sets the raw request body as a string.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP headers from the request.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the query string parameters from the request URL.
    /// </summary>
    public IDictionary<string, string> Query { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the HTTP method of the request (e.g., POST, PUT).
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the request URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the request body.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the request was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
