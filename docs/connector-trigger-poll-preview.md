# Connector Trigger Poll Preview

This preview is the first package that can run a Connector Namespace Poll
trigger in an Azure Functions .NET isolated app. It resolves the trigger
configuration through ARM, checks queue state, receives leased events,
hydrates inline or linked outputs, invokes one function per event, and
acknowledges only successful invocations.

## Build the preview packages

From the repository root:

```powershell
dotnet pack .\Microsoft.Azure.Functions.Extensions.Connector.sln `
    --configuration Release `
    --output .\out\pkg
```

The local feed contains both packages required by the isolated worker:

- `Microsoft.Azure.Functions.Extensions.Connector`
- `Microsoft.Azure.Functions.Worker.Extensions.Connector`

Add the worker package to a Function App using the version produced in
`out\pkg`:

```powershell
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector `
    --prerelease `
    --source <repository-root>\out\pkg
```

## Configure a Poll function

The initial listener supports one event per invocation. `Concurrency` controls
how many single-event invocations can run simultaneously on one worker.

```csharp
using Azure.Connectors.Sdk.Office365.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;

public sealed class Functions
{
    [Function("OnNewEmail")]
    public void OnNewEmail(
        [ConnectorTrigger(
            DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
            Connection = "ConnectorNamespace",
            TriggerConfigName = "onnewemail-poll",
            MaxBatchSize = 1,
            Concurrency = 8)]
        Office365OnNewEmailTriggerPayload payload)
    {
        // Process one connector event.
    }
}
```

Configure the Connector Namespace resource and credential:

```text
ConnectorNamespace__resourceId=/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Web/connectorGateways/<namespace-name>
ConnectorNamespace__credential=managedidentity
```

For local development, omit `ConnectorNamespace__credential` to use the
developer credential selected by the Functions Azure client factory, such as
the account authenticated through `az login`.

The identity must be able to read the trigger configuration through ARM and
must have the Connector Namespace access policy required for Poll runtime
operations. The named trigger configuration must already exist, be enabled,
use `deliveryMode: Poll`, and expose the four polling endpoints.

Runnable .NET, TypeScript, and Python examples are available under
[`test/poll`](../test/poll/README.md). The .NET sample consumes the typed
Office 365 payload, while the TypeScript and Python samples demonstrate the
generic JSON binding metadata used before a preview extension bundle is
available.

## Current limitations

- This package is an experimental preview and is not production-ready.
- The effective `MaxBatchSize` must be `1`. Array binding and multi-event
  function invocations are not implemented.
- `Concurrency` is supported, but only as concurrent single-event invocations.
  One Receive call may fill the currently available invocation slots, up to
  the service limit of 32.
- The function receives only the connector `outputs` payload. The stable
  Connector Namespace `messageId` is not exposed yet, so applications cannot
  use it as a transport-level deduplication key in this preview.
- Delivery is at least once. Functions that perform non-idempotent side
  effects must use a durable business-level idempotency key where available.
- The message lock is fixed at two minutes and is not renewed. If processing
  completes after the lock expires, acknowledgement can return `NotFound` and
  the event can be delivered again.
- Failed or cancelled functions and linked-output download failures are not
  acknowledged. Connector Namespace may redeliver them until its retention
  limit; the extension does not yet provide poison-message handling or a
  dead-letter policy.
- Polling endpoints are resolved at listener startup but are not automatically
  refreshed after runtime endpoint failures.
- Empty-queue and failure backoff use fixed internal defaults with jitter;
  they are not configurable yet.
- Linked outputs are validated and limited to 100 MiB, but each payload is
  materialized in memory before invocation. Higher concurrency can therefore
  increase worker memory pressure.
- The full worker binding for metadata-rich `ConnectorEvent<T>` values is not
  implemented. Lock tokens remain internal and are never exposed.
- Non-.NET languages require a future preview extension-bundle release before
  they can consume this Poll listener through generic binding metadata.
- Local package build and Function App compilation can be validated in this
  repository. A configured Connector Namespace and Azure identity are still
  required for a live end-to-end Poll smoke test.
