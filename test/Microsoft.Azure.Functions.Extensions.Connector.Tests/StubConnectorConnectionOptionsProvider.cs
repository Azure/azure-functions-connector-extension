// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

internal sealed class StubConnectorConnectionOptionsProvider(
    ConnectorConnectionOptions? options = null) : IConnectorConnectionOptionsProvider
{
    public ConnectorConnectionOptions Get(string connectionName) =>
        options ?? throw new InvalidOperationException(
            "No Connector connection options were configured for this test.");
}
