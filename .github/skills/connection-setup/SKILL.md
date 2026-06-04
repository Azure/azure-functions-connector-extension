---
name: connection-setup
description: 'Create and configure Connector Namespace connections for the Azure Functions Connector Extension. USE WHEN: setting up a new connector connection, creating a Connector Namespace, authorizing OAuth consent, adding access policies, or configuring deployed app settings. Covers Office365, SharePoint, Teams, and any Microsoft.Web/connections connector. NOT FOR: trigger registration (use trigger-registration skill), extension development, or code generation.'
---

# Connector Namespace Connection Setup

Automates the end-to-end connection lifecycle for connector-triggered Azure Functions.

## When to Use

- Developer needs a new connector connection for local dev or a deployed Function App
- Developer needs to authorize (OAuth consent) a connection
- Developer needs to wire connection URLs into deployed app settings
- Developer needs to grant access policies (CLI identity for local, managed identity for deployed)

## Prerequisites

- Azure CLI ≥ 2.75.0 installed and authenticated (`az login`)
- `connector-namespace` CLI extension installed (see below)
- Target subscription and resource group known
- For deployed scenarios: Function App with managed identity enabled
- **Supported regions** for Connector Namespace: `westcentralus`. Only the Connector Namespace `location` must be in a supported region; the resource group and Function App can be in any region.

### Install the connector-namespace CLI extension

```bash
# Bash
curl -fsSL https://aka.ms/connector-namespace-cli-install | sh
```

```powershell
# PowerShell
irm https://aka.ms/connector-namespace-cli-install-ps | iex
```

```bash
# Verify
az extension show --name connector-namespace --query "{name:name, version:version}" -o table
```

## Procedure

### Step 1: Create or Select Connector Namespace

Check for an existing Connector Namespace in the resource group:

```powershell
$resourceGroup = "<resource-group>"

az connector-namespace list -g $resourceGroup -o table
```

If none exists, create one:

```powershell
$namespaceName = "<namespace-name>"
$location = "westcentralus"  # Supported region

az connector-namespace create -g $resourceGroup -n $namespaceName --location $location
```

Enable a system-assigned managed identity (required for trigger callback authentication):

```powershell
az connector-namespace identity assign -g $resourceGroup --namespace $namespaceName --system-assigned
```

### Step 2: Create Connection

```powershell
$connectorName = "<connector-name>"      # e.g., "office365", "sharepointonline", "teams"
$connectionName = "<connection-name>"    # e.g., "office365-conn"

az connector-namespace connection create `
    -g $resourceGroup --namespace $namespaceName `
    -n $connectionName --connector-name $connectorName
```

The connection starts in **Error** state (unauthenticated). Proceed to Step 3.

### Step 3: OAuth Consent (In-Browser)

Retrieve the consent link and open it in the default browser:

```powershell
$result = az connector-namespace connection list-consent-links `
    -g $resourceGroup --namespace $namespaceName `
    --connection-name $connectionName `
    --parameters '[{"parameterName":"token","redirectUrl":"https://portal.azure.com"}]' `
    -o json | ConvertFrom-Json

$link = $result.value[0].link
Start-Process $link
```

After the browser consent completes, verify the connection status:

```powershell
az connector-namespace connection show `
    -g $resourceGroup --namespace $namespaceName `
    -n $connectionName `
    --query "properties.statuses[0].status" -o tsv
```

Expected: `Connected`.

If status remains `Error` after consent, re-run the consent link command and try again. If repeated failures occur, delete and recreate the connection.

### Step 4: Get Connection Runtime URL

```powershell
$runtimeUrl = az connector-namespace connection show `
    -g $resourceGroup --namespace $namespaceName `
    -n $connectionName `
    --query "properties.connectionRuntimeUrl" -o tsv
Write-Output "Runtime URL: $runtimeUrl"
```

### Step 5: Add Access Policies

> **Note:** Access policies are only needed when your function calls connector **actions** at runtime. For **trigger-only** scenarios (function only receives callbacks), skip this step.

#### For local development (Azure CLI identity)

```powershell
$userObjectId = az ad signed-in-user show --query "id" -o tsv
$tenantId = az account show --query "tenantId" -o tsv

az connector-namespace connection access-policy create `
    -g $resourceGroup --namespace $namespaceName `
    --connection-name $connectionName -n local-dev `
    --principal "identity.object-id=$userObjectId identity.tenant-id=$tenantId type=ActiveDirectory"
```

#### For deployed Function App (system-assigned managed identity)

```powershell
$functionAppName = "<function-app-name>"
$msiObjectId = az functionapp identity show -g $resourceGroup -n $functionAppName --query "principalId" -o tsv
$tenantId = az account show --query "tenantId" -o tsv

az connector-namespace connection access-policy create `
    -g $resourceGroup --namespace $namespaceName `
    --connection-name $connectionName -n functionapp-msi `
    --principal "identity.object-id=$msiObjectId identity.tenant-id=$tenantId type=ActiveDirectory"
```

> ACL propagation takes 1-5 minutes. If you get 403 errors immediately after adding, wait and retry.

## Cleanup / Teardown

If setup partially fails or resources are no longer needed, delete the connection first, then delete the namespace:

```powershell
az connector-namespace connection delete `
    -g $resourceGroup --namespace $namespaceName `
    -n $connectionName

az connector-namespace delete -g $resourceGroup -n $namespaceName
```

## Supported Connectors

`arm`, `azureblob`, `azureeventgrid`, `azuremonitorlogs`, `office365`, `office365users`, `onedriveforbusiness`, `sharepointonline`, `teams`, `kusto`, `smtp`, `keyvault`, `planner`, `todo`, and any `Microsoft.Web/connections` connector name.

## Next Steps

- **Triggers:** To register polling triggers (e.g., OnNewEmail, OnNewFile), use the [trigger-registration skill](../trigger-registration/SKILL.md).