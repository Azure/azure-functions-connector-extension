// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingUri
{
    internal static Uri Parse(string? value, string propertyPath)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            throw new JsonException(
                $"Connector Poll response property '{propertyPath}' must be an absolute HTTPS URI.");
        }

        Validate(uri, propertyPath, static message => new JsonException(message));
        return uri;
    }

    internal static Uri Validate(Uri? uri, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(uri, parameterName);
        Validate(uri, parameterName, static message => new ArgumentException(message));
        return uri;
    }

    internal static string Redact(Uri uri)
    {
        string value = $"{uri.GetLeftPart(UriPartial.Authority)}/[REDACTED]";
        return string.IsNullOrEmpty(uri.Query)
            ? value
            : $"{value}?[REDACTED]";
    }

    private static void Validate(
        Uri uri,
        string name,
        Func<string, Exception> createException)
    {
        if (!uri.IsAbsoluteUri ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw createException(
                $"Connector Poll URI '{name}' must be absolute HTTPS and must not contain user information or a fragment.");
        }
    }
}
