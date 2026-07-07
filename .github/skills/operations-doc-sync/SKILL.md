---
name: operations-doc-sync
description: 'Update docs/operations-functions-match.md so it matches the latest trigger operations in the parent SDK repos (Connectors-NET-SDK, connectors-python-sdk, Connectors-nodejs-SDK). USE WHEN: syncing the operations-to-functions mapping doc after new connectors/triggers land in the SDKs, auditing the doc for missing connector sections or trigger rows, or verifying .NET TriggerCallbackPayload / Python typed-model coverage. NOT FOR: writing trigger code (use trigger-registration skill), connection setup (use connection-setup skill), or editing the SDKs themselves.'
---

# Sync operations-functions-match.md with the parent SDKs

`docs/operations-functions-match.md` maps every connector **trigger operation** to its
Azure Functions signature across the three SDKs. This skill explains how the doc is
sourced and how to bring it up to date when new connectors or triggers ship.

## What the doc is based on

Each table row = one connector **trigger operation** (`operationId`), with columns:

| Column | Source of truth |
| --- | --- |
| Operation ID | The connector's managed-API `operationId` |
| Description | Trigger summary (managed API / SDK docstring) |
| .NET Payload Type | The `TriggerCallbackPayload<T>` subclass in `Connectors-NET-SDK`, or the `string` default if none |
| Python Type | The typed model class in `connectors-python-sdk` (has a `from_json`), or the `str` default if none |
| TypeScript Type | The typed return type in `Connectors-nodejs-SDK`, or the `unknown` default if none |

Each `## <Connector> Connector` heading is followed by a locale-less Learn reference
link (see conventions). There is intentionally **no** batch/single "Type" column: both
SDKs normalize every callback shape into a list (`value: List<T>`), so the distinction
never changes the handler signature.

## Source repositories

The three SDK repos are expected as sibling clones next to this repo (adjust paths if
yours differ):

- `../Connectors-NET-SDK` — `src/Azure.Connectors.Sdk/Generated/*Extensions.cs`
- `../connectors-python-sdk` — `src/azure/connectors/*.py`
- `../Connectors-nodejs-SDK` — `src/generated/`

**If a sibling clone is missing**, clone it before extracting (shallow clone is enough
since you only read the latest generated sources):

```powershell
$repos = @{
  'Connectors-NET-SDK'    = 'https://github.com/Azure/Connectors-NET-SDK.git'
  'connectors-python-sdk' = 'https://github.com/Azure/connectors-python-sdk.git'
  'Connectors-nodejs-SDK' = 'https://github.com/Azure/Connectors-nodejs-SDK.git'
}
foreach ($name in $repos.Keys) {
  if (-not (Test-Path "..\$name")) { git clone --depth 1 $repos[$name] "..\$name" }
}
```

If you cannot clone locally (no network/credentials), fall back to reading the files
directly on GitHub — e.g. fetch `https://raw.githubusercontent.com/Azure/<repo>/main/<path>`
or use the `github-mcp-server` `get_file_contents` / `search_code` tools — and apply the
same extraction logic. If only one repo is available, sync just the columns it covers
(the .NET repo alone is enough to keep the operation list and `.NET Payload Type`
authoritative) and leave the other language columns unchanged.

Always `git fetch` and confirm you are on the latest `origin/main` before extracting
(skip for a fresh `--depth 1` clone, which is already at latest):

```powershell
cd ..\Connectors-NET-SDK;   git fetch origin -q; git rev-list --count HEAD..origin/main
cd ..\connectors-python-sdk; git fetch origin -q; git rev-list --count HEAD..origin/main
```

A non-zero count means you must pull/checkout the latest before syncing.

## Conventions (must follow)

- **Deprecated triggers are excluded.** Do NOT add rows for SDK methods whose
  description/docstring is marked `(deprecated)` (e.g. SharePoint `OnNewFile` /
  `OnUpdatedFile` folder triggers).
- **Default (no typed model):** put the raw-JSON binding type in the cell — `string`
  (.NET), `str` (Python), `unknown` (TypeScript). Do not leave cells empty. Type names
  differ across SDKs and coverage is uneven, so fill each language column from its own
  SDK — never copy the .NET class name into the Python/TypeScript cells.
