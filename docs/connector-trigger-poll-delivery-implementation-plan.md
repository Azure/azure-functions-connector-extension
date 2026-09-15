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

Deliver production Poll support as eight stacked PRs while preserving existing
Webhook behavior. The authoritative behavioral and architectural decisions are
in `connector-trigger-poll-delivery-design.md`; this document tracks
implementation boundaries, dependencies, and completion criteria.

## Verified Baseline

The proof of concept verified this end-to-end path:

1. Authenticate to ARM with `https://management.azure.com/.default`.
2. Read a Poll trigger configuration using API version
   `2026-05-01-preview`.
3. Extract the server-generated Poll runtime endpoints.
4. Authenticate to the runtime with `https://apihub.azure.com/.default`.
5. Receive messages, convert complete trigger `outputs`, execute a typed
   function, and acknowledge successful processing.

The supported Connector Gateway ARM resource type is:

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

Replace the earlier public `MaxEvents` seam with the user-facing invocation
controls:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorGateway",
    TriggerConfigName = "%CONNECTOR_TRIGGER_CONFIG%",
    BatchSize = 4,
    Concurrency = 8)]
```

Changes:

- Add `BatchSize` and `Concurrency` to both host and isolated-worker
  attributes.
- Remove public `MaxEvents`.
- Add host-level `ConnectorOptions`:

  ```csharp
  public int DefaultBatchSize { get; set; } = 1;
  public int DefaultConcurrency { get; set; } = 16;
  ```

- Treat `0` on either attribute property as use-host-default.
- Accept `BatchSize` values from `0` through `32` and require the effective
  value to be from `1` through `32`.
- Accept non-negative `Concurrency` and require the effective value to be
  greater than zero.
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
ConnectorGateway__resourceId=/subscriptions/.../resourceGroups/.../providers/Microsoft.Web/connectorGateways/...
ConnectorGateway__credential=managedidentity
ConnectorGateway__clientId=<optional-user-assigned-identity-client-id>
ConnectorGateway__managedIdentityResourceId=<optional-resource-id>
```

Changes:

- Resolve the literal `Connection` prefix through `IConfiguration`.
- Require `Connection`, `resourceId`, and `TriggerConfigName` only in Poll
  mode.
- Parse resource IDs with `Azure.Core.ResourceIdentifier`.
- Require exactly a resource-group-scoped
  `Microsoft.Web/connectorGateways/{gateway}` resource.
- Reject invalid subscription GUIDs, child-resource paths, full URLs, query
  strings, and fragments.
- Add managed identity credential selection with test seams.
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
ConnectorQueueStatus
```

Requirements:

- Use explicit `System.Text.Json` mappings.
- Require non-empty message IDs and lock tokens.
- Require exactly one of `outputs` and `outputsLink`.
- Validate linked-output URI and declared content size.
- Parse mixed acknowledgement results: `Acknowledged`, `NotFound`, and
  `Failed`.
- Never expose or log lock tokens or signed output URLs.
- Keep generated `Azure.Connectors.Sdk` types out of the host protocol layer.

## PR 4: ARM Endpoint Resolver

Implement `IConnectorPollingEndpointResolver`.

Requirements:

- Read:

  ```text
  {connectorGatewayResourceId}/triggerConfigs/{escapedName}?api-version=2026-05-01-preview
  ```

- Authenticate with the ARM scope.
- Verify that delivery mode is Poll and the trigger is enabled.
- Extract `receiveUri`, `acknowledgeUri`, `hasMessagesUri`, and
  `approximateQueueDepthUri`.
- Require absolute HTTPS endpoints.
- Cache by connection and trigger-config name.
- Refresh only for endpoint-specific stale failures.

## PR 5: Runtime and Linked-Output Clients

Implement:

```csharp
ReceiveAsync(endpoints, maxEvents, cancellationToken)
AcknowledgeAsync(endpoints, locks, cancellationToken)
GetQueueStatusAsync(endpoints, cancellationToken)
```

Requirements:

- Authenticate runtime operations with the API Hub scope.
- Preserve existing endpoint query parameters.
- Explicitly send `maxEvents` in the range 1-32.
- Parse `x-ms-more-messages-available`.
- Process acknowledgement statuses per item.
- Do not transparently retry Receive or Acknowledge after ambiguous failures.
- Allow bounded transient retries only for safe queue-status operations.

Add a dedicated linked-output client or narrowly scoped collaborator:

- Require an absolute HTTPS URI with no user information or fragment.
- Never log or persist the complete signed URI.
- Do not attach an API Hub token unless the service confirms it is required.
- Enforce both declared `contentSize` and actual bytes read.
- Bound buffering, download concurrency, and redirects according to the
  finalized service contract.
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
  `BatchSize` of exactly `1`.
- Array `T[]` and `ConnectorEvent<T>[]` bindings accept an effective
  `BatchSize` of `1` or greater; a final invocation batch may contain fewer
  items.
- Apply shape validation after resolving `DefaultBatchSize`, so a scalar
  parameter with `BatchSize = 0` fails when the host default is greater than
  `1`.
- Fail incompatible bindings during indexing or listener startup rather than
  ignoring `BatchSize`, dropping events, or changing the parameter shape.
- Keep `Concurrency` independent of parameter shape.
- Support closed generic payload types; reject unresolved open generic
  functions.
- Preserve each Poll `MessageId` with its corresponding payload.
- Use `null` for Webhook `MessageId` unless the service defines an equivalent
  stable Webhook identifier.
- Keep `lockToken` internal.
- Do not add scalar `[BindingName("messageId")]` or parallel metadata arrays.

## PR 7: Poll Listener

Replace the placeholder with a lifecycle-safe, capacity-aware message pump.

Capacity calculation:

```csharp
int availableSlots = effectiveConcurrency - activeInvocations;
int maxEvents = Math.Min(32, availableSlots * effectiveBatchSize);
```

Requirements:

- Receive only when `maxEvents > 0`.
- Do not prefetch beyond immediate invocation capacity.
- Partition received events into invocation batches of at most `BatchSize`.
- Run no more than `Concurrency` function invocations per worker.
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

Add the scale monitor or target scaler supported by the repository's WebJobs
host version.

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
- Reuse the endpoint resolver and queue-status client.
- Obtain metrics only through the current server-provided
  `approximateQueueDepthUri`.
- Scale from approximate queue depth without treating it as an exact count or
  a prerequisite for Receive.
- Calculate target workers from effective per-worker capacity:

  ```text
  ceil(pendingEvents / (effectiveConcurrency * effectiveBatchSize))
  ```

- Return conservative decisions when queue status is unavailable.
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

## Open Service Dependencies

The design document tracks the authoritative open questions for linked-output
authentication, lifetime, redelivery, maximum size, response shape,
compression, redirects, retry safety, integrity metadata, deletion, and valid
hosts. PR 5 must not guess those service semantics.
