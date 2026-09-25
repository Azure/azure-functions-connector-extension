# Connector Webhook Sample Apps

These apps validate Connector Webhook delivery from each supported language
worker:

- [`dotnet`](./dotnet) - .NET isolated worker with typed payloads.
- [`nodejs`](./nodejs) - Node.js v4 with a blob output binding.
- [`python`](./python) - Python v2 with a blob output binding.

Each sample references the repository-local Connector extension for
development. Follow the language-specific README for build and run
instructions.

For Connector Namespace Poll delivery, use the separate
[`test/poll`](../poll/README.md) samples.
