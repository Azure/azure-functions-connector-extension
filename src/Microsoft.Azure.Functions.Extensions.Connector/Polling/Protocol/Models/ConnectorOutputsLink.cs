// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorOutputsLink
{
    internal ConnectorOutputsLink(Uri uri)
    {
        Uri = ConnectorPollingUri.Validate(uri, nameof(uri));
    }

    internal Uri Uri { get; }

    public override string ToString() =>
        $"{nameof(ConnectorOutputsLink)} {{ " +
        $"{nameof(Uri)} = {ConnectorPollingUri.Redact(Uri)} }}";
}
