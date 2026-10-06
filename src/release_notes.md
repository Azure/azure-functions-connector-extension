## What's Changed

### Microsoft.Azure.Functions.Extensions.Connector

- Add preview support for Connector Namespace Poll triggers with single-event and batched delivery, configurable batching, concurrent calls, and target scaling, Poll message IDs, and scaling based on pending events.
- Configure Poll with a connection-scoped `pollingEndpoint` and a binding-scoped `TriggerConfigName`, while preserving Webhook defaults.

### Microsoft.Azure.Functions.Worker.Extensions.Connector

- Add preview support for Connector Namespace Poll triggers in .NET isolated worker apps, with single-event and batched delivery and Poll message IDs.
- Add `TriggerConfigName` metadata for Poll bindings.
