---
name: poll-trigger-registration
description: 'Register Connector Namespace trigger configs for Azure Functions host-pull Poll delivery. USE WHEN: creating deliveryMode Poll trigger configs, obtaining pollingEndpoints, deriving <Connection>__pollingEndpoint and TriggerConfigName settings, configuring Poll identity access, enabling Poll triggers, or troubleshooting Poll registration. NOT FOR: callback/Webhook notificationDetails (use webhook-trigger-registration), connection creation or OAuth consent (use connection-setup), or extension internals.'
---

# Connector Poll Trigger Registration for Azure Functions

Registers a Connector Namespace Trigger Config with `deliveryMode: Poll`,
returns the service-generated `pollingEndpoints`, and configures the values
required by the Azure Functions Connector extension.

## When to Use

- The Function binding uses `DeliveryMode = Poll`.
- The Function host must receive and acknowledge leased Connector events.
- A Poll Trigger Config must be created through ARM because the current portal
  and `az connector-namespace trigger create` command do not expose the Poll
  delivery contract.
- The user needs the generated Poll endpoints and ready-to-copy Function app
  settings.

For Connector Namespace callback delivery, use the
[`webhook-trigger-registration` skill](../webhook-trigger-registration/SKILL.md).

## Prerequisites

- Azure CLI authenticated with permission to manage the Connector Namespace.
- An existing connected Connector Namespace connection. Use
  [`connection-setup`](../connection-setup/SKILL.md) first if needed.
- A Function App identity for Azure deployments, or a signed-in developer
  identity for local development.
- The identity must have a connection-level access policy on the connection
  referenced by the Trigger Config.

## Required Inputs

Collect:

```powershell
$subscriptionId = "<subscription-id>"
$resourceGroup = "<resource-group>"
$namespaceName = "<connector-namespace-name>"
$triggerConfigName = "<trigger-config-name>"
$connectionName = "<connection-name>"
$connectorName = "<connector-name>"       # e.g. office365
$operationName = "<operation-name>"       # e.g. OnNewEmailV3
$connectionPrefix = "ConnectorNamespace"  # ConnectorTrigger.Connection
$triggerConfigSetting = "OnNewEmailTriggerConfigName"
```

Connector-specific operation parameters are also required. For example:

```powershell
$parameters = @(
    @{
        name = "folderPath"
        value = "Inbox"
    }
)
```

## Step 1: Create the Poll Trigger Config

Ask whether the Function App, Poll binding, and connection access policy are
ready. Use `Enabled` by default. If the Function is not ready, ask the user
whether to create the Trigger Config as `Disabled`; do not choose the disabled
state without confirmation.

Set the confirmed initial state, then build the ARM resource ID and request
body:

```powershell
$initialState = "Enabled" # Use "Disabled" only when the user confirms.
$apiVersion = "2026-05-01-preview"
$namespaceResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$namespaceName"
$triggerConfigResourceId = "$namespaceResourceId/triggerConfigs/$triggerConfigName"

function Set-PollTriggerConfig {
    param(
        [Parameter(Mandatory)]
        [string] $ResourceId,

        [Parameter(Mandatory)]
        [string] $ApiVersion,

        [Parameter(Mandatory)]
        [string] $Body
    )

    $bodyFile = Join-Path $env:TEMP "$([IO.Path]::GetRandomFileName()).json"
    try {
        [IO.File]::WriteAllText(
            $bodyFile,
            $Body,
            [Text.UTF8Encoding]::new($false))

        $response = az rest `
            --method put `
            --uri "$ResourceId?api-version=$ApiVersion" `
            --body "@$bodyFile" `
            -o json
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create or update the Connector Namespace Poll Trigger Config."
        }

        return $response | ConvertFrom-Json
    }
    finally {
        Remove-Item $bodyFile -ErrorAction SilentlyContinue
    }
}

$triggerProperties = @{
    connectionDetails = @{
        connectionName = $connectionName
        connectorName = $connectorName
    }
    deliveryMode = "Poll"
    description = "Poll delivery - $connectorName $operationName"
    operationName = $operationName
    parameters = $parameters
    state = $initialState
    type = "NotSpecified"
}

$requestBody = @{
    properties = $triggerProperties
} | ConvertTo-Json -Depth 8 -Compress

$triggerConfig = Set-PollTriggerConfig `
    -ResourceId $triggerConfigResourceId `
    -ApiVersion $apiVersion `
    -Body $requestBody

