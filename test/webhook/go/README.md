# Generic Go Webhook Test App

## What it tests

- Uses the native Go worker's `app.GenericTrigger` API and the local Connector host extension.
- Receives raw JSON without a connector SDK; this is not an HTTP-triggered function.
- The handler validates payloads and fails on invalid input.

## Prerequisites

- Go 1.24+, Azure Functions Core Tools 4.12+, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).
- Azure native Go apps run on Linux in Flex Consumption.

## Build, test, and run

From the repository root:

```powershell
Push-Location .\test\webhook\go
go test ./...
dotnet build .\extensions.csproj
func start
Pop-Location
```

`go test` checks payload validation and trigger registration without Azure. Start Azurite before `func start`. To test a local callback:

```powershell
Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:7071/runtime/webhooks/connector?functionName=OnGenericWebhook' `
    -ContentType 'application/json' `
    -Body '{"body":{"arbitrary":true}}'
```

For Azure callbacks, use `func start --enableAuth`, expose the host through the approved public tunnel, and follow the [Webhook setup](../README.md#required-and-optional-configuration).
