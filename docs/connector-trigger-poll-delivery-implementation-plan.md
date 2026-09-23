# Connector Trigger Poll Delivery Implementation Plan

## Table of Contents

- [Purpose](#purpose)
- [Verified Baseline](#verified-baseline)
- [Stacking Rules](#stacking-rules)
- [PR 1: Trigger Contract](#pr-1-trigger-contract)
- [PR 2: Configuration](#pr-2-configuration)
- [PR 3: Protocol Models](#pr-3-protocol-models)
- [PR 4: ARM Endpoint Resolver](#pr-4-arm-endpoint-resolver)
- [PR 5: Runtime and Linked-Output Clients](#pr-5-runtime-and-linked-output-clients)
- [PR 6: Worker Binding](#pr-6-worker-binding)
- [PR 7: Poll Listener](#pr-7-poll-listener)
- [PR 8: Scaling and Completion](#pr-8-scaling-and-completion)
- [Cross-Stack Validation](#cross-stack-validation)
- [Open Service Dependencies](#open-service-dependencies)

## Purpose

Deliver production Poll support through a stacked PR sequence while preserving
existing Webhook behavior. The stack now includes the trigger contract,
configuration, protocol models, target scaler, runtime clients, a concurrent
listener with invocation batching, and the worker binding. This document
tracks the remaining implementation boundaries, dependencies, and completion
criteria; the authoritative behavioral and architectural decisions are in
`connector-trigger-poll-delivery-design.md`.

## Verified Baseline

The proof of concept verified this end-to-end path:

1. Authenticate to ARM with `https://management.azure.com/.default`.
2. Read a Poll trigger configuration using API version
   `2026-05-01-preview`.
3. Extract the server-generated Poll runtime endpoints.
4. Authenticate to the runtime with `https://apihub.azure.com/.default`.
5. Receive messages, convert complete trigger `outputs`, execute a typed
   function, and acknowledge successful processing.

The supported Connector Namespace ARM resource type is:

```text
Microsoft.Web/connectorGateways
```

Phase 0 is complete: sanitized evidence is preserved outside the repository,
temporary POC code and settings were removed, the test trigger was disabled,
and the committed baseline builds with all existing tests passing.

## Stacking Rules

- PR 1 targets `main`.
- Each later PR targets the branch for the preceding PR until that PR merges.
- Rebase the next branch onto the merged lower PR before changing its base.
- Keep each PR independently buildable and testable.
- Do not combine the unrelated Webhook trigger-response proposal with this
  stack.

## PR 1: Trigger Contract

Replace the earlier public `MaxEvents` seam with independent batching and per-instance event-capacity controls:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorNamespace",
    TriggerConfigName = "%CONNECTOR_TRIGGER_CONFIG%",
    IsBatched = true,
    MaxBatchSize = 4,
    Concurrency = 8)]
```

Changes:

- Add `MaxBatchSize` and `Concurrency` to both host and isolated-worker
  attributes.
- Remove public `MaxEvents`.
- Add host-level `ConnectorOptions`:

  ```csharp
  public int DefaultMaxBatchSize { get; set; } = 1;
  public int DefaultConcurrency { get; set; } = 16;
  ```

- Treat `0` on either attribute property as use-host-default.
- Accept `MaxBatchSize` as `0` for the host default or from `1` through `32`,
  and require the effective value to be from `1` through `32`.
- Accept non-negative `Concurrency` and require the effective value to be
  greater than zero.
- Define `Concurrency` as the maximum concurrent function invocations per
  worker instance.
- Keep `MaxBatchSize` independent of scaling and use it with remaining
  invocation capacity to calculate Connector Namespace `maxEvents`.
- Preserve `Webhook` as the default.
- Keep `Connection` literal; do not apply `%...%` name resolution to it.
- Continue allowing `%...%` resolution for `TriggerConfigName`.
- Update attribute, option, and listener-selection tests.

Completion criteria:

- Host and worker contracts remain synchronized.
- Existing Webhook tests remain unchanged and pass.
- No public `MaxEvents` property remains.

## PR 2: Configuration

Add validated immutable configuration and dependency registration.

Configuration shape:

```text
ConnectorNamespace__resourceId=/subscriptions/.../resourceGroups/.../providers/Microsoft.Web/connectorGateways/...
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId=<optional-user-assigned-identity-client-id>
ConnectorNamespace__managedIdentityResourceId=<optional-resource-id>
```

Changes:

- Resolve the literal `Connection` prefix through `IConfiguration`.
- Require `Connection`, `resourceId`, and `TriggerConfigName` only in Poll mode.
- Document that Connector Namespace setup provisions the Poll trigger configuration and that `TriggerConfigName` identifies it; the extension does not create or convert trigger configurations.
- Document that Poll trigger configuration is not currently available through the Connector Namespace portal or the delivery-mode options of the current `az connector-namespace trigger create` command, and must use a raw ARM PUT request such as `az rest --method put`.
- Parse resource IDs with `Azure.Core.ResourceIdentifier`.
- Require exactly a resource-group-scoped
  `Microsoft.Web/connectorGateways/{gateway}` resource.
- Reject invalid subscription GUIDs, child-resource paths, full URLs, query
  strings, and fragments.
- Add `Azure.Identity` 1.17.1 and `Microsoft.Extensions.Azure` 1.13.1.
- Register the shared Microsoft Extensions Azure services.
- Pass the full named connection section to `AzureComponentFactory.CreateTokenCredential`, matching other first-party Functions extensions.
- Document and test the standard paths:
  - Local development omits `credential` and uses the shared developer-identity behavior.
  - Azure deployment uses `credential=managedidentity`.
  - `clientId` or `managedIdentityResourceId` may select a user-assigned identity.
- Register Poll services through `AddConnector`.

Completion criteria:

- Invalid Poll configuration fails startup with actionable errors.
- Webhook mode requires no Poll configuration.

## PR 3: Protocol Models

Add explicit internal models for:

```text
ConnectorPollingEndpoints
ConnectorPollMessage
ConnectorOutputsLink
ConnectorMessageLock
ConnectorReceiveResult
ConnectorAcknowledgeResult
ConnectorAcknowledgeItemResult
ConnectorAcknowledgeStatus
ConnectorQueueStatus
```

Requirements:

- Use nullable internal wire DTOs with explicit `System.Text.Json` mappings, then validate into immutable protocol models.
- Use a source-generated JSON context so wire names and supported payloads are explicit.
- Require non-empty message IDs and lock tokens.
- Require exactly one of `outputs` and `outputsLink`.
- Preserve inline `outputs` as owned `BinaryData`; do not retain a borrowed `JsonElement` or deserialize into connector-specific types.
- Validate the linked-output URI. The Receive contract does not include a
  declared content size.
- Parse mixed acknowledgement results and preserve unknown future statuses with an extensible string-backed value. Known values are `Acknowledged`, `NotFound`, and `Failed`; only `Acknowledged` is successful.
- Treat Poll `messageId` as unique for the current implementation while
  Connector Namespace confirms its exact uniqueness scope.
- Never expose or log lock tokens or signed output URLs.
- Use secret-safe `ToString()` implementations for token- and signed-URI-bearing models.
- Keep generated `Azure.Connectors.Sdk` types out of the host protocol layer.
- Keep wire parsing, protocol validation, status interpretation, and URI redaction independent of WebJobs types so they can move to a future Connectors Polling SDK.

## PR 4: ARM Endpoint Resolver

Implement `IConnectorPollingEndpointResolver`.

Requirements:

- Read:

  ```text
  {connectorNamespaceResourceId}/triggerConfigs/{escapedName}?api-version=2026-05-01-preview
  ```

- Authenticate with the ARM scope.
- Document that ARM GET requires the control-plane action `Microsoft.Web/connectorGateways/triggerconfigs/read`.
- Treat built-in Reader at the Connector Namespace resource scope as the least-privilege built-in-role proposal, pending an end-to-end test with no broader inherited permissions.
- Verify that delivery mode is Poll and the trigger is enabled.
- Extract `receiveUri`, `acknowledgeUri`, and `approximateQueueDepthUri`.
- Do not call `hasMessagesUri`; Receive already reports whether more messages
  are available, so a separate preflight request would add latency and create a
  time-of-check/time-of-use race.
- Require absolute HTTPS endpoints.
- Cache by connection and trigger-config name.
- Before publishing final customer guidance, run a service-backed authorization test with no broader inherited permissions to confirm or correct the proposed ARM role.

Next endpoint-contract PR:

- Replace `resourceId`-based ARM discovery with the customer-provided opaque
  polling base URL derived from `pollingEndpoints.receiveUri`.
- Add a `PollingEndpoint` binding property whose value can use Functions app
  setting resolution, for example `%OnNewEmail_Endpoint%`.
- Continue sharing one `Connection` prefix across Functions that use the same
  Connector Namespace. Endpoint settings remain per Function because each
  Trigger Config has a different base URL.
- Treat the base URL as trigger-specific. Do not share it across a Connector
  Namespace or derive it from the namespace name; it may contain a gateway
  GUID.
- Derive the base by parsing `pollingEndpoints.receiveUri` as an absolute HTTPS
  URI, requiring its final path segment to be exactly `receive`, and removing
  only that segment. Do not use unrestricted string replacement.
- The base includes `/triggerConfigs/<triggerConfigName>`. Append only the
  fixed `/receive`, `/acknowledge`, and `/approximateQueueDepth` operations.
- Remove `TriggerConfigName` from the Poll runtime contract when
  `PollingEndpoint` replaces ARM discovery.
- Treat the configured endpoint as authoritative and remove the ARM resolver,
  and endpoint cache.
- When the configured endpoint is unreachable or a derived Poll route returns
  `404 Not Found` or `410 Gone`, surface an explicit configured-endpoint error;
  do not attempt ARM discovery.
- Preserve compatibility with existing APIM polling URLs when the service
  moves to DNS.

## PR 5: Runtime and Linked-Output Clients

Implement:

```csharp
ReceiveAsync(endpoints, maxEvents, cancellationToken)
AcknowledgeAsync(endpoints, locks, cancellationToken)
GetApproximateQueueDepthAsync(cancellationToken)
```

Receive and acknowledgement belong to the delivery client. Approximate queue
depth belongs to the dedicated scaling client.

Requirements:

- Authenticate runtime operations with the API Hub scope.
- Require the Function identity to have an access policy on the connection referenced by the trigger config.
- Preserve the service-backed authorization evidence: the same API Hub token
  and approximate-queue-depth endpoint returned `200 OK` with the connection access
  policy, `403 Forbidden` without it, and `200 OK` after restoration.
- Preserve existing endpoint query parameters.
- Explicitly send `maxEvents` in the range 1-32.
- Parse `x-ms-more-messages-available`.
- Process acknowledgement statuses per item.
- Do not transparently retry Receive or Acknowledge after ambiguous failures.
- Allow bounded transient retries only for safe queue-depth operations.

Add a dedicated linked-output client or narrowly scoped collaborator:

- Require an absolute HTTPS URI with no user information or fragment.
- Never log or persist the complete signed URI.
- Do not attach an API Hub token; the URI signature fully authorizes the GET.
- Disable redirects and automatic decompression.
- Require `application/json; charset=utf-8`.
- Enforce actual bytes read with an absolute 100 MiB (104,857,600-byte)
  maximum.
- Allow bounded retries because repeated signed GETs are safe and idempotent
  while the link and content remain valid.
- Treat the HTTPS authority as opaque; it varies by cloud, region, scale unit,
  and environment.
- Normalize downloaded content to the same complete `outputs` JSON used by
  inline messages.

## PR 6: Worker Binding

Add the transport and conversion contract for:

```text
T
T[]
ConnectorEvent<T>
ConnectorEvent<T>[]
```

Public metadata envelope:

```csharp
public sealed class ConnectorEvent<T>
{
    public required T Data { get; init; }

    public string? MessageId { get; init; }
}
```

Requirements:

- The declared concrete function parameter type is the conversion source of
  truth.
- Scalar `T` and `ConnectorEvent<T>` bindings require an effective
  `MaxBatchSize` of exactly `1`.
- Array `T[]` and `ConnectorEvent<T>[]` bindings accept an effective
  `MaxBatchSize` of `1` or greater; a final invocation batch may contain fewer
  items.
- Apply shape validation after resolving `DefaultMaxBatchSize`, so a scalar
  parameter with `MaxBatchSize = 0` fails when the host default is greater than
  `1`.
- Fail incompatible bindings during indexing or listener startup rather than
  ignoring `MaxBatchSize`, dropping events, or changing the parameter shape.
- Keep `Concurrency` independent of parameter shape.
- Support closed generic payload types; reject unresolved open generic
  functions.
- Preserve each Poll `MessageId` with its corresponding payload.
- Use `null` for Webhook `MessageId` unless the service defines an equivalent
  stable Webhook identifier.
- Keep `lockToken` internal.
- Do not add scalar `[BindingName("messageId")]` or parallel metadata arrays.

Implementation status:

- The host-to-worker deferred transport and scalar/array converters are
  implemented.
- `CollectionModelBindingData` conversion is covered independently.
- End-to-end multi-event collection binding is implemented.
- Cardinality-one bindings reject an effective `MaxBatchSize` greater than
  one; cardinality-many bindings accept values through the protocol limit of
  32.

## PR 7: Poll Listener

Invocation batching is implemented in the lifecycle-safe, capacity-aware
message pump. Poison handling and complete lock-budget telemetry remain for a
later PR.

The listener now:

- Honors explicit one/many cardinality independently from `MaxBatchSize`.
- Resolves endpoints and receives no more than the remaining invocation
  capacity multiplied by `MaxBatchSize`, capped at 32.
- Partitions Receive results into invocation batches.
- Normalizes inline and linked outputs before dispatch.
- Acknowledges all prepared messages only after a successful invocation.
- Passes metadata-rich scalar and collection values through the worker binding.

Capacity calculation:

```csharp
int availableInvocationSlots =
    effectiveConcurrency - activeInvocationCount;
int availableMessageCapacity =
    availableInvocationSlots * effectiveMaxBatchSize;
int maxEvents = Math.Min(32, availableMessageCapacity);
```

Requirements:

- Receive only when `maxEvents > 0`.
- Do not prefetch beyond remaining invocation capacity.
- Count an event as pending from Receive until acknowledgement completes or a failed attempt finishes without acknowledgement, including hydration and function execution.
- Partition each Receive result into invocation batches containing at most
  `MaxBatchSize` events.
- Allow no more than `Concurrency` active function invocations per worker
  instance.
- Bound linked-output hydration and account for its time in the fixed
  two-minute lock budget.
- Pass normalized payloads and per-event metadata through the worker binding.
- Acknowledge every event in a successful invocation batch.
- Acknowledge no events from a failed or cancelled invocation batch.
- Send acknowledgement requests with at most 32 locks.
- Drain immediately only when the service reports more messages and capacity
  is available.
- Add cancellation-aware empty-queue backoff with jitter.
- Make start, stop, cancel, and dispose behavior idempotent.
- Stop receiving during shutdown, allow bounded in-flight completion, and
  leave unfinished events unacknowledged.

The first implementation is concurrent by design; it must not revert to
sequential message processing.

## PR 8: Scaling and Completion

Complete and validate the target scaler supported by the repository's WebJobs
host version. The target-scaler implementation is already present in the
stack; service-backed validation and release-readiness work remain.

Requirements:

- Use #26 only as a historical reference for the Functions scale-controller
  integration:
  - `ITargetScaler` and `ITargetScalerProvider`.
  - The reflectively discovered
    `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` hook.
  - Reading effective trigger and host options from `TriggerMetadata`.
  - Scale Monitor registration and scale-status validation.
- Do not reuse #26's positional `connectorNamespace`/`triggerName` attribute
  contract, Namespace API models or paths, authentication assumptions, mock
  metrics provider, or conflicting metadata names.
- Verify the referenced host interfaces and reflective registration signature
  against the current Functions host and Scale Monitor before implementation.
- Reuse the endpoint resolver and queue-depth client.
- Obtain metrics only through the current server-provided
  `approximateQueueDepthUri`.
- Scale from approximate queue depth without treating it as an exact count or
  a prerequisite for Receive.
- Calculate target workers from effective invocation concurrency.
  `MaxBatchSize` is listener invocation grouping and must not affect the
  target:

  ```text
  ceil(pendingEvents / effectiveConcurrency)
  ```

- Keep `MaxBatchSize` and Connector Namespace `maxEvents` independent of the scaling calculation.
- Return conservative decisions when queue depth is unavailable.
- Validate scale from zero.
- Add integration tests, samples, and final user documentation.

Poll delivery is not production-complete until this PR is delivered.

## Cross-Stack Validation

Every PR must preserve:

- Webhook default behavior and dispatch.
- Host/worker metadata compatibility.
- Safe logging without payloads, credentials, tokens, lock tokens, or signed
  URLs.
- At-least-once delivery semantics.
- No automatic replay of ambiguous Receive or Acknowledge operations.

Final smoke test:

1. Enable a dedicated Poll trigger.
2. Start a Function App using the local extension build.
3. Generate new connector events.
4. Verify typed payload-only and metadata-rich invocation.
5. Verify batching, concurrency, and success-only acknowledgement.
6. Verify linked output when the service test path is available.
7. Confirm acknowledged events do not reappear and failed events are
   redelivered.
8. Confirm queue depth returns to zero.
9. Stop the host and restore the trigger to its disabled state.

## Confirmed Linked-Output Service Contract

The Connector Namespace team confirmed the linked-output authentication,
lifetime, redelivery, 100 MiB maximum, response shape, content type,
compression, redirects, retry safety, integrity metadata, deletion, and
authority behavior. PR 5 must implement the contract recorded in the design
document.
