// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorPollingEndpoints
{
    private const string RuntimeHostSuffix = ".logic.azure.com";
    private const string ApiSegment = "api";
    private const string ConnectorGatewaysSegment = "connectorGateways";
    private const string TriggerConfigsSegment = "triggerConfigs";
    private const string ReceiveOperation = "receive";
    private const string AcknowledgeOperation = "acknowledge";
    private const string ApproximateQueueDepthOperation =
        "approximateQueueDepth";
    private const string InvalidEndpointMessage =
        "Connector trigger PollingEndpoint must be a valid Trigger Config polling base URL.";

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

    internal static ConnectorPollingEndpoints Create(string pollingEndpoint)
    {
        if (string.IsNullOrWhiteSpace(pollingEndpoint) ||
            !pollingEndpoint.Equals(
                pollingEndpoint.Trim(),
                StringComparison.Ordinal) ||
            !Uri.TryCreate(
                pollingEndpoint,
                UriKind.Absolute,
                out Uri? baseUri))
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

        string rawPath = GetRawPath(pollingEndpoint);
        string canonicalPath = GetRawPath(
            baseUri.GetLeftPart(UriPartial.Path));
        string rawQuery = GetRawQuery(pollingEndpoint);
        if (!rawPath.Equals(canonicalPath, StringComparison.Ordinal) ||
            !rawQuery.Equals(baseUri.Query, StringComparison.Ordinal))
        {
            throw InvalidPollingEndpoint();
        }

        ValidateRuntimeAuthority(baseUri);
        ValidateTriggerConfigPath(baseUri);

        return new ConnectorPollingEndpoints(
            AppendOperation(baseUri, rawQuery, ReceiveOperation),
            AppendOperation(baseUri, rawQuery, AcknowledgeOperation),
            AppendOperation(
                baseUri,
                rawQuery,
                ApproximateQueueDepthOperation));
    }

    public override string ToString() =>
        $"{nameof(ConnectorPollingEndpoints)} {{ " +
        $"{nameof(ReceiveUri)} = {ConnectorPollingUri.Redact(ReceiveUri)}, " +
        $"{nameof(AcknowledgeUri)} = {ConnectorPollingUri.Redact(AcknowledgeUri)}, " +
        $"{nameof(ApproximateQueueDepthUri)} = {ConnectorPollingUri.Redact(ApproximateQueueDepthUri)} }}";

    private static Uri AppendOperation(
        Uri baseUri,
        string rawQuery,
        string operation)
    {
        string basePath = baseUri
            .GetLeftPart(UriPartial.Path)
            .TrimEnd('/');
        return new Uri(
            $"{basePath}/{operation}{rawQuery}",
            UriKind.Absolute);
    }

    private static string GetRawPath(string absoluteUri)
    {
        int schemeSeparator = absoluteUri.IndexOf(
            Uri.SchemeDelimiter,
            StringComparison.Ordinal);
        int authorityEnd = absoluteUri.IndexOfAny(
            ['/', '?', '#'],
            schemeSeparator + Uri.SchemeDelimiter.Length);
        if (authorityEnd < 0 || absoluteUri[authorityEnd] != '/')
        {
            return "/";
        }

        int pathEnd = absoluteUri.IndexOfAny(
            ['?', '#'],
            authorityEnd);
        return pathEnd < 0
            ? absoluteUri[authorityEnd..]
            : absoluteUri[authorityEnd..pathEnd];
    }

    private static string GetRawQuery(string absoluteUri)
    {
        int queryStart = absoluteUri.IndexOf('?');
        return queryStart < 0
            ? string.Empty
            : absoluteUri[queryStart..];
    }

    private static void ValidateTriggerConfigPath(Uri baseUri)
    {
        string[] segments = baseUri.AbsolutePath.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5 ||
            baseUri.AbsolutePath.EndsWith('/') ||
            !segments[0].Equals(
                ApiSegment,
                StringComparison.OrdinalIgnoreCase) ||
            !segments[1].Equals(
                ConnectorGatewaysSegment,
                StringComparison.OrdinalIgnoreCase) ||
            !IsValidOpaqueSegment(segments[2]) ||
            !segments[3].Equals(
                TriggerConfigsSegment,
                StringComparison.OrdinalIgnoreCase) ||
            !IsValidOpaqueSegment(segments[4]))
        {
            throw InvalidPollingEndpoint();
        }
    }

    private static void ValidateRuntimeAuthority(Uri baseUri)
    {
        if (!baseUri.IsDefaultPort ||
            !baseUri.IdnHost.EndsWith(
                RuntimeHostSuffix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidPollingEndpoint();
        }
    }

    private static bool IsValidOpaqueSegment(string segment)
    {
        string value = Uri.UnescapeDataString(segment);
        return !string.IsNullOrWhiteSpace(value) &&
            !value.Contains('/') &&
            !value.Contains('\\');
    }

    private static InvalidOperationException InvalidPollingEndpoint(
        Exception? innerException = null) =>
        new(InvalidEndpointMessage, innerException);
}
