# Generic Go Poll Test App

## What it tests

- Uses the native Go worker's `app.GenericTrigger` API and the local Connector host extension.
- Receives raw JSON; no connector SDK or connector-specific model is required.
- The handler validates payloads. The host extension handles Poll receive and acknowledgement.

## Prerequisites

- Go 1.24+, Azure Functions Core Tools 4.12+, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).
- Azure native Go apps run on Linux in Flex Consumption.

## Configure

- Follow the [Poll setup](../README.md#required-setup).
- Copy `local.settings.example.json` to `local.settings.json` and set `ConnectorNamespace__pollingEndpoint` and `GenericTriggerConfigName`.

## Build, test, and run

From the repository root:

```powershell
Push-Location .\test\poll\go
go test ./...
dotnet build .\extensions.csproj
func start
Pop-Location
```

`go test` checks payload validation and trigger registration without Azure. Start Azurite before `func start`.
