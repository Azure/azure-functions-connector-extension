## What's Changed

### Microsoft.Azure.Functions.Extensions.Connector

- Add preview support for Connector Namespace Poll triggers with single-event and batched delivery, configurable batching, single-event concurrent calls, independent target scaling based on pending events, and Poll message IDs.
- Apply `MaxConcurrentCalls` only to single-event delivery, aligning with Service Bus. Process one batch at a time per listener and ignore positive `MaxConcurrentCalls` on batch bindings with a startup warning.
- Configure Poll with a connection-scoped `pollingEndpoint` and a binding-scoped `TriggerConfigName`, while preserving Webhook defaults.

### Microsoft.Azure.Functions.Worker.Extensions.Connector

- Add preview support for Connector Namespace Poll triggers in .NET isolated worker apps, with single-event and batched delivery and Poll message IDs.
- Add `TriggerConfigName` metadata for Poll bindings.
