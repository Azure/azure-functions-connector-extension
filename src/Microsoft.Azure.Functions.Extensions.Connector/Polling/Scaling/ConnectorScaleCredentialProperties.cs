// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Property names used to inject credentials into Connector scale metadata.
/// </summary>
public static class ConnectorScaleCredentialProperties
{
    /// <summary>
    /// Gets the property name for the API Hub token credential.
    /// </summary>
    public const string ApiHubTokenCredential =
        "Connector.ApiHubTokenCredential";
}
