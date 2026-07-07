# Azure Functions Connector Extension (Preview)

> **⚠️ Preview Extension Notice**
>
> This extension is in early preview and may contain breaking changes without prior notice. It includes features that are still under development and not yet ready for production use. Users should be aware that:
>
> - Trigger behavior and binding contracts may change between versions
> - Performance characteristics and stability may vary across releases
> - Security updates may require version upgrades
>
> This feature preview has no SLA provided during the preview. DO NOT use the preview features in any production or critical environments.
> We welcome feedback and contributions — please [open an issue](https://github.com/Azure/azure-functions-connector-extension/issues) with questions, suggestions, or bug reports.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://dev.azure.com/azfunc/public/_apis/build/status/1710?branchName=main)](https://dev.azure.com/azfunc/public/_build?definitionId=1710&branchName=main)

An Azure Functions trigger extension for receiving webhook callbacks from Connector Namespace managed connectors (Office 365, Teams, SharePoint, etc.).

- [Learn Documentation](https://learn.microsoft.com/azure/azure-functions/functions-connectors-overview)
- [Try Samples](https://aka.ms/functions-connectors-samples)

## NuGet Packages

The following NuGet packages are available as part of this project.

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Worker.Extensions.Connector.svg?label=Microsoft.Azure.Functions.Worker.Extensions.Connector)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Connector)

## Extension Bundle

For non-.NET languages (Node.js, Python, etc.), use the **preview extension bundle** version 4.42.0 or greater. Add the following to your `host.json`:

```json
{
    "version": "2.0",
    "extensionBundle": {
        "id": "Microsoft.Azure.Functions.ExtensionBundle.Preview",
        "version": "[4.42.0, 5.0.0)"
    }
}
```

## Overview

This extension enables Azure Functions to receive trigger callbacks from Connector Namespace managed connectors. When a connector event occurs (e.g., new email arrives), Connector Namespace sends a webhook callback to your function.

**Endpoint Pattern:**

```text
POST /runtime/webhooks/connector?functionName={FunctionName}&code={connector_extension_key}
```

## Authentication

The connector webhook endpoint requires a **system key** named `connector_extension`. This key is automatically created when the extension registers its webhook handler.

To get the key:

```bash
# Azure CLI
az functionapp keys list -g <resource-group> -n <function-app> --query "systemKeys.connector_extension" -o tsv

# Or find it in Azure Portal: Function App > App keys > System keys > connector_extension
```

The full callback URL for Connector Namespace:

```text
https://<function-app-domain>/runtime/webhooks/connector?functionName=<function-name>=<connector_extension_key>
```

## Features

- **Connector trigger binding** - receive callbacks from Connector Namespace managed connectors
- **POCO binding** - bind directly to SDK types like `Office365OnNewEmailTriggerPayload`
- **String/JSON binding** - bind to raw JSON string
- **.NET isolated worker** - modern .NET isolated worker model
- **Node.js support** - generic trigger binding for Node.js functions
- **Python support** - generic trigger binding for Python functions

## Installation

### .NET Isolated Worker

Add the worker extension NuGet package or project reference:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector --prerelease
```

### Strongly-typed Payloads

For strongly-typed trigger payloads, add the SDK or extension package for your language:

#### .NET

```bash
dotnet add package Azure.Connectors.Sdk --prerelease
```

#### Python

The Python extension package (`azurefunctions-extensions-connectors`) integrates the [Connector SDK](https://github.com/Azure/Connectors-python-sdk) with the Functions runtime for typed bindings:

```bash
pip install azure-functions>=2.2.0b4 # For Python version >= 3.13
pip install azure-functions>=1.26.0b3 # For Python version < 3.12
pip install azurefunctions-extensions-connectors
```

#### Node.js

```bash
npm install @azure/connectors
```

### Connector SDKs

The underlying Connector SDKs provide typed models:

| Language | Package | Repository |
| ---------- | --------- | ------------ |
| .NET | [Azure.Connectors.Sdk](https://www.nuget.org/packages/Azure.Connectors.Sdk) | [Connectors-NET-SDK](https://github.com/Azure/Connectors-NET-SDK) |
| Python | [azurefunctions-extensions-connectors](https://pypi.org/project/azurefunctions-extensions-connectors) | [connectors-python-sdk](https://github.com/Azure/Connectors-python-sdk) |
| Node.js | [@azure/connectors](https://www.npmjs.com/package/@azure/connectors) | [Connectors-NodeJS-SDK](https://github.com/Azure/Connectors-nodejs-sdk) |

## Usage

### Supported Binding Types

- `string` - raw JSON body
- POCO/model types - strongly-typed SDK models (see individual SDK docs for available types)

## Documentation

- **[Operations to Functions Signature Mapping](./docs/operations-functions-match.md)** - Complete reference of all connector trigger operations and their Azure Functions signatures across .NET, Python, and TypeScript SDKs

## Copilot Skills

This repository includes [Copilot Skills](https://docs.github.com/en/copilot/customizing-copilot/copilot-extensions/building-copilot-skills) that provide guided workflows for setting up and configuring connector triggers:

| Skill | Description |
| ----- | ----------- |
| [connection-setup](./.github/skills/connection-setup/SKILL.md) | Create and configure Connector Namespace connections, authorize OAuth consent, and add access policies |
| [trigger-registration](./.github/skills/trigger-registration/SKILL.md) | Register polling trigger configs that call back to an Azure Function on connector events |
| [operations-doc-sync](./.github/skills/operations-doc-sync/SKILL.md) | Update `docs/operations-functions-match.md` to match the latest trigger operations in the parent SDK repos |

## Connector Namespace Aceess

- [Connector Portal](https://connectors.azure.com/)
- [Connector Namespace CLI Reference](https://github.com/Azure/Connectors/blob/main/public-preview/connector-namespace-cli/complete-reference.md)

## Test Samples

These samples build and reference local extension code and are meant for extension testing:

- **[.NET Isolated](./test/dotnet)** - .NET isolated worker sample
- **[Node.js](./test/nodejs)** - Node.js v4 with blob output
- **[Python](./test/python)** - Python v2

## Project Structure

```text
azure-functions-connector-extension/
├── src/
│   ├── Microsoft.Azure.Functions.Extensions.Connector/          # WebJobs host extension
│   │   ├── ConnectorExtensionConfigProvider.cs                  #   Extension config, HTTP routing
│   │   ├── ConnectorHttpRequestProcessor.cs                     #   Request parsing, function dispatch
│   │   ├── ConnectorTriggerAttribute.cs                         #   Trigger attribute
│   │   ├── ConnectorTriggerBinding.cs                           #   Trigger binding
│   │   ├── ConnectorTriggerBindingProvider.cs                   #   Binding provider
│   │   ├── ConnectorListener.cs                                 #   Function registration
│   │   ├── ConnectorFunctionRegistration.cs                     #   Function metadata
│   │   ├── ConnectorStartup.cs                                  #   WebJobs startup hook
│   │   └── ConnectorWebJobsBuilderExtensions.cs                 #   DI registration
│   └── Microsoft.Azure.Functions.Worker.Extensions.Connector/   # Worker extension (.NET isolated)
│       ├── ConnectorTriggerAttribute.cs                         #   Trigger attribute
│       └── Converters/                                          #   Type converters
├── test/
│   ├── dotnet/                                                  # .NET isolated worker test app
│   ├── nodejs/                                                  # Node.js sample with blob output
│   ├── python/                                                  # Python sample with blob output
│   ├── test-requests.http                                       # HTTP test requests
│   └── Microsoft.Azure.Functions.Extensions.Connector.Tests/    # Unit tests
└── eng/                                                         # Build and CI infrastructure
```

## Building

Requires .NET SDK 10.0.100 or later (see `global.json`).

```bash
dotnet build
dotnet test
```

## Roadmap

The following features are planned for future releases:

- [ ] **Webhook auto-registration** - Automatically register trigger configs with the Connector Namespace on function deployment
- [ ] **Batch dispatch** - Support array parameter binding for per-item processing
- [ ] **Distributed tracing** - Add DiagnosticScope for Application Insights integration

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
