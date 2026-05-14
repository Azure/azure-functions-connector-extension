---
name: trigger-registration
description: 'Register Connector Namespace trigger configs for Azure Functions with the ConnectorTrigger extension. USE WHEN: setting up polling triggers (e.g., OnNewEmail, OnNewFile) that call back to an Azure Function, scaffolding a new Function App project with ConnectorTrigger, wiring callback URLs, or troubleshooting trigger configs. NOT FOR: connection setup (use connection-setup skill), extension internals development.'
---

# Connector Trigger Registration for Azure Functions

Registers polling trigger configs on a Connector Namespace so that connector events (new email, new file, etc.) call back to your Azure Function via the ConnectorTrigger extension.

## When to Use

- Developer needs a connector trigger (e.g., "when a new email arrives in Office365")
- Developer has an existing Connector Namespace connection (use the `connection-setup` skill first if not)
- Developer needs to scaffold a new Function App project with `[ConnectorTrigger]`
- Developer needs to wire the callback URL from a deployed or local Function App

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Connector Namespace with a connected connector (see `connection-setup` skill)
- The Connector Namespace must have a **system-assigned managed identity** enabled
- **Supported regions** for Connector Namespace: `brazilsouth`, `centraluseuap`, `eastus2euap`, `centralusstage`, `eastusstage`

## Key Concepts

### Extension Webhook Endpoint

The `Microsoft.Azure.Functions.Extensions.Connector` extension registers a webhook route on the Function App:

```text
POST /runtime/webhooks/connector?functionName={FunctionName}&code={connector_extension_key}
```

- `functionName` must exactly match the `[Function("...")]` attribute name
- `connector_extension` is a system key auto-generated when the extension loads
- Locally (`func start`), the system key is not enforced

### Trigger Config vs Connection

```text
Connector Namespace
├── connections/
│   └── office365-conn         ← auth + runtime URL (connection-setup skill)
└── triggerConfigs/
    └── onnewemail-trigger     ← poll + callback config (THIS skill)
```

## Scaffolding a New Function App Project

### 1. Initialize with azd

#### .NET

```shell
azd init -t functions-quickstart-dotnet-azd
```

#### Python

```shell
azd init -t functions-quickstart-python-azd
```

#### TypeScript

```shell
azd init -t functions-quickstart-typescript-azd
```

#### JavaScript

```shell
azd init -t functions-quickstart-javascript-azd
```

> **Note:** The `azd init` templates create a `host.json` file. For non-.NET languages (Node.js, Python, etc.), update `host.json` to use the **experimental extension bundle** version 4.6.0 or greater:
> ```json
> {
>     "version": "2.0",
>     "extensionBundle": {
>         "id": "Microsoft.Azure.Functions.ExtensionBundle.Experimental",
>         "version": "[4.6.0, 5.0.0)"
>     }
> }
> ```

### 2. Replace the HTTP trigger with a ConnectorTrigger function

Delete any sample HTTP functions and add the connector extension and SDK packages:

#### .NET

Install the latest pre-release NuGet packages:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Connector --prerelease
dotnet add package Azure.Connectors.Sdk --prerelease
```

#### Python

Add to `requirements.txt`:

```text
azure-functions>=2.2.0b3
azurefunctions-extensions-connectors
```

#### TypeScript / JavaScript

```bash
npm install @azure/functions@^4.0.0
npm install @azure/connectors
```

### 3. Replace the HTTP trigger with a ConnectorTrigger function

Delete any sample HTTP trigger functions and replace with:

#### .NET

```csharp
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Office365.Models;

[Function("OnNewEmail")]
public void OnNewEmail(
    [ConnectorTrigger]
    Office365OnNewEmailTriggerPayload payload)
{
    _logger.LogInformation("From: {From}, Subject: {Subject}",
        payload.From, payload.Subject);
}
```

#### Python

```python
import azure.functions as func
import azurefunctions.extensions.connectors.office365 as office365
import logging
from typing import List

app = func.FunctionApp()

@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="emails")
def on_new_email(emails: List[office365.ClientReceiveMessage]) -> None:
    logging.info("OnNewEmail trigger received")

    for email in emails:
        logging.info(f"Subject: {email.subject}")
        logging.info(f"From: {email.from_}")
```

#### TypeScript

```typescript
import { app, InvocationContext } from "@azure/functions";

app.generic("OnNewEmail", {
  trigger: { type: "connectorTrigger", name: "payload" },
  handler: async (payload: unknown, context: InvocationContext) => {
    const data = typeof payload === "string" ? JSON.parse(payload) : payload;
    for (const email of data?.body?.value ?? []) {
      context.log(`From: ${email.from}, Subject: ${email.subject}`);
    }
  },
});
```

### 4. Run locally

```shell
func start
```

The extension logs the webhook endpoint at startup:
```
Connector endpoint: http://localhost:7071/runtime/webhooks/connector
```

## Local Development with Port Forwarding

To test triggers locally, you need to expose your local Function App so the Connector Namespace can reach it.

### Step 1: Start the Function App locally

```bash
func start
```

Confirm the app is running on `http://localhost:7071`.

> **🔔 Confirm:** Is the Function App running? (Yes / No)

### Step 2: Create a dev tunnel

> ⚠️ **Security Warning:** The following steps expose your local Function App to the public internet. Only proceed if you understand the implications.