if ($triggerConfig.properties.state -ne $initialState) {
    throw "Connector Namespace Poll Trigger Config was not created in the requested state."
}
```

Do not add `notificationDetails`, a callback URL, or callback authentication.
Those properties belong to Webhook delivery.

Create the Trigger Config enabled unless the user explicitly chooses disabled
because the Function is not ready. Configure the Function settings immediately
after retrieving its service-generated endpoints.

## Step 2: Retrieve and Share `pollingEndpoints`

Use the PUT response when it contains `properties.pollingEndpoints`; otherwise
retrieve the resource:

```powershell
if ($null -eq $triggerConfig.properties.pollingEndpoints) {
    $triggerConfig = az rest `
        --method get `
        --uri "$triggerConfigResourceId?api-version=$apiVersion" `
        -o json | ConvertFrom-Json
}

$pollingEndpoints = $triggerConfig.properties.pollingEndpoints
if ($null -eq $pollingEndpoints -or
    [string]::IsNullOrWhiteSpace($pollingEndpoints.receiveUri)) {
    throw "Connector Namespace did not return properties.pollingEndpoints.receiveUri."
}

$pollingEndpoints | ConvertTo-Json -Depth 4
```

Share the complete service-generated object with the requesting user:

- `receiveUri`
- `acknowledgeUri`
- `hasMessagesUri`
- `approximateQueueDepthUri`

These values are application configuration. Show them directly to the user,
but do not commit them, copy them into issue reports, or include them in
general logs or summaries.

## Step 3: Derive the Function Settings

Validate the Receive URI and remove only the final
`/triggerConfigs/<name>/receive` path:

```powershell
$receiveUri = [Uri]$pollingEndpoints.receiveUri
$pathMatch = [regex]::Match(
    $receiveUri.AbsolutePath,
    "^(?<base>/api/connectorGateways/[^/]+)/triggerConfigs/[^/]+/receive$",
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

if (-not $pathMatch.Success) {
    throw "The service-generated receiveUri does not match the supported Connector Namespace Poll path."
}

$endpointBuilder = [UriBuilder]$receiveUri
$endpointBuilder.Path = $pathMatch.Groups["base"].Value
$pollingEndpoint = $endpointBuilder.Uri.AbsoluteUri

Write-Output ""
Write-Output "Function settings:"
Write-Output "${connectionPrefix}__pollingEndpoint=$pollingEndpoint"
Write-Output "$triggerConfigSetting=$triggerConfigName"
```

Preserve the service-generated authority, gateway identifier, and query
exactly. Do not derive them from the Connector Namespace resource ID.

The resulting binding is:

```csharp
[ConnectorTrigger(
    DeliveryMode = ConnectorTriggerDeliveryMode.Poll,
    Connection = "ConnectorNamespace",
    TriggerConfigName = "%OnNewEmailTriggerConfigName%")]
```

## Step 4: Configure Identity and Access

### Connector Namespace identity

The Connector Namespace managed identity polls the connector operation. Grant
that identity its own connection-level access policy:

```powershell
$namespacePrincipalId = az resource show `
    --ids $namespaceResourceId `
    --query "identity.principalId" `
    -o tsv
$tenantId = az account show --query "tenantId" -o tsv

if ([string]::IsNullOrWhiteSpace($namespacePrincipalId)) {
    throw "The Connector Namespace does not have a system-assigned managed identity."
}

az connector-namespace connection access-policy create `
    -g $resourceGroup `
    --namespace $namespaceName `
    --connection-name $connectionName `
    -n namespace-trigger-runtime `
    --principal "identity.object-id=$namespacePrincipalId identity.tenant-id=$tenantId type=ActiveDirectory"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to grant the Connector Namespace identity access to the connection."
}
```

This policy authorizes Connector Namespace to run the connector trigger. It is
separate from the policy for the identity that calls the Poll runtime
endpoints.

### Function or developer identity

For Azure, configure managed identity:

```powershell
az functionapp config appsettings set `
    -g $resourceGroup `
    -n "<function-app-name>" `
    --settings `
        "${connectionPrefix}__pollingEndpoint=$pollingEndpoint" `
        "${connectionPrefix}__credential=managedidentity" `
        "$triggerConfigSetting=$triggerConfigName"
```

For local development, omit `${connectionPrefix}__credential`; the extension uses the standard Functions developer credential behavior.

Grant the selected identity an access policy on the connection referenced by
the Trigger Config. There is no namespace-level access policy.

## Step 5: Enable a Trigger Config Created Disabled

Skip this step when the Trigger Config was created enabled. If the user chose
`Disabled` because the Function was not ready, confirm that the Function
settings and both connection access policies are now complete, then enable it:

```powershell
if ($triggerConfig.properties.state -eq "Disabled") {
    $triggerProperties.state = "Enabled"
    $requestBody = @{
        properties = $triggerProperties
    } | ConvertTo-Json -Depth 8 -Compress

    $triggerConfig = Set-PollTriggerConfig `
        -ResourceId $triggerConfigResourceId `
        -ApiVersion $apiVersion `
        -Body $requestBody

    if ($triggerConfig.properties.state -ne "Enabled") {
        throw "Connector Namespace Poll Trigger Config was not enabled."
    }
}
```

## Required Final Output

Always return:

1. The Trigger Config resource ID and current state. If it remains disabled,
   explain that the Function will not receive events until it is enabled.
2. The complete `properties.pollingEndpoints` object.
3. The derived gateway-level setting:

   ```text
   <Connection>__pollingEndpoint=<gateway-level-base>
   ```

4. The binding-level setting:

   ```text
   <TriggerConfigSetting>=<trigger-config-name>
   ```

5. Whether the Connector Namespace and Function/developer connection-level
   access policies and Function app settings were configured.

Never return credentials, bearer tokens, lock tokens, connector payloads, or
signed linked-output URLs.

## Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| Poll binding reports missing `pollingEndpoint` | The named connection section is incomplete | Configure `<Connection>__pollingEndpoint` with the derived gateway-level base |
| Poll binding reports missing `TriggerConfigName` | Binding metadata or its app-setting expression is missing | Set `TriggerConfigName` and ensure the referenced app setting resolves |
| Runtime returns `403 Forbidden` | The caller is authenticated but lacks connection authorization | Add a connection-level access policy for the selected identity |
| Runtime route returns `404` or `410` | Stale endpoint or Trigger Config name | Retrieve the current `pollingEndpoints` and verify the derived settings |
| No events arrive | Trigger Config is disabled or the connector operation parameters are wrong | Verify `properties.state`, operation name, and connector-specific parameters |
