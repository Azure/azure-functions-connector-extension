// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Azure;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal interface IConnectorConnectionOptionsProvider
{
    ConnectorConnectionOptions Get(
        string connectionName,
        AzureComponentFactory? componentFactory = null);
}
