# Azure Functions Connector Extension

An Azure Functions trigger extension for receiving webhook callbacks from AI Gateway managed connectors (Office 365, Teams, SharePoint, etc.).

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
- **Python support** - generic trigger binding for Python functions

## Installation

### .NET Isolated Worker

Add the worker extension NuGet package or project reference:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector
```

### Connector SDK (for typed payloads)

For strongly-typed connector payloads, add the Connector SDK:

```xml
<!-- Option 1: NuGet package (when available) -->
<PackageReference Include="Microsoft.Azure.Workflows.Connectors.Sdk" Version="1.0.0" />

<!-- Option 2: Local project reference (for development) -->
<ProjectReference Include="path/to/azure-logicapps-connector-sdk/src/Microsoft.Azure.Workflows.Connectors.Sdk/Microsoft.Azure.Workflows.Connectors.Sdk.csproj" />
```

## Usage

### .NET Isolated Worker

```csharp
using Microsoft.Azure.Connectors.DirectClient.Office365;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;

public class ConnectorFunctions
{
    [Function("OnNewEmail")]
    [BlobOutput("emails/{rand-guid}.json", Connection = "BlobStoreConnection")]
    public string OnNewEmail(
        [ConnectorTrigger(Connector = "office365", Operation = "OnNewEmailV3", Connection = "Office365Connection")] 
        Office365OnNewEmailV3TriggerPayload payload)
    {
        var emails = payload.Body?.Value ?? [];
        // Process emails...
        return JsonSerializer.Serialize(payload);
    }
}
```

### ConnectorTrigger Attribute

| Property | Description |
|----------|-------------|
| `Connector` | Connector name (e.g., `"office365"`, `"sharepointonline"`, `"teams"`) |
| `Operation` | Trigger operation ID (e.g., `"OnNewEmailV3"`, `"OnNewChannelMessage"`) |
| `Connection` | Connection name from app settings |

### Supported Binding Types

- `string` - raw JSON body
- `JObject` / `JArray` - parsed JSON
- POCO types - SDK types like `Office365OnNewEmailV3TriggerPayload`

### Python

```python
import azure.functions as func
import json

app = func.FunctionApp()

@app.function_name(name="OnNewEmail")
@app.generic_trigger(
    arg_name="payload",
    type="connectorTrigger",
    connector="office365",
    operation="OnNewEmailV3",
    connection="Office365Connection")
@app.blob_output(arg_name="output", path="emails/{rand-guid}.json", connection="BlobStoreConnection")
def on_new_email(payload: str, output: func.Out[str]) -> None:
    data = json.loads(payload)
    output.set(payload)
```

## Samples

- **[.NET Isolated](./samples/dotnet-isolated)** - .NET 8 isolated worker with POCO binding
- **[Python](./samples/python)** - Python v2 with blob output

## Project Structure

```text
azure-functions-connector-extension/
├── src/
│   ├── Microsoft.Azure.Functions.Extensions.Connector/         # WebJobs host extension
│   │   ├── ConnectorExtensionConfigProvider.cs                 # Extension config, HTTP routing
│   │   ├── ConnectorHttpRequestProcessor.cs                    # Request parsing, callback
│   │   ├── ConnectorTriggerBinding.cs                          # Trigger binding
│   │   └── ConnectorListener.cs                                # Function registration
│   └── Microsoft.Azure.Functions.Worker.Extensions.Connector/  # Worker extension (.NET isolated)
├── samples/
│   ├── dotnet-isolated/    # .NET 8 sample with POCO binding
│   └── python/             # Python sample with blob output
└── test/
    └── Microsoft.Azure.Functions.Extensions.Connector.Tests/   # Unit tests
```

## Building

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
