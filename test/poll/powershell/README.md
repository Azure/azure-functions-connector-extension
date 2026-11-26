# Generic PowerShell Poll Test App

## What it tests

- Uses `OnGenericPoll/function.json` for a single-event `connectorTrigger`.
- Accepts worker-deserialized objects or JSON strings; invalid input fails the invocation.
- No connector SDK or PowerShell modules are required.

## Prerequisites

- PowerShell 7.4, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Configure

Follow the [Poll setup](../README.md#required-setup), then copy `local.settings.example.json` to `local.settings.json` and set `ConnectorNamespace__pollingEndpoint` and `GenericTriggerConfigName`.

## Build, test, and run

```powershell
Push-Location .\test\poll\powershell
.\test-trigger.ps1
dotnet build .\extensions.csproj
func start
Pop-Location
```

`test-trigger.ps1` checks binding metadata and payload handling without Azure or Pester. Start Azurite before `func start`.
