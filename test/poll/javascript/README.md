# JavaScript Poll Test App

## What it tests

- Uses the Office 365 SDK helper and receives batches of up to four Poll events.
- No TypeScript build step is needed.

## Prerequisites

- Node.js 24+, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Configure

Follow the [Poll setup](../README.md#required-setup), then copy `local.settings.example.json` to `local.settings.json` and set `ConnectorNamespace__pollingEndpoint` and `OnNewEmailTriggerConfigName`.

## Build, test, and run

```powershell
Push-Location .\test\poll\javascript
npm ci
npm test
dotnet build .\extensions.csproj
func start
Pop-Location
```

`npm test` checks SDK registration and batch normalization without Azure. Start Azurite before `func start`; npm uses the feed configured at user or CI level.
