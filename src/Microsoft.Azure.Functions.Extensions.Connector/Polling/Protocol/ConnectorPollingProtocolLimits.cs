// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingProtocolLimits
{
    internal const int MaximumBatchSize = 32;

    internal const int MaximumOutputsPayloadSizeInBytes = 100 * 1024 * 1024;
}
