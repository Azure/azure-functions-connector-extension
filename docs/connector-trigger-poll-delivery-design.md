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
  - [Queue Status](#queue-status)
  - [Authentication](#authentication)
  - [Delivery Semantics](#delivery-semantics)
- [Proposed User Contract](#proposed-user-contract)
  - [Connector Namespace Trigger Configuration](#connector-namespace-trigger-configuration)
  - [Runtime Endpoint Configuration](#runtime-endpoint-configuration)
  - [Identity and Permissions](#identity-and-permissions)
  - [Ordering Guidance](#ordering-guidance)
  - [Payload and Message Metadata](#payload-and-message-metadata)
- [Internal Architecture](#internal-architecture)
  - [Configured Polling Endpoints](#1-configured-polling-endpoints)
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

Working implementation design. The trigger-contract seam is implemented on
`feature/connector-trigger-poll-delivery`; the production Poll components
remain to be delivered through the stacked PR sequence below.

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
- `ConnectorTriggerBinding.CreateListenerAsync` selects `ConnectorListener`
  for Webhook and the placeholder `ConnectorPollingListener` for Poll.
- `ConnectorListener` still registers the function for webhook routing and
  otherwise has inert lifecycle methods.
- `ConnectorExtensionConfigProvider` registers the webhook handler and
  dispatches callback payloads through `ITriggeredFunctionExecutor`.
- `ConnectorPollingListener.StartAsync` intentionally throws
  `NotSupportedException` until the production message pump is implemented.

The public contract uses independent `MaxBatchSize` and `TargetPendingEventThreshold` properties. `MaxBatchSize` controls invocation batching. `TargetPendingEventThreshold` is a scaling-only fallback when the Functions scale pipeline does not supply runtime `InstanceConcurrency`.

Poll delivery must run in the host extension, not in a language worker. This allows the same acquisition behavior to support .NET, Python, Node.js, and other extension-bundle consumers.

## Connector Namespace Runtime Contract

A Poll trigger configuration exposes four opaque, server-generated endpoints:

- `receiveUri`
- `acknowledgeUri`
- `hasMessagesUri`
- `approximateQueueDepthUri`

Clients must obtain these URLs from the trigger configuration response. They must not manually construct the runtime hostname or substitute a Connector Namespace resource name for the complete ARM resource identifier.

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

### Queue Status

```http
GET {hasMessagesUri}
GET {approximateQueueDepthUri}
```

Both values are approximate and must not be treated as prerequisites for Receive.

### Authentication

| Operation | Plane | Token audience/scope |
| --- | --- | --- |
| Receive, acknowledge, queue status | Runtime data plane | `https://apihub.azure.com/.default` |

### Delivery Semantics

- Delivery is at least once.
- The message lock is exactly two minutes and is not caller-configurable.
- An unacknowledged message becomes visible again with the same `messageId` and a new `lockToken`.
- An expired lock token produces a per-message `NotFound` acknowledgement result.
- Unacknowledged messages can be redelivered without an attempt limit.
- Queue message TTL is fixed at seven days and is not caller-configurable.
- Ordering is not guaranteed.
- Consumers must use `messageId` as the deduplication key.

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
        MaxBatchSize = 4,
        TargetPendingEventThreshold = 16)]
    Office365OnNewEmailTriggerPayload[] payloads)
{
    // Events are delivered in batches of at most four; scaling targets sixteen pending events per worker when runtime concurrency is unavailable.
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

### Runtime Endpoint Configuration

`Connection` is a literal app-setting prefix, consistent with other Azure Functions extensions. The connection section supplies the gateway-level Poll runtime endpoint and credential configuration:

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId=<optional-user-assigned-client-id>
ConnectorNamespace__managedIdentityResourceId=<optional-user-assigned-resource-id>
```

`TriggerConfigName` remains trigger metadata because it identifies the event source within the Connector Namespace. It supports Functions `%...%` name resolution.

Customer setup or provisioning tooling derives `pollingEndpoint` from the Trigger Config's service-generated `pollingEndpoints.receiveUri` by removing the final `/triggerConfigs/<name>/receive` segments. The extension treats the configured authority and gateway identifier as opaque trusted configuration. It does not discover Poll endpoints through ARM, parse a Connector Namespace resource ID, or accept debug bearer-token app settings.

The extension combines the configured gateway-level endpoint with the resolved Trigger Config name and appends the fixed `/receive`, `/acknowledge`, and `/approximateQueueDepth` operations.
### Identity and Permissions

The extension follows the standard Functions identity-based connection pattern used by other Azure extensions. It passes the complete named connection section to `AzureComponentFactory.CreateTokenCredential`.

| Environment | Configuration | Behavior |
|---|---|---|
| Local development | Omit `credential` | Use the developer credential behavior supplied by `AzureComponentFactory`, such as the account authenticated through `az login` |
| Azure, system-assigned identity | `credential=managedidentity` | Use the Function App's system-assigned managed identity |
| Azure, user-assigned identity | `credential=managedidentity` plus `clientId` or `managedIdentityResourceId` | Use the selected user-assigned managed identity |

Poll runtime operations request the `https://apihub.azure.com/.default` scope. The selected Function identity must have an access policy on the connection referenced by the Trigger Config.

When Scale Controller hosts the scaler, it can inject the Function App's API Hub credential through `ConnectorScaleCredentialProperties.ApiHubTokenCredential`. The injected credential is an internal hosting seam, not a customer app setting. Customer-configured debug bearer-token settings are not supported by the extension.
### Ordering Guidance

Connector Poll delivery does not guarantee ordering. `messageId` is stable across redelivery and is intended for deduplication; it does not define event order.

Connector Namespace does not currently add a sequence number, enqueue time, partition key, or ordering key to the Poll message envelope. The position of a message in a Receive response is also not an ordering contract.

Applications that require ordering must use source-specific ordering information when the connector payload provides it. They may buffer and reorder events in application code or forward events to an ordered downstream system, such as a Service Bus entity using sessions with an appropriate source event identifier as the session ID. The extension cannot infer a universal ordering key across connectors.

Max batch size and the scaling target are separate settings:

- `MaxBatchSize` is the maximum number of Connector events supplied to one function invocation.
- `TargetPendingEventThreshold` is the configured number of pending Connector events represented by one worker when runtime `InstanceConcurrency` is unavailable.
- A value of `0` uses the corresponding host-level default.
- Effective `MaxBatchSize` must be between `1` and `32`.
- Effective `TargetPendingEventThreshold` must be greater than zero.
- `TargetPendingEventThreshold` does not control Receive capacity, invocation batching, or listener parallelism.

The effective max batch size must be compatible with the declared function parameter:

| Function parameter | Allowed effective `MaxBatchSize` |
|---|---:|
| `T` | Exactly `1` |
| `ConnectorEvent<T>` | Exactly `1` |
| `T[]` | `1` through `32` |
| `ConnectorEvent<T>[]` | `1` through `32` |

Host-level defaults:

```csharp
public sealed class ConnectorOptions
{
    public int DefaultMaxBatchSize { get; set; } = 1;

    public int DefaultTargetPendingEventThreshold { get; set; } = 16;
}
```

Target-scaler precedence is:

```text
TargetScalerContext.InstanceConcurrency
    ?? trigger TargetPendingEventThreshold
    ?? DefaultTargetPendingEventThreshold
```

The target scaler does not multiply runtime instance concurrency or the configured threshold by `MaxBatchSize`.
### Payload and Message Metadata

The extension supports both payload-only and metadata-rich bindings.

Payload-only, one event per invocation:

```csharp
public void OnNewEmail(
    [ConnectorTrigger(MaxBatchSize = 1)]
    Office365OnNewEmailTriggerPayload email)
{
}
```

Payload-only batch:

```csharp
public void OnNewEmail(
    [ConnectorTrigger(MaxBatchSize = 8)]
    Office365OnNewEmailTriggerPayload[] emails)
{
}
```

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

Metadata-rich single event:

```csharp
public void OnNewEmail(
    [ConnectorTrigger(MaxBatchSize = 1)]
    ConnectorEvent<Office365OnNewEmailTriggerPayload> email)
{
    if (email.MessageId is { } deduplicationId)
    {
        Deduplicate(deduplicationId);
    }
}
```

Metadata-rich batch:

```csharp
public void OnNewEmail(
    [ConnectorTrigger(MaxBatchSize = 8)]
    ConnectorEvent<Office365OnNewEmailTriggerPayload>[] emails)
{
    foreach (ConnectorEvent<Office365OnNewEmailTriggerPayload> email in emails)
    {
        Process(email.Data, email.MessageId);
    }
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

Open generic function signatures are not supported:

```csharp
// Not supported: Azure Functions cannot index an unresolved payload type.
[Function("GenericFunction")]
public void Run<T>(
    [ConnectorTrigger] ConnectorEvent<T> message)
{
}
```

Azure Functions must discover concrete binding types during function indexing. The Connector extension remains connector-agnostic because it uses the closed type declared by the function rather than referencing or registering every generated `Azure.Connectors.Sdk` model.

## Internal Architecture

### 1. Configured Polling Endpoints

`ConnectorConnectionOptionsProvider` reads the named connection's `pollingEndpoint` and creates the selected credential through `AzureComponentFactory`. `ConnectorPollingEndpoints` combines that configured gateway-level endpoint with the resolved Trigger Config name and constructs the fixed runtime operation URIs.

The endpoint is configuration, not an ARM-discovered resource. Missing or invalid configuration fails scaler construction with an actionable error.
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

    Task<ConnectorQueueStatus> GetQueueStatusAsync(
        ConnectorPollingEndpoints endpoints,
        CancellationToken cancellationToken);
}
```

The runtime client also needs a dedicated linked-output download operation or
an equivalent narrowly scoped collaborator. It must not use a general client
that automatically attaches bearer tokens to signed content URLs.

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
- Queue-status requests can use ordinary bounded transient retries.

### 3. Listener Selection

Keep the existing Webhook listener behavior unchanged.

Change `ConnectorTriggerBinding.CreateListenerAsync` to select:

- `ConnectorListener` for Webhook.
- `ConnectorPollingListener` for Poll.

Do not turn the current class into a large mode-switching listener.

### 4. Polling Listener

Listener execution policy is outside PR #33. The scaling contract must not be reused as a listener admission or concurrency setting. In particular, `TargetPendingEventThreshold` is not copied into `ConnectorPollingOptions` and does not determine Receive size, active invocation count, or local batching.

A later listener change can introduce a separately named execution-concurrency contract without changing the target-scaler metadata.
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

The isolated-worker transport should follow the modern deferred-binding approach used by Kafka and Event Hubs so each item retains its metadata during conversion. Exact transport serialization remains internal and must not change the public payload-only shape.

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

Listener polling alone is incomplete because an app at zero workers cannot poll. Connector therefore implements `ITargetScaler` using approximate queue depth from the configured `/approximateQueueDepth` runtime endpoint.

Target-scaler precedence follows Event Hubs and Service Bus:

```text
effectiveTarget =
    TargetScalerContext.InstanceConcurrency
    ?? configured TargetPendingEventThreshold
```

The configured value is the trigger's nonzero `TargetPendingEventThreshold`, otherwise `DefaultTargetPendingEventThreshold`. Every effective value must be greater than zero.

```text
targetWorkerCount =
    ceil(approximateQueueDepth / effectiveTarget)
```

`MaxBatchSize` does not participate in this calculation. The scaler treats approximate depth as a scale signal rather than an exact count of immediately receivable events.

The production metrics provider queries the configured runtime endpoint with the Function App's API Hub credential. Caller cancellation propagates; other metric-query failures emit a scale warning and return a fresh zero-depth sample.

Historical PR #26 remains useful only for the Functions scale-controller integration pattern:

- `ITargetScaler` and `ITargetScalerProvider`.
- The reflectively discovered `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` registration signature.
- Reading trigger metadata and host-level options in the scaler provider.
- Scale Monitor validation through trigger registration and scale-status requests.
### 9. Dependency Registration

`ConnectorWebJobsBuilderExtensions.AddConnector` registers:

- Azure credential services.
- The named Poll runtime `HttpClient`.
- `ConnectorConnectionOptionsProvider`.
- `IConnectorQueueDepthClientFactory`.
- The trigger-specific target-scaler provider through the reflective scale-registration hook.
## Error Handling

- Invalid Poll attribute/configuration: fail listener startup with an actionable error.
- Missing or invalid configured `pollingEndpoint` or Trigger Config name: fail scaler construction.
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
- Attribute `MaxBatchSize` and `TargetPendingEventThreshold` overrides flow correctly.
- Zero-valued overrides use host-level defaults.
- Max batch size and target pending-event threshold ranges are validated.
- Scalar `T` and `ConnectorEvent<T>` reject an effective `MaxBatchSize` greater
  than `1`.
- Scalar parameters using `MaxBatchSize = 0` are validated against
  `DefaultMaxBatchSize`.
- Array parameters accept an effective `MaxBatchSize` of `1` or greater.
- `TargetPendingEventThreshold` remains independent of scalar and array binding shape.
- Invalid combinations fail clearly.
- Binding chooses the correct listener.
- Payload-only and metadata-rich target types are recognized.

### Connection and endpoint tests

- Configured `pollingEndpoint` parsing.
- Trigger Config name resolution.
- Fixed runtime-operation URI construction.
- Missing or invalid endpoint handling.

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
- Queue-status parsing.
- No unsafe retries for lease-sensitive operations.

### Listener tests

- Empty queue backoff.
- Each invocation receives no more than effective `MaxBatchSize`.
- A successful invocation acknowledges every message in that invocation batch.
- A failed invocation acknowledges no messages from that invocation batch.
- Inline and linked outputs produce the same function-facing payload shape.
- A linked-output failure leaves only that message unacknowledged.
- Large-output hydration is bounded and consumes lock budget.
- Payload-only `T` and `T[]` conversion.
- Metadata-rich `ConnectorEvent<T>` and `ConnectorEvent<T>[]` conversion.
- `MessageId` remains paired with the correct payload under batching.
- `lockToken` is never exposed to function code.
- `x-ms-more-messages-available` drains immediately.
- Stop and cancellation behavior.
- Expired locks and acknowledgement `NotFound`.
- Duplicate `messageId` delivery.

### Scale tests

- Zero/non-zero depth decisions.
- Approximate values and transient failures.
- Runtime `InstanceConcurrency` precedence, configured-threshold fallback, and `MaxBatchSize` independence.
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

- Replace the scaling property with `TargetPendingEventThreshold` in both attributes.
- Add `DefaultTargetPendingEventThreshold = 16` while retaining the independent batch-size default.
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

- Add explicit Receive, acknowledgement, queue-status, and endpoint models.
- Model `outputs` and `outputsLink` as mutually exclusive output sources.
- Add safe validation and serialization tests.

### PR 4: Endpoint resolver

- Read the trigger configuration through ARM using API version
  `2026-05-01-preview`.
- Extract and cache the four opaque runtime endpoints.
- Verify Poll mode, enabled state, HTTPS endpoints, and ARM authentication.

### PR 5: Runtime client

- Implement Receive, acknowledgement, and queue-status operations.
- Add linked-output retrieval with URI redaction, bounded size, and finalized
  authentication and retry semantics.
- Avoid transparent retries for lease-sensitive Receive and acknowledgement
  operations.

### PR 6: Worker binding

- Add the deferred-binding transport needed to preserve per-event metadata.
- Support `T`, `T[]`, `ConnectorEvent<T>`, and `ConnectorEvent<T>[]`.
- Keep the host independent of generated `Azure.Connectors.Sdk` types.

### PR 7: Poll listener

- Implement capacity-based Receive using calculated `maxEvents`.
- Add invocation batching and a separately named listener-concurrency contract; do not reuse the scaling threshold.
- Hydrate linked outputs within lock and memory budgets.
- Acknowledge all messages in a successful invocation batch and none from a
  failed or cancelled invocation.
- Add lifecycle, backoff, telemetry, and shutdown behavior.

### PR 8: Scaling and completion

- Add the scale monitor or target scaler using approximate queue depth.
- Validate scale from zero.
- Add integration tests, samples, and final documentation.
- Decide whether demonstrated reuse justifies extracting the internal client
  into a public package.

## Initial File-Level Work

Expected new or changed surfaces:

```text
src/Microsoft.Azure.Functions.Extensions.Connector/
  ConnectorOptions.cs
  ConnectorTriggerAttribute.cs
  ConnectorWebJobsBuilderExtensions.cs
  Polling/
    Clients/ConnectorQueueDepthClient.cs
    Models/ConnectorPollingEndpoints.cs
    Scaling/ConnectorMetricsProvider.cs
    Scaling/ConnectorScalerProvider.cs
    Scaling/ConnectorTargetScaler.cs
    Scaling/ConnectorTriggerMetadataNames.cs
    Scaling/ConnectorTriggerMetrics.cs

src/Microsoft.Azure.Functions.Worker.Extensions.Connector/
  ConnectorTriggerAttribute.cs

test/Microsoft.Azure.Functions.Extensions.Connector.Tests/
  ConnectorOptionsBindingTests.cs
  ConnectorTriggerAttributeTests.cs
  Polling/Clients/ConnectorQueueDepthClientTests.cs
  Polling/Scaling/ConnectorScalerProviderTests.cs
  Polling/Scaling/ConnectorTargetScalerTests.cs
```

Names and file boundaries are preliminary and should follow repository conventions discovered during implementation.

## Open Questions

1. Should endpoint discovery be mandatory, or should advanced users be able to provide the four runtime URLs directly?
2. Will Connector Namespace expose a fully qualified namespace or stable non-ARM discovery endpoint so clients do not need ARM access to bootstrap polling endpoints?
3. Does the complete ARM discovery and Poll runtime path support a Function App and Connector Namespace in different subscriptions?
4. Which Functions scaling interface is appropriate for this extension version?
5. Should future service signals supplement the runtime `InstanceConcurrency` override and configured pending-event threshold?
6. What default empty-queue backoff and jitter should be used?
7. Should a long-running execution be allowed to finish after the two-minute lock budget, or should the extension cancel it?
8. When should endpoint cache entries be refreshed?
9. What evidence would justify extracting the internal protocol client into a separate public package?

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
