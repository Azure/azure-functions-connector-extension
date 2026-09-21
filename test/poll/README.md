# Connector Poll Sample Apps

These apps validate the same Connector Namespace Poll trigger from each
supported language worker:

- `dotnet` - .NET isolated with a typed Office 365 payload.
- `nodejs` - TypeScript generic binding with an untyped JSON payload.
- `python` - Python v2 generic binding with an untyped JSON payload.

Each sample uses:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %ConnectorTriggerConfigName%
MaxBatchSize = 1
Concurrency = 4
```

The local settings files are intentionally untracked. Configure:

```text
ConnectorNamespace__resourceId=/subscriptions/<subscription>/resourceGroups/<resource-group>/providers/Microsoft.Web/connectorGateways/<namespace>
ConnectorTriggerConfigName=<enabled-poll-trigger-config>
```

The signed-in developer identity must be able to read the trigger
configuration through ARM and must have an access policy on the connection
referenced by that trigger.

Build the .NET sample against the repository-local projects:

```powershell
dotnet build .\test\poll\dotnet\PollSample.csproj
```

To validate the packed preview NuGet instead:

```powershell
dotnet pack .\Microsoft.Azure.Functions.Extensions.Connector.sln `
    --configuration Release `
    --output .\out\pkg

dotnet build .\test\poll\dotnet\PollSample.csproj `
    --configuration Release `
    -p:ConnectorPackageVersion=0.3.0-alpha.dev `
    -p:RestoreAdditionalProjectSources="$PWD\out\pkg"
```

Build the host extension before starting Node.js or Python:

```powershell
npm --prefix .\test\poll\nodejs ci
dotnet build .\test\poll\nodejs\NodePollExtensions.csproj

py -3.13 -m pip install `
    -r .\test\poll\python\requirements.txt `
    --target .\test\poll\python\.python_packages\lib\site-packages
dotnet build .\test\poll\python\PythonPollExtensions.csproj
```

Start Azurite, then run `func start` from the selected sample directory.

Current preview limitations are documented in
[`docs/connector-trigger-poll-preview.md`](../../docs/connector-trigger-poll-preview.md).
