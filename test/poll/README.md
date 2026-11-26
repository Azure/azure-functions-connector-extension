# Connector Poll Repository Test Apps

These repository test apps do not change the requirements of existing Poll apps. Customer-facing .NET samples are in [Azure-Samples/functions-connectors-net](https://github.com/Azure-Samples/functions-connectors-net).

Poll is the simplest local starting point: the Function host connects outbound to the real Connector Namespace, so no public callback or dev tunnel is needed. These test apps validate the trigger across supported language workers:

- `dotnet` - .NET isolated with a typed Office 365 payload and Poll `MessageId`.
- [`typescript`](./typescript) - TypeScript batched binding with the Office 365 helper from `@azure/functions-extensions-connectors`.
- [`javascript`](./javascript/README.md) - JavaScript equivalent of the batched Office 365 SDK app, without a TypeScript compilation step.
- `python` - Python 3.13+ with `@app.connector_trigger`, batched Poll delivery, and Office 365 models from `azure-connectors`.
- [`java`](./java/README.md) - Java `@CustomBinding` with a raw single-event JSON string.
- [`powershell`](./powershell/README.md) - PowerShell `function.json` with single-event JSON input and worker deserialization.
- [`go`](./go/README.md) - native Go worker with `GenericTrigger` and a raw single-event binding; no Connector SDK.

## Required setup

Create an authorized connector connection and an enabled Poll Trigger Config using the [Poll registration skill](../../.github/skills/poll-trigger-registration/SKILL.md). Grant the Function or developer identity a connection-level access policy. The namespace identity's access is handled automatically.

The .NET, TypeScript, JavaScript, and Python test apps use the same binding settings:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %OnNewEmailTriggerConfigName%
```

`Connection` names the Function configuration prefix, not the connector connection resource. These test apps use an app-setting expression for the trigger name, so set `OnNewEmailTriggerConfigName`.

## Test behavior and optional tuning

The .NET, TypeScript, JavaScript, and Python test apps enable batched invocation with a maximum of 4 events and a scaling target of 16 pending events per instance. .NET isolated uses `IsBatched = true`, Node.js uses `cardinality: 'many'`, and Python uses `cardinality=func.Cardinality.MANY`. Each listener processes one batch at a time. Batch bindings omit the concurrent-call setting; a positive value is ignored with a startup warning, and a negative value is invalid. The generic Java, PowerShell, and Go test apps use single-event delivery to focus on raw JSON handling; this is a test choice, not a feature limitation.

`MaxConcurrentCalls` applies only to single-event delivery. Its built-in default is 16 per listener on one instance, with no CPU-core multiplier. Processing capacity remains occupied through preparation, invocation, and acknowledgement. Scaling uses `ceil(approximateQueueDepth / effectiveTarget)`, preferring a supplied runtime `InstanceConcurrency` over the configured threshold; neither batch size nor `MaxConcurrentCalls` changes the scaling formula or is changed by the runtime scaling override.

An Office 365 Poll event contains an `outputs.body` that can be a single email or a `{ "value": [...] }` collection, independently of Functions invocation cardinality. The Node.js extension normalizes both shapes across the batch into `EmailTriggerContext.emails`; TypeScript types do not add runtime validation. Python uses a `typing.List[str]` parameter with one JSON string per Poll event, then uses `TriggerCallbackPayload.from_dict` to parse each event into typed `GraphClientReceiveMessage` objects, including nested attachments and sensitivity labels. One invocation can contain several Poll events, and each event can contain several emails.

The Python test app requires `azure-functions>=2.4.0`, which supports the Poll decorator arguments and decoding of batched `collection_string` input:

```python
from typing import List

@app.function_name(name="OnNewEmailPoll")
@app.connector_trigger(
    arg_name="payloads",
    delivery_mode=func.DeliveryMode.POLL,
    connection="ConnectorNamespace",
    trigger_config_name="%OnNewEmailTriggerConfigName%",
    cardinality=func.Cardinality.MANY,
    max_batch_size=4,
    target_pending_event_threshold=16,
)
def on_new_email_poll(payloads: List[str]) -> None:
    ...
```

Use snake_case decorator arguments; the SDK emits the host's camelCase binding metadata.

Use `typing.List[str]` rather than the built-in `list[str]` annotation: the 2.4.0 binding type check accepts the former.

Linked-output events are delivered one per invocation and serialized host-wide to bound memory. Their configurable download limit defaults to 100 MiB; it is not a supported payload-size or process-memory guarantee. See the [linked-output configuration](../../docs/connector-trigger-configuration.md#linked-output-download-limit) for overrides and memory considerations.

The batch, concurrency, scaling, and polling-interval settings are optional. Their defaults and precedence are listed in the [configuration reference](../../docs/connector-trigger-configuration.md). No Connector-specific `host.json` settings are required; explicit binding values do not need to be repeated as host defaults.

## Local connection settings

Copy the test app's `local.settings.example.json` to `local.settings.json`, which remains gitignored, then configure:

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
OnNewEmailTriggerConfigName=<poll-trigger-config-name>
```

Never add or commit a real endpoint or credential value.

Obtain the settings from the trigger configuration's `pollingEndpoints.receiveUri`:

| Receive URI | `ConnectorNamespace__pollingEndpoint` |
| --- | --- |
| `https://<connector-namespace-host>/api/triggerconfigs/<name>/receive` | `https://<connector-namespace-host>` |
| `https://<host>/api/connectorGateways/<id>/triggerconfigs/<name>/receive` | `https://<host>/api/connectorGateways/<id>` |

Configure `<name>` as `OnNewEmailTriggerConfigName`. The extension constructs the operation paths and supplies `maxEvents` on Receive requests. Do not include a query string, `/api` in the stable-DNS setting, or a Trigger Config in either setting.

Both endpoint forms accept one optional trailing slash.

For local development, `Connection = ConnectorNamespace` uses the signed-in developer credential when no credential selector is configured. For Azure, configure `ConnectorNamespace__credential=managedidentity` and optionally `ConnectorNamespace__clientId` or `ConnectorNamespace__managedIdentityResourceId`. The selected identity must have an access policy on the connection referenced by the trigger.

## Build prerequisites

- .NET SDK 10.0.100+ is only needed to build the repository-local development extension projects (`extensions.csproj`).
- Azure Functions Core Tools v4 and Azurite are needed to run the test apps.
- The NuGet feed is configured in the repository's `NuGet.config`.

- Node.js test apps require Node.js 24+ for `@azure/functions-extensions-connectors` 1.0.0.
- Python test apps require Python 3.13+ for `azure-functions` 2.4.0.

Install Python dependencies from the test app's `requirements.txt`. Building the local host extension does not install Python packages. See [worker dependency and bundle configuration](../../docs/connector-trigger-configuration.md#non-net-workers).

Each Node.js test app inherits the npm feed from user or CI configuration. Run npm commands from its directory.

If an Azure Artifacts feed returns `E401`, configure credentials in your user-level `.npmrc` using the [Azure Artifacts npm authentication guidance](https://learn.microsoft.com/azure/devops/artifacts/npm/npmrc). Do not commit credentials.

## Build and run

Build the .NET test app against the repository-local projects:

```powershell
dotnet build .\test\poll\dotnet\PollSample.csproj
```

Build the host extension before starting Node.js or Python:

The extension projects copy the current checkout's DLLs and generated metadata into each test app's `bin` after the Functions SDK refreshes its output. Rebuild after extension changes; restarting Core Tools without rebuilding can load an older DLL.

```powershell
Push-Location .\test\poll\typescript
npm ci
npm run build
Pop-Location
dotnet build .\test\poll\typescript\NodePollExtensions.csproj

py -3.13 -m venv .\test\poll\python\.venv
.\test\poll\python\.venv\Scripts\python.exe -m pip install `
    -r .\test\poll\python\requirements.txt
dotnet build .\test\poll\python\PythonPollExtensions.csproj
```

Start Azurite, then run `func start` from the selected test app directory. For Python, activate the test app's virtual environment first:

```powershell
Set-Location .\test\poll\python
.\.venv\Scripts\Activate.ps1
func start
```

Keep the virtual environment inside the test app directory so the local worker recognizes its dependencies. Installing an SDK into an external environment does not ensure Core Tools selects it.

Validate the typed test app code without connecting to Azure:

```powershell
Push-Location .\test\poll\typescript
npm test
Pop-Location
.\test\poll\python\.venv\Scripts\python.exe -m unittest discover -s .\test\poll\python -p test_function_app.py
```

The Python command assumes the test app's virtual environment has the dependencies from `requirements.txt` installed.

For JavaScript, Java, PowerShell, and Go build, test, and run commands, follow their linked READMEs above. Each app builds the repository-local host extension before `func start`; do not add an extension bundle alongside that local build.