- **No batch/single "Type" column.** Both SDKs normalize every callback shape into a
  list (`value: List<T>`), so the distinction never changes the handler signature.
- **Each `## <Connector> Connector` heading is followed by a locale-less Learn link:**
  `_📖 Connector reference: [learn.microsoft.com/connectors/<slug>](https://learn.microsoft.com/connectors/<slug>/)_`
  Omit the `en-us` locale so Learn redirects to the reader's locale. The `<slug>` is the
  connector's managed-API name — read it from the .NET client's
  `ConnectorName => "<slug>"`. Verify the URL resolves (some connectors, e.g. Orderful,
  have no Learn page — skip the link for those).
- **Keep the Table of Contents in sync** — every `## <Connector> Connector` heading
  needs a matching TOC anchor link.

## Step 1 — Extract the authoritative .NET trigger list

The **full** set of .NET trigger operations for a connector is the
`{Connector}TriggerOperations` static class (region `#region Trigger Operation
Constants`) — one `public const string OnX = "operationId";` per trigger, **including
untyped ones**. Do NOT rely only on `TriggerCallbackPayload<T>` classes; those cover
only the *typed* subset (e.g. Microsoft Bookings has 3 trigger constants but 0 typed
payloads). Extract every operation and mark whether it has a typed payload:

```powershell
cd ..\Connectors-NET-SDK\src\Azure.Connectors.Sdk\Generated
$rows=@()
Get-ChildItem *Extensions.cs | ForEach-Object {
  $conn=$_.BaseName -replace 'Extensions$',''; $c=Get-Content $_.FullName
  $typed=@{}
  for($i=0;$i -lt $c.Count;$i++){
    if($c[$i] -match 'operationId: ([\w\.]+)\)\.'){ $op=$matches[1]
      for($j=$i+1;$j -lt $i+6 -and $j -lt $c.Count;$j++){ if($c[$j] -match 'class (\w+TriggerPayload)\s*:\s*TriggerCallbackPayload<([^>]+)>'){ $typed[$op]=$matches[1]; break } } } }
  $inOps=$false
  for($i=0;$i -lt $c.Count;$i++){
    if($c[$i] -match 'TriggerOperations$'){ $inOps=$true }
    if($inOps -and $c[$i] -match 'const string \w+ = "([^"]+)"'){ $op=$matches[1]
      $desc=''; for($k=$i-1;$k -gt $i-5 -and $k -ge 0;$k--){ if($c[$k] -match '///\s*(.+)' -and $matches[1].Trim() -notin '<summary>','</summary>'){ $desc=$matches[1].Trim(); break } }
      $rows += [pscustomobject]@{Connector=$conn;Op=$op;Typed=($typed[$op] ?? '-');Desc=$desc} }
    if($inOps -and $c[$i] -match '#endregion Trigger Operation'){ $inOps=$false } }
}
$rows   # operationId, typed payload class (or '-'), description, per connector
```

Every `operationId` in this list should appear in the doc. `Typed` non-`-` rows drive
the `.NET Payload Type` column; `Typed = -` rows use the `string` default.

## Step 2 — Find NEW / missing triggers in the Python SDK

