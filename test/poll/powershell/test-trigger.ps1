$ErrorActionPreference = 'Stop'
$functionPath = Join-Path $PSScriptRoot 'OnGenericPoll'
$binding = (Get-Content (Join-Path $functionPath 'function.json') -Raw | ConvertFrom-Json).bindings[0]
if ($binding.type -ne 'connectorTrigger' -or $binding.name -ne 'Payload' -or
    $binding.direction -ne 'in' -or $binding.dataType -ne 'string' -or
    $binding.deliveryMode -ne 'Poll' -or $binding.connection -ne 'ConnectorNamespace' -or
    $binding.triggerConfigName -ne '%GenericTriggerConfigName%') {
    throw 'Incorrect generic Poll binding.'
}
foreach ($payload in @('{"body":{"arbitrary":true}}', '[{"arbitrary":true}]', '{}', '[]', @{ body = @{ arbitrary = $true } }, ,@(@{ arbitrary = $true }), ,@())) {
    $messages = @(& (Join-Path $functionPath 'run.ps1') -Payload $payload 6>&1)
    if ($messages.Count -ne 1 -or $messages[0].ToString() -ne 'Received generic Poll connector payload.') {
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
Write-Output 'Generic Poll PowerShell tests passed.'
