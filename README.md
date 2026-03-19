# Azure Functions Connector Extension

A webhook-style Azure Functions extension that exposes HTTP endpoints for receiving JSON/string payloads from external services like Microsoft Graph, SharePoint, Teams, GitHub, and other webhook providers.

## Overview

The Connector Extension provides a simple way to create Azure Functions that respond to webhook calls from external systems. Similar to the EventGrid extension pattern, it exposes a single HTTP endpoint that routes requests to the appropriate function based on a `functionName` query parameter.

**Endpoint Pattern:**

```text
POST /runtime/webhooks/connector?functionName={FunctionName}
```

## Features

- **Single endpoint** for all connector-triggered functions
- **Full request context** - access body, headers, query params, HTTP method
- **JSON/string payloads** - works with any content type
- **Isolated worker support** - .NET 8 isolated worker model
- **Python support** - works with generic trigger binding
- **Microsoft Graph integration** - ideal for O365, SharePoint, Teams webhooks

## Installation

### .NET Isolated

Add the worker extension NuGet package:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector
```

## ConnectorContext Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Body` | string | Raw request body |
| `Headers` | Dictionary<string, string> | HTTP headers |
| `Query` | Dictionary<string, string> | Query string parameters |
| `Method` | string | HTTP method (GET, POST, etc.) |
| `Url` | string | Full request URL |
| `ContentType` | string | Content-Type header value |
| `Timestamp` | DateTimeOffset | When the request was received |

## Samples

See the [samples](./samples) directory for complete working examples:

- **[.NET Isolated](./samples/dotnet-isolated)** - .NET 8 isolated worker examples including O365, Teams, SharePoint, and GitHub webhook handlers
- **[Python](./samples/python)** - Python v2 programming model examples
- **[Test Requests](./samples/test-requests.http)** - HTTP file with sample requests for testing

## Project Structure

```text
azure-functions-connector-extension/
├── src/
│   ├── Microsoft.Azure.Functions.Extensions.Connector/         # WebJobs host extension
│   └── Microsoft.Azure.Functions.Worker.Extensions.Connector/  # Worker extension
├── samples/
│   ├── dotnet-isolated/    # .NET 8 isolated examples
│   ├── python/             # Python v2 examples
│   └── test-requests.http  # HTTP test file
├── test/
│   └── Extensions.Connector.Tests/  # Unit tests
└── README.md
```

## Building from Source

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

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
