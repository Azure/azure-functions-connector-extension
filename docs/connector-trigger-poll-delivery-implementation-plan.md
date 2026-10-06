# Connector Trigger Poll Delivery Implementation Plan

## Table of Contents

- [Purpose](#purpose)
- [Verified Baseline](#verified-baseline)
- [Stacking Rules](#stacking-rules)
- [PR 1: Trigger Contract](#pr-1-trigger-contract)
- [PR 2: Configuration](#pr-2-configuration)
- [PR 3: Protocol Models](#pr-3-protocol-models)
- [PR 4: Polling Endpoint Configuration](#pr-4-polling-endpoint-configuration)
- [PR 5: Runtime and Linked-Output Clients](#pr-5-runtime-and-linked-output-clients)
- [PR 6: Worker Binding](#pr-6-worker-binding)
- [PR 7: Poll Listener](#pr-7-poll-listener)
- [PR 8: Scaling and Completion](#pr-8-scaling-and-completion)
- [Cross-Stack Validation](#cross-stack-validation)
- [Deferred Linked-Output Release Work](#deferred-linked-output-release-work)

## Purpose

Deliver production Poll support through a stacked PR sequence while preserving existing Webhook behavior. The stack now includes the trigger contract, configuration, protocol models, target scaler, runtime clients, a concurrent listener with invocation batching, and the worker binding. This document records the delivered implementation boundaries, open service dependencies, and final completion criteria; the authoritative behavioral and architectural decisions are in `connector-trigger-poll-delivery-design.md`.

The connection-scoped `pollingEndpoint` and `TriggerConfigName` migration, together with scaling/release completion, are combined in the final stacked Poll delivery PR. No additional implementation PR follows it.

## Verified Baseline

The proof of concept verified this end-to-end path:

1. Obtain the server-generated `pollingEndpoints.receiveUri` from an enabled Poll trigger configuration.
2. Split the endpoint into the gateway-level Poll base and Trigger Config name.
3. Authenticate to the runtime with `https://apihub.azure.com/.default`.
4. Receive messages, convert complete trigger `outputs`, execute a typed function, and acknowledge successful processing.

Phase 0 is complete: sanitized evidence is preserved outside the repository, temporary POC code and settings were removed, the test trigger was disabled, and the committed baseline builds with all existing tests passing.

## Stacking Rules

- PR 1 targets `main`.
- Each later PR targets the branch for the preceding PR until that PR merges.
- Rebase the next branch onto the merged lower PR before changing its base.
- Keep each PR independently buildable and testable.
- Do not combine the unrelated Webhook trigger-response proposal with this stack.

## PR 1: Trigger Contract

Replace the earlier public `MaxEvents` seam with independent batching and per-instance event-capacity controls:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorNamespace",
    TriggerConfigName = "%OnNewEmailTriggerConfigName%",
    IsBatched = true,
    MaxBatchSize = 4,
    MaxConcurrentCalls = 8,
    TargetPendingEventThreshold = 16)]
```

Changes:

- Add `MaxBatchSize`, `MaxConcurrentCalls`, and `TargetPendingEventThreshold` to both host and isolated-worker attributes.
- Remove public `MaxEvents`.
- Add host-level `ConnectorOptions`:

  ```csharp
  public int DefaultMaxBatchSize { get; set; } = 1;
  public int DefaultMaxConcurrentCalls { get; set; } = 16;
  public int DefaultTargetPendingEventThreshold { get; set; } = 16;
  ```

- Treat `0` on each attribute property as use-host-default.
- Accept `MaxBatchSize` as `0` for the host default or from `1` through `32`, and require the effective value to be from `1` through `32`.
- Accept non-negative `MaxConcurrentCalls` and `TargetPendingEventThreshold` values and require each effective value to be greater than zero.
- Define `MaxConcurrentCalls` as the maximum number of active invocation-processing tasks per worker instance.
- Define `TargetPendingEventThreshold` as the desired pending events per worker instance used only for target-based scaling.
- Determine Receive capacity from invocation slots: `min(32, availableInvocationSlots * effectiveMaxBatchSize)`.
- Keep `MaxBatchSize` independent of scaling; use it for invocation batching and to convert available invocation slots into Receive event capacity.
- Preserve `Webhook` as the default.
- Keep `Connection` literal; do not apply `%...%` name resolution to it.
- Resolve `TriggerConfigName` through Functions `%...%` app-setting resolution.
- Update attribute, option, and listener-selection tests.

Completion criteria:

- Host and worker contracts remain synchronized.
- Existing Webhook tests remain unchanged and pass.
- No public `MaxEvents` property remains.

## PR 2: Configuration

Add validated immutable configuration and dependency registration.

Configuration shape:

```text
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId=<optional-user-assigned-identity-client-id>
ConnectorNamespace__managedIdentityResourceId=<optional-resource-id>
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
OnNewEmailTriggerConfigName=<trigger-config-name>
```

Changes:

- Resolve the literal `Connection` prefix through `IConfiguration`.
- Require `Connection`, its `pollingEndpoint`, and `TriggerConfigName` only in Poll mode.
- Document that Connector Namespace setup provisions the Poll trigger configuration and that `TriggerConfigName` identifies it; the extension does not create or convert trigger configurations.
- Document that Poll trigger configuration is not currently available through the Connector Namespace portal or the delivery-mode options of the current `az connector-namespace trigger create` command, and must use a raw ARM PUT request such as `az rest --method put`.
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
```

Requirements:

- Use nullable internal wire DTOs with explicit `System.Text.Json` mappings, then validate into immutable protocol models.
- Use a source-generated JSON context so wire names and supported payloads are explicit.
- Require non-empty message IDs and lock tokens.
- Require exactly one of `outputs` and `outputsLink`.
- Preserve inline `outputs` as owned `BinaryData`; do not retain a borrowed `JsonElement` or deserialize into connector-specific types.
- Validate the linked-output URI. The Receive contract does not include a declared content size.
- Parse mixed acknowledgement results and preserve unknown future statuses with an extensible string-backed value. Known values are `Acknowledged`, `NotFound`, and `Failed`; only `Acknowledged` is successful.
- Treat Poll `messageId` as unique for the current implementation while Connector Namespace confirms its exact uniqueness scope.
- Never expose or log lock tokens or signed output URLs.
- Use secret-safe `ToString()` implementations for token- and signed-URI-bearing models.
- Keep generated `Azure.Connectors.Sdk` types out of the host protocol layer.
- Keep wire parsing, protocol validation, status interpretation, and URI redaction independent of WebJobs types so they can move to a future Connectors Polling SDK.

## PR 4: Polling Endpoint Configuration

Requirements:

- Add a `TriggerConfigName` binding property whose value can use Functions app setting resolution, for example `%OnNewEmailTriggerConfigName%`.
- Continue sharing one `Connection` prefix across Functions that use the same Connector Namespace. Store its gateway-level base in `<Connection>__pollingEndpoint`.
- Preserve the service-provided host and gateway identifier exactly; do not derive either value from the Connector Namespace resource name or ID.
- Append `/triggerConfigs/<TriggerConfigName>` and only the fixed `/receive`, `/acknowledge`, and `/approximateQueueDepth` operations.
- Require HTTPS on the default port, treat the configured authority as opaque trusted application configuration, and require the exact path `/api/connectorGateways/<non-empty-gateway-id>`.
- Reject endpoint values that already contain `triggerConfigs`, an operation, or any other extra segment.
- Validate `TriggerConfigName` as one non-empty path segment.
- Perform no control-plane endpoint discovery and maintain no endpoint cache.
- When the configured endpoint is unreachable or a derived Poll route returns `404 Not Found` or `410 Gone`, surface an explicit configured-endpoint error; do not attempt alternate endpoint discovery.
- Preserve compatibility with existing APIM polling URLs when the service moves to DNS.

## PR 5: Runtime and Linked-Output Clients

Implement:

```csharp
ReceiveAsync(endpoints, maxEvents, cancellationToken)
AcknowledgeAsync(endpoints, locks, cancellationToken)
GetApproximateQueueDepthAsync(cancellationToken)
```

Receive and acknowledgement belong to the delivery client. Approximate queue depth belongs to the dedicated scaling client.

Requirements:

- Authenticate runtime operations with the API Hub scope.
- Require the Function identity to have an access policy on the connection referenced by the trigger config.
- Preserve the service-backed authorization evidence: the same API Hub token and approximate-queue-depth endpoint returned `200 OK` with the connection access policy, `403 Forbidden` without it, and `200 OK` after restoration.
- Preserve existing endpoint query parameters.
- Explicitly send `maxEvents` in the range 1-32.
- Parse `x-ms-more-messages-available`.
- Process acknowledgement statuses per item.
- Do not transparently retry Receive or Acknowledge after ambiguous failures.
- Use an extension-owned 30-second HTTP timeout for Receive, Acknowledge, and queue depth, and a 2-minute timeout for linked-output downloads. Acquire credentials before starting the HTTP timeout.
- Allow bounded transient retries only for safe queue-depth operations.
- Follow up by moving transport timeout and retry-policy ownership into the Connectors Polling SDK. The extension can expose user overrides through `ConnectorOptions` and map them into SDK options after Connector Namespace publishes supported latency and retry guidance.

Add a dedicated linked-output client or narrowly scoped collaborator:

- Require an absolute HTTPS URI with no user information or fragment.
- Never log or persist the complete signed URI.
- Do not attach an API Hub token; the URI signature fully authorizes the GET.
- Disable redirects and automatic decompression.
- Require `application/json; charset=utf-8`.
- Enforce actual bytes read with an absolute 100 MiB (104,857,600-byte) maximum.
- Allow bounded retries because repeated signed GETs are safe and idempotent while the link and content remain valid.
- Treat the linked-output HTTPS authority as opaque; it varies by cloud, region, scale unit, and environment. Authenticated Poll runtime requests use the opaque HTTPS authority from trusted application configuration and retain strict canonical path validation before an API Hub token is acquired.
- Normalize downloaded content to the same complete `outputs` JSON used by inline messages.

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

- The declared concrete function parameter type is the conversion source of truth.
- Scalar `T` and `ConnectorEvent<T>` bindings require an effective `MaxBatchSize` of exactly `1`.
- Array `T[]` and `ConnectorEvent<T>[]` bindings accept an effective `MaxBatchSize` of `1` or greater; a final invocation batch may contain fewer items.
- Apply shape validation after resolving `DefaultMaxBatchSize`, so a scalar parameter with `MaxBatchSize = 0` fails when the host default is greater than `1`.
- Fail incompatible bindings during indexing or listener startup rather than ignoring `MaxBatchSize`, dropping events, or changing the parameter shape.
- Keep `MaxConcurrentCalls` and `TargetPendingEventThreshold` independent of parameter shape.
- Support closed generic payload types; reject unresolved open generic functions.
- Preserve each Poll `MessageId` with its corresponding payload.
- Use `null` for Webhook `MessageId` unless the service defines an equivalent stable Webhook identifier.
- Keep `lockToken` internal.
- Do not add scalar `[BindingName("messageId")]` or parallel metadata arrays.

Implementation status:

- The host-to-worker deferred transport and scalar/array converters are implemented.
- `CollectionModelBindingData` conversion is covered independently.
- End-to-end multi-event collection binding is implemented.
- Cardinality-one bindings reject an effective `MaxBatchSize` greater than one; cardinality-many bindings accept values through the protocol limit of 32.

## PR 7: Poll Listener

Invocation batching is implemented in the lifecycle-safe, capacity-aware message pump. A richer poison-message policy and additional lock-budget telemetry are post-preview enhancements rather than another PR in this stack.

The listener now:

- Honors explicit one/many cardinality independently from `MaxBatchSize`.
- Uses the configured endpoints and receives no more than `availableInvocationSlots * MaxBatchSize`, capped at 32.
- Partitions Receive results into chunks of at most `MaxBatchSize`, with one active processing task per occupied invocation slot.
- Preserves normal batching for inline outputs and invokes each linked output individually behind a singleton host-wide limiter.
- Acknowledges all prepared messages only after a successful invocation.
- Passes metadata-rich scalar and collection values through the worker binding.

Capacity calculation:

```csharp
int availableInvocationSlots =
    effectiveMaxConcurrentCalls - activeInvocations.Count;
int maxEvents = Math.Min(
    32,
    availableInvocationSlots * effectiveMaxBatchSize);
```

Requirements:

- Receive only when `maxEvents > 0`.
- Do not create more active processing tasks than effective `MaxConcurrentCalls`.
- Partition each Receive result into invocation batches containing at most `MaxBatchSize` events.
- Allow no more than `MaxConcurrentCalls` active invocation-processing tasks per worker instance.
- Group inline messages normally, but place every linked-output message in a single-event invocation.
- Use one singleton host-wide linked-output invocation slot and hold it from before download through function execution and acknowledgement.
- Keep the slot process-local so each scaled-out Functions host can process one linked-output invocation; do not add distributed serialization.
- Do not infer a fixed client-side lock deadline because Connector Namespace controls the duration and does not currently return expiration metadata.
- Pass normalized payloads and per-event metadata through the worker binding.
- Acknowledge every event in a successful invocation batch.
- Acknowledge no events from a failed or cancelled invocation batch.
- Send acknowledgement requests with at most 32 locks.
- Drain immediately only when the service reports more messages and capacity is available.
- Add cancellation-aware empty-queue backoff with jitter.
- Make start, stop, cancel, and dispose behavior idempotent.
- Serialize Start/Stop, release completed pump state before restarting, and retain cancellation sources until their pump finishes even when Stop is cancelled or Dispose races with shutdown.
- Stop receiving during shutdown, use host drain mode to finish in-flight work without a listener-owned grace timer, otherwise cancel processing immediately, and leave unfinished events unacknowledged. Cancel and Dispose always cancel processing.

Inline processing remains concurrent. Only linked-output invocations are serialized host-wide to bound retained payload memory.

## PR 8: Scaling and Completion

Complete and validate the target scaler supported by the repository's WebJobs host version. The target-scaler implementation is already present in the stack. This final PR combines the configured endpoint migration with service-backed validation and release-readiness work.

Requirements:

- Use #26 only as a historical reference for the Functions scale-controller integration:
  - `ITargetScaler` and `ITargetScalerProvider`.
  - The reflectively discovered
    `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` hook.
  - Reading effective trigger and host options from `TriggerMetadata`.
  - Scale Monitor registration and scale-status validation.
- Do not reuse #26's positional `connectorNamespace`/`triggerName` attribute contract, Namespace API models or paths, authentication assumptions, mock metrics provider, or conflicting metadata names.
- Verify the referenced host interfaces and reflective registration signature against the current Functions host and Scale Monitor before implementation.
- Reuse the immutable configured endpoints and queue-depth client.
- Obtain metrics only through the current server-provided `approximateQueueDepthUri`.
- Scale from approximate queue depth without treating it as an exact count or a prerequisite for Receive.
- Calculate target workers from the configured target pending-event threshold:

  ```text
  ceil(pendingEvents / effectiveTargetPendingEventThreshold)
  ```

- Resolve `effectiveTargetPendingEventThreshold` from the trigger's nonzero `TargetPendingEventThreshold`, otherwise `DefaultTargetPendingEventThreshold`.
- Keep `TargetScalerContext.InstanceConcurrency`, `MaxConcurrentCalls`, and `MaxBatchSize` out of the scaling calculation.
- Document that aggregate queue depth cannot distinguish inline from linked events. Revisit the calculation if the service exposes payload-class or byte backlog metrics.
- Return zero depth when a queue-depth query fails so the target can scale down to zero.
- Validate scale from zero.
- Add integration tests, samples, and final user documentation.

This final PR completes the inline Poll implementation. Linked-output release validation remains deferred as described below.

## Cross-Stack Validation

Every PR must preserve:

- Webhook default behavior and dispatch.
- Host/worker metadata compatibility.
- Safe logging without payloads, credentials, tokens, lock tokens, or signed URLs.
- At-least-once delivery semantics.
- No automatic replay of ambiguous Receive or Acknowledge operations.

Final smoke test:

1. Enable a dedicated Poll trigger.
2. Start a Function App using the local extension build.
3. Generate new connector events.
4. Verify typed payload-only and metadata-rich invocation.
5. Verify batching, concurrent-call limits, and success-only acknowledgement.
6. Verify linked output when the service test path is available. Until then, rely on synthetic unit tests because Connector Namespace does not emit linked-output messages in the available environment.
7. Confirm acknowledged events do not reappear and failed events are redelivered.
8. Confirm queue depth returns to zero.
9. Stop the host and restore the trigger to its disabled state.

## Deferred Linked-Output Release Work

The Connector Namespace team confirmed the intended linked-output authentication, lifetime, redelivery, 100 MiB wire maximum, response shape, content type, compression, redirects, retry safety, integrity metadata, deletion, and authority behavior. The extension implements that protocol with synthetic coverage, but the service-side feature is still in development and does not emit linked-output messages in the available test environment.

The 100 MiB value remains a defensive protocol ceiling, not a validated customer payload guarantee. Before enabling or advertising linked-output support:

- Run service-backed .NET, Node.js, and Python tests with representative sizes through 100 MiB and establish the supported payload limit from measured peak memory and execution behavior.
- Measure deferred worker transport amplification and remove avoidable full-payload copies where possible.
- Measure the singleton limiter with multiple Poll functions and determine whether the `functions × MaxConcurrentCalls` waiter bound requires a bounded global queue, non-waiting admission, or a byte-budget limiter.
- Verify mixed workloads do not allow linked waiters to starve inline processing.
- Obtain service-owned lock-expiration metadata before adding any expiration-based admission decision; never hardcode or expose a user-configured lock duration.
- Validate linked-heavy scaling and request linked-count or byte-backlog signals if aggregate approximate depth under-scales the workload.
- Complete signed-link, retry, cancellation, shutdown, acknowledgement, redelivery, retention, deletion, telemetry, and sensitive-data E2E checks.

The detailed acceptance checklist is maintained in the design document under **Deferred Linked-Output Release Work**. Do not choose arbitrary payload, waiter, lock-reserve, or scaling thresholds before service-backed evidence is available.
