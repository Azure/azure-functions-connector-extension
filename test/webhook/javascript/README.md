# JavaScript Webhook Test App

## What it tests

- Uses the Office 365 SDK helper and writes the callback payload to Blob Storage.
- Runs directly on Node.js without a TypeScript compilation step.

## Prerequisites

- Node.js 24+, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Build, test, and run

```powershell
Push-Location .\test\webhook\javascript
npm ci
npm test
dotnet build .\extensions.csproj
func start
Pop-Location
```

Copy `local.settings.example.json` to `local.settings.json`. `npm test` checks callback normalization and blob output without Azure. Start Azurite before `func start`.

To test a local callback, POST an email in `body` or a collection in `body.value` to `http://localhost:7071/runtime/webhooks/connector?functionName=OnNewEmail`. For Azure callbacks, use `func start --enableAuth`, expose the host through the approved public tunnel, and follow the [Webhook setup](../README.md#required-and-optional-configuration).
