# Generic PowerShell Webhook Test App

## What it tests

- Declares `connectorTrigger` in `OnGenericWebhook/function.json` and uses default Webhook delivery.
- Accepts deserialized objects or JSON strings; invalid input fails the invocation.
- No connector SDK or PowerShell modules are required.

## Prerequisites

- PowerShell 7.4, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Build, test, and run

```powershell
Push-Location .\test\webhook\powershell
.\test-trigger.ps1
dotnet build .\extensions.csproj
func start
Pop-Location
```

`test-trigger.ps1` checks binding metadata and payload handling without Azure or Pester. Start Azurite before `func start`. To test a local callback:

```powershell
Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:7071/runtime/webhooks/connector?functionName=OnGenericWebhook' `
    -ContentType 'application/json' `
    -Body '{"body":{"arbitrary":true}}'
```

For Azure callbacks, use `func start --enableAuth`, expose the host through the approved public tunnel, and follow the [Webhook setup](../README.md#required-and-optional-configuration).
