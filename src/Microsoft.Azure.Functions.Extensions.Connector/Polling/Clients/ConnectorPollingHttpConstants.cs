// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingHttpConstants
{
    internal const string ApiHubScope =
        "https://apihub.azure.com/.default";
    internal const string RuntimeClientName = "ConnectorPollingRuntime";
    internal const string LinkedOutputClientName =
        "ConnectorPollingLinkedOutputs";
    internal const string MaxEventsQueryParameter = "maxEvents";
    internal const string MoreMessagesAvailableHeader =
        "x-ms-more-messages-available";
    internal const string BearerAuthenticationScheme = "Bearer";
    internal const string Utf8CharacterSet = "utf-8";
    internal const int MaximumSafeOperationAttempts = 3;

    // TODO: Move Poll transport timeout and retry policy to the future
    // Connectors Polling SDK. The extension can map user overrides into SDK
    // options after Connector Namespace publishes its operational guidance.
    internal static readonly TimeSpan RuntimeTimeout =
        TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan LinkedOutputTimeout =
        TimeSpan.FromMinutes(2);
    internal static readonly TimeSpan BaseRetryDelay =
        TimeSpan.FromMilliseconds(100);
}
