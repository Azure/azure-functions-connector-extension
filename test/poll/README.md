# Connector Poll Sample Apps

These apps validate the same Connector Namespace Poll trigger from each supported language worker:

- `dotnet` - .NET isolated with a typed Office 365 payload and Poll `MessageId`.
- `nodejs` - TypeScript batched binding with Office 365 payload types from `@azure/connectors`.
- `python` - Python v2 single-event generic binding that parses JSON into Office 365 models from `azure-connectors`.

Each sample uses Poll delivery with the same connection and endpoint settings:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %OnNewEmailTriggerConfigName%
```

.NET isolated and Node.js enable batched invocation with `MaxBatchSize = 4`; each listener processes one batch at a time, and scaling uses `TargetPendingEventThreshold = 16`. .NET isolated uses `IsBatched = true`; Node.js uses `cardinality: 'many'`. Batch bindings omit `MaxConcurrentCalls`; a positive value is ignored with a startup warning, and a negative value is invalid. Python uses `cardinality=func.Cardinality.ONE`, `maxBatchSize=1`, `maxConcurrentCalls=1`, and `targetPendingEventThreshold=16` because the Python worker's generic binding decoder does not accept batched `collection_string` input.

`MaxConcurrentCalls` applies only to single-event delivery, aligning with the [Service Bus extension](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus#hostjson-settings). Its built-in default is 16 per listener on one instance, with no CPU-core multiplier; the Python sample explicitly selects 1. Processing capacity remains occupied through preparation, invocation, and acknowledgement. Scaling uses `ceil(approximateQueueDepth / effectiveTarget)`, preferring a supplied runtime `InstanceConcurrency` over the configured threshold; neither batch size nor `MaxConcurrentCalls` changes the scaling formula or is changed by the runtime scaling override.

An Office 365 Poll event contains an `outputs.body` that can be a single email or a `{ "value": [...] }` collection, independently of Functions invocation cardinality. The Node.js sample types both shapes using SDK models; these TypeScript types do not add runtime validation. The Python generic binding receives JSON as a string and uses `TriggerCallbackPayload.from_dict` with an SDK-model parser to normalize both shapes into typed `GraphClientReceiveMessage` objects, including nested attachments and sensitivity labels. One Python invocation still processes one Poll event, which may contain several emails.

Linked-output events are delivered one per invocation and serialized host-wide to bound memory. Connector Namespace does not yet emit linked-output messages in the available test environment, so the samples currently validate inline batching only.

Copy each sample's checked-in `local.settings.example.json` to `local.settings.json`, which remains gitignored, then configure:

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
OnNewEmailTriggerConfigName=<poll-trigger-config-name>
```

Never add or commit a real endpoint or credential value.

Obtain the two values from the trigger configuration's `pollingEndpoints.receiveUri`. Remove the final `/triggerConfigs/<name>/receive` segments to obtain the gateway-level `ConnectorNamespace__pollingEndpoint`, and configure that `<name>` as `OnNewEmailTriggerConfigName`. Preserve the service-provided host and gateway identifier exactly. The host may use the current `logic.azure.com` form or a `connectornamespaces` stable-DNS form; the extension treats it as opaque trusted configuration.

For local development, `Connection = ConnectorNamespace` uses the signed-in developer credential when no credential selector is configured. For Azure, configure `ConnectorNamespace__credential=managedidentity` and optionally `ConnectorNamespace__clientId` or `ConnectorNamespace__managedIdentityResourceId`. The selected identity must have an access policy on the connection referenced by the trigger.

Build the .NET sample against the repository-local projects:

```powershell
dotnet build .\test\poll\dotnet\PollSample.csproj
```

Build the host extension before starting Node.js or Python:

Their extension projects copy the current checkout's DLLs and generated extension metadata into each sample's `bin` only after the Functions SDK has refreshed and cleaned its output. Rebuild after extension changes; restarting Core Tools without rebuilding can load an older DLL.

```powershell
Push-Location .\test\poll\nodejs
npm ci
npm run build
Pop-Location
dotnet build .\test\poll\nodejs\NodePollExtensions.csproj

py -3.13 -m pip install `
    -r .\test\poll\python\requirements.txt `
    --target .\test\poll\python\.python_packages\lib\site-packages
dotnet build .\test\poll\python\PythonPollExtensions.csproj
```

Start Azurite, then run `func start` from the selected sample directory.

Validate the typed sample code without connecting to Azure:

```powershell
Push-Location .\test\poll\nodejs
npm run build
Pop-Location
.\test\poll\python\.venv\Scripts\python.exe -m unittest discover -s .\test\poll\python -p test_function_app.py
```

The Python command assumes the sample's virtual environment has the dependencies from `requirements.txt` installed.
