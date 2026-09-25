// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed record ConnectorTriggerMetrics(
    long ApproximateQueueDepth,
    DateTime SampledAtUtc);
