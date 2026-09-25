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
PollingEndpoint = %OnNewEmailEndpoint%
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

Copy each sample's checked-in `local.settings.example.json` to
`local.settings.json`, which remains gitignored, then configure:

```text
OnNewEmailEndpoint=https://<scale-unit>.<region>.logic.azure.com/api/connectorGateways/<connector-namespace-id>/triggerconfigs/<poll-trigger-config-name>
```

Never add or commit a real endpoint or credential value.

Obtain the value from the trigger configuration's
`pollingEndpoints.receiveUri`: require an absolute HTTPS URI whose final path
segment is exactly `receive`, then remove only that segment. The resulting
base must end in `/triggerconfigs/<poll-trigger-config-name>` and must not
include `/receive`, `/acknowledge`, or `/approximateQueueDepth`.

For local development, `Connection = ConnectorNamespace` uses the signed-in
developer credential when no credential selector is configured. For Azure,
configure `ConnectorNamespace__credential=managedidentity` and optionally
`ConnectorNamespace__clientId` or
`ConnectorNamespace__managedIdentityResourceId`. The selected identity must
have an access policy on the connection referenced by the trigger.

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
