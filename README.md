# Azure Functions Connector Extension (Preview)

> **⚠️ Preview Extension Notice**
>
> This extension is in preview and may contain breaking changes without prior notice. It includes features that are still under development and not yet ready for production use. Users should be aware that:
>
> - Trigger behavior and binding contracts may change between versions
> - Performance characteristics and stability may vary across releases
> - Security updates may require version upgrades
>
> Testing in non-production environments is strongly recommended before GA.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://dev.azure.com/azfunc/public/_apis/build/status/1710?branchName=main)](https://dev.azure.com/azfunc/public/_build?definitionId=1710&branchName=main)

An Azure Functions trigger extension for receiving webhook callbacks from AI Gateway managed connectors (Office 365, Teams, SharePoint, etc.).

> **Preview:** This extension is currently in preview. See the [Roadmap](#roadmap) for planned work before GA.

## NuGet Packages

The following NuGet packages are available as part of this project.

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Azure.Functions.Extensions.Connector.svg?label=microsoft.azure.functions.worker.extensions.connector)](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Connector)

## Extension Bundle

For non-.NET languages (Node.js, Python, etc.), use the **experimental extension bundle** version 4.6.0 or greater. Add the following to your `host.json`:

```json
{
    "version": "2.0",
    "extensionBundle": {
        "id": "Microsoft.Azure.Functions.ExtensionBundle.Experimental",
        "version": "[4.6.0, 5.0.0)"
    }
}
```

## Overview

This extension enables Azure Functions to receive trigger callbacks from AI Gateway managed connectors. When a connector event occurs (e.g., new email arrives), AI Gateway sends a webhook callback to your function.

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

The full callback URL for AI Gateway:

```text
https://<function-app>.azurewebsites.net/runtime/webhooks/connector?functionName=OnNewEmail&code=<connector_extension_key>
```

## Features

- **Connector trigger binding** - receive callbacks from AI Gateway managed connectors
- **POCO binding** - bind directly to SDK types like `Office365OnNewEmailV3TriggerPayload`
- **String/JSON binding** - bind to raw string, `JObject`, or `JArray`
- **.NET 8 isolated worker** - modern .NET isolated worker model
- **Node.js support** - generic trigger binding for Node.js functions
- **Python support** - generic trigger binding for Python functions

## Installation

### .NET Isolated Worker

Add the worker extension NuGet package or project reference:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector
```

### Connector SDK (for typed payloads)

For strongly-typed connector payloads, add the [connectors-net-sdk](https://github.com/Azure/connectors-net-sdk)K:

```xml
<!-- Option 1: NuGet package (when available) -->
<PackageReference Include="Microsoft.Azure.Workflows.Connectors.Sdk" Version="1.0.0" />

<!-- Option 2: Local project reference (for development) -->
<ProjectReference Include="path/to/azure-logicapps-connector-sdk/src/Microsoft.Azure.Workflows.Connectors.Sdk/Microsoft.Azure.Workflows.Connectors.Sdk.csproj" />
```

## Usage

### Supported Binding Types

- `string` - raw JSON body
- `JObject` / `JArray` - parsed JSON
- POCO types - SDK types like `Office365OnNewEmailV3TriggerPayload`

## Samples

- **[.NET Isolated](./test/SampleApp)** - .NET isolated worker sample
- **[Node.js](./samples/nodejs)** - Node.js v4 with blob output
- **[Python](./samples/python)** - Python v2 with blob output

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
├── samples/
│   ├── nodejs/                                                  # Node.js sample with blob output
│   └── python/                                                  # Python sample with blob output
├── test/
│   ├── SampleApp/                                               # .NET isolated worker sample app
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

- [ ] **Header validation** - Validate AI Gateway-specific headers (e.g., `x-ms-connector-name`, `x-ms-operation-id`) once spec is finalized
- [ ] **Connection handshake** - Support webhook connection validation, including:
  - Trigger validation (verify connector/operation matches registered function)
  - Connection validation (verify connection name is authorized)
- [ ] **Batch dispatch** - Support array parameter binding for batch processing
- [ ] **Distributed tracing** - Add DiagnosticScope for Application Insights integration
- [ ] **Binding expressions** - Consider adding `BindingDataContract` for output binding expressions (e.g., `{body.id}`). Currently skipped because AI Gateway's nested array structure (`body.value[]`) doesn't map cleanly to binding expressions.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com.).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
