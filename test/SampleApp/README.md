# .NET Isolated Sample App

This sample demonstrates how to use the Connector Extension with .NET isolated worker.

## Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Build the solution** (from repo root):

   ```bash
   cd samples/dotnet-isolated
   dotnet build
   ```

2. **Start Azurite** (in another terminal):

   ```bash
   azurite --silent
   ```

3. **Run the function app:**

   ```bash
   func start
   ```

## Available Functions

| Function | Binding Type | Description |
| -------- | ------------ | ----------- |
| `FullContext` | ConnectorContext | Full request context access |
| `RawBody` | string | Raw body as string |
| `TypedPayload` | OrderPayload | Custom POCO binding |
| `OnEmail` | O365Email | Typed O365 email model |
| `OnTeamsMessage` | TeamsMessage | Typed Teams message model |
| `OnSharePointItem` | SharePointItem | Typed SharePoint item model |
| `GitHubWebhook` | GitHubEvent | Typed GitHub event model |

## Code Examples

### Full Context Access

```csharp
[Function("FullContext")]
public void RunWithFullContext([ConnectorTrigger] ConnectorContext context)
{
    _logger.LogInformation($"Method: {context.Method}");
    _logger.LogInformation($"Body: {context.Body}");
    _logger.LogInformation($"ContentType: {context.ContentType}");
    
    foreach (var header in context.Headers)
    {
        _logger.LogInformation($"Header: {header.Key} = {header.Value}");
    }
}
```

### Raw Body as String

```csharp
[Function("RawBody")]
public void RunWithRawBody([ConnectorTrigger] string body)
{
    _logger.LogInformation($"Received body ({body.Length} chars): {body}");
}
```

### Typed POCO Binding (Custom)

```csharp
public class OrderPayload
{
    public string? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
}

[Function("TypedPayload")]
public void RunWithTypedPayload([ConnectorTrigger] OrderPayload order)
{
    _logger.LogInformation($"Order {order.OrderId} from {order.CustomerName}: ${order.Amount}");
}
```

### Built-in Typed Models

The extension includes strongly-typed models for common scenarios:

```csharp
using Microsoft.Azure.Functions.Worker.Extensions.Connector.Models;

[Function("OnEmail")]
public void OnEmail([ConnectorTrigger] O365Email email)
{
    _logger.LogInformation($"From: {email.From}");
    _logger.LogInformation($"Subject: {email.Subject}");
    _logger.LogInformation($"Has Attachments: {email.HasAttachments}");
}

[Function("GitHubWebhook")]
public void OnGitHubPush([ConnectorTrigger] GitHubEvent evt)
{
    _logger.LogInformation($"Repo: {evt.Repository?.FullName}");
    _logger.LogInformation($"Pusher: {evt.Sender?.Login}");
    _logger.LogInformation($"Commits: {evt.Commits?.Count}");
}
```

**Available Models:** `O365Email`, `TeamsMessage`, `SharePointItem`, `GitHubEvent`

## Testing

```bash
# FullContext
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=FullContext" \
     -H "Content-Type: application/json" \
     -H "X-Custom-Header: test" \
     -d '{"message": "Hello from .NET!"}'

# OnEmail (typed)
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnEmail" \
     -H "Content-Type: application/json" \
     -d '{"id": "msg-123", "subject": "Hello!", "from": "sender@example.com"}'

# TypedPayload (custom POCO)
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=TypedPayload" \
     -H "Content-Type: application/json" \
     -d '{"orderId": "ORD-123", "customerName": "John", "amount": 99.99}'
```

## Project Configuration

### Program.cs

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
```

### csproj Reference

```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Connector" Version="0.1.0-alpha" />
```
