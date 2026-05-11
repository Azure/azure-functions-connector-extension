// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;
using Azure.Connectors.Sdk.Office365.Models;

namespace SampleApp;

/// <summary>
/// Sample Azure Function using the Connector trigger extension.
/// Receives trigger callbacks from Connector Namespace managed connectors.
/// </summary>
public class ConnectorFunctions
{
    private readonly ILogger<ConnectorFunctions> _logger;

    public ConnectorFunctions(ILogger<ConnectorFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receives Office 365 email trigger callbacks and saves to blob storage.
    /// </summary>
    [Function("OnNewEmail")]
    [BlobOutput("connector-messages/{rand-guid}.json", Connection = "AzureWebJobsStorage")]
    public string OnNewEmail(
        [ConnectorTrigger("my-connector-namespace", "email-newemail-trigger")]
        Office365OnNewEmailTriggerPayload payload)
    {
        _logger.LogInformation("Received connector trigger payload");

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }
}
