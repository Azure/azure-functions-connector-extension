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
  - [Delivery Semantics](#delivery-semantics)
- [Proposed User Contract](#proposed-user-contract)
  - [Connector Namespace Trigger Configuration](#connector-namespace-trigger-configuration)
  - [Current ARM Discovery Configuration](#current-arm-discovery-configuration)
  - [Identity and Permissions](#identity-and-permissions)
  - [Ordering Guidance](#ordering-guidance)
  - [Payload and Message Metadata](#payload-and-message-metadata)
- [Internal Architecture](#internal-architecture)
  - [Endpoint Resolver](#1-endpoint-resolver)
  - [Poll-delivery HTTP Client](#2-poll-delivery-http-client)
  - [Listener Selection](#3-listener-selection)
  - [Polling Listener](#4-polling-listener)
  - [Function Registration and Trigger Values](#5-function-registration-and-trigger-values)
  - [Lifecycle and Shutdown](#6-lifecycle-and-shutdown)
  - [Lock Budget](#7-lock-budget)
  - [Scaling](#8-scaling)
  - [Dependency Registration](#9-dependency-registration)
- [Error Handling](#error-handling)
- [Telemetry](#telemetry)
- [Testing Plan](#testing-plan)
  - [Attribute and binding tests](#attribute-and-binding-tests)
  - [Endpoint resolver tests](#endpoint-resolver-tests)
  - [HTTP client tests](#http-client-tests)
  - [Listener tests](#listener-tests)
  - [Scale tests](#scale-tests)
  - [End-to-end test](#end-to-end-test)
- [Stacked PR Delivery Sequence](#stacked-pr-delivery-sequence)
  - [PR 1: Trigger contract](#pr-1-trigger-contract)
  - [PR 2: Configuration](#pr-2-configuration)
  - [PR 3: Protocol models](#pr-3-protocol-models)
  - [PR 4: Endpoint resolver](#pr-4-endpoint-resolver)
  - [PR 5: Runtime client](#pr-5-runtime-client)
  - [PR 6: Worker binding](#pr-6-worker-binding)
  - [PR 7: Poll listener](#pr-7-poll-listener)
  - [PR 8: Scaling and completion](#pr-8-scaling-and-completion)
- [Initial File-Level Work](#initial-file-level-work)
- [Open Questions](#open-questions)
- [Supporting Information: Provisioning a Poll Trigger Configuration](#supporting-information-provisioning-a-poll-trigger-configuration)
- [Reference](#reference)

## Status

Working implementation design. The stacked Poll branches now include the
trigger contract, configuration, protocol models, target scaler, runtime
clients, a concurrent listener with explicit invocation batching, and the
isolated-worker binding. Final service-backed validation and the
service-exposed per-trigger endpoint setting remain to be delivered.

## Summary

Connector Namespace supports two event delivery modes for a connector trigger:

| Delivery mode | Behavior |
| --- | --- |
| Webhook | Connector Namespace pushes a callback to the Functions extension webhook. |
| Poll | The Functions host receives leased messages from Connector Namespace and acknowledges successful processing. |

Poll is another delivery mode of the existing Connector trigger, not a different event source. A trigger such as Office 365 `OnNewEmailV3` produces the same trigger output in either mode.

The initial implementation should live entirely in this extension repository. Use an internal Azure SDK-style HTTP client built with `HttpClient` and `TokenCredential`; do not require a new public poll-delivery NuGet package for the first implementation.

Keep the protocol client, listener, scaler, and ARM endpoint resolver behind separate internal interfaces. This preserves a clean extraction path if another runtime later needs the same protocol.

## Goals

- Add Poll as a delivery mode for the existing Connector trigger binding.
- Preserve existing Webhook behavior and compatibility.
- Present the same trigger payload to user functions in either mode.
- Receive and acknowledge messages using Connector Namespace runtime endpoints.
- Respect the service's leasing, redelivery, and acknowledgement semantics.
- Support clean listener startup and shutdown.
- Add scaling support, including scale from zero.
- Keep control-plane endpoint discovery separate from data-plane polling.

## Non-goals

- Adding polling methods to generated clients in `Azure.Connectors.Sdk`.
- Publishing a separate poll-delivery package in the first implementation.
- Treating Poll as a connector operation generated from Swagger.
- Providing exactly-once delivery.
- Configuring the service lock duration or queue TTL.
- Constructing polling endpoint URLs from a regional hostname or Connector Namespace resource identifier.

## Future Enhancements

- Add rich Connector SDK client bindings, similar to client bindings offered by extensions such as Storage. This would let applications bind to generated Connector clients without constructing and managing those clients themselves. This is useful beyond Poll delivery but is not required for the initial Poll implementation.

## Current Extension Architecture

The trigger-contract seam has been implemented:

- `ConnectorTriggerDeliveryMode` defines `Webhook` and `Poll`.
- The host and isolated-worker attributes expose the initial Poll metadata.
- `Webhook` remains the default.
- `ConnectorTriggerBinding` directly implements `ITriggerBinding` avoiding obsolete WebJobs binding-strategy
  APIs.
- `ConnectorTriggerBinding.CreateListenerAsync` selects `ConnectorListener`
  for Webhook and `ConnectorPollingListener` for Poll.
- `ConnectorListener` still registers the function for webhook routing and
  otherwise has inert lifecycle methods.
- `ConnectorExtensionConfigProvider` registers the webhook handler and
  dispatches callback payloads through `ITriggeredFunctionExecutor`.
- `ConnectorPollingListener` runs a lifecycle-safe message pump with bounded
  concurrency, sequential linked-output hydration within each invocation
  batch, and acknowledgement of successful single-event or batched
  invocations.
- Invocation cardinality is explicit. Scalar cardinality supplies one event to
  each invocation; batched cardinality partitions Receive results into groups
  of at most the effective `MaxBatchSize`.

The baseline seam used an earlier `MaxEvents` property. PR 1 replaces it with
the public `MaxBatchSize` and `Concurrency` properties described below.
`MaxBatchSize` contributes to the capacity-based service `maxEvents`
calculation, while `Concurrency` remains independent and defines the maximum
concurrent function invocations per instance for target-based scaling.

Poll delivery must run in the host extension, not in a language worker. This allows the same acquisition behavior to support .NET, Python, Node.js, and other extension-bundle consumers.

## Connector Namespace Runtime Contract

A Poll trigger configuration currently exposes four opaque, server-generated
endpoints:

- `receiveUri`
- `acknowledgeUri`
- `hasMessagesUri`
- `approximateQueueDepthUri`

The current APIM URLs have this observed shape:

```text
https://<scale-unit>.<region>.logic.azure.com/api/connectorGateways/<connector-namespace-id>/triggerConfigs/<trigger-config-name>/<operation>
```

For example, `pollingEndpoints.receiveUri` ends in `/receive`. This format is
documented only to explain the current transition; clients must not construct
it from a Connector Namespace name, region, trigger configuration name, or
gateway identifier.

Until the service adds `pollingEndpoints.baseUrl`, a customer that needs the
trigger-specific base URL for Function configuration can derive it from
`receiveUri` by parsing the HTTPS URI and removing only the final `/receive`
path segment:

```text
receiveUri:
https://<authority>/api/connectorGateways/<id>/triggerConfigs/<name>/receive

PollingEndpoint base:
https://<authority>/api/connectorGateways/<id>/triggerConfigs/<name>
```

Do not derive the base with an unrestricted string replacement. Validate that
the final path segment is exactly `receive`, remove that segment, and preserve
the remaining authority and path as opaque. In a future service deployment,
the Trigger Config response will expose this value directly as
`pollingEndpoints.baseUrl`.

Clients must obtain these URLs from the trigger configuration response. Each
trigger configuration has its own base URL; base URLs are not shared across a
Connector Namespace. The authority may contain a gateway GUID now or after a
future DNS migration. Clients must not construct a hostname from the Connector
Namespace name or assume a pattern such as
`<namespace>.connectornamespace.net/triggerConfigs/<triggerConfigName>`.

Existing APIM polling URLs remain valid when the service moves to DNS. The
service plans to expose the per-trigger polling base URL in Trigger Config
properties so customers can place it in Function settings or ARM deployments.
That property is expected in the next service deployment. Until it is
available, the extension continues to consume the complete endpoints returned
by the current Trigger Config ARM response.

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

Connector Namespace currently returns trigger outputs inline when they fit within the 64 KB queue message-size limit. Larger outputs are returned through an `outputsLink` without truncating the complete trigger outputs.

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

The extension normalizes both forms into the same complete trigger `outputs`
JSON before worker conversion. Function code must not need to distinguish
between inline and linked delivery.

Linked-output processing:

1. Validate `outputsLink.uri`.
2. Download the complete trigger outputs.
3. Enforce configured and absolute byte limits while streaming the response,
   with an absolute maximum of 100 MiB (104,857,600 bytes).
4. Validate that the downloaded content is complete JSON in the expected
   trigger-output shape.
5. Pass the normalized outputs through the same payload conversion used for
   inline messages.
6. Invoke the function.
7. Acknowledge only after content retrieval, conversion, and function
   execution all succeed.

If linked content cannot be retrieved or validated, the extension must not
invoke the function for that message and must not acknowledge it. Other
messages from the same Receive response remain independently processable.

The signed `outputsLink.uri` is sensitive:

- Never log, persist, or emit the complete URI.
- Never include its query string in exception messages or telemetry.
- Require an absolute HTTPS URI.
- Reject user information and fragments.
- Do not attach the API Hub bearer token; the URI signature fully authorizes
  the GET.
- Do not follow redirects. The content endpoint does not return
  application-level redirects.
- Dispose download responses and streams promptly.

The service does not return `contentSize`. The implementation must count
actual bytes read and stop when the configured or absolute 100 MiB maximum is
exceeded. The service limit is calculated from compact, uncompressed UTF-8
JSON. Linked-content responses are `application/json; charset=utf-8` and do
not use gzip or Brotli transfer encoding.

The signed GET returns the complete `outputs` object directly, including
`headers` and `body`. Repeating the GET is safe and idempotent while the link
is valid and the content remains available. No public ETag, checksum, or
content hash is currently returned.

Signed links remain valid for three to four hours; the exact expiration is
encoded in the URI. The service signs the link on every Receive, although the
text can remain identical within the same expiration hour. The lock token is
always renewed on redelivery. Acknowledgement removes the queue message but
does not invalidate an issued link. Linked content is normally deleted by
eight-day retention cleanup, or earlier if the trigger or Connector Namespace
is deleted.

The outputs-link authority can vary by cloud, region, scale unit, and
environment. Treat the absolute HTTPS URI as opaque rather than allow-listing
a hostname or Azure domain.

The first implementation may buffer one complete hydrated output in memory
because existing worker conversion is JSON-based, but it must not download all
large outputs in a Receive batch simultaneously. Linked-output hydration must
be bounded and included in concurrency and memory-budget decisions.

The two-minute message lock starts when Receive leases the message. Download
and validation time therefore consume lock budget and must be included in
lock-budget telemetry and execution-admission decisions.

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

Approximate queue depth is used only for target scaling and must not be treated
as a prerequisite for Receive. The service also returns `hasMessagesUri`, but
the extension does not call it because Receive already reports
`x-ms-more-messages-available`; a separate preflight request would add latency
and create a time-of-check/time-of-use race.

### Authentication

| Operation | Plane | Token audience/scope |
| --- | --- | --- |
| Read trigger configuration | ARM control plane | `https://management.azure.com/.default` |
| Receive, acknowledge, queue depth | Runtime data plane | `https://apihub.azure.com/.default` |

### Delivery Semantics

- Delivery is at least once.
- The message lock is exactly two minutes and is not caller-configurable.
- An unacknowledged message becomes visible again with the same `messageId` and a new `lockToken`.
- An expired lock token produces a per-message `NotFound` acknowledgement result.
- Unacknowledged messages can be redelivered without an attempt limit.
- Queue message TTL is fixed at seven days and is not caller-configurable.
- Ordering is not guaranteed.
- Consumers must use `messageId` as the deduplication key.
- The current design assumes `messageId` is unique across Poll deliveries.
  Connector Namespace confirmation of the exact uniqueness scope is pending.

## Proposed User Contract

Add delivery mode and Poll configuration to both trigger attributes:

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
        TriggerConfigName = "%CONNECTOR_TRIGGER_CONFIG_NAME%",
        IsBatched = true,
        MaxBatchSize = 4,
        Concurrency = 8)]
    Office365OnNewEmailTriggerPayload[] payloads)
{
    // Four events per invocation, with up to eight concurrent invocations.
}
```

`Webhook` must remain the default.

### Connector Namespace Trigger Configuration

As part of setting up Poll delivery, the customer provisions a trigger configuration on the Connector Namespace with:

- The connector connection and trigger operation to poll.
- Any parameters required by that connector operation.
- `deliveryMode` set to `Poll`.
- The trigger configuration enabled before the Function listener starts.

The Connector Namespace portal does not currently support provisioning a Poll trigger configuration, and the current `az connector-namespace trigger create` command does not expose `deliveryMode`. Until those surfaces support Poll, provisioning must use a raw ARM PUT request, such as `az rest --method put`, with `deliveryMode` set to `Poll`. Customer documentation must provide that provisioning flow separately from the Function trigger configuration.

The Function's `TriggerConfigName` identifies this service-side trigger configuration. It is not the Function name, and the Functions extension does not provision it.

The extension does not create, update, enable, or convert Connector Namespace trigger configurations. It reads the named trigger configuration, obtains its server-generated polling endpoints, and validates that it is enabled and uses Poll delivery. Customers must also grant the Function identity an access policy on the connection referenced by that trigger configuration.

### Current ARM Discovery Configuration

`Connection` is a literal app setting name or configuration prefix, consistent with other Azure Functions extensions. It is not itself resolved through `%...%` substitution. The Connector Namespace resource ID and credential configuration belong in app settings rather than function metadata:

```text
ConnectorNamespace__resourceId=/subscriptions/{subscription}/resourceGroups/{resource-group}/providers/Microsoft.Web/connectorGateways/{gateway}
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId={optional-user-assigned-managed-identity-client-id}
ConnectorNamespace__managedIdentityResourceId={optional-user-assigned-managed-identity-resource-id}
```

`TriggerConfigName` remains trigger metadata because it identifies the event source within the namespace. It should support Functions name resolution so environment-specific configuration is not embedded in attributes.

This describes the current ARM-discovery contract only. The replacement
contract will use the trigger-specific base URL exposed as
`pollingEndpoints.baseUrl`. The configured `PollingEndpoint` includes the
`/triggerConfigs/<triggerConfigName>` path and therefore identifies the Trigger
Config. The extension appends only `/receive`, `/acknowledge`, and
`/approximateQueueDepth`. `TriggerConfigName` is removed from the future Poll
runtime contract when this endpoint contract replaces ARM discovery.

The full Connector Namespace resource ID is required by the current service contract because the polling endpoints are exposed by the ARM GET operation for a trigger configuration. A Connector Namespace name alone does not identify its subscription and resource group and is not sufficient to build that request. Using the full ID also permits a Function App and Connector Namespace to reside in different resource groups or subscriptions when authorization allows it.

This differs from bindings such as Service Bus and Event Hubs, where the configured fully qualified namespace is itself a stable data-plane endpoint. Connector Poll currently requires an ARM bootstrap step:

```text
Connector Namespace resource ID + trigger config name
    -> ARM GET trigger configuration
    -> opaque polling endpoints
```

This is a requirement of the current ARM resolver, not an intrinsic part of
the public Poll trigger contract. Endpoint discovery stays behind
`IConnectorPollingEndpointResolver` so the service-exposed per-trigger base URL
can replace the ARM bootstrap path. That endpoint-contract PR intentionally
changes the Poll trigger attribute by adding `PollingEndpoint` and removing
`TriggerConfigName`.

Before listener startup succeeds, the resolver must verify that the referenced trigger configuration:

- Exists and is enabled.
- Has `deliveryMode` set to `Poll`.
- Returns all required polling endpoints.

A Webhook trigger configuration cannot be used by a Function configured for Poll delivery.

### Identity and Permissions

The target Connector Namespace and the caller identity are separate:

```text
ConnectorNamespace__resourceId
    Target Connector Namespace

ConnectorNamespace__credential and identity selectors
    Identity used to access it
```

The extension follows the standard Functions identity-based connection pattern used by Service Bus, Event Hubs, Storage, Event Grid, and Cosmos DB. It passes the complete named connection section to `AzureComponentFactory.CreateTokenCredential`.

| Environment | Configuration | Behavior |
|---|---|---|
| Local development | Omit `credential`, or use `credential=managedidentity` with an unavailable MI selector | Use the Functions developer-identity behavior provided by `AzureComponentFactory`; for example, the account authenticated through `az login` |
| Azure, system-assigned identity | `credential=managedidentity` | Use the Function App's system-assigned managed identity; if the managed identity endpoint reports authentication unavailable, the extension falls back to `DefaultAzureCredential` for local/private-stamp diagnostics |
| Azure, user-assigned identity | `credential=managedidentity` plus `clientId` or `managedIdentityResourceId` | Use the selected user-assigned managed identity |

Authentication uses two token audiences:

| Operation | Token audience/scope |
|---|---|
| Read the trigger configuration through ARM | `https://management.azure.com/.default` |
| Receive, acknowledge, and query queue depth | `https://apihub.azure.com/.default` |

The request URI identifies the target Connector Namespace; the credential does not receive or infer that target resource ID. ARM and Connector Namespace authorize the caller represented by the bearer token against the requested resource.

When Scale Controller hosts the scaler, it may inject dedicated app-identity credentials for ARM and API Hub through `ConnectorScaleCredentialProperties`. The extension routes ARM discovery to the former and queue-depth operations to the latter. This lets Scale Controller use its existing fixed-audience `ManagedIdentityTokenCredential` without making the extension impersonate the Function App.

ARM endpoint discovery requires the following control-plane action:

```text
Microsoft.Web/connectorGateways/triggerconfigs/read
```

Required permissions:

| Access | Requirement | Scope |
|---|---|---|
| ARM endpoint discovery | `Microsoft.Web/connectorGateways/triggerconfigs/read`, included in the built-in **Reader** role | Connector Namespace resource, or inherited from its resource group or subscription |
| Poll runtime endpoints | Access policy for the Function identity | Connection referenced by the trigger config |

**Reader** is an Azure built-in role definition; it is not assigned to the Function App automatically. The Function identity must receive an explicit Reader role assignment covering the target Connector Namespace unless it already inherits Reader from the target resource group or subscription. This assignment is also required when the Connector Namespace is in another subscription.

The runtime access policy is configured under:

```text
Connector Namespace
└── connections/{connectionName}
    └── accessPolicies/{callerObjectId}
```

Obtaining a token for `https://apihub.azure.com/.default` authenticates the identity, and the connection access policy authorizes that identity to use the Poll runtime endpoints.

A service-backed queue-depth authorization test confirmed this separation:
the same API Hub token and endpoint returned `200 OK` with the connection
access policy, `403 Forbidden` after the policy was removed, and `200 OK`
after the policy was restored.

### Ordering Guidance

Connector Poll delivery does not guarantee ordering. `messageId` is stable across redelivery and is intended for deduplication; it does not define event order.

Connector Namespace does not currently add a sequence number, enqueue time, partition key, or ordering key to the Poll message envelope. The position of a message in a Receive response is also not an ordering contract.

Applications that require ordering must use source-specific ordering information when the connector payload provides it. They may buffer and reorder events in application code or forward events to an ordered downstream system, such as a Service Bus entity using sessions with an appropriate source event identifier as the session ID. The extension cannot infer a universal ordering key across connectors.

Max batch size and concurrency are separate settings:

- Cardinality controls whether the function receives one event or an array.
- `MaxBatchSize` is the maximum number of Connector events supplied to one
  function invocation. Receive capacity may cover multiple concurrent
  invocation batches.
- `Concurrency` is the maximum number of concurrent function invocations allowed on one worker instance.
- A value of `0` on either attribute property means to use the host-level default.
- `MaxBatchSize = 1` preserves one event per function invocation.
- `MaxBatchSize` must be `0` to use the host-level default, or between `1` and
  `32`; the effective value is always between `1` and `32`.
- `Concurrency` must be zero or greater; the effective value must be greater
  than zero.
- `MaxBatchSize` controls batching only and does not participate in the target-based scaling calculation.
- Maximum in-flight messages are approximately `MaxBatchSize * Concurrency`.

Batching must be explicitly enabled:

- .NET isolated sets `IsBatched = true`.
- Node.js and TypeScript set `cardinality: "many"`.
- Python sets `cardinality=func.Cardinality.MANY`.
- Generic `function.json` bindings, including PowerShell, set
  `"cardinality": "many"`.

The effective max batch size must be compatible with the declared
cardinality and resulting function parameter:

| Cardinality | Function parameter | Allowed effective `MaxBatchSize` |
|---|---|---:|
| One | `T` | Exactly `1` |
| One | `ConnectorEvent<T>` | Exactly `1` |
| Many | `T[]` | `1` through `32` |
| Many | `ConnectorEvent<T>[]` | `1` through `32` |

Validation uses the effective value after applying host-level defaults. A
scalar parameter with `MaxBatchSize = 0` is therefore invalid when
`DefaultMaxBatchSize` is greater than `1`.

The language binding or worker converter that can see the real target type
must reject an incompatible scalar binding during function indexing or
listener startup with an actionable error. Host-side PR 1 cannot reliably
infer the target shape for every language worker, so this validation belongs
to PR 6. The extension must not silently ignore `MaxBatchSize`, truncate received
events, or automatically change the function parameter shape.

`Concurrency` is independent of parameter shape. For example, a scalar
parameter with `MaxBatchSize = 1` and `Concurrency = 8` is valid and permits
up to eight concurrent single-event invocations. With a batched parameter,
the same concurrency permits up to eight concurrent invocation batches.

An event counts as in flight from the time Receive leases it until the
listener acknowledges it or finishes handling a failed attempt without
acknowledgement. This includes linked-output hydration, function execution,
and acknowledgement processing.

Host-level defaults:

```csharp
public sealed class ConnectorOptions
{
    public int DefaultConcurrency { get; set; } = 16;

    public int DefaultMaxBatchSize { get; set; } = 1;
}
```

The service `maxEvents` query parameter is calculated from the remaining
invocation capacity and capped at 32:

```csharp
int availableInvocationSlots =
    effectiveConcurrency - activeInvocationCount;

int availableMessageCapacity =
    availableInvocationSlots * effectiveMaxBatchSize;

int maxEvents = Math.Min(
    32,
    availableMessageCapacity);
```

The listener issues Receive only when `maxEvents > 0` and always sends the calculated value explicitly. It must not rely on the service default of 32 because that could lease more messages than the worker can immediately process.

Examples:

| Max batch size | Concurrency | Active invocations | `maxEvents` |
| ---: | ---: | ---: | ---: |
| 1 | 16 | 0 | 16 |
| 1 | 16 | 10 | 6 |
| 4 | 8 | 0 | 32 |
| 4 | 8 | 3 | 20 |
| 8 | 2 | 0 | 16 |
| 32 | 16 | 0 | 32 |
| 4 | 8 | 8 | 0 |

The listener does not prefetch beyond remaining invocation capacity. Every
received message immediately starts its fixed two-minute lock budget, so
leasing messages into a local waiting buffer increases lock expiration and
duplicate-delivery risk.

When the concurrent-invocation limit is reached, the listener does not issue
Receive. Even when `x-ms-more-messages-available` is true, immediate draining
occurs only when invocation capacity is available.

### Payload and Message Metadata

The extension supports payload-only and metadata-rich bindings in scalar and
batched forms:

| Invocation shape | Payload-only parameter | Metadata-rich parameter |
| --- | --- | --- |
| One event | `T` | `ConnectorEvent<T>` |
| Batched events | `T[]` | `ConnectorEvent<T>[]` |

These are parameter-shape examples, not complete trigger declarations. Every
Poll binding must also satisfy the configuration contract in
[Proposed User Contract](#proposed-user-contract). Batched .NET isolated
bindings additionally set `IsBatched = true`; generic language bindings use
cardinality `many`. `MaxBatchSize` controls only the maximum number of events
in one invocation.

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

`ConnectorEvent<T>` exposes only application-safe metadata. Poll always
populates `MessageId`. Webhook populates it only if Connector Namespace later
supplies an equivalent stable identifier with documented retry semantics;
otherwise it is `null`. The extension must not generate a replacement
`MessageId`, because an invocation-local identifier would not remain stable
across delivery retries.

Do not add speculative metadata such as `CorrelationId`, `DeliveryAttempt`, or
`EnqueuedTime` until Connector Namespace supplies those fields and defines their
semantics. Public metadata can be extended later without changing the payload
type. The `lockToken` is an acknowledgement capability and must remain internal
to the host extension.

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

Open generic function signatures such as
`Run<T>(ConnectorEvent<T> message)` are not supported because Azure Functions
cannot index an unresolved payload type.

Azure Functions must discover concrete binding types during function indexing. The Connector extension remains connector-agnostic because it uses the closed type declared by the function rather than referencing or registering every generated `Azure.Connectors.Sdk` model.

## Internal Architecture

### 1. Endpoint Resolver

Introduce:

```csharp
internal interface IConnectorPollingEndpointResolver
{
    Task<ConnectorPollingEndpoints> ResolveAsync(
        ConnectorPollingTriggerOptions options,
        CancellationToken cancellationToken);
}
```

Responsibilities:

- Read the trigger configuration through ARM.
- Authenticate using the ARM audience.
- Extract the three `pollingEndpoints` URLs consumed by the extension.
- Validate that required endpoints are absolute HTTPS URLs.
- Resolve the namespace resource ID and credential from the named connection configuration.
- Cache resolved endpoints by connection and trigger-config name.

The resolver must not be part of the runtime data-plane client.

> **Future configured-endpoint contract:** When the Trigger Config property is
> deployed, customers will provide its opaque, per-trigger polling base URL
> through a `PollingEndpoint` binding property that resolves from a Function
> app setting, such as `%OnNewEmail_Endpoint%`. The value is the complete
> `pollingEndpoints.baseUrl`, including Trigger Config identity. `Connection`
> remains shared by Functions that use the same Connector Namespace. Remove
> `TriggerConfigName` from Poll configuration, along with the ARM endpoint
> resolver and endpoint cache. Never derive the authority from the Connector
> Namespace name; the URL may contain a gateway GUID and is not shared with
> other trigger configurations. Treat the configured base as authoritative. If
> a derived Poll route cannot be reached or returns an endpoint-specific
> `404 Not Found` or `410 Gone`, report an explicit configured-endpoint failure
> instead of querying ARM for replacement URLs. Existing APIM URLs remain valid
> through the service's DNS migration.

### 2. Poll-delivery HTTP Client

Introduce an internal client:

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

Queue-depth queries and linked-output downloads use dedicated narrowly scoped
collaborators. The linked-output client must not use a general client that
automatically attaches bearer tokens to signed content URLs.

Implementation guidance:

- Use `IHttpClientFactory`.
- Use `TokenCredential` for `https://apihub.azure.com/.default`.
- Validate `maxEvents` before sending.
- Merge `maxEvents` into an existing query string safely.
- Treat endpoint URLs as opaque.
- Use explicit JSON models and `System.Text.Json`.
- Deserialize into nullable wire DTOs, then validate once into immutable protocol models.
- Model `outputs` and `outputsLink` as mutually exclusive content sources.
- Preserve inline `outputs` as owned `BinaryData`; do not retain a borrowed `JsonElement` or deserialize into connector-specific models.
- Normalize linked content to the same logical `outputs` representation used
  by inline messages.
- Apply actual-bytes-read limits to linked outputs, capped at 100 MiB.
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

1. Resolve polling endpoints.
2. Determine available invocation capacity from effective `MaxBatchSize` and `Concurrency`.
3. Call Receive with `maxEvents` capped by 32 and no greater than current processing capacity.
4. If Receive is empty, apply cancellation-aware backoff with jitter.
5. Hydrate linked outputs using bounded content-download concurrency.
6. Exclude messages whose linked outputs could not be retrieved or validated;
   leave them unacknowledged.
7. Partition successfully hydrated messages into invocation batches of at
   most `MaxBatchSize`.
8. Dispatch no more than `Concurrency` function invocations at once.
9. For each invocation batch, pass normalized `outputs` values and safe
    metadata through the binding/conversion path.
10. Retain each message's `messageId` and `lockToken` internally.
11. If an invocation succeeds, mark every message in that invocation batch
    for acknowledgement.
12. If an invocation fails or is cancelled, leave every message in that
    invocation batch unacknowledged.
13. Batch-acknowledge successful message locks in requests of at most 32 items.
14. If `x-ms-more-messages-available` is true and invocation capacity is
    available, immediately drain another Receive batch.
15. Otherwise continue using the normal polling cadence.

Concurrency counts function invocations, not individual messages. For
example, `MaxBatchSize = 4` and `Concurrency = 8` permits up to eight active
invocations and approximately 32 in-flight messages.

Acknowledgement is all-or-none per function invocation. Partial success within
one invocation requires a future explicit per-item result contract; the
extension must not infer which messages completed before a function failure.

### 5. Function Registration and Trigger Values

The existing webhook path registers a function in `ConnectorExtensionConfigProvider`. Poll mode does not need webhook routing, but it still needs:

- Function name
- `ITriggeredFunctionExecutor`
- Binding metadata/options

Refactor registration metadata so it is not owned solely by the webhook config provider.

Both delivery modes should preserve the same logical connector payload. Poll transport also carries the stable `messageId` so metadata-rich bindings can create `ConnectorEvent<T>`. Webhook uses the same envelope with a null `MessageId` unless the service defines an equivalent stable identifier. Neither mode carries `lockToken` into user code.

For payload-only bindings, the function receives only the connector `outputs` value or array of values. For metadata-rich bindings, each `outputs` value and its available `messageId` are converted into one `ConnectorEvent<T>`.

The host-side canonical message representation is:

```csharp
internal sealed record ConnectorTriggerMessage(
    string MessageId,
    string LockToken,
    JsonElement Outputs);
```

The isolated-worker transport follows the modern deferred-binding approach
used by Kafka and Event Hubs so each item retains its metadata during
conversion. Exact transport serialization remains internal and does not change
the public payload-only shape.

### 6. Lifecycle and Shutdown

`StartAsync`:

- Validate Poll configuration.
- Resolve endpoints.
- Start exactly one message-pump task per listener instance.

`StopAsync` and `Cancel`:

- Stop issuing new Receive calls.
- Cancel empty-queue waits promptly.
- Allow in-flight function executions a bounded completion period.
- Acknowledge completed successes when possible.
- Leave unfinished items unacknowledged for redelivery.

`Dispose` must be idempotent.

### 7. Lock Budget

The service does not return `lockedUntil`; the listener only knows the fixed two-minute duration.

The extension should:

- Record the local Receive completion time.
- Emit a warning when processing approaches the lock limit.
- Avoid beginning new work from a batch when too little lock budget remains.
- Accept that long-running functions can produce duplicate execution.
- Document application-level deduplication using `messageId`.

Do not invent client-side lock renewal because the service has no renewal endpoint.

### 8. Scaling

Listener polling alone is incomplete because an app at zero instances cannot poll.

Implement a scale monitor or target scaler using approximate queue depth:

- Resolve the same trigger endpoints.
- Query approximate depth with bounded retries.
- Return no-work/scale-in decisions conservatively.
- Scale out based on effective invocation concurrency.
- Avoid equating approximate depth with immediately receivable messages.

Scaling should use a separate service from the listener and poll client.

Target scaling uses invocation concurrency only:

```text
targetWorkerCount =
    ceil(approximateQueueDepth / effectiveConcurrency)
```

`Concurrency` is the effective number of active function invocations per
worker and therefore the target-scaling capacity. `MaxBatchSize` controls
listener invocation grouping; it does not reduce the worker target. Keeping
it out of target arithmetic avoids under-scaling for partially filled batches
or when approximate depth does not map to immediately receivable full
batches. Connector Namespace `maxEvents` remains the listener calculation
`min(32, (effectiveConcurrency - activeInvocations) *
effectiveMaxBatchSize)`.

Historical PR #26 is useful only as a reference for the Functions
scale-controller integration. Reusable extension-side patterns include:

- `ITargetScaler` and `ITargetScalerProvider`.
- The reflectively discovered
  `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` registration
  signature.
- Reading trigger metadata and host-level `ConnectorOptions` inside the scaler
  provider.
- Calculating target workers from approximate queue depth and effective
  invocation concurrency:

  ```text
  ceil(pendingEvents / effectiveConcurrency)
  ```

- Scale Monitor validation through trigger registration and scale-status
  requests.

Do not reuse the Connector Namespace side of #26:

- Its positional `connectorNamespace` and `triggerName` attribute contract.
- Its Namespace API paths, request/response models, or authentication
  assumptions.
- Its mock queue-depth provider.
- Any metadata names that conflict with the current `Connection` and
  `TriggerConfigName` contract.

The production metrics provider must use the current endpoint-discovery and
runtime contracts in this document, specifically the server-provided
`approximateQueueDepthUri`. Before implementing PR 8, verify that the host and
Scale Monitor still require the interfaces and reflective registration
signature demonstrated by #26.

### 9. Dependency Registration

Update `ConnectorWebJobsBuilderExtensions.AddConnector` to register:

- A default `TokenCredential`.
- Named ARM and runtime `HttpClient` instances or equivalent handlers.
- Endpoint resolver.
- Poll-delivery client.
- Poll listener factory.
- Scale provider/monitor.

Register `Microsoft.Extensions.Azure` services and reuse `AzureComponentFactory.CreateTokenCredential` with the named connection section. This matches other first-party Functions extensions and preserves the standard local developer-identity and Azure managed-identity behavior. The component factory also provides the test override seam.

## Error Handling

- Invalid Poll attribute/configuration: fail listener startup with an actionable error.
- Trigger config missing or not in Poll mode: fail startup.
- Missing polling endpoints: fail startup.
- Authentication/authorization failure: log resource identity and audience, never the token.
- Receive failure: do not assume whether a batch was leased; retry only after backoff.
- Function failure: log and leave the message unacknowledged.
- Acknowledge `NotFound`: treat as an expired/already-used lock, not an extension crash.
- Acknowledge `Failed`: log per item and allow redelivery.
- Partial acknowledgement: process each result independently.

## Telemetry

Record without payload or token content:

- Function name and trigger-config name.
- Receive duration and returned message count.
- Linked-output download count, declared size, actual size, and duration.
- Linked-output retrieval and validation failures.
- Empty receives and backoff duration.
- Function execution success/failure count.
- Acknowledgement status counts.
- Lock-budget warnings.
- Approximate queue depth and scale decision.
- Stable correlation using hashed or safe identifiers where required.

## Testing Plan

### Attribute and binding tests

- Webhook remains the default.
- Poll properties flow from worker metadata into the host attribute.
- Attribute `MaxBatchSize` and `Concurrency` overrides flow correctly.
- Zero-valued overrides use host-level defaults.
- Max batch size and concurrency ranges are validated.
- Scalar `T` and `ConnectorEvent<T>` reject an effective `MaxBatchSize` greater
  than `1`.
- Scalar parameters using `MaxBatchSize = 0` are validated against
  `DefaultMaxBatchSize`.
- Array parameters accept an effective `MaxBatchSize` of `1` or greater.
- `Concurrency` remains valid and independent for scalar and array bindings.
- Invalid combinations fail clearly.
- Binding chooses the correct listener.
- Payload-only and metadata-rich target types are recognized.

### Endpoint resolver tests

- Correct ARM URI and API version.
- Correct ARM token scope.
- Polling endpoint parsing.
- Cache hit and refresh behavior.
- Missing/invalid endpoint handling.

### HTTP client tests

- Correct API Hub token scope.
- `maxEvents` omitted/default and range validation.
- Existing endpoint query parameters are preserved.
- Receive and acknowledgement serialization.
- Mixed inline and linked message deserialization.
- Exactly one of `outputs` and `outputsLink` is required.
- Signed outputs-link URI validation and log redaction.
- Configured and absolute actual-bytes-read limit enforcement, including the
  104,857,600-byte boundary and over-limit behavior.
- Linked-output download response parsing.
- Linked-output retrieval does not attach an unintended bearer token.
- Redirect behavior follows the finalized service contract.
- Transient linked-output retry behavior follows the finalized service
  contract.
- Mixed acknowledgement statuses.
- Queue-depth parsing.
- No unsafe retries for lease-sensitive operations.

### Listener tests

- Empty queue backoff.
- Receive size is capped by effective `MaxBatchSize`, remaining invocation capacity, and 32.
- Each invocation receives no more than effective `MaxBatchSize`.
- Active function invocations never exceed effective `Concurrency`.
- A successful invocation acknowledges every message in that invocation batch.
- A failed invocation acknowledges no messages from that invocation batch.
- Concurrent invocation results remain associated with the correct message locks.
- Inline and linked outputs produce the same function-facing payload shape.
- A linked-output failure leaves only that message unacknowledged.
- Large-output hydration is bounded and consumes lock budget.
- Payload-only `T` and `T[]` conversion.
- Metadata-rich `ConnectorEvent<T>` and `ConnectorEvent<T>[]` conversion.
- `MessageId` remains paired with the correct payload under batching and concurrency.
- `lockToken` is never exposed to function code.
- `x-ms-more-messages-available` drains immediately.
- Stop and cancellation behavior.
- Expired locks and acknowledgement `NotFound`.
- Duplicate `messageId` delivery.

### Scale tests

- Zero/non-zero depth decisions.
- Approximate values and transient failures.
- Effective-concurrency target calculations and `MaxBatchSize` independence.
- Endpoint/auth failure behavior.

### End-to-end test

Use a Poll trigger configuration such as Office 365 `OnNewEmailV3`:

1. Register with `deliveryMode: Poll`.
2. Send test email.
3. Start Function host.
4. Receive one event with `maxEvents=1`.
5. Execute function.
6. Acknowledge the event.
7. Verify it does not reappear.
8. Run a failure case and verify redelivery after two minutes.

## Stacked PR Delivery Sequence

Each PR targets the preceding branch until the lower PR merges. Every PR must
preserve Webhook behavior and pass its focused build and tests.

### PR 1: Trigger contract

- Replace public `MaxEvents` with `MaxBatchSize` and `Concurrency` in both
  attributes.
- Add host-level defaults of `DefaultMaxBatchSize = 1` and
  `DefaultConcurrency = 16`.
- Preserve `Webhook` as the default and keep host/worker metadata synchronized.
- Update contract and listener-selection tests.

### PR 2: Configuration

- Resolve the literal `Connection` prefix through Functions configuration.
- Add immutable Poll and connection options.
- Validate the configured resource ID with `ResourceIdentifier`.
- Require a resource-group-scoped
  `Microsoft.Web/connectorGateways/{gateway}` resource with a valid
  subscription GUID and no child-resource path.
- Add credential selection and dependency registration.

### PR 3: Protocol models

- Add explicit Receive, acknowledgement, queue-depth, and endpoint models.
- Model `outputs` and `outputsLink` as mutually exclusive output sources.
- Add safe validation and serialization tests.

### PR 4: Endpoint resolver

- Read the trigger configuration through ARM using API version
  `2026-05-01-preview`.
- Extract and cache the three runtime endpoints used by the extension:
  Receive, Acknowledge, and Approximate Queue Depth.
- Verify Poll mode, enabled state, HTTPS endpoints, and ARM authentication.

### PR 5: Runtime client

- Implement Receive, acknowledgement, and queue-depth operations.
- Add linked-output retrieval with URI redaction, bounded size, and finalized
  authentication and retry semantics.
- Avoid transparent retries for lease-sensitive Receive and acknowledgement
  operations.

### Interim PR: First runnable Poll package

- Add a minimal message pump.
- Require `MaxBatchSize = 1` while honoring configurable `Concurrency`.
- Resolve endpoints, receive only up to available
  invocation capacity, normalize inline and linked outputs, and dispatch one
  event per invocation through the existing string binding.
- Acknowledge only successful invocations.
- Add cancellation-aware polling backoff, bounded shutdown, package-consumption
  validation, and preview limitations guidance.
- Defer metadata-rich binding, batch invocation, poison handling, and full
  lock-budget telemetry to later PRs.

### PR 6: Worker binding

- Add the deferred-binding transport needed to preserve per-event metadata.
- Support `T`, `T[]`, `ConnectorEvent<T>`, and `ConnectorEvent<T>[]`.
- Keep the host independent of generated `Azure.Connectors.Sdk` types.
- Complete the transport and conversion contract. PR 7 uses the collection
  transport for end-to-end multi-event invocations.

### PR 7: Poll listener

- Implement capacity-based Receive using calculated `maxEvents`.
- Add explicit invocation batching and bounded concurrency.
- Partition each Receive result into invocation batches of at most
  `MaxBatchSize`.
- Hydrate linked outputs sequentially within each invocation batch to avoid
  simultaneous large downloads while preserving the payload-to-message-lock
  association.
- Acknowledge all messages in a successful invocation batch and none from a
  failed or cancelled invocation.
- Preserve the existing lifecycle, backoff, and bounded-shutdown behavior, and
  add complete lock-budget telemetry.

### PR 8: Scaling and completion

- Complete service-backed validation of the target scaler that uses
  approximate queue depth.
- Validate scale from zero.
- Add integration tests, samples, and final documentation.
- Decide whether demonstrated reuse justifies extracting the internal client
  into a public package.

## Initial File-Level Work

Expected new or changed surfaces:

```text
src/Microsoft.Azure.Functions.Extensions.Connector/
  ConnectorTriggerAttribute.cs
  ConnectorTriggerBinding.cs
  ConnectorListener.cs
  ConnectorWebJobsBuilderExtensions.cs
  Polling/
    ConnectorPollingListener.cs
    ConnectorPollingEndpoints.cs
    ConnectorPollingEndpointResolver.cs
    ConnectorPollDeliveryClient.cs
    ConnectorPollDeliveryModels.cs
    ConnectorPollingOptions.cs
    ConnectorPollingScaleMonitor.cs

src/Microsoft.Azure.Functions.Worker.Extensions.Connector/
  ConnectorTriggerAttribute.cs
  ConnectorTriggerDeliveryMode.cs

test/Microsoft.Azure.Functions.Extensions.Connector.Tests/
  ConnectorTriggerAttributeTests.cs
  ConnectorTriggerBindingTests.cs
  ConnectorPollingEndpointResolverTests.cs
  ConnectorPollDeliveryClientTests.cs
  ConnectorPollingListenerTests.cs
  ConnectorPollingScaleMonitorTests.cs
```

Names and file boundaries are preliminary and should follow repository conventions discovered during implementation.

## Open Questions

1. Does the complete ARM discovery and Poll runtime path support a Function App and Connector Namespace in different subscriptions?
2. Should future service concurrency signals augment the current `ceil(depth / effectiveConcurrency)` target model?
3. Should a long-running execution be allowed to finish after the two-minute lock budget, or should the extension cancel it?
4. What evidence would justify extracting the internal protocol client into a separate public package?

## Supporting Information: Provisioning a Poll Trigger Configuration

The Connector Namespace portal does not currently support provisioning a Poll trigger configuration, and the current `az connector-namespace trigger create` command does not expose `deliveryMode`. Until those surfaces support Poll, use a raw ARM PUT request.

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
    "state": "Disabled",
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

Creating the trigger configuration as `Disabled` allows the Function and its connection access policy to be configured before polling begins. Set the trigger configuration to `Enabled` before starting the Function listener. The listener fails startup when the trigger configuration is disabled.

## Reference

- AzureUX-BPM PR 16351458: Connector Namespace trigger-config pull-delivery runtime design.
- #26: historical Functions scale-controller integration reference only; its
  Connector Namespace contract is obsolete.
- Connector Namespace runtime behavior verified against `receive`, `acknowledge`, `hasMessages`, and `approximateQueueDepth`.
- Kafka per-event metadata envelope: `KafkaEventData<T>` in `Azure/azure-functions-kafka-extension`.
- Event Hubs batch metadata model: `EventData[]` in `Azure/azure-sdk-for-net`.
- Modern isolated-worker deferred binding: Kafka and Event Hubs converters in `Azure/azure-functions-dotnet-worker`.
- Earlier package-oriented exploration: `Connectors-NET-SDK/docs/poll-trigger-delivery-design.md`.
