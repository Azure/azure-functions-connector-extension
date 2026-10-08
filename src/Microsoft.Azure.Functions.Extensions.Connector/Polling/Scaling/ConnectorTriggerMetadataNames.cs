// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorTriggerMetadataNames
{
    internal const string Connection = "connection";
    internal const string TargetPendingEventThreshold = "targetPendingEventThreshold";
    internal const string DeliveryMode = "deliveryMode";
    internal const string TriggerConfigName = "triggerConfigName";
}
