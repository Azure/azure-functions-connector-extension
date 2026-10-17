# Azure Functions Connector Extension

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://dev.azure.com/azfunc/public/_apis/build/status/1710?branchName=main)](https://dev.azure.com/azfunc/public/_build?definitionId=1710&branchName=main)
[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.Connector.svg?label=Microsoft.Azure.Functions.Worker.Extensions.Connector)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Connector)

- [Learn Documentation](https://learn.microsoft.com/azure/azure-functions/functions-connectors-overview)
- [Try Samples](https://aka.ms/functions-connectors-samples)

Receive events from Connector Namespace managed connectors, including Office 365 Outlook, SharePoint, and Teams.

| Delivery mode | How it works | Local development |
| --- | --- | --- |
| **Poll** | The Functions host receives queued events and acknowledges successful invocations. | Start here: no public callback URL or dev tunnel is needed. |
| **Webhook** | Connector Namespace sends events to the Function App's callback endpoint. | Requires a public, authenticated dev tunnel for service callbacks. |

Poll is presented first for its simpler local setup. **Webhook remains the binding default** when `DeliveryMode` is omitted.

## Installation

For .NET isolated:

```powershell
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector
```

SDK model packages are optional; a trigger can bind to `string` or JSON without them. See [payload types and SDKs](./docs/connector-trigger-configuration.md#payload-types-and-sdks).

For Node.js or Python, see [non-.NET worker setup](./docs/connector-trigger-configuration.md#non-net-workers). The [repository test samples](#test-samples) build the current checkout; published packages and bundles can lag changes in this checkout.

## Usage

Both modes require an existing, authorized Connector Namespace connection and a trigger configuration for the connector operation. The extension does not register trigger configurations automatically. Use the [connection setup skill](./.github/skills/connection-setup/SKILL.md) to create and authorize a connection.

### Poll delivery

1. Register an enabled Trigger Config with `deliveryMode: Poll` using the [Poll registration skill](./.github/skills/poll-trigger-registration/SKILL.md).
2. Grant the namespace identity and the Function/developer identity their connection-level access policies.
3. Configure the binding and connection settings below.

For a .NET isolated function, the minimum Poll parameter configuration is:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorNamespace",
    TriggerConfigName = "%OnNewEmailTriggerConfigName%")]
string payload
```

`Connection` is an app-setting prefix, not the name of the Office 365 or other connection resource in Azure.

For local .NET isolated development, configure `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectorNamespace__pollingEndpoint": "https://<connector-namespace-host>",
    "OnNewEmailTriggerConfigName": "<trigger-config-name>"
  }
}
```

Derive `pollingEndpoint` from the Trigger Config's service-generated `pollingEndpoints.receiveUri`:

| Receive URI | `ConnectorNamespace__pollingEndpoint` |
| --- | --- |
| `https://<connector-namespace-host>/api/triggerconfigs/<name>/receive` | `https://<connector-namespace-host>` |
| `https://<host>/api/connectorGateways/<id>/triggerconfigs/<name>/receive` | `https://<host>/api/connectorGateways/<id>` |

Supply the trigger name separately. Do not configure a complete Receive URL or a standalone `/api` base.

Locally, omit the credential selector to use your developer identity. For a deployed app using managed identity, set `ConnectorNamespace__credential=managedidentity`. Never commit real endpoints or credentials.

Start Azurite, then start the Function host with Core Tools. No inbound port forwarding is needed. Follow the [Poll samples](./test/poll/README.md) for complete build and run commands.

Poll delivery is at least once: failed invocations are not acknowledged and may be redelivered. Use `ConnectorEvent<T>.MessageId` for application-level deduplication.

### Webhook delivery

For a .NET isolated function, no Connector-specific attribute parameters are required:

```csharp
[ConnectorTrigger]
string payload
```

Register this callback on the Connector Namespace Trigger Config:

```text
https://<function-app-host>/runtime/webhooks/connector?functionName=<FunctionName>
```

`functionName` must match the function name. Deployed callbacks require the automatically generated `connector_extension` system key. Configure it separately in `notificationDetails.authentication` as a `QueryString` value named `code`, rather than embedding it in the stored callback URL.