The Python trigger methods (`async def on_*` / `*_trigger`) mostly return raw
`json.loads(...)`. Only a few connectors ship typed models (a dataclass with a
`from_json` classmethod, e.g. Office 365's `GraphClientReceiveMessage`). Find any
trigger whose description is **not already in the doc**:

```powershell
cd ..\connectors-python-sdk\src\azure\connectors
$doc = Get-Content <repo>\docs\operations-functions-match.md -Raw
Get-ChildItem *.py | ForEach-Object {
  $mod=$_.BaseName; $c=Get-Content $_.FullName
  for ($i=0; $i -lt $c.Count; $i++) {
    if ($c[$i] -match '^\s{4}async def (\w+)\(') {
      $name=$matches[1]; $ds=@(); $open=$false
      for ($j=$i+1; $j -lt $i+20 -and $j -lt $c.Count; $j++) {
        $t=$c[$j].Trim()
        if ($t -eq '"""') { if ($open){break} else {$open=$true; continue} }
        if ($open) { $ds+=$t }
      }
      $desc = ($ds | Where-Object {$_ -ne ''} | Select-Object -First 1)
      if (($ds -join ' ') -match 'This trigger|Triggers a flow|Triggers when|start a flow') {
        if ($desc -and -not ($doc -match [regex]::Escape($desc))) {
          Write-Output "MISSING [$mod] $name :: $desc"
        }
      }
    }
  }
}
```

Ignore false positives whose description is already represented in the doc under a
slightly different wording (e.g. WDATP "Triggers when…" vs the doc's "When…").
Discovered typed models can be listed with:
`Select-String -Path *.py -Pattern 'def from_json\(cls, payload'`.

## Step 2b — Check the nodejs SDK

The nodejs SDK (`../Connectors-nodejs-SDK/src/generated/*Extensions.ts`) covers far
fewer connectors than .NET. Its trigger methods are `public async <name>Async(...)`
with an `@remarks Triggers …` comment and a `/subscriptions/…` or `/trigger…` path.
Typed triggers return a `TriggerBatchResponse<T>`-style type; otherwise use the doc's
`unknown` default for the `TypeScript Type` cell. List them with:

```powershell
cd ..\Connectors-nodejs-SDK\src\generated
Select-String -Path *Extensions.ts -Pattern 'public async \w+Async|@remarks .*[Tt]rigger'
```

Only treat a method as a trigger if it has an `@remarks Triggers` note or a
subscription/trigger request path — many `*Async` methods are actions.

## Step 3 — Resolve operationIds and descriptions

`operationId` and `description` come from Step 1 (.NET) for SDK connectors, or from the
official reference at `https://learn.microsoft.com/connectors/<connectorName>/` for
managed-API-only connectors. The Python method name is the snake_case of the
`operationId` (e.g. `subscribe_webhook_trigger` ⇒ `SubscribeWebhookTrigger`). Fill each
language's type column from its own SDK, using that language's default (`string` / `str`
/ `unknown`) when the SDK has no typed model.

## Step 4 — Edit the doc

For each genuinely new, non-deprecated trigger:

1. Add the row to the existing connector's table, **or** add a new
   `## <Connector> Connector` section. Each row has 5 columns: `Operation ID |
   Description | .NET Payload Type | Python Type | TypeScript Type`, and each new
   section heading is followed by the locale-less Learn link (see conventions).
2. Add a matching entry to the Table of Contents.
3. Insert the section **alphabetically** by connector display name (the doc lists all
   connectors A–Z), and add its TOC link in the same alphabetical position.
4. For connectors that ship SDK code samples (.NET/Python/TypeScript signature
   blocks), add those only if the operation has a typed payload; otherwise a raw-JSON
   binding (`string`/`str`/`unknown`) is sufficient and no sample is needed.

## Step 5 — Verify

- Every `operationId` from each `{Connector}TriggerOperations` class (Step 1) appears
  exactly once in the doc.
- Every typed `*TriggerPayload` class is referenced in the correct `.NET Payload Type`
  cell.
- No `(deprecated)` operations were added.
- Every new heading has a TOC link and every TOC link resolves to a heading.
- Connector sections and their TOC links are in the same **alphabetical** order.
- `.NET` / `Python` / `TypeScript` cell values follow the conventions above.

## Notes

- The **.NET SDK is the authoritative source for the full trigger list** (via the
  `{Connector}TriggerOperations` constants) and for typed payloads (via
  `TriggerCallbackPayload<T>`). A connector can expose trigger operations with **no**
  typed payload (e.g. Microsoft Bookings) — those still get rows, with `string`/`str`/
  `unknown` cells.
- A connector may have only a name constant in `ConnectorNames.cs` with **no** generated
  client/triggers (e.g. `commondataservice` / Microsoft Dataverse) — that is **not**
  .NET trigger support; source such rows from Python/managed API instead.
- Managed-API-only connectors (no SDK code) still get a row so the doc stays the
  complete union of trigger operations, using the raw-JSON convention.
- Update `## Source References` at the bottom of the doc if a source repo path changes.
