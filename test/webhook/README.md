# Connector Webhook Test Apps

For local development without a public callback, start with the [Poll test apps](../poll/README.md).

These test apps validate Connector Webhook delivery across language workers:

- [`dotnet`](./dotnet) - .NET isolated worker with typed payloads.
- [`typescript`](./typescript/README.md) - Node.js v4 TypeScript SDK app with a blob output binding.
- [`javascript`](./javascript/README.md) - Node.js v4 JavaScript SDK app with a blob output binding.
- [`python`](./python) - Python v2 with a blob output binding.
- [`java`](./java/README.md) - Java `@CustomBinding` with a raw JSON string and no connector SDK.
- [`powershell`](./powershell/README.md) - PowerShell `function.json` with JSON input and no connector SDK.
- [`go`](./go/README.md) - native Go worker with `GenericTrigger` and a raw binding; no Connector SDK.

Each test app references the repository-local Connector extension. Follow its README for build and run instructions. Java, PowerShell, and Go use `OnGenericWebhook` without connector-specific payload models.

## Required and optional configuration

The .NET binding needs only `[ConnectorTrigger]`; `DeliveryMode` is optional and defaults to `Webhook`. Poll-only `Connection`, `TriggerConfigName`, `pollingEndpoint`, batching, concurrency, and scaling settings are not needed.

For callbacks from Azure, register a public `notificationDetails.callbackUrl` with the correct `functionName`, plus separate `QueryString` authentication named `code` containing the `connector_extension` system key. Local service callbacks require a public dev tunnel and `func start --enableAuth`; a direct local callback test does not require the tunnel.

No Connector-specific `host.json` settings are required. See the [configuration reference](../../docs/connector-trigger-configuration.md#webhook-delivery) for required service configuration versus optional binding settings, and the [Webhook registration skill](../../.github/skills/webhook-trigger-registration/SKILL.md) for setup.
