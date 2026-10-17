# Connector Poll Sample Apps

Poll is the simplest local starting point: the Function host connects outbound to the real Connector Namespace, so no public callback or dev tunnel is needed. These apps validate the same trigger from each supported language worker:

- `dotnet` - .NET isolated with a typed Office 365 payload and Poll `MessageId`.
- `nodejs` - TypeScript batched binding with Office 365 payload types from `@azure/connectors`.
- `python` - Python 3.13+ with `@app.connector_trigger`, batched Poll delivery, and Office 365 models from `azure-connectors`.

## Required setup

Create an authorized connector connection and an enabled Poll Trigger Config using the [Poll registration skill](../../.github/skills/poll-trigger-registration/SKILL.md). Authorize both the namespace identity and the Function/developer identity with connection-level access policies.

Each sample requires the same binding settings:

```text
DeliveryMode = Poll
Connection = ConnectorNamespace
TriggerConfigName = %OnNewEmailTriggerConfigName%
```

`Connection` names the Function configuration prefix, not the connector connection resource. Because these samples use an app-setting expression for the trigger name, their `OnNewEmailTriggerConfigName` setting must be populated.

## Sample behavior and optional tuning

All three samples enable batched invocation with a maximum of 4 events and a scaling target of 16 pending events per instance. .NET isolated uses `IsBatched = true`, Node.js uses `cardinality: 'many'`, and Python uses `cardinality=func.Cardinality.MANY`. Each listener processes one batch at a time. Batch bindings omit the concurrent-call setting; a positive value is ignored with a startup warning, and a negative value is invalid.

`MaxConcurrentCalls` applies only to single-event delivery. Its built-in default is 16 per listener on one instance, with no CPU-core multiplier. Processing capacity remains occupied through preparation, invocation, and acknowledgement. Scaling uses `ceil(approximateQueueDepth / effectiveTarget)`, preferring a supplied runtime `InstanceConcurrency` over the configured threshold; neither batch size nor `MaxConcurrentCalls` changes the scaling formula or is changed by the runtime scaling override.

An Office 365 Poll event contains an `outputs.body` that can be a single email or a `{ "value": [...] }` collection, independently of Functions invocation cardinality. The Node.js sample types both shapes using SDK models; these TypeScript types do not add runtime validation. Python uses a `typing.List[str]` parameter with one JSON string per Poll event, then uses `TriggerCallbackPayload.from_dict` to parse each event into typed `GraphClientReceiveMessage` objects, including nested attachments and sensitivity labels. One invocation can contain several Poll events, and each event can contain several emails.

The Python sample requires `azure-functions>=2.4.0b1`, which supports the Poll decorator arguments and decoding of batched `collection_string` input:

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

Use `typing.List[str]` rather than the built-in `list[str]` annotation: the 2.4.0b1 binding type check accepts the former.

Linked-output events are delivered one per invocation and serialized host-wide to bound memory. Local .NET isolated smoke testing against a Brazil South resource backed by BrazilUS verified a 96 KiB synthetic attachment through linked delivery, including content integrity and acknowledgement. Broader payload-size, language-worker, and regional validation remains incomplete.

The batch, concurrency, scaling, and polling-interval settings are optional. Their defaults and precedence are listed in the [configuration reference](../../docs/connector-trigger-configuration.md). No Connector-specific `host.json` settings are required; explicit binding values do not need to be repeated as host defaults.

## Local connection settings

Copy each sample's checked-in `local.settings.example.json` to `local.settings.json`, which remains gitignored, then configure:

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

Use .NET SDK 10.0.100 or later, Azure Functions Core Tools v4, and Azurite. The NuGet feed is configured in the repository's `NuGet.config`.

The Python Poll sample requires Python 3.13 or newer because `azure-functions` 2.4.0b1 declares that minimum Python version.

Each Node.js sample's `.npmrc` sets only `omit-lockfile-registry-resolved=true`. Lockfiles retain package versions and integrity hashes without embedding registry URLs. npm feed selection comes from user-level or CI configuration. Run npm commands from the selected sample directory.

If the selected Azure Artifacts feed returns `E401`, configure credentials in your user-level `.npmrc` using the [Azure Artifacts npm authentication guidance](https://learn.microsoft.com/azure/devops/artifacts/npm/npmrc). Never commit credentials to the sample configuration.

## Build and run

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

py -3.13 -m venv .\test\poll\python\.venv
.\test\poll\python\.venv\Scripts\python.exe -m pip install `
    -r .\test\poll\python\requirements.txt
dotnet build .\test\poll\python\PythonPollExtensions.csproj
```

Start Azurite, then run `func start` from the selected sample directory. For Python, activate the sample-local virtual environment first:

```powershell
Set-Location .\test\poll\python
.\.venv\Scripts\Activate.ps1
func start
```

Keep the virtual environment inside the sample directory so the local worker recognizes its SDK dependencies. Merely installing a newer SDK into an external environment does not ensure Core Tools selects it.

Validate the typed sample code without connecting to Azure:

```powershell
Push-Location .\test\poll\nodejs
npm run build
Pop-Location
.\test\poll\python\.venv\Scripts\python.exe -m unittest discover -s .\test\poll\python -p test_function_app.py
```

The Python command assumes the sample's virtual environment has the dependencies from `requirements.txt` installed.
