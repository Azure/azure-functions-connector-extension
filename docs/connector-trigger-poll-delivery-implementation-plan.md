# Connector Trigger Poll Delivery Implementation Plan

## Table of Contents

- [Purpose](#purpose)
- [Verified Baseline](#verified-baseline)
- [Stacking Rules](#stacking-rules)
- [PR 1: Trigger Contract](#pr-1-trigger-contract)
- [PR 2: Configuration](#pr-2-configuration)
- [PR 3: Protocol Models](#pr-3-protocol-models)
- [PR 4: Configured Runtime Endpoints](#pr-4-configured-runtime-endpoints)
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

The proof of concept verified the runtime path now used by the extension:

1. Configure the gateway-level Poll runtime endpoint from the Trigger Config's service-generated Receive URI.
2. Authenticate to the runtime with `https://apihub.azure.com/.default`.
3. Query approximate queue depth, receive messages, execute a typed function, and acknowledge successful processing.

The extension no longer discovers runtime endpoints through ARM.
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

Replace the earlier public scaling seam with an explicit pending-event target:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorNamespace",
    TriggerConfigName = "%CONNECTOR_TRIGGER_CONFIG%",
    MaxBatchSize = 4,
    TargetPendingEventThreshold = 16)]
```

Changes:

- Add `TargetPendingEventThreshold` to both host and isolated-worker attributes.
- Add `DefaultTargetPendingEventThreshold = 16` to `ConnectorOptions`.
- Treat `0` as use-host-default.
- Accept a non-negative trigger value and require the resolved default to be greater than zero.
- Keep `MaxBatchSize` independent from target scaling.
- Preserve `Webhook` as the default.
- Keep `Connection` literal; continue allowing `%...%` resolution for `TriggerConfigName`.

Completion criteria:

- Host and worker contracts remain synchronized.
- Existing Webhook tests remain unchanged and pass.
- The former overloaded scaling property is removed.
## PR 2: Configuration

Add validated immutable runtime-endpoint configuration and dependency registration.

```text
ConnectorNamespace__pollingEndpoint=https://<host>/api/connectorGateways/<connector-namespace-id>
ConnectorNamespace__credential=managedidentity
ConnectorNamespace__clientId=<optional-user-assigned-identity-client-id>
ConnectorNamespace__managedIdentityResourceId=<optional-resource-id>
```

Changes:

- Resolve the literal `Connection` prefix through Functions configuration.
- Require `Connection`, `pollingEndpoint`, and `TriggerConfigName` only in Poll mode.
- Treat the configured authority and gateway identifier as opaque trusted configuration.
- Derive fixed Trigger Config runtime-operation URIs from the configured gateway endpoint and resolved Trigger Config name.
- Do not perform ARM endpoint discovery or accept debug bearer-token settings.
- Pass the full named connection section to `AzureComponentFactory.CreateTokenCredential`.
- Register Poll and scaling services through `AddConnector`.

Completion criteria:

- Invalid Poll configuration fails with actionable errors.
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
- Never expose or log lock tokens or signed output URLs.
- Use secret-safe `ToString()` implementations for token- and signed-URI-bearing models.
- Keep generated `Azure.Connectors.Sdk` types out of the host protocol layer.
- Keep wire parsing, protocol validation, status interpretation, and URI redaction independent of WebJobs types so they can move to a future Connectors Polling SDK.

## PR 4: Configured Runtime Endpoints

Implement `ConnectorPollingEndpoints.Create` from the configured gateway-level `pollingEndpoint` and resolved Trigger Config name.

Requirements:

- Preserve the configured authority and gateway path.
- Append only `/triggerConfigs/<escaped-name>` and the fixed runtime operation suffixes.
- Require absolute HTTPS endpoints.
- Do not parse a Connector Namespace ARM resource ID or call ARM.
- Fail missing or invalid endpoint configuration with actionable errors.
## PR 5: Runtime and Linked-Output Clients

Implement:

```csharp
ReceiveAsync(endpoints, maxEvents, cancellationToken)
AcknowledgeAsync(endpoints, locks, cancellationToken)
GetQueueStatusAsync(endpoints, cancellationToken)
```

Requirements:

- Authenticate runtime operations with the API Hub scope.
- Require the Function identity to have an access policy on the connection referenced by the trigger config.
- Add a service-backed authorization test for the connection access policy.
- Preserve existing endpoint query parameters.
- Explicitly send `maxEvents` in the range 1-32.
- Parse `x-ms-more-messages-available`.
- Process acknowledgement statuses per item.
- Do not transparently retry Receive or Acknowledge after ambiguous failures.
- Allow bounded transient retries only for safe queue-status operations.

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
- Keep `TargetPendingEventThreshold` independent of parameter shape and listener behavior.
- Support closed generic payload types; reject unresolved open generic
  functions.
- Preserve each Poll `MessageId` with its corresponding payload.
- Use `null` for Webhook `MessageId` unless the service defines an equivalent
  stable Webhook identifier.
- Keep `lockToken` internal.
- Do not add scalar `[BindingName("messageId")]` or parallel metadata arrays.

## PR 7: Poll Listener

Implement the Poll listener independently from target scaling.

Requirements:

- Do not use `TargetPendingEventThreshold` for Receive sizing, batching, or listener concurrency.
- Keep invocation batching controlled by `MaxBatchSize`.
- Introduce any listener execution-concurrency setting under a separate name and in the listener PR that owns it.
- Preserve success-only acknowledgement, lifecycle, backoff, lock-budget, and shutdown behavior.
## PR 8: Scaling and Completion

Add the target scaler supported by the repository's WebJobs host version.

Requirements:

- Use `ITargetScaler` and `ITargetScalerProvider` through the reflectively discovered `AddConnectorScaleForTrigger(IWebJobsBuilder, TriggerMetadata)` hook.
- Read the connection, resolved Trigger Config name, and target threshold from `TriggerMetadata`.
- Query only the configured `/approximateQueueDepth` runtime endpoint using the API Hub credential.
- Follow Event Hubs and Service Bus target-scaler precedence:

  ```text
  effectiveTarget =
      TargetScalerContext.InstanceConcurrency
      ?? trigger TargetPendingEventThreshold
      ?? DefaultTargetPendingEventThreshold
  ```

- Require the effective target to be greater than zero.
- Calculate `ceil(pendingEvents / effectiveTarget)`.
- Keep `MaxBatchSize` out of the scaling calculation.
- Propagate caller cancellation; log other query failures and return a fresh zero-depth sample.
- Validate scale from zero and mixed runtime/configured concurrency paths.

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
5. Verify batching and success-only acknowledgement; listener concurrency is validated in its owning PR.
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
