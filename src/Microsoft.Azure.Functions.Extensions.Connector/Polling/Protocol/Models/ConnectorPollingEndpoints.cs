// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Constructs Poll operation URLs from a stable-DNS hostname or a legacy gateway base URL.
/// </summary>
internal sealed class ConnectorPollingEndpoints
{
    private const string StableDnsApiPath = "/api";
    private const string LegacyGatewayPathPrefix = $"{StableDnsApiPath}/connectorGateways/";
    private const string TriggerConfigsSegment = "triggerConfigs";
    private const string ReceiveOperation = "receive";
    private const string AcknowledgeOperation = "acknowledge";
    private const string ApproximateQueueDepthOperation =
        "approximateQueueDepth";
    private const string InvalidEndpointMessage =
        "Connector connection pollingEndpoint must be https://<connector-namespace-host> or https://<host>/api/connectorGateways/<gateway-id>, without a query string.";
    private const string InvalidTriggerConfigNameMessage =
        "Connector trigger TriggerConfigName must be a valid path segment.";

    internal ConnectorPollingEndpoints(
        Uri receiveUri,
        Uri acknowledgeUri,
        Uri approximateQueueDepthUri)
    {
        ReceiveUri = ConnectorPollingUri.Validate(receiveUri, nameof(receiveUri));
        AcknowledgeUri = ConnectorPollingUri.Validate(acknowledgeUri, nameof(acknowledgeUri));
        ApproximateQueueDepthUri = ConnectorPollingUri.Validate(
            approximateQueueDepthUri,
            nameof(approximateQueueDepthUri));
    }

    internal Uri ReceiveUri { get; }

    internal Uri AcknowledgeUri { get; }

    internal Uri ApproximateQueueDepthUri { get; }

    /// <summary>
    /// Creates Receive, Acknowledge, and Approximate Queue Depth URLs.
    /// </summary>
    /// <param name="pollingEndpoint">
    /// Either <c>https://&lt;connector-namespace-host&gt;</c> for stable DNS, or
    /// <c>https://&lt;host&gt;/api/connectorGateways/&lt;gateway-id&gt;</c> for legacy routing.
    /// Query strings are not supported in either form.
    /// Explicit non-default ports are preserved.
    /// Do not include a Trigger Config, operation, or a standalone <c>/api</c> path.
    /// </param>
    /// <param name="triggerConfigName">The Trigger Config name, supplied as one path segment.</param>
    internal static ConnectorPollingEndpoints Create(
        string pollingEndpoint,
        string triggerConfigName)
    {
        // The delivery client adds maxEvents; configured bases must not supply query parameters.
        if (string.IsNullOrWhiteSpace(pollingEndpoint) ||
            !pollingEndpoint.Equals(pollingEndpoint.Trim(), StringComparison.Ordinal) ||
            pollingEndpoint.Contains('?') ||
            !Uri.TryCreate(
                pollingEndpoint,
                UriKind.Absolute,
                out Uri? baseUri) ||
            !baseUri.IsWellFormedOriginalString())
        {
            throw InvalidPollingEndpoint();
        }

        try
        {
            ConnectorPollingUri.Validate(baseUri, nameof(pollingEndpoint));
        }
        catch (ArgumentException exception)
        {
            throw InvalidPollingEndpoint(exception);
        }

        string path = baseUri.AbsolutePath;
        // Uri normalizes dot segments; reject those rather than changing the configured route.
        if (!GetOriginalPath(pollingEndpoint).Equals(path, StringComparison.Ordinal))
        {
            throw InvalidPollingEndpoint();
        }

        // Remove only one slash so repeated trailing slashes remain invalid.
        if (path.EndsWith('/'))
        {
            path = path[..^1];
        }

        string runtimePath;
        if (path.Length == 0)
        {
            // Stable-DNS endpoints supply only the authority; add the runtime API prefix.
            runtimePath = StableDnsApiPath;
        }
        else if (path.StartsWith(LegacyGatewayPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            IsValidOpaqueSegment(path[LegacyGatewayPathPrefix.Length..]))
        {
            // Legacy endpoints already include the gateway routing prefix.
            runtimePath = path;
        }
        else
        {
            throw InvalidPollingEndpoint();
        }

        string runtimeBase = $"{baseUri.GetLeftPart(UriPartial.Authority)}{runtimePath}";
        string triggerConfigSegment =
            ValidateAndEscapeTriggerConfigName(triggerConfigName);

        return new ConnectorPollingEndpoints(
            AppendOperation(runtimeBase, triggerConfigSegment, ReceiveOperation),
            AppendOperation(runtimeBase, triggerConfigSegment, AcknowledgeOperation),
            AppendOperation(
                runtimeBase,
                triggerConfigSegment,
                ApproximateQueueDepthOperation));
    }

    public override string ToString() =>
        $"{nameof(ConnectorPollingEndpoints)} {{ " +
        $"{nameof(ReceiveUri)} = {ConnectorPollingUri.Redact(ReceiveUri)}, " +
        $"{nameof(AcknowledgeUri)} = {ConnectorPollingUri.Redact(AcknowledgeUri)}, " +
        $"{nameof(ApproximateQueueDepthUri)} = {ConnectorPollingUri.Redact(ApproximateQueueDepthUri)} }}";

    private static Uri AppendOperation(
        string runtimeBase,
        string triggerConfigSegment,
        string operation)
    {
        return new Uri(
            $"{runtimeBase}/{TriggerConfigsSegment}/{triggerConfigSegment}/{operation}",
            UriKind.Absolute);
    }

    private static string GetOriginalPath(string endpoint)
    {
        // URI validation has already rejected queries and fragments, so the remainder is only the path.
        int authorityStart = endpoint.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal) +
            Uri.SchemeDelimiter.Length;
        int pathStart = endpoint.IndexOf('/', authorityStart);
        return pathStart < 0 ? "/" : endpoint[pathStart..];
    }

    private static string ValidateAndEscapeTriggerConfigName(
        string triggerConfigName)
    {
        // Require an unescaped name so it is encoded exactly once when building the path.
        if (string.IsNullOrWhiteSpace(triggerConfigName) ||
            !triggerConfigName.Equals(
                triggerConfigName.Trim(),
                StringComparison.Ordinal) ||
            triggerConfigName.Contains('%') ||
            triggerConfigName.Any(char.IsControl) ||
            !IsValidOpaqueSegment(triggerConfigName))
        {
            throw new InvalidOperationException(
                InvalidTriggerConfigNameMessage);
        }

        return Uri.EscapeDataString(triggerConfigName);
    }

    private static bool IsValidOpaqueSegment(string segment)
    {
        // Decode first so encoded separators and dot segments cannot bypass validation.
        string value = Uri.UnescapeDataString(segment);
        return !string.IsNullOrWhiteSpace(value) &&
            !value.Equals(".", StringComparison.Ordinal) &&
            !value.Equals("..", StringComparison.Ordinal) &&
            !value.Contains('/') &&
            !value.Contains('\\');
    }

    private static InvalidOperationException InvalidPollingEndpoint(
        Exception? innerException = null) =>
        new(InvalidEndpointMessage, innerException);
}
