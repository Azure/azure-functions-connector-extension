using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;
using Microsoft.Extensions.Logging;

namespace DotNetIsolatedSampleApp;

/// <summary>
/// Sample functions demonstrating different ways to use the ConnectorTrigger.
/// 
/// To invoke these functions, send HTTP POST requests to:
/// POST http://localhost:7071/runtime/webhooks/connector?functionName={FunctionName}
/// </summary>
public class ConnectorFunctions
{
    private readonly ILogger<ConnectorFunctions> _logger;

    public ConnectorFunctions(ILogger<ConnectorFunctions> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // Example 1: Using ConnectorContext for full request access
    // =========================================================================
    /// <summary>
    /// Receives the full ConnectorContext with body, headers, query params, etc.
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=FullContext&customParam=hello" \
    ///      -H "Content-Type: application/json" \
    ///      -H "X-Custom-Header: my-value" \
    ///      -d '{"message": "Hello World"}'
    /// </summary>
    [Function("FullContext")]
    public void RunWithFullContext([ConnectorTrigger] ConnectorContext context)
    {
        _logger.LogInformation("=== ConnectorContext Example ===");
        _logger.LogInformation("Body: {Body}", context.Body);
        _logger.LogInformation("Method: {Method}", context.Method);
        _logger.LogInformation("Content-Type: {ContentType}", context.ContentType);
        _logger.LogInformation("Timestamp: {Timestamp}", context.Timestamp);
        
        _logger.LogInformation("Headers:");
        foreach (var header in context.Headers)
        {
            _logger.LogInformation("  {Key}: {Value}", header.Key, header.Value);
        }
        
        _logger.LogInformation("Query Parameters:");
        foreach (var param in context.Query)
        {
            _logger.LogInformation("  {Key}: {Value}", param.Key, param.Value);
        }
    }

    // =========================================================================
    // Example 2: Using string for raw body access
    // =========================================================================
    /// <summary>
    /// Receives just the raw request body as a string.
    /// Useful when you need simple text processing.
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=RawBody" \
    ///      -H "Content-Type: text/plain" \
    ///      -d 'This is plain text content'
    /// </summary>
    [Function("RawBody")]
    public void RunWithRawBody([ConnectorTrigger] string body)
    {
        _logger.LogInformation("=== Raw Body Example ===");
        _logger.LogInformation("Received body ({Length} chars): {Body}", body.Length, body);
    }

    // =========================================================================
    // Example 3: Using custom POCO for automatic JSON deserialization
    // =========================================================================
    /// <summary>
    /// Automatically deserializes the JSON body into a custom POCO type.
    /// No manual parsing required!
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=TypedPayload" \
    ///      -H "Content-Type: application/json" \
    ///      -d '{"orderId": "ORD-12345", "customerName": "John Doe", "amount": 99.99, "items": ["Widget", "Gadget"]}'
    /// </summary>
    [Function("TypedPayload")]
    public void RunWithTypedPayload([ConnectorTrigger] OrderPayload order)
    {
        _logger.LogInformation("=== Typed Payload Example ===");
        _logger.LogInformation("Order ID: {OrderId}", order.OrderId);
        _logger.LogInformation("Customer: {CustomerName}", order.CustomerName);
        _logger.LogInformation("Amount: ${Amount:F2}", order.Amount);
        _logger.LogInformation("Items: {Items}", string.Join(", ", order.Items ?? []));
    }

    // =========================================================================
    // Example 4: Webhook receiver for external services
    // =========================================================================
    /// <summary>
    /// Example of receiving webhooks from external services (GitHub, Stripe, etc.)
    /// Uses the strongly-typed GitHubEvent model for automatic deserialization.
    /// 
    /// Invoke with (simulating a GitHub push webhook):
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=GitHubWebhook" \
    ///      -H "Content-Type: application/json" \
    ///      -H "X-GitHub-Event: push" \
    ///      -d '{"action": "created", "ref": "refs/heads/main", "repository": {"id": 123, "name": "my-repo", "full_name": "owner/my-repo"}, "sender": {"login": "developer", "id": 456}}'
    /// </summary>
    [Function("GitHubWebhook")]
    public void RunGitHubWebhook([ConnectorTrigger] GitHubEvent payload)
    {
        _logger.LogInformation("=== GitHub Webhook (Typed) ===");
        _logger.LogInformation("Action: {Action}", payload.Action);
        _logger.LogInformation("Ref: {Ref}", payload.Ref);
        _logger.LogInformation("Repository: {RepoName}", payload.Repository?.FullName);
        _logger.LogInformation("Sender: {Sender}", payload.Sender?.Login);
        
        if (payload.Commits?.Count > 0)
        {
            _logger.LogInformation("Commits: {CommitCount}", payload.Commits.Count);
            foreach (var commit in payload.Commits)
            {
                _logger.LogInformation("  - {Message} by {Author}", commit.Message, commit.Author?.Name);
            }
        }
    }

    // =========================================================================
    // Example 5: O365 Email webhook with typed model
    // =========================================================================
    /// <summary>
    /// Receives O365 email notifications with strongly-typed deserialization.
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnEmail" \
    ///      -H "Content-Type: application/json" \
    ///      -d '{"id": "msg-123", "subject": "Hello!", "from": "sender@example.com", "body": "Email content", "receivedDateTime": "2026-03-18T10:00:00Z"}'
    /// </summary>
    [Function("OnEmail")]
    public void OnEmail([ConnectorTrigger] O365Email email)
    {
        _logger.LogInformation("=== O365 Email (Typed) ===");
        _logger.LogInformation("ID: {Id}", email.Id);
        _logger.LogInformation("From: {From}", email.From);
        _logger.LogInformation("Subject: {Subject}", email.Subject);
        _logger.LogInformation("Received: {ReceivedDateTime}", email.ReceivedDateTime);
        _logger.LogInformation("Has Attachments: {HasAttachments}", email.HasAttachments);
    }

    // =========================================================================
    // Example 6: Teams message with typed model
    // =========================================================================
    /// <summary>
    /// Receives Teams message notifications with strongly-typed deserialization.
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnTeamsMessage" \
    ///      -H "Content-Type: application/json" \
    ///      -d '{"id": "msg-456", "content": "Hello team!", "from": {"id": "user-1", "displayName": "John Doe"}, "teamId": "team-123", "channelId": "channel-456"}'
    /// </summary>
    [Function("OnTeamsMessage")]
    public void OnTeamsMessage([ConnectorTrigger] TeamsMessage message)
    {
        _logger.LogInformation("=== Teams Message (Typed) ===");
        _logger.LogInformation("ID: {Id}", message.Id);
        _logger.LogInformation("From: {From}", message.From?.DisplayName);
        _logger.LogInformation("Content: {Content}", message.Content);
        _logger.LogInformation("Team: {TeamId}, Channel: {ChannelId}", message.TeamId, message.ChannelId);
    }

    // =========================================================================
    // Example 7: SharePoint item with typed model
    // =========================================================================
    /// <summary>
    /// Receives SharePoint item change notifications with strongly-typed deserialization.
    /// 
    /// Invoke with:
    /// curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnSharePointItem" \
    ///      -H "Content-Type: application/json" \
    ///      -d '{"id": "item-789", "name": "Document.docx", "changeType": "created", "siteUrl": "https://contoso.sharepoint.com", "listTitle": "Documents"}'
    /// </summary>
    [Function("OnSharePointItem")]
    public void OnSharePointItem([ConnectorTrigger] SharePointItem item)
    {
        _logger.LogInformation("=== SharePoint Item (Typed) ===");
        _logger.LogInformation("ID: {Id}", item.Id);
        _logger.LogInformation("Name: {Name}", item.Name);
        _logger.LogInformation("Change Type: {ChangeType}", item.ChangeType);
        _logger.LogInformation("Site: {SiteUrl}", item.SiteUrl);
        _logger.LogInformation("List: {ListTitle}", item.ListTitle);
    }
}

/// <summary>
/// Sample POCO for demonstrating automatic JSON deserialization.
/// </summary>
public class OrderPayload
{
    public string? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public List<string>? Items { get; set; }
    public DateTime? CreatedAt { get; set; }
}
