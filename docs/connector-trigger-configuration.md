# Connector Trigger Configuration

Start with [Poll delivery](#poll-delivery) for local development without a public callback. [Webhook delivery](#webhook-delivery) remains the binding default when `DeliveryMode` is omitted.

Both modes require an authorized Connector Namespace connection, the correct connector operation and its inputs, and an enabled Trigger Config. Those are Azure service resources, separate from the Function binding. The extension does not provision them.

## Poll delivery

### Required configuration

| Where | Setting | Requirement |
| --- | --- | --- |
| Function binding | `DeliveryMode` | Set to `Poll`. |
| Function binding | `Connection` | Name of the Function configuration prefix, such as `ConnectorNamespace`; not the Azure connector connection resource name. |
| Function binding | `TriggerConfigName` | The Azure Trigger Config name, supplied as a literal or `%AppSettingName%`. |
| Function settings | `<Connection>__pollingEndpoint` | The connection endpoint derived from the service-generated Receive URI; see the table below. |
| Function settings | App setting referenced by `%AppSettingName%` | Required only when the binding uses that expression. No extra name setting is needed for a literal. |
| Connector Namespace | Trigger Config | Set service `deliveryMode` to `Poll`; do not configure Webhook `notificationDetails`. |
| Connector Namespace | Connection access policies | Authorize both the namespace identity that runs the connector trigger and the Function/developer identity that calls the Poll endpoints. |

Endpoint forms:

| Service `pollingEndpoints.receiveUri` | `<Connection>__pollingEndpoint` |
| --- | --- |
| `https://<connector-namespace-host>/api/triggerconfigs/<name>/receive` | `https://<connector-namespace-host>` |
| `https://<host>/api/connectorGateways/<id>/triggerconfigs/<name>/receive` | `https://<host>/api/connectorGateways/<id>` |

Use a service-provided endpoint in one of these forms; preserve its authority and gateway ID rather than guessing a hostname or removing query parameters from another URL. The extension constructs the operation paths; do not configure a query string, a full Receive URL, or a standalone `/api` base. Endpoints require HTTPS without user information or fragments.

### Identity settings

| Connection setting | Required? | Use |
| --- | --- | --- |
| `<Connection>__credential` | Optional | Omit for the standard developer credential behavior locally. Set to `managedidentity` when selecting managed identity for a deployed app. |
| `<Connection>__clientId` | Optional | Select a user-assigned managed identity by client ID. |
| `<Connection>__managedIdentityResourceId` | Optional | Select a user-assigned managed identity by resource ID, instead of client ID. |

For system-assigned managed identity, enable the identity on the Function App and omit both identity selectors. Whichever identity is selected must have a connection-level access policy. Never commit credentials or real endpoint settings.

### Optional binding parameters (.NET isolated)

| Parameter | Default when omitted | Behavior |
| --- | --- | --- |
| `IsBatched` | `false` | Set to `true` and use an array parameter to receive batches. This does not increase the batch size by itself. |
| `MaxBatchSize` | Host default, initially `1` | Maximum events in one invocation: `1` through `32`. Scalar bindings require an effective value of `1`. |
| `MaxConcurrentCalls` | Host default, initially `16`, for single-event bindings | Maximum active processing tasks per listener on one instance, including invocation and acknowledgement. Omit for batching; positive batch values are ignored with a startup warning. |
| `TargetPendingEventThreshold` | Host default, initially `16` | Pending-event target per instance for target-based scaling. This is independent of batch size and invocation concurrency. |

Omitted or zero `MaxBatchSize` and `TargetPendingEventThreshold` values select their host defaults. `MaxConcurrentCalls` follows that rule only for single-event bindings. Negative values are invalid. Batched listeners always process one batch at a time per listener, including acknowledgement; they do not inherit the single-event concurrency default.

For batches of up to 32, add:

```csharp
IsBatched = true,
MaxBatchSize = 32
```

Use `T[]` or `ConnectorEvent<T>[]`. You do not also need `defaultMaxBatchSize` in `host.json`. Receiving a batch of exactly 32 is not guaranteed.

For other workers, use these equivalents:

| .NET isolated | Node.js / generic binding | Python `@app.connector_trigger` |
| --- | --- | --- |
| `DeliveryMode`, `Connection`, `TriggerConfigName` | `deliveryMode`, `connection`, `triggerConfigName` | `delivery_mode`, `connection`, `trigger_config_name` |
| `IsBatched = true` | `cardinality: "many"` | `cardinality=func.Cardinality.MANY` |
| `MaxBatchSize`, `MaxConcurrentCalls`, `TargetPendingEventThreshold` | `maxBatchSize`, `maxConcurrentCalls`, `targetPendingEventThreshold` | `max_batch_size`, `max_concurrent_calls`, `target_pending_event_threshold` |

The Python Poll test app's native decorators and batch decoding require `azure-functions>=2.4.0b1` and Python 3.13+. Set `delivery_mode=func.DeliveryMode.POLL` and annotate batched parameters as `typing.List[str]`, one JSON string per event. The 2.4.0b1 binding type check does not accept the built-in `list[str]` annotation. With `func.Cardinality.ONE`, use `str` and an effective `max_batch_size` of `1`. Omit `max_concurrent_calls` for batches. See the [language-specific Poll test apps](../test/poll/README.md) for complete bindings.

### Optional `host.json` defaults

No Connector-specific host settings are required. For .NET isolated, the minimal file is:

```json
{
  "version": "2.0"
}
```

To change defaults, place these optional properties under `extensions.connector`:

| Property | Built-in default | Applies to |
| --- | --- | --- |
| `defaultMaxBatchSize` | `1` | Poll bindings that omit `MaxBatchSize` or set it to zero; range `1` through `32`. |
| `defaultMaxConcurrentCalls` | `16` | Single-event Poll bindings that omit `MaxConcurrentCalls` or set it to zero; must be positive. |
| `defaultTargetPendingEventThreshold` | `16` | Poll bindings that omit `TargetPendingEventThreshold` or set it to zero; must be positive. |
| `maxPollingInterval` | `"00:00:30"` | Maximum empty-queue delay; must be at least `"00:00:01"`. This does not control the connector's upstream polling frequency or the platform's scale-check interval. |
| `maxLinkedOutputSizeInBytes` | `104857600` (100 MiB) | Maximum downloaded bytes per Poll `outputsLink`; must be a positive 32-bit integer. Applies to all Poll listeners on the host, not inline Receive responses or Webhook requests. |

Explicit positive binding values override their corresponding host defaults; duplicating them in both places is unnecessary. `maxLinkedOutputSizeInBytes` is host-only, with no per-function binding override. For scaling, a runtime-supplied `InstanceConcurrency` takes precedence over the configured target. The scaler requests `ceil(approximateQueueDepth / effectiveTarget)` instances; it does not change batch size or listener concurrency.

Empty-queue backoff starts at one second, doubles up to `maxPollingInterval`, and resets when a Receive returns messages. Jitter is capped by the idle-delay limit. While more events are available, the listener can poll again without an added delay when processing capacity permits. Receive failures have a separate backoff. See the [concurrency and scaling guide](./connector-poll-concurrency-visual-guide.md).

### Delivery and validation notes

Delivery is at least once. Successful invocations are acknowledged; failed ones can be redelivered after the service-managed lock expires. Scope deduplication to the namespace and trigger configuration together with `ConnectorEvent<T>.MessageId`; do not assume cross-trigger uniqueness. Single-event processing capacity includes linked-output waiting, invocation, and acknowledgement.

Linked outputs are downloaded by the host, normalized to the ordinary payload shape, and invoked one event at a time behind a host-wide limiter. The download limit is not a supported payload-size guarantee; worker conversion also consumes memory. Outstanding hosting, failure and lease validation is described in the [Poll design](./connector-trigger-poll-delivery-design.md#deferred-linked-output-release-work).

### Linked-output download limit

The default limit is 100 MiB (104,857,600 bytes) per linked output. Set `extensions.connector.maxLinkedOutputSizeInBytes` to lower or raise it:

```json
{
  "version": "2.0",
  "extensions": {
    "connector": {
      "maxLinkedOutputSizeInBytes": 104857600
    }
  }
}
```

To override this value through app settings, use `AzureFunctionsJobHost__extensions__connector__maxLinkedOutputSizeInBytes`. Locally, place that setting's string value in `local.settings.json` under `Values`. Restart the host after changing configuration. Zero and negative limits are invalid; there is no unlimited mode.

The limit counts the complete downloaded UTF-8 JSON, including metadata and base64-encoded file content, not just the decoded file size. Both declared `Content-Length` and actual downloaded bytes are checked. An oversized output is not invoked or acknowledged and may be redelivered after its service-managed lock expires; correct the source or configuration to avoid repeated failures.

This is an extension guardrail, not a Connector Namespace service maximum or a supported-payload guarantee. Downloads and worker conversion are buffered, so raising the limit can exhaust host or worker memory even with the host-wide limiter. Validate the complete workload on the selected hosting size before raising it. This option does not change inline Receive bounds, memory allocation, worker transport limits, or the Webhook-only `FUNCTIONS_REQUEST_BODY_SIZE_LIMIT`.

Use the [Poll registration skill](../.github/skills/poll-trigger-registration/SKILL.md) for the ARM request, identity policies, and endpoint derivation.

## Webhook delivery

### Required configuration

| Where | Setting | Requirement |
| --- | --- | --- |
| Function binding | Connector trigger | In .NET, `[ConnectorTrigger]` is sufficient; no Connector-specific attribute parameters are mandatory. |
| Connector Namespace | `notificationDetails.callbackUrl` | Public callback endpoint with `functionName` matching the Function name. |
| Connector Namespace | `notificationDetails.authentication` | For deployed callbacks, `type: QueryString`, `name: code`, and the `connector_extension` system-key value. Keep the key separate from the stored callback URL. |
| Connector Namespace | Namespace identity | Enable the namespace's system-assigned managed identity as described in the registration skill. |

The callback path is:

```text
https://<function-app-host>/runtime/webhooks/connector?functionName=<FunctionName>
```

The Functions host creates the `connector_extension` system key when the extension loads. Local direct calls do not enforce it by default; public dev tunnels must use `func start --enableAuth` and system-key authentication.

### Optional or inapplicable settings

| Setting | Webhook behavior |
| --- | --- |
| `DeliveryMode` | Optional; defaults to `Webhook`. Set it explicitly if that makes the binding clearer. |
| `Connection`, `TriggerConfigName`, `<Connection>__pollingEndpoint` | Not needed by a Webhook-only trigger. A connection used for connector actions is a separate concern. |
| `MaxBatchSize`, `MaxConcurrentCalls`, `TargetPendingEventThreshold`, Poll host defaults | Poll-only controls; do not use them to tune Webhook callbacks. |
| `IsBatched` | Leave omitted for Webhook. Array payload types can describe the connector payload itself; they do not enable Poll-style batching of callbacks. |
| Trigger Config `metadata` | Optional for runtime callback delivery; can identify the Function App destination in the portal. It does not replace callback authentication. |

Webhook-only reception does not require a Function/developer connection access policy. Add one if function code also invokes connector actions.

### Payload size

Webhook requests are subject to the Functions host request-body limit and upstream connector limits. The extension does not impose a separate payload-size cap. Base64-encoded attachments count toward the complete callback JSON size.

The callback body is buffered and passed to the worker as JSON. A larger host limit is not a supported payload-size guarantee; account for memory use and validate the full delivery path before increasing it.

To configure the Functions host limit, set the app-wide `FUNCTIONS_REQUEST_BODY_SIZE_LIMIT` app setting in bytes. For example, `52428800` sets 50 MiB. In Azure, use the Function App's **Environment variables > App settings**. Locally, add the string value to the existing `local.settings.json` `Values` object:

```json
{
  "FUNCTIONS_REQUEST_BODY_SIZE_LIMIT": "52428800"
}
```

Restart the Function App or local host after changing it. This is not a Connector option or a `host.json` property. It applies to HTTP/Webhook request bodies, not Poll linked-output downloads, and cannot bypass hosting-platform or source-service limits. See the [Functions app-setting reference](https://learn.microsoft.com/azure/azure-functions/functions-app-settings#functions_request_body_size_limit).

Use the [Webhook registration skill](../.github/skills/webhook-trigger-registration/SKILL.md) for callback authentication, destination metadata, and dev tunnels, or the [Webhook samples](../test/webhook/README.md) for code.

## Payload types and SDKs

Both modes support raw `string`/JSON and compatible model types. SDK packages are optional when binding raw payloads.

| Language | Optional typed-payload package | SDK repository |
| --- | --- | --- |
| .NET | `Azure.Connectors.Sdk` | [Connectors-NET-SDK](https://github.com/Azure/Connectors-NET-SDK) |
| Python | `azurefunctions-extensions-connectors` / `azure-connectors` | [connectors-python-sdk](https://github.com/Azure/connectors-python-sdk) |
| Node.js / TypeScript | `@azure/connectors` | [Connectors-NodeJS-SDK](https://github.com/Azure/Connectors-NodeJS-SDK) |

`ConnectorEvent<T>` and `ConnectorEvent<T>[]` expose Poll `MessageId` metadata. Webhook has no equivalent stable delivery ID unless the service supplies one. See [operations to function signatures](./operations-functions-match.md) for connector-specific payload shapes.

## Non-.NET workers

Extension bundles contain the host extension NuGet, `Microsoft.Azure.Functions.Extensions.Connector`, not language SDKs. The Python SDK, `azure-functions`, is installed with pip and released independently. Typed Python integration (`azurefunctions-extensions-connectors`) and connector models (`azure-connectors`), when used, are separate pip dependencies as well.

For .NET isolated apps, `Microsoft.Azure.Functions.Worker.Extensions.Connector` is a separate application NuGet dependency, not a language SDK supplied by the host extension bundle.

For Node.js and Python deployments using extension bundles, select a preview bundle containing the required Connector features until they are available in the main bundle. The configuration uses the preview bundle ID:

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
    "version": "[4.42.0, 5.0.0)"
  }
}
```

The example version range is not a minimum-version or feature-availability guarantee; verify the contents of the selected published bundle. Published bundles may lag the current checkout. The [local Poll test apps](../test/poll/README.md) build the host extension directly instead of relying on a published bundle.

The Python Poll test app requires Python 3.13+ and `azure-functions>=2.4.0b1`. The Webhook sample uses the same minimum on Python 3.13+, while retaining `azure-functions>=1.26.0b3` and `PYTHON_ISOLATE_WORKER_DEPENDENCIES=1` for Python 3.10-3.12. Typed Webhook integration additionally uses `azurefunctions-extensions-connectors`.
