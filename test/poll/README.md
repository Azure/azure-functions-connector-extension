# Connector Poll Sample Apps

These apps validate the same Connector Namespace Poll trigger from each
supported language worker:

- `dotnet` - .NET isolated with a typed Office 365 payload and Poll `MessageId`.
- `nodejs` - TypeScript generic binding with an untyped JSON payload.
- `python` - Python v2 generic binding with an untyped JSON payload.

Each sample uses:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %ConnectorTriggerConfigName%
Cardinality = Many
MaxBatchSize = 4
Concurrency = 4
```

The samples enable batched invocation. .NET isolated uses `IsBatched = true`;
Node.js uses `cardinality: 'many'`; and Python uses
`cardinality=func.Cardinality.MANY`. Omitting batching or using cardinality
`one` supplies one event per invocation and requires an effective
`MaxBatchSize` of `1`.

Linked-output events are delivered one per invocation and serialized
host-wide to bound memory. Connector Namespace does not yet emit linked-output
messages in the available test environment, so the samples currently validate
inline batching only.

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
