using Microsoft.Azure.Connectors.DirectClient.Office365;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNetIsolatedSampleApp;

/// <summary>
/// Sample Azure Function using the Connector trigger extension.
/// Receives trigger callbacks from AI Gateway managed connectors.
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
    /// Uses SDK types for strongly-typed payload binding.
    /// </summary>
    [Function("OnNewEmail")]
    [BlobOutput("connector-messages/{rand-guid}.json", Connection = "BlobStoreConnection")]
    public string OnNewEmail(
        [ConnectorTrigger(
            Connector = "office365",
            Operation = "OnNewEmailV3",
            Connection = "Office365Connection")]
        Office365OnNewEmailV3TriggerPayload payload)
    {
        var emails = payload.Body?.Value ?? [];

        foreach (var email in emails)
        {
            _logger.LogInformation("Subject: {Subject}, From: {From}",
                email.Subject, email.From);
        }

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
