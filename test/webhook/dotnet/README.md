# .NET Isolated Webhook Test App

This test app validates Connector Namespace Webhook delivery with the .NET isolated worker. `OnNewEmail` binds the callback payload to `Office365OnNewEmailTriggerPayload` and writes it to Blob Storage.

## Prerequisites

- .NET 10.0 SDK
- Azure Functions Core Tools v4+
- Azurite or an Azure Storage account

## Setup

1. Build the test app from the repository root:

   ```powershell
   dotnet build .\test\webhook\dotnet\SampleApp.csproj
   ```

2. Start Azurite:

   ```powershell
   azurite --silent
   ```

3. Start the Function host from `test\webhook\dotnet`:

   ```powershell
   func start
   ```

## Function

```csharp
[Function("OnNewEmail")]
[BlobOutput(
    "connector-messages/{rand-guid}.json",
    Connection = "BlobStoreConnection")]
public string OnNewEmail(
    [ConnectorTrigger]
    Office365OnNewEmailTriggerPayload payload)
{
    return JsonSerializer.Serialize(payload);
}
```

The test app references the repository-local Connector worker extension and `Azure.Connectors.Sdk` `1.0.0`.

## Local callback test

```powershell
Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:7071/runtime/webhooks/connector?functionName=OnNewEmail" `
    -ContentType "application/json" `
    -Body '{"body":{"value":[{"subject":"Test email","from":"sender@example.com"}]}}'
```

Local Core Tools runs do not enforce the Connector system key unless authentication is explicitly enabled. Deployed callbacks must include the `connector_extension` system key.
