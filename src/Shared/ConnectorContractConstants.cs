// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Shared;

internal static class ConnectorMediaTypes
{
    internal const string Json = "application/json";
}

internal static class ConnectorBindingDataContract
{
    internal const string Version = "1.0";
    internal const string Source = "AzureConnectorEvent";
    internal const string ContentType = ConnectorMediaTypes.Json;

    internal static class PropertyNames
    {
        internal const string Data = "data";
        internal const string DeliveryMode = "deliveryMode";
        internal const string MessageId = "messageId";
    }
}
