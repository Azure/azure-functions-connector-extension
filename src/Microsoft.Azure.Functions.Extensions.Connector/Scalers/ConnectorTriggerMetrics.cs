// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;


internal sealed class ConnectorTriggerMetrics
{    
    public int PendingEvents { get; set; }
    
    public DateTime SampledAtUtc { get; set; } = DateTime.UtcNow;
}