> 💡 Prefer the CLI over VS Code UI? Use the `devtunnel` CLI instead — see [Dev Tunnels CLI quickstart](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started).
> ```bash
> devtunnel user login  # choose the option that matches your org policy
> devtunnel create <your-tunnel-id> -a  # custom tunnel id for a persistent, reusable URL
> devtunnel port create <your-tunnel-id> -p 7071
> devtunnel host <your-tunnel-id> --allow-anonymous
> ```

#### VS Code port forwarding

1. Navigate to the **Ports** view in the Panel region (`Ports: Focus on Ports View`) and select **Forward a Port**
2. If you haven't logged in with GitHub before, you'll be prompted to sign in
3. Enter port `7071` — port forwarding starts and the Ports view updates to show the forwarded port and its **Forwarded Address** (e.g., `https://<id>-7071.uks1.devtunnels.ms`)
4. Change the visibility by right-clicking on the port and selecting **Port Visibility** → **Public**. Public ports don't require sign in

> **🔔 Confirm:** Is the port forwarded with Public visibility and do you see the tunnel URL in the Ports panel? (Yes / No)

### Step 4: Clean up after testing

When done testing, **always** revoke public access:

1. In the **Ports** panel, right-click the forwarded port
2. Select **Stop Forwarding Port** (or set visibility back to **Private**)

> ⚠️ Do not leave your local Function App publicly exposed longer than necessary.

## Registering a Trigger Config

### Step 1: Get the Callback URL

#### Deployed Function App

```powershell
$resourceGroup = "<resource-group>"
$functionAppName = "<function-app-name>"
$functionName = "<function-name>"  # must match [Function("...")] attribute

$connectorExtensionKey = az functionapp keys list -g $resourceGroup -n $functionAppName --query "systemKeys.connector_extension" -o tsv
$callbackUrl = "https://$functionAppName.azurewebsites.net/runtime/webhooks/connector?functionName=$functionName&code=$connectorExtensionKey"
```

#### Local development (with dev tunnel)

Use the tunnel URL from the **Local Development with Port Forwarding** section above:

```powershell
$tunnelUrl = "<your-tunnel-url>"  # from VS Code Ports panel, e.g., https://<id>-7071.uks1.devtunnels.ms
$functionName = "<function-name>"
$callbackUrl = "$tunnelUrl/runtime/webhooks/connector?functionName=$functionName"
```

> **Note:** For local testing, the tunnel must have **Public** visibility (anonymous access). The Connector Namespace cannot authenticate to private tunnels. Functions CLI does not enforce system key authentication locally.

### Step 2: Create Trigger Config

```powershell
$subscriptionId = "<subscription-id>"
$resourceGroup = "<resource-group>"
$namespaceName = "<namespace-name>"
$nsId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$namespaceName"

$triggerName = "<trigger-config-name>"   # e.g., "onnewemail-trigger"
$connectionName = "<connection-name>"    # e.g., "office365-conn"
$connectorName = "<connector-name>"      # e.g., "office365"
$operationName = "<operation-name>"      # e.g., "OnNewEmail"

$token = az account get-access-token `
    --resource "https://management.core.windows.net/" `
    --query "accessToken" -o tsv

$body = @{
    properties = @{
        operationName = $operationName
        connectionDetails = @{
            connectorName = $connectorName
            connectionName = $connectionName
        }
        notificationDetails = @{
            callbackUrl = $callbackUrl
            httpMethod = "Post"
        }
        parameters = @(
            # Add connector-specific parameters here, e.g.:
            # @{ name = "folderId"; value = "Inbox" }
        )
    }
} | ConvertTo-Json -Depth 4

$uri = "https://management.azure.com${nsId}/triggerConfigs/${triggerName}?api-version=2026-05-01-preview"
try {
    $response = Invoke-WebRequest -Uri $uri -Method PUT -Body $body `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" }
    Write-Output "Status: $($response.StatusCode)"
} catch {
    Write-Output "Error: $($_.Exception.Response.StatusCode)"
    $_.ErrorDetails.Message
}
```

### Step 3: Verify Trigger Config

```powershell
az rest --method GET `
    --uri "https://management.azure.com${nsId}/triggerConfigs/${triggerName}?api-version=2026-05-01-preview" `
    --query "properties.{operation:operationName, state:state, callback:notificationDetails.callbackUrl}" `
    -o table
```

Expected: `state = Enabled`.

### Step 4: Update Callback URL

To point an existing trigger config to a different callback (e.g., after redeploying or switching tunnels):

```powershell
# Re-run the PUT from Step 2 with the updated callbackUrl
```

### Step 5: List All Trigger Configs

```powershell
az rest --method GET `
    --uri "https://management.azure.com${nsId}/triggerConfigs?api-version=2026-05-01-preview" `
    --query "value[].{name:name, operation:properties.operationName, state:properties.state}" `
    -o table
```

## Troubleshooting

### Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `Could not find member 'connectionName'` | Used `connectionName` at top level | Wrap in `connectionDetails` object |
| `Could not find member 'callbackUrl'` | Put `callbackUrl` at properties level | Wrap in `notificationDetails` object |
| `Could not find member 'parameterName'` | Used `parameterName` in params array | Use `name` field instead |
| Trigger provisions but never fires | Missing `notificationDetails` or empty `callbackUrl` | Ensure `notificationDetails.callbackUrl` is set |
| `az rest` PUT returns no output | `az rest` swallows non-2xx responses | Use `Invoke-WebRequest` for PUT operations |

### Polling Interval

The Connector Namespace polls the connector every 1-5 minutes. After polling detects new content, it POSTs the payload to your callback URL.

