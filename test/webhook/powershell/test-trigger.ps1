$ErrorActionPreference = 'Stop'
$functionPath = Join-Path $PSScriptRoot 'OnGenericWebhook'
$binding = (Get-Content (Join-Path $functionPath 'function.json') -Raw | ConvertFrom-Json).bindings[0]
if ($binding.type -ne 'connectorTrigger' -or $binding.name -ne 'Payload' -or
    $binding.direction -ne 'in' -or $binding.dataType -ne 'string' -or
    $binding.PSObject.Properties.Name -contains 'deliveryMode' -or
    $binding.PSObject.Properties.Name -contains 'connection' -or
    $binding.PSObject.Properties.Name -contains 'triggerConfigName') {
    throw 'Incorrect generic Webhook binding.'
}
foreach ($payload in @('{"body":{"arbitrary":true}}', '[{"arbitrary":true}]', '{}', '[]', @{ body = @{ arbitrary = $true } }, ,@(@{ arbitrary = $true }), ,@())) {
    $messages = @(& (Join-Path $functionPath 'run.ps1') -Payload $payload 6>&1)
    if ($messages.Count -ne 1 -or $messages[0].ToString() -ne 'Received generic Webhook connector payload.') {
        throw 'Expected one success log for a valid payload.'
    }
}
foreach ($payload in @('not JSON', 'null', '42', '""', '')) {
    $failed = $false
    try {
        & (Join-Path $functionPath 'run.ps1') -Payload $payload
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw "Invalid payload succeeded: $payload"
    }
}
Write-Output 'Generic Webhook PowerShell tests passed.'
