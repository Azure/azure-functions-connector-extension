# Connector Webhook Sample Apps

For local development without a public callback, start with the [Poll samples](../poll/README.md).

These apps validate Connector Webhook delivery from each supported language
worker:

- [`dotnet`](./dotnet) - .NET isolated worker with typed payloads.
- [`nodejs`](./nodejs) - Node.js v4 with a blob output binding.
- [`python`](./python) - Python v2 with a blob output binding.

Each sample references the repository-local Connector extension for
development. Follow the language-specific README for build and run
instructions.

## Required and optional configuration

The .NET binding needs only `[ConnectorTrigger]`; `DeliveryMode` is optional and defaults to `Webhook`. Poll-only `Connection`, `TriggerConfigName`, `pollingEndpoint`, batching, concurrency, and scaling settings are not needed.

For callbacks from Azure, register a public `notificationDetails.callbackUrl` with the correct `functionName`, plus separate `QueryString` authentication named `code` containing the `connector_extension` system key. Local service callbacks require a public dev tunnel and `func start --enableAuth`; a direct local callback test does not require the tunnel.

No Connector-specific `host.json` settings are required. See the [configuration reference](../../docs/connector-trigger-configuration.md#webhook-delivery) for required service configuration versus optional binding settings, and the [Webhook registration skill](../../.github/skills/webhook-trigger-registration/SKILL.md) for setup.
