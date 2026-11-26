# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

param($Payload, $TriggerMetadata)

$ErrorActionPreference = 'Stop'
$parsed = $Payload
if ($Payload -is [string]) {
    $parsed = ConvertFrom-Json -InputObject $Payload -AsHashtable -NoEnumerate -ErrorAction Stop
}
if ($null -eq $parsed -or ($parsed -isnot [System.Collections.IDictionary] -and $parsed -isnot [array])) {
    throw 'Expected a JSON connector payload object or array.'
}
Write-Information 'Received generic Poll connector payload.' -InformationAction Continue
