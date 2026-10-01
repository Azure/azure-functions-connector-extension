# Connector Poll Sample Apps

These apps validate the same Connector Namespace Poll trigger from each
supported language worker:

- `dotnet` - .NET isolated with a typed Office 365 payload and Poll `MessageId`.
- `nodejs` - TypeScript generic binding with an untyped JSON payload.
- `python` - Python v2 generic binding with an untyped JSON payload.

Each sample uses Poll delivery with the same connection and endpoint settings:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %OnNewEmailTriggerConfigName%
```

.NET isolated and Node.js enable batched invocation with `MaxBatchSize = 4`
and `Concurrency = 4`. .NET isolated uses `IsBatched = true`; Node.js uses
`cardinality: 'many'`. Python uses `cardinality=func.Cardinality.ONE`,
`maxBatchSize=1`, and `concurrency=1` because the Python worker's generic
binding decoder does not accept batched `collection_string` input.

Linked-output events are delivered one per invocation and serialized
host-wide to bound memory. Connector Namespace does not yet emit linked-output
messages in the available test environment, so the samples currently validate
inline batching only.

Copy each sample's checked-in `local.settings.example.json` to
`local.settings.json`, which remains gitignored, then configure:

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
OnNewEmailTriggerConfigName=<poll-trigger-config-name>
```

Never add or commit a real endpoint or credential value.

Obtain the two values from the trigger configuration's
`pollingEndpoints.receiveUri`. Remove the final `/triggerConfigs/<name>/receive` segments to obtain the gateway-level `ConnectorNamespace__pollingEndpoint`, and configure that `<name>` as
`OnNewEmailTriggerConfigName`. Preserve the service-provided host and gateway identifier exactly. The host may use the current `logic.azure.com` form or a `connectornamespaces` stable-DNS form; the extension treats it as opaque trusted configuration.

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
