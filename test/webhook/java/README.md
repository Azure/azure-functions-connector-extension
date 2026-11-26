# Generic Java Webhook Test App

## What it tests

- Uses `@CustomBinding(type = "connectorTrigger")` to receive raw JSON without a connector SDK.
- Checked-in `OnGenericWebhook/function.json` loads the Maven-built JAR and uses default Webhook delivery.
- The handler logs receipt and fails on missing or blank input.

## Prerequisites

- Java 17+, Maven 3.9+, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Build, test, and run

```powershell
Push-Location .\test\webhook\java
mvn package
dotnet build .\extensions.csproj
func start
Pop-Location
```

Set `JAVA_HOME` to the JDK installation. `mvn package` runs JUnit tests and creates the JAR loaded by the checked-in metadata. Start Azurite before `func start`. To test a local callback:

```powershell
Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:7071/runtime/webhooks/connector?functionName=OnGenericWebhook' `
    -ContentType 'application/json' `
    -Body '{"body":{"arbitrary":true}}'
```

For Azure callbacks, use `func start --enableAuth`, expose the host through the approved public tunnel, and follow the [Webhook setup](../README.md#required-and-optional-configuration).
