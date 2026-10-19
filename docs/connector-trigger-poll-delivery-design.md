# Connector Trigger Poll Delivery Design

## Table of Contents

- [Status](#status)
- [Summary](#summary)
- [Goals](#goals)
- [Non-goals](#non-goals)
- [Future Enhancements](#future-enhancements)
- [Current Extension Architecture](#current-extension-architecture)
- [Connector Namespace Runtime Contract](#connector-namespace-runtime-contract)
  - [Receive](#receive)
  - [Large Trigger Outputs](#large-trigger-outputs)
  - [Acknowledge](#acknowledge)
  - [Queue Depth](#queue-depth)
  - [Authentication](#authentication)
  - [Timeouts and Retry Policy](#timeouts-and-retry-policy)
  - [Delivery Semantics](#delivery-semantics)
- [Proposed User Contract](#proposed-user-contract)
  - [Connector Namespace Trigger Configuration](#connector-namespace-trigger-configuration)
  - [Polling Endpoint Configuration](#polling-endpoint-configuration)
  - [Identity and Permissions](#identity-and-permissions)
  - [Ordering Guidance](#ordering-guidance)
  - [Batching and Concurrency](#batching-and-concurrency)
  - [Payload and Message Metadata](#payload-and-message-metadata)
- [Internal Architecture](#internal-architecture)
  - [Polling Endpoint Configuration](#1-polling-endpoint-configuration)
  - [Poll-delivery HTTP Client](#2-poll-delivery-http-client)
  - [Listener Selection](#3-listener-selection)
  - [Polling Listener](#4-polling-listener)
  - [Function Registration and Trigger Values](#5-function-registration-and-trigger-values)
  - [Lifecycle and Shutdown](#6-lifecycle-and-shutdown)
  - [Message Locks and Redelivery](#7-message-locks-and-redelivery)
  - [Scaling](#8-scaling)
  - [Dependency Registration](#9-dependency-registration)
- [Error Handling](#error-handling)
- [Telemetry](#telemetry)
- [Regression Coverage](#regression-coverage)
- [Deferred Linked-Output Release Work](#deferred-linked-output-release-work)
- [Delivery Plan](#delivery-plan)
- [Open Questions](#open-questions)
- [Supporting Information: Provisioning a Poll Trigger Configuration](#supporting-information-provisioning-a-poll-trigger-configuration)
- [Reference](#reference)

## Status

Working implementation design. The stacked Poll branches now include the trigger contract, configuration, protocol models, target scaler, runtime clients, a listener with bounded single-event concurrency and sequential invocation batching, and the isolated-worker binding. The final stacked PR uses the configured, connection-scoped `pollingEndpoint` plus `TriggerConfigName` contract and completes cross-stack validation.

Read the document in three layers:

1. **Connector Namespace Runtime Contract** describes the observed service protocol.
2. **Proposed User Contract** defines the configuration and function-facing contract implemented by the final stacked PR.
3. **Internal Architecture** describes the implemented stack.

## Summary

Connector Namespace supports two event delivery modes for a connector trigger:

| Delivery mode | Behavior |
| --- | --- |
| Webhook | Connector Namespace pushes a callback to the Functions extension webhook. |
| Poll | The Functions host receives leased messages from Connector Namespace and acknowledges successful processing. |

Poll is another delivery mode of the existing Connector trigger, not a different event source. A trigger such as Office 365 `OnNewEmailV3` produces the same trigger output in either mode.

The implementation lives in this extension repository and uses an internal Azure SDK-style HTTP client built with `HttpClient` and `TokenCredential`. It does not require a separate public poll-delivery NuGet package.

Keep the protocol client, listener, scaler, and endpoint configuration behind separate internal interfaces. This preserves a clean extraction path if another runtime later needs the same protocol.

## Goals

- Add Poll as a delivery mode for the existing Connector trigger binding.
- Preserve existing Webhook behavior and compatibility.
- Present the same trigger payload to user functions in either mode.
- Receive and acknowledge messages using Connector Namespace runtime endpoints.
- Respect the service's leasing, redelivery, and acknowledgement semantics.
- Support clean listener startup and shutdown.
- Add scaling support, including scale from zero.
- Keep endpoint configuration separate from data-plane polling.

## Non-goals

- Adding polling methods to generated clients in `Azure.Connectors.Sdk`.
- Publishing a separate poll-delivery package in the first implementation.
- Treating Poll as a connector operation generated from Swagger.
- Providing exactly-once delivery.
- Configuring the service lock duration or queue TTL.
- Constructing polling endpoint URLs from a regional hostname or Connector Namespace resource identifier.

## Future Enhancements

- Extract the Azure SDK-style Poll transport into a Connectors Polling SDK. The SDK should own API Hub authentication, HTTP operations, timeout and retry policy, protocol serialization and validation, and secret-safe errors. The Functions extension should retain binding and listener lifecycle, concurrency and batching, and mapping extension configuration into SDK options.
- Add rich Connector SDK client bindings, similar to client bindings offered by extensions such as Storage. Applications could then bind to generated Connector clients without constructing and managing those clients.
- Integrate Connector Namespace delivery-count, dead-letter, and failed-message observability capabilities after the service publishes those contracts. The extension must not infer delivery attempts or acknowledge permanently failed messages without a service-owned destination.

## Current Extension Architecture

The implemented trigger contract includes:

- `ConnectorTriggerDeliveryMode` defines `Webhook` and `Poll`.
- The host and isolated-worker attributes expose the Poll metadata.
- `Webhook` remains the default.
- `ConnectorTriggerBinding` directly implements `ITriggerBinding`, avoiding obsolete WebJobs binding-strategy APIs.
- `ConnectorTriggerBinding.CreateListenerAsync` selects `ConnectorListener` for Webhook and `ConnectorPollingListener` for Poll.
- `ConnectorListener` still registers the function for webhook routing and otherwise has inert lifecycle methods.
- `ConnectorExtensionConfigProvider` registers the webhook handler and dispatches callback payloads through `ITriggeredFunctionExecutor`.
- `ConnectorPollingListener` runs a lifecycle-safe message pump with bounded single-event concurrency, sequential batch processing per listener, host-wide serialization of linked-output invocations, and acknowledgement of successful single-event or batched invocations.
- Invocation cardinality is explicit. Scalar cardinality supplies one event to each invocation; batched cardinality partitions Receive results into groups of at most the effective `MaxBatchSize`.

The public contract uses independent `MaxBatchSize`, `MaxConcurrentCalls`, and `TargetPendingEventThreshold` properties. `MaxBatchSize` controls events per invocation, `MaxConcurrentCalls` bounds active single-event processing tasks per listener on one worker instance, and `TargetPendingEventThreshold` controls only target-based scaling. Batched listeners process one chunk at a time, including preparation, invocation, and acknowledgement.

Poll delivery must run in the host extension, not in a language worker. This allows the same acquisition behavior to support .NET, Python, Node.js, and other extension-bundle consumers.

## Connector Namespace Runtime Contract

A Poll trigger configuration currently exposes four opaque, server-generated endpoints:

- `receiveUri`
- `acknowledgeUri`
- `hasMessagesUri`
- `approximateQueueDepthUri`

The current APIM URLs have this observed shape:

```text
https://<scale-unit>.<region>.logic.azure.com/api/connectorGateways/<connector-namespace-id>/triggerconfigs/<trigger-config-name>/<operation>
```

Hostname-scoped gateways return this form instead:

```text
https://<connector-namespace-host>/api/triggerconfigs/<trigger-config-name>/<operation>
```

Configure `pollingEndpoint` as follows, without a query string:

| Receive URI | `pollingEndpoint` |
| --- | --- |
| `https://<connector-namespace-host>/api/triggerconfigs/<name>/receive` | `https://<connector-namespace-host>` |
| `https://<host>/api/connectorGateways/<id>/triggerconfigs/<name>/receive` | `https://<host>/api/connectorGateways/<id>` |

For example, `pollingEndpoints.receiveUri` ends in `/triggerconfigs/<name>/receive`. Split the service-returned URI into the gateway-level Poll endpoint and Trigger Config name:

```text
receiveUri:
https://<scale-unit>.<region>.logic.azure.com/api/connectorGateways/<id>/triggerconfigs/<name>/receive

Connection pollingEndpoint:
https://<scale-unit>.<region>.logic.azure.com/api/connectorGateways/<id>

TriggerConfigName:
<name>
```

Do not derive these values with unrestricted string replacement. Preserve the service-provided host and gateway identifier exactly. The gateway-level endpoint is shared by Trigger Configs in that Connector Namespace; each binding supplies its own Trigger Config name.

The extension accepts a hostname-only HTTPS endpoint or an HTTPS legacy endpoint ending at `/api/connectorGateways/<gateway-id>`. Both forms accept one optional trailing slash, which is removed before constructing operation paths. Repeated trailing slashes, a standalone `/api` setting, a Trigger Config path, an operation, or a query string are rejected. Both forms require HTTPS without user information or fragments. The authority and gateway identifier remain opaque, trusted application configuration. The delivery client adds `maxEvents` to Receive requests.

### Receive

```http
GET {receiveUri}?maxEvents={n}
Authorization: Bearer <API Hub token>
```

- `maxEvents` defaults to `32`.
- Valid range: `1` through `32`.
- The response contains a `messages` array.
- `x-ms-more-messages-available` is a best-effort indication that more messages remain.

Each message contains:

```json
{
  "messageId": "stable-message-id",
  "lockToken": "opaque-current-delivery-token",
  "outputs": {}
}
```

### Large Trigger Outputs

Connector Namespace returns outputs inline when the complete encoded queue message, including metadata, fits the 64 KiB queue-storage limit. This is not a 64 KiB raw-output threshold. Larger messages retain a backing-run locator and are projected as an `outputsLink` on Receive without truncating the trigger outputs.

A Receive response may contain both inline and linked messages:

```json
{
  "messages": [
    {
      "messageId": "8b0d8e07-...",
      "lockToken": "<opaque-lock-token>",
      "outputs": {
        "headers": {
          "content-type": "application/json"
        },
        "body": {
          "id": "12345",
          "status": "created"
        }
      }
    },
    {
      "messageId": "d61b1a2c-...",
      "lockToken": "<opaque-lock-token>",
      "outputsLink": {
        "uri": "https://<service-endpoint>/.../contents/triggerOutputs?<signed-parameters>"
      }
    }
  ]
}
```

Each message must contain exactly one output source:

| `outputs` | `outputsLink` | Result |
| --- | --- | --- |
| Present | Absent | Valid inline message |
| Absent | Present | Valid linked message |
| Present | Present | Invalid protocol response |
| Absent | Absent | Invalid protocol response |

The extension normalizes both forms into the same complete trigger `outputs` JSON before worker conversion. Function code must not need to distinguish between inline and linked delivery.

Linked-output processing:

1. Validate `outputsLink.uri`.
2. Download the complete trigger outputs.
3. Enforce `extensions.connector.maxLinkedOutputSizeInBytes` while streaming the response; the default is 100 MiB (104,857,600 bytes).
4. Validate that the downloaded content is complete JSON in the expected trigger-output shape.
5. Pass the normalized outputs through the same payload conversion used for inline messages.
6. Invoke the function.
7. Acknowledge only after content retrieval, conversion, and function execution all succeed.

If linked content cannot be retrieved or validated, the extension must not invoke the function for that message and must not acknowledge it. Other messages from the same Receive response remain independently processable.

Linked outputs use a stricter invocation policy because worker conversion materializes payloads up to the configured download limit:

- Inline messages continue to use normal invocation batching.
- Every linked-output message is delivered in its own invocation. A batched function receives an array containing one event.
- A singleton host-wide limiter permits only one linked-output invocation at a time across all Connector Poll listeners.
- The limiter is process-local. Every scaled-out Functions host receives its own slot; do not use distributed coordination that would serialize linked outputs across instances.
- The limiter is held from before download through function execution and acknowledgement, so another linked payload cannot be hydrated while the first remains live.
- `MaxBatchSize` remains a maximum; it does not guarantee that every invocation contains that number of events.

This follows the serialized batch execution and backpressure patterns used by other Functions extensions. Inline invocations can overlap in single-event mode; batch chunks are sequential per listener. A mixed chunk processes its inline group first and then each linked event individually, retaining its processing capacity until all sub-invocations and acknowledgements finish.

Receive leases messages before the listener knows whether their outputs are inline or linked. Linked messages waiting for the host-wide limiter or earlier events in their chunk retain those leases. Adding hosts does not transfer already-leased messages to those hosts, and time spent waiting consumes the service-managed lease.

The signed `outputsLink.uri` is sensitive:

- Never log, persist, or emit the complete URI.
- Never include its query string in exception messages or telemetry.
- Require an absolute HTTPS URI.
- Reject user information and fragments.
- Do not attach the API Hub bearer token; the URI signature fully authorizes the GET.
- Do not follow redirects. The content endpoint does not return application-level redirects.
- Dispose download responses and streams promptly.

The service does not return `contentSize`. The implementation counts actual bytes read and stops when the configured `maxLinkedOutputSizeInBytes` is exceeded. The host-wide setting defaults to 100 MiB, accepts positive 32-bit integer byte counts, and can be overridden with `AzureFunctionsJobHost__extensions__connector__maxLinkedOutputSizeInBytes`; it has no unlimited mode. This does not change the separate Receive response bound or Webhook HTTP body limit. Byte accounting applies to compact, uncompressed UTF-8 JSON, not the decoded source-file size. The extension default is a guardrail, not a confirmed Connector Namespace maximum. Linked-content responses are `application/json; charset=utf-8` and do not use gzip or Brotli transfer encoding.

The signed GET returns the complete `outputs` object directly, including `headers` and `body`. Repeating the GET is safe and idempotent while the link is valid and the content remains available. No public ETag, checksum, or content hash is currently returned.

Signed-link expiration is service-owned and encoded in the URI. The service signs the link on each Receive, although its text can remain identical within the same expiration hour. Acknowledgement removes the queue message but does not invalidate an issued link. Queue TTL and backing-run content retention are separate lifetimes; deleting the owning trigger or namespace can also make content unavailable. Do not derive a client retention or lease deadline from a service implementation default.

The outputs-link authority can vary by cloud, region, scale unit, and environment. Treat the absolute HTTPS URI as opaque rather than allow-listing a hostname or Azure domain.

The implementation may buffer one complete hydrated linked output in memory because existing worker conversion is JSON-based. It must not retain multiple linked outputs concurrently in one host process.

The configured wire limit is not a process-memory guarantee. The deferred binding envelope is written from the existing UTF-8 payload to avoid an additional host-side UTF-16 escaping buffer. Full buffering, envelope construction and worker decoding still amplify memory use. The host-wide limiter bounds concurrent amplification but does not eliminate the peak for one message. Validate the supported maximum under constrained memory on the target hosting size; reduce the advertised limit or change the worker transport if one message can exhaust the host.

The service owns the message-lock duration and may change it independently. The current response does not expose lock-expiration metadata, so the extension must not infer a fixed client-side deadline.

### Acknowledge

```http
POST {acknowledgeUri}
Authorization: Bearer <API Hub token>
Content-Type: application/json

{
  "messages": [
    {
      "messageId": "stable-message-id",
      "lockToken": "opaque-current-delivery-token"
    }
  ]
}
```

The request accepts at most 32 messages. The response contains an ordered result for each submitted message:

- `Acknowledged`
- `NotFound`
- `Failed`

A successful HTTP response can contain mixed per-message statuses.

### Queue Depth

```http
GET {approximateQueueDepthUri}
```

Approximate queue depth is used only for target scaling and must not be treated as a prerequisite for Receive. The service also returns `hasMessagesUri`, but the extension does not call it because Receive already reports `x-ms-more-messages-available`; a separate preflight request would add latency and create a time-of-check/time-of-use race.

The current service counts both visible and invisible (leased) queue messages. The count is approximate and can lag enqueue, Receive and acknowledgement; it is not the number of immediately receivable messages. The response supplies no separate visible/leased, inline/linked or byte-backlog counts. Consequently, a positive scaling signal can include work already owned by another host.

### Authentication

| Operation | Plane | Token audience/scope |
| --- | --- | --- |
| Receive, acknowledge, queue depth | Runtime data plane | `https://apihub.azure.com/.default` |

### Timeouts and Retry Policy

Receive, acknowledge, and approximate queue-depth HTTP operations use a shared 30-second timeout. Linked-output downloads use a 2-minute timeout. Credential acquisition occurs before the HTTP timeout begins.

Receive and acknowledge requests are not transparently retried after ambiguous failures. If acknowledgement fails, the messages remain unacknowledged and can be redelivered after their locks expire. Long term, the Connectors Polling SDK should own the transport timeout and retry policy. The extension can expose user overrides through `ConnectorOptions` and map them into SDK options after Connector Namespace publishes supported latency and retry guidance.

### Delivery Semantics

- Delivery is at least once.
- The message-lock duration is controlled by Connector Namespace and is not caller-configurable.
- An unacknowledged message becomes visible again with the same `messageId` and a new `lockToken`.
- An expired lock token produces a per-message `NotFound` acknowledgement result.
- Unacknowledged messages can be redelivered without an attempt limit.
- Queue message TTL is fixed at seven days and is not caller-configurable.
- Ordering is not guaranteed.
- `messageId` is the backing queue's message identifier and remains stable on redelivery; `lockToken` represents the current delivery receipt.
- Scope application deduplication to the namespace/trigger configuration together with `messageId`. Do not assume that IDs provide a global ordering key or a cross-trigger uniqueness contract.

## Proposed User Contract

The target Connector trigger binding surface is:

```csharp
public enum ConnectorTriggerDeliveryMode
{
    Webhook,
    Poll,
}
```

Conceptual usage:

```csharp
[Function("OnNewEmail")]
public void Run(
    [ConnectorTrigger(
        DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
        Connection = "ConnectorNamespace",
        TriggerConfigName = "%OnNewEmailTriggerConfigName%",
        IsBatched = true,
        MaxBatchSize = 4,
        TargetPendingEventThreshold = 16)]
    Office365OnNewEmailTriggerPayload[] payloads)
{
    // One batch at a time per listener, with at most four events per invocation.
}
```

`Webhook` must remain the default.

### Connector Namespace Trigger Configuration

As part of setting up Poll delivery, the customer provisions a trigger configuration on the Connector Namespace with:

- The connector connection and trigger operation to poll.
- Any parameters required by that connector operation.
- `deliveryMode` set to `Poll`.
- The trigger configuration enabled before the Function listener starts.

The extension does not create, update, enable, or convert Connector Namespace trigger configurations. Customers obtain `pollingEndpoints.receiveUri` from the provisioned Trigger Config, configure its gateway-level base and Trigger Config name separately, and grant the Function identity an access policy on the connection referenced by that Trigger Config.

### Polling Endpoint Configuration

`Connection` is a literal app setting name or configuration prefix, consistent with other Azure Functions extensions. It is not itself resolved through `%...%` substitution. It identifies the Poll endpoint and credential configuration shared by Functions that use the same Connector Namespace:

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<gateway-id>
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId={optional-user-assigned-managed-identity-client-id}
ConnectorNamespace__managedIdentityResourceId={optional-user-assigned-managed-identity-resource-id}
```

`TriggerConfigName` identifies one Trigger Config and may use Functions app-setting resolution:

```text
OnNewEmailTriggerConfigName=Email-Polling1
```

The setting may contain a direct value, a platform-level Key Vault reference, or a platform-level Azure App Configuration reference. Configuration providers loaded only inside a language worker are insufficient because the host extension must resolve the Trigger Config name before starting the listener.

Use a hostname-only `pollingEndpoint` for stable DNS or the legacy base ending at `/api/connectorGateways/<gateway-id>`. The extension adds `/api` for hostname-only settings, then appends `/triggerConfigs/<TriggerConfigName>` and only:

- `/receive`
- `/acknowledge`
- `/approximateQueueDepth`

Multiple Trigger Configs share the same connection prefix and gateway-level endpoint. `TriggerConfigName` must be one non-empty path segment. The extension performs no Connector Namespace resource-ID parsing, control-plane endpoint discovery, or endpoint caching.

### Identity and Permissions

The Poll endpoint, Trigger Config name, and caller identity are configured as:

```text
ConnectorNamespace__pollingEndpoint
    Gateway-level Poll endpoint

OnNewEmailTriggerConfigName
    Trigger Config name

ConnectorNamespace__credential and identity selectors
    Identity used to access it
```

The extension follows the standard Functions identity-based connection pattern used by Service Bus, Event Hubs, Storage, Event Grid, and Cosmos DB. It passes the complete named connection section to `AzureComponentFactory.CreateTokenCredential`.

| Environment | Configuration | Behavior |
| --- | --- | --- |
| Local development | Omit `credential` | Use the Functions developer-identity behavior provided by `AzureComponentFactory`; for example, the account authenticated through `az login` |
| Azure, system-assigned identity | `credential=managedidentity` | Use only the Function App's system-assigned managed identity |
| Azure, user-assigned identity | `credential=managedidentity` plus `clientId` or `managedIdentityResourceId` | Use the selected user-assigned managed identity |

Poll runtime operations use the API Hub token audience:

| Operation | Token audience/scope |
| --- | --- |
| Receive, acknowledge, and query queue depth | `https://apihub.azure.com/.default` |

The configured endpoint identifies the target Trigger Config. Connector Namespace authorizes the caller represented by the bearer token against the connection used by that Trigger Config.

Required permissions:

| Access | Requirement | Scope |
| --- | --- | --- |
| Poll runtime endpoints | Access policy for the Function identity | Connection referenced by the trigger config |

The runtime access policy is configured under:

```text
Connector Namespace
└── connections/{connectionName}
    └── accessPolicies/{callerObjectId}
```

Obtaining a token for `https://apihub.azure.com/.default` authenticates the identity, and the connection access policy authorizes that identity to use the Poll runtime endpoints.

### Ordering Guidance

Connector Poll delivery does not guarantee ordering. `messageId` is stable across redelivery and is intended for deduplication; it does not define event order.

Connector Namespace does not currently add a sequence number, enqueue time, partition key, or ordering key to the Poll message envelope. The position of a message in a Receive response is also not an ordering contract.

Applications that require ordering must use source-specific ordering information when the connector payload provides it. They may buffer and reorder events in application code or forward events to an ordered downstream system, such as a Service Bus entity using sessions with an appropriate source event identifier as the session ID. The extension cannot infer a universal ordering key across connectors.

### Batching and Concurrency

Batch size, listener concurrency, and target scaling are separate settings:

- Cardinality controls whether the function receives one event or an array.
- `MaxBatchSize` is the maximum number of Connector events supplied to one function invocation.
- `MaxConcurrentCalls` is the maximum number of single-event processing tasks active per listener on one worker instance. It includes preparation, linked-output waiting, invocation, and acknowledgement, and has no CPU-core multiplier.
- `TargetPendingEventThreshold` is the desired number of pending Connector events per worker instance used only for target-based scaling.
- A value of `0` on an applicable attribute property uses the corresponding host-level default.
- `MaxBatchSize = 1` preserves one event per function invocation.
- Effective `MaxBatchSize` must be between `1` and `32`.
- Effective single-event `MaxConcurrentCalls` and `TargetPendingEventThreshold` must be greater than zero.
- Batched listeners process one chunk at a time, even when `MaxBatchSize = 1`. A batch binding should omit `MaxConcurrentCalls` or leave it at zero; a positive trigger value is ignored with a warning at listener startup. Negative values remain invalid. `DefaultMaxConcurrentCalls` applies only to single-event listeners.
- Linked-output messages are invoked individually and host-wide linked-output execution is serialized.

Batching must be explicitly enabled:

- .NET isolated sets `IsBatched = true`.
- Node.js and TypeScript set `cardinality: "many"`.
- Python 3.13+ uses `@app.connector_trigger(cardinality=func.Cardinality.MANY, ...)` with `azure-functions>=2.4.0b1`; annotate its parameter as `typing.List[str]`, with one JSON string per Poll event.
- Generic `function.json` bindings, including PowerShell, set `"cardinality": "many"`.

The effective max batch size must be compatible with the declared cardinality and resulting function parameter:

| Cardinality | Function parameter | Allowed effective `MaxBatchSize` |
| --- | --- | ---: |
| One | `T` | Exactly `1` |
| One | `ConnectorEvent<T>` | Exactly `1` |
| Many | `T[]` | `1` through `32` |
| Many | `ConnectorEvent<T>[]` | `1` through `32` |

Validation uses the effective value after applying applicable host-level defaults. A scalar parameter with `MaxBatchSize = 0` is invalid when `DefaultMaxBatchSize` is greater than `1`.

Applying `MaxConcurrentCalls` only to single-event delivery aligns with the [Service Bus extension's single-message-only setting](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus#hostjson-settings). Connector Poll does not implement Service Bus sessions or session concurrency.

Host-level defaults:

```csharp
public sealed class ConnectorOptions
{
    public int DefaultMaxBatchSize { get; set; } = 1;

    public int DefaultMaxConcurrentCalls { get; set; } = 16;

    public int DefaultTargetPendingEventThreshold { get; set; } = 16;
}
```

The listener receives only when processing capacity is available. Single-event capacity is calculated as:

```csharp
int availableProcessingCapacity =
    effectiveMaxConcurrentCalls - activeProcessingTasks;
int maxEvents = Math.Min(32, availableProcessingCapacity);
```

A batched listener requests at most the effective `MaxBatchSize` events when idle and performs no further Receive until its current chunk finishes. For example, a batched binding with `MaxBatchSize = 4` requests at most four events and processes one chunk at a time; a single-event binding with `MaxConcurrentCalls = 2` requests at most two events when idle. The service can return partial batches. Different listeners or instances can process batches concurrently, but sequential local processing does not guarantee ordering.

`TargetPendingEventThreshold` does not participate in these admission calculations and is not a hard limit on locally pending events.

### Payload and Message Metadata

The extension supports payload-only and metadata-rich bindings in scalar and batched forms:

| Invocation shape | Payload-only parameter | Metadata-rich parameter |
| --- | --- | --- |
| One event | `T` | `ConnectorEvent<T>` |
| Batched events | `T[]` | `ConnectorEvent<T>[]` |

These are parameter-shape examples, not complete trigger declarations. Every Poll binding must also satisfy the configuration contract in [Proposed User Contract](#proposed-user-contract). Batched .NET isolated bindings additionally set `IsBatched = true`; generic language bindings use cardinality `many`. `MaxBatchSize` controls only the maximum number of events in one invocation.

Applications that need the stable Connector delivery `messageId` use a per-event envelope:

```csharp
public sealed class ConnectorEvent<T>
{
    public required T Data { get; init; }

    /// <summary>
    /// Stable Connector Namespace delivery identifier when supplied by the
    /// delivery protocol. Present for Poll and null for Webhook unless the
    /// service later defines an equivalent stable Webhook identifier.
    /// </summary>
    public string? MessageId { get; init; }
}
```

This follows the Kafka and Event Hubs pattern in which metadata remains attached to each event. It is preferred over scalar `[BindingName("messageId")]` parameters or parallel `messageIds[]` arrays because those contracts become ambiguous or index-sensitive for batched invocations.

`ConnectorEvent<T>` exposes only application-safe metadata. Poll always populates `MessageId`. Webhook populates it only if Connector Namespace later supplies an equivalent stable identifier with documented retry semantics; otherwise it is `null`. The extension must not generate a replacement `MessageId`, because an invocation-local identifier would not remain stable across delivery retries.

Do not add speculative metadata such as `CorrelationId`, `DeliveryAttempt`, or `EnqueuedTime` until Connector Namespace supplies those fields and defines their semantics. Public metadata can be extended later without changing the payload type. The `lockToken` is an acknowledgement capability and must remain internal to the host extension.

The extension does not map trigger-config names to `Azure.Connectors.Sdk` model types. The function parameter's declared type remains the source of truth. The worker converter supports:

| Function parameter | Conversion |
| --- | --- |
| `T` | Deserialize one message's `outputs` into `T` |
| `T[]` | Deserialize each message's `outputs` into an array of `T` |
| `ConnectorEvent<T>` | Deserialize `outputs` into `Data` and attach the available `MessageId` |
| `ConnectorEvent<T>[]` | Create one metadata envelope per received message |

For connectors without a generated SDK model, `T` may be `string`, `JsonElement`, `object`, or an application-defined POCO.

`T` represents a concrete function parameter type. Closed generic types are supported:

```csharp
ConnectorEvent<Office365OnNewEmailTriggerPayload>
ConnectorEvent<MyCustomPayload>
ConnectorEvent<JsonElement>
ConnectorEvent<string>
```

The converter identifies a metadata-rich single-event target by inspecting its generic type definition:

```csharp
if (targetType.IsGenericType &&
    targetType.GetGenericTypeDefinition() == typeof(ConnectorEvent<>))
{
    Type payloadType = targetType.GetGenericArguments()[0];
    // Deserialize outputs into payloadType and construct ConnectorEvent<payloadType>.
}
```

For a batch target, the converter inspects the array element type and creates one closed `ConnectorEvent<T>` for every received message.

Open generic function signatures such as `Run<T>(ConnectorEvent<T> message)` are not supported because Azure Functions cannot index an unresolved payload type.

Azure Functions must discover concrete binding types during function indexing. The Connector extension remains connector-agnostic because it uses the closed type declared by the function rather than referencing or registering every generated `Azure.Connectors.Sdk` model.

## Internal Architecture

### 1. Polling Endpoint Configuration

The host resolves `<Connection>__pollingEndpoint` from the connection section and resolves `TriggerConfigName` through Functions app-setting resolution during listener startup and scale-controller reconstruction. One shared endpoint builder accepts an HTTPS hostname-only endpoint or a legacy base ending at `/api/connectorGateways/<non-empty-gateway-id>`. It adds `/api` only for the hostname-only form, validates the Trigger Config name as one path segment, and constructs immutable Receive, Acknowledge, and Approximate Queue Depth URIs.

The authority and gateway identifier remain opaque. The implementation performs no control-plane lookup, resource-identifier parsing, alternate endpoint discovery, or endpoint caching. Invalid connection or binding configuration fails startup before any runtime request or token acquisition.

### 2. Poll-delivery HTTP Client

The runtime client contract is:

```csharp
internal interface IConnectorPollDeliveryClient
{
    Task<ConnectorReceiveResult> ReceiveAsync(
        ConnectorPollingEndpoints endpoints,
        int maxEvents,
        CancellationToken cancellationToken);

    Task<ConnectorAcknowledgeResult> AcknowledgeAsync(
        ConnectorPollingEndpoints endpoints,
        IReadOnlyList<ConnectorMessageLock> messages,
        CancellationToken cancellationToken);
}
```

Queue-depth queries and linked-output downloads use dedicated narrowly scoped collaborators. The linked-output client must not use a general client that automatically attaches bearer tokens to signed content URLs.

Implementation guidance:

- Use `IHttpClientFactory`.
- Use `TokenCredential` for `https://apihub.azure.com/.default`.
- Validate `maxEvents` before sending.
- Merge `maxEvents` into an existing query string safely.
- Preserve validated endpoint identifiers and queries without reconstruction.
- Use explicit JSON models and `System.Text.Json`.
- Deserialize into nullable wire DTOs, then validate once into immutable protocol models.
- Model `outputs` and `outputsLink` as mutually exclusive content sources.
- Preserve inline `outputs` as owned `BinaryData`; do not retain a borrowed `JsonElement` or deserialize into connector-specific models.
- Normalize linked content to the same logical `outputs` representation used by inline messages.
- Apply actual-bytes-read limits to every runtime response. Cap Receive and Acknowledge responses at 100 MiB, use the configured limit for linked outputs, and cap the queue-depth JSON response at 64 KiB.
- Never log bearer tokens, lock tokens, or payload bodies.
- Never log signed outputs-link URLs.
- Preserve unknown acknowledgement statuses as unsuccessful extensible string values so a future service status does not break the entire response.
- Expose enough response metadata for listener decisions and diagnostics.

Avoid automatic HTTP retries for Receive and Acknowledge:

- A lost Receive response may already have leased messages.
- A lost Acknowledge response may already have deleted messages.
- Queue-depth requests can use ordinary bounded transient retries.

### 3. Listener Selection

Keep the existing Webhook listener behavior unchanged.

Use `ConnectorTriggerBinding.CreateListenerAsync` to select:

- `ConnectorListener` for Webhook.
- `ConnectorPollingListener` for Poll.

Do not turn the current class into a large mode-switching listener.

### 4. Polling Listener

`ConnectorPollingListener` owns the message pump:

1. Validate the configured polling base and construct the three runtime routes.
2. Remove completed processing tasks and calculate remaining capacity: one task per batched listener, or `MaxConcurrentCalls` tasks per single-event listener.
3. When capacity is available, call Receive with `maxEvents` equal to `MaxBatchSize` for batched cardinality, or `min(32, MaxConcurrentCalls - activeProcessingTasks)` for single cardinality. Do not Receive at zero capacity.
4. If Receive is empty, apply cancellation-aware backoff with jitter. Reset the failure backoff after every successful Receive.
5. Partition received messages into chunks of at most `MaxBatchSize`.
6. Start one processing task for the received batched chunk, or one per event in single-event mode. Retain capacity through preparation, invocation, and acknowledgement.
7. Within each chunk, group inline messages into one invocation and split linked-output messages into single-event invocations.
8. Process sub-invocations sequentially within that chunk.
9. Before downloading a linked output, acquire the singleton host-wide linked-output limiter and hold it through acknowledgement.
10. Exclude a linked message whose output cannot be retrieved or validated and leave it unacknowledged.
11. Pass normalized `outputs` values and safe metadata through the binding/conversion path.
12. Retain each message's `messageId` and `lockToken` internally.
13. If an invocation succeeds, acknowledge every message in that invocation.
14. If an invocation fails or is cancelled, leave every message in that invocation unacknowledged.
15. If `x-ms-more-messages-available` is true and processing capacity is available, immediately drain another Receive response; batched listeners first wait for their entire current chunk to finish.
16. Otherwise continue using the normal polling cadence.

`TargetPendingEventThreshold` does not participate in listener admission, Receive sizing, batching, or invocation concurrency.

Acknowledgement is all-or-none per function invocation. Partial success within one invocation requires a future explicit per-item result contract; the extension must not infer which messages completed before a function failure.

### 5. Function Registration and Trigger Values

The existing webhook path registers a function in `ConnectorExtensionConfigProvider`. Poll mode does not need webhook routing, but it still needs:

- Function name
- `ITriggeredFunctionExecutor`
- Binding metadata/options

Refactor registration metadata so it is not owned solely by the webhook config provider.

Both delivery modes should preserve the same logical connector payload. Poll transport also carries the stable `messageId` so metadata-rich bindings can create `ConnectorEvent<T>`. Webhook uses the same envelope with a null `MessageId` unless the service defines an equivalent stable identifier. Neither mode carries `lockToken` into user code.

For payload-only bindings, the function receives only the connector `outputs` value or array of values. For metadata-rich bindings, each `outputs` value and its available `messageId` are converted into one `ConnectorEvent<T>`.

The host-side canonical input representation is:

```csharp
internal sealed class ConnectorTriggerEventInput
{
    internal BinaryData Outputs { get; }

    internal string? MessageId { get; }

    internal ConnectorTriggerDeliveryMode DeliveryMode { get; }
}
```

The isolated-worker transport follows the modern deferred-binding approach used by Kafka and Event Hubs so each item retains its metadata during conversion. Exact transport serialization remains internal and does not change the public payload-only shape.

### 6. Lifecycle and Shutdown

`StartAsync`:

- Validate Poll configuration.
- Resolve endpoints.
- Start exactly one message-pump task per listener instance.
- Serialize Start/Stop calls so a restart waits for the previous pump to finish draining before starting a new one.

`StopAsync`:

- Stop issuing new Receive calls.
- Cancel empty-queue waits promptly.
- Use the host's `IDrainModeManager` policy: leave processing uncancelled in drain mode so existing work can finish; otherwise request processing cancellation immediately. Do not impose a listener-owned grace timer.
- Acknowledge completed successes when possible.
- Leave unfinished items unacknowledged for redelivery.
- After the pump completes, clear its lifecycle state and dispose its cancellation sources so the listener can restart. A cancelled Stop wait must not clear a pump that is still running.

`Cancel` and `Dispose` cancel both receiving and processing, regardless of drain mode. `Dispose` must be idempotent and prevent future starts. Dispose cancellation sources only after their message pump completes.

### 7. Message Locks and Redelivery

The service does not return `lockedUntil` or a lock duration, and Connector Namespace may change the duration in the future. The extension therefore does not infer a fixed client-side expiration deadline or skip work based on local elapsed time.

Long-running functions, host failures, acknowledgement failures, and lock expiration can all produce duplicate execution. Applications must implement idempotency or deduplication using `messageId`.

Do not invent client-side lock renewal because the service has no renewal endpoint. If Connector Namespace later returns per-message expiration metadata, the listener can use that service-owned value to avoid starting work that cannot be acknowledged.

### 8. Scaling

Listener polling alone is incomplete because an app at zero instances cannot poll.

Implement a scale monitor or target scaler using approximate queue depth:

- Resolve the same trigger endpoints.
- Query approximate depth with bounded retries.
- The current failure fallback logs a warning and returns zero depth. This does not distinguish an unavailable observation from an empty queue; the extension/controller failure contract is tracked separately in [#44](https://github.com/Azure/azure-functions-connector-extension/issues/44).
- Scale out based on the configured target pending-event threshold per worker instance.
- Avoid equating approximate depth with immediately receivable messages.

Scaling should use a separate service from the listener and poll client.

Target scaling follows the runtime-concurrency precedence used by Event Hubs and Service Bus:

```text
effectiveTarget =
    TargetScalerContext.InstanceConcurrency
    ?? configured TargetPendingEventThreshold

configured TargetPendingEventThreshold =
    trigger TargetPendingEventThreshold when nonzero
    otherwise DefaultTargetPendingEventThreshold

targetWorkerCount =
    ceil(approximateQueueDepth / effectiveTarget)
```

The effective target must be greater than zero. `MaxBatchSize` and `MaxConcurrentCalls` do not participate in the scaling calculation. `InstanceConcurrency` overrides only the scaler's denominator; it does not alter single-event listener concurrency or enable concurrent batches. The configured target is not a hard local pending-event limit, and the formula does not automatically compensate for execution duration or sequential batch processing.

Historical PR #26 is useful only as a reference for the Functions scale-controller integration. Reusable extension-side patterns include:

- `ITargetScaler` and `ITargetScalerProvider`.
- The reflectively discovered `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` registration signature.
- Reading trigger metadata and host-level `ConnectorOptions` inside the scaler provider.
- Calculating target workers from approximate queue depth and the configured target pending-event threshold.
- Scale Monitor validation through trigger registration and scale-status requests.

Do not reuse the Connector Namespace side of #26:

- Its positional `connectorNamespace` and `triggerName` attribute contract.
- Its Namespace API paths, request/response models, or authentication assumptions.
- Its mock queue-depth provider.
- Any metadata names that conflict with the target `Connection` and `TriggerConfigName` contract.

The production metrics provider combines the connection's `pollingEndpoint` with `TriggerConfigName` to construct the Queue Depth URI. The final cross-stack validation verifies that the host and Scale Monitor use the current interfaces and reflective registration signature.

### 9. Dependency Registration

`ConnectorWebJobsBuilderExtensions.AddConnector` registers:

- A default `TokenCredential`.
- A named runtime `HttpClient` or equivalent handler.
- Configured-endpoint validation and route construction.
- Poll-delivery client.
- Poll listener factory.
- Scale provider/monitor.

Register `Microsoft.Extensions.Azure` services and reuse `AzureComponentFactory.CreateTokenCredential` with the named connection section. This matches other first-party Functions extensions and preserves the standard local developer-identity and Azure managed-identity behavior. The component factory also provides the test override seam.

## Error Handling

- Invalid Poll attribute/configuration: fail listener startup with an actionable error.
- Missing or invalid connection `pollingEndpoint` or `TriggerConfigName`: fail startup.
- Unreachable configured endpoint: report the endpoint operation without logging the complete URL or attempting alternate endpoint discovery.
- Authentication/authorization failure: log resource identity and audience, never the token.
- Receive failure: do not assume whether a batch was leased; retry with cancellation-aware exponential backoff capped at 30 seconds.
- Function failure: log and leave every message in the invocation batch unacknowledged.
- Acknowledge `NotFound`: treat as an expired/already-used lock, not an extension crash.
- Acknowledge `Failed`: log per item and allow redelivery.
- Partial acknowledgement: process each result independently.

## Telemetry

Record without payload or token content:

- Function name and a safe trigger identifier; do not record the complete connection `pollingEndpoint`.
- Receive duration and returned message count.
- Linked-output download count, declared size, actual size, and duration.
- Linked-output retrieval and validation failures.
- Pending linked-output permit acquisitions and their wait duration. This count
  excludes the current holder and later leased events in a batched chunk that
  have not yet requested a permit; it is not the complete leased-message count.
- Empty receives and backoff duration.
- Function execution success/failure count.
- Acknowledgement status counts.
- Approximate queue depth and scale decision.
- Stable correlation using hashed or safe identifiers where required.

Duplicate counts require application-level deduplication or a future service
delivery-count contract; the extension must not infer them from process-local
history. The telemetry above describes observability requirements, not a guarantee
that every field is emitted by the released extension.

## Regression Coverage

Maintain coverage for:

- Binding compatibility, defaults, overrides, cardinality and startup validation.
- Stable-DNS and legacy endpoint construction, identity selection and safe protocol handling.
- Configured byte limits, signed-link authentication separation, redaction and bounded retries.
- Inline batching, single-event concurrency, linked-output serialization and success-only acknowledgement.
- Cancellation, draining, restart, lock failures and duplicate delivery.
- Target-scaling arithmetic, runtime `InstanceConcurrency` precedence and queue-depth failures.

## Deferred Linked-Output Release Work

The download, conversion, invocation and acknowledgement paths are implemented. Remaining qualification is tracked in [#39](https://github.com/Azure/azure-functions-connector-extension/issues/39); partial evidence is not a universal support guarantee. Outstanding work is:

- Establish supported payload sizes across language workers and Flex Consumption memory profiles under sustained and mixed workloads, including explicit download-limit overrides. Verify Connector Namespace feature availability and Functions Scale Controller rollout independently; their resources do not need to be in the same region. Other hosting plans are outside the current qualification scope.
- Validate lease aging, memory retention and inline fairness with multiple Poll listeners and longer linked-output backlogs.
- Complete service-backed coverage for expired locks and links, failed invocations, redelivery, retention/deletion, cancellation and shutdown.
- Validate queue-driven scale from zero and longer scale-out scenarios with reliable instance telemetry, including capacity arriving after messages are leased.
- Obtain an authoritative service output-size/accounting contract. Expiration-based admission remains conditional on a future service expiry/renewal contract; current admission does not depend on a client-side lease clock.

Keep the current host-wide limiter until an alternative is shown to preserve memory and lease safety. Do not infer a supported payload size from the configured download limit, or introduce a linked-message scaling multiplier without appropriate service signals and workload evidence.

## Delivery Plan

The stacked PR sequence, completion criteria, and file-level implementation work are maintained in [`connector-trigger-poll-delivery-implementation-plan.md`](connector-trigger-poll-delivery-implementation-plan.md). Keeping delivery tracking in one document prevents the design from becoming stale as branches are implemented and rebased.

## Open Questions

1. Runtime `InstanceConcurrency` already overrides the configured scaling target. How should runtime invocation concurrency map to useful event capacity when linked-output invocations are serialized host-wide?
2. Will Connector Namespace expose per-message expiry or renewal/release capabilities to support lease-aware admission? The current Receive contract supplies none of these.
3. What linked-output payload size can each supported language and host SKU process without unacceptable memory amplification?
4. What workload evidence would justify changing the current one-per-host linked-output admission policy, particularly lease-ahead, waiter bounds and inline fairness?
5. What is Connector Namespace's supported trigger-output maximum and byte-accounting contract, independently of the extension's configurable download limit?

## Supporting Information: Provisioning a Poll Trigger Configuration

The Connector Namespace portal does not currently support provisioning a Poll trigger configuration, and the current `az connector-namespace trigger create` command does not expose `deliveryMode`. Until those surfaces support Poll, use a raw ARM PUT request.

This control-plane request is a customer setup step only and is not used by the Functions extension at runtime.

Create a request body such as:

```json
{
  "properties": {
    "connectionDetails": {
      "connectionName": "<connection-name>",
      "connectorName": "office365"
    },
    "deliveryMode": "Poll",
    "description": "Poll delivery - When a new email arrives",
    "operationName": "OnNewEmailV3",
    "parameters": [
      {
        "name": "folderPath",
        "value": "Inbox"
      }
    ],
    "state": "Enabled",
    "type": "NotSpecified"
  }
}
```

The connector operation and parameters are connector-specific. Provision the trigger configuration with:

```powershell
$connectorNamespaceResourceId = "<connector-namespace-resource-id>"
$triggerConfigName = "<trigger-config-name>"

az rest `
    --method put `
    --uri "$connectorNamespaceResourceId/triggerConfigs/$triggerConfigName?api-version=2026-05-01-preview" `
    --body "@poll-trigger.json"
```

Do not include `pollingEndpoints`, `id`, `name`, `systemData`, or other server-generated response properties in the request body. Connector Namespace generates the polling endpoints.

Create the Poll trigger configuration as `Enabled` by default. If the Function is not ready, it may be created as `Disabled` only as an explicit setup choice; enable it after retrieving the generated polling endpoints, configuring the Function settings, and adding the connection-level access policy.

## Reference

- #26: historical Functions scale-controller integration reference only; its Connector Namespace contract is obsolete.
- Connector Namespace runtime behavior verified against `receive`, `acknowledge`, `hasMessages`, and `approximateQueueDepth`.
- Kafka per-event metadata envelope: `KafkaEventData<T>` in `Azure/azure-functions-kafka-extension`.
- Event Hubs batch metadata model: `EventData[]` in `Azure/azure-sdk-for-net`.
- Modern isolated-worker deferred binding: Kafka and Event Hubs converters in `Azure/azure-functions-dotnet-worker`.
- Earlier package-oriented exploration: `Connectors-NET-SDK/docs/poll-trigger-delivery-design.md`.