Webhook-only functions do not need `pollingEndpoint`, `Connection`, or `TriggerConfigName`. For local service callbacks, use a public dev tunnel and start Core Tools with `func start --enableAuth`. See the [Webhook registration skill](./.github/skills/webhook-trigger-registration/SKILL.md) and [Webhook samples](./test/webhook/README.md).

### Required settings and optional tuning

| Configuration | Poll | Webhook |
| --- | --- | --- |
| `DeliveryMode` | Required: `Poll` | Optional: defaults to `Webhook` |
| `Connection` | Required: app-setting prefix | Not needed |
| `TriggerConfigName` | Required: literal or `%SettingName%` | Not needed |
| `<Connection>__pollingEndpoint` | Required | Not needed |
| Callback URL and system-key authentication | Not used | Required for deployed/service callbacks |
| Batching, concurrency, and scaling settings | Optional | Poll-only tuning does not apply |

**No Connector-specific `host.json` settings are mandatory.** The minimal .NET isolated file is `{"version":"2.0"}`. Set tuning values in the binding or use host defaults; you do not need to duplicate them.

See the [configuration reference](./docs/connector-trigger-configuration.md) for every binding option, defaults, identity settings, and mode-specific requirements.

## Documentation

- [Trigger configuration reference](./docs/connector-trigger-configuration.md): required and optional settings for both delivery modes.
- [Batching, concurrency, and scaling](./docs/connector-poll-concurrency-visual-guide.md): how the independent Poll controls interact.
- [Operations to function signatures](./docs/operations-functions-match.md): connector payloads across .NET, Python, and TypeScript.
- [Poll design and release-validation status](./docs/connector-trigger-poll-delivery-design.md): transport, linked outputs, retries, shutdown, and remaining validation.
- [Learn documentation](https://learn.microsoft.com/azure/azure-functions/functions-connectors-overview) and [published samples](https://aka.ms/functions-connectors-samples).
- [Connector portal](https://connectors.azure.com/) and [Connector Namespace CLI reference](https://github.com/Azure/Connectors/blob/main/public-preview/connector-namespace-cli/complete-reference.md).

## Copilot Skills

| Skill | Purpose |
| --- | --- |
| [connection-setup](./.github/skills/connection-setup/SKILL.md) | Create and authorize a connector connection and configure access policies. |
| [poll-trigger-registration](./.github/skills/poll-trigger-registration/SKILL.md) | Register host-pull Poll delivery and derive Function settings. |
| [webhook-trigger-registration](./.github/skills/webhook-trigger-registration/SKILL.md) | Register callback delivery and system-key authentication. |

## Test Samples

These samples reference the repository-local extension for development:

- [Poll delivery](./test/poll/README.md): .NET isolated, TypeScript, and Python.
- [Webhook delivery](./test/webhook/README.md): .NET isolated, Node.js, and Python.

## Building

Requires .NET SDK 10.0.100 or later; see `global.json`.

```powershell
dotnet build
dotnet test
```

NuGet uses the public Azure Artifacts feed configured in `NuGet.config`. For Node.js sample feed setup and authentication, see the [sample build prerequisites](./test/poll/README.md#build-prerequisites). Keep credentials in user-level or CI configuration, never in checked-in files.

## Roadmap

- [ ] **Webhook auto-registration**: register trigger configurations on function deployment.
- [ ] **Distributed tracing**: add DiagnosticScope for Application Insights integration.

## Data Collection

The Azure Functions Connector Extension collects telemetry data, including exceptions, warnings, and informational logs generated by the extension while running in the managed Azure Functions environment, and sends this telemetry to Microsoft. Microsoft uses this telemetry to diagnose issues, improve reliability, and enhance the product experience. This telemetry collection cannot be disabled.

The telemetry collected by the extension does not include personal information, customer application payload contents, or application logs, exceptions, failures, warnings, or informational messages generated by customer Function App code.

You must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the Azure Functions Connector Extension extension operates as your consent to these practices.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com.).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
