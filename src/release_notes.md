## What's Changed

### Microsoft.Azure.Functions.Extensions.Connector

- Fix a Webhook routing regression introduced in `0.4.0-alpha` that returned HTTP 404 for existing callback URLs. Register Webhook listeners using the public function name without the host-generated `Functions.` prefix.
- Preserve Poll listener registration and delivery behavior.

### Microsoft.Azure.Functions.Worker.Extensions.Connector

- Update the referenced host extension to `0.4.1-alpha` to include the Webhook routing fix for .NET isolated worker apps.
