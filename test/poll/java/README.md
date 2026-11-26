# Generic Java Poll Test App

## What it tests

- Uses `@CustomBinding(type = "connectorTrigger")` to receive raw JSON without a connector SDK.
- Checked-in `OnGenericPoll/function.json` supplies Poll-specific binding metadata.
- The handler logs receipt and fails on missing or blank input.

## Prerequisites

- Java 17+, Maven 3.9+, Azure Functions Core Tools v4, and Azurite.
- .NET SDK 10+ only to build the local development extension project (`extensions.csproj`).

## Configure

Follow the [Poll setup](../README.md#required-setup), then copy `local.settings.example.json` to `local.settings.json` and set `ConnectorNamespace__pollingEndpoint` and `GenericTriggerConfigName`.

## Build, test, and run

```powershell
Push-Location .\test\poll\java
mvn package
dotnet build .\extensions.csproj
func start
Pop-Location
```

Set `JAVA_HOME` to the JDK installation. `mvn package` runs JUnit tests and creates the JAR loaded by the checked-in metadata. Start Azurite before `func start`.
