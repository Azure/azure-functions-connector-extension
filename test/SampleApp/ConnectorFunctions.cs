using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Extensions.Logging;

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
        string payload)
    {
        _logger.LogInformation("Received connector trigger payload: {Length} bytes", payload?.Length ?? 0);

        return payload ?? "{}";
    }
}
