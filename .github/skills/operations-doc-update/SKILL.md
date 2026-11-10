---
name: operations-doc-update
description: 'Refresh the connector trigger payload reference for .NET, Python, and TypeScript. USE WHEN: updating docs/operations-functions-match.md, discovering trigger payload types or matching Connector Namespace operations, checking released SDKs, or maintaining the connector-trigger detail pages. NOT FOR: creating connections, registering trigger configurations, or changing extension code.'
---

# Refreshing the connector trigger payload reference

The reference helps customers find strongly typed payloads for Connector Namespace trigger operations. Refresh the API inventory, published package mappings, Microsoft Learn links, and examples together.

## 1. Discover the API operations

Use a readable Connector Namespace and enumerate every page of its `managedApis` list. Export every connector's Swagger using `api-version=2026-05-01-preview&export=true`; the list's `capabilities` field is not a reliable trigger filter. Collect candidate operations when `x-ms-trigger` is `single` or `batch` and `x-ms-visibility` is not `internal`; keep `advanced` operations.

Treat deprecation flags in the API or Microsoft Learn as candidates for review, not proof of Connector Namespace deprecation or availability. Verify flagged candidates against the current portal or another authoritative source, include those confirmed supported, and label them deprecated for Connector Namespace only when that status is independently confirmed. The CLI's filtered operation list can omit portal-visible candidates. Do not silently classify unverified candidates as unsupported; report them and ask for evidence if necessary. Omit connectors with no selected triggers.

The Connectors Power Platform team confirmed that the Dataverse `SubscribeOnDeletedItems`, `SubscribeOnNewItems`, and `SubscribeOnUpdatedItems` triggers are fully supported for Logic Apps and Connectors and are not deprecated in Connector Namespace. Do not label these operations deprecated based only on API or Microsoft Learn flags. Their payload schemas are resolved dynamically for the selected table; explain the dynamic schema and raw-JSON handling rather than describing the absence of a fixed row class as a missing SDK mapping.

Excluding `internal` is inferred from designer behavior, not a separately verified portal specification. Describe the result as a dated snapshot from one namespace; availability can vary by namespace and over time.

The following script collects candidates, not the final portal-supported inventory. Its `deprecated` field records a source flag or text match, not a Connector Namespace status. Run it in the session workspace, not in the repository. Set `CONNECTOR_NAMESPACE_RESOURCE_ID` to the namespace resource ID and `CONNECTOR_INVENTORY_PATH` to a private output file. Use the user's existing Azure CLI login or credentials they have provided through an approved secure mechanism. If no authenticated session or provided credentials are available, stop and ask the user to authenticate before accessing the API; do not ask them to paste a bearer token into chat. Set `CONNECTOR_ARM_TOKEN` from `az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv` without printing or saving the token. Fetch all pages and retry transient failures; investigate any remaining error instead of silently publishing an incomplete inventory.

```python
import concurrent.futures
import json
import os
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

token = os.environ["CONNECTOR_ARM_TOKEN"]
resource_id = os.environ["CONNECTOR_NAMESPACE_RESOURCE_ID"].strip("/")
output = Path(os.environ["CONNECTOR_INVENTORY_PATH"])
version = "2026-05-01-preview"
base = f"https://management.azure.com/{resource_id}/managedApis"

def get(url):
    parts = urllib.parse.urlsplit(url)
    if parts.scheme != "https" or parts.hostname != "management.azure.com":
        raise ValueError(f"Unexpected ARM endpoint: {url}")
    authorization = "Bearer " + token
    request = urllib.request.Request(url, headers={"Authorization": authorization})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if error.code not in (429, 500, 502, 503, 504) or attempt == 2:
                raise
        except (OSError, TimeoutError):
            if attempt == 2:
                raise
        time.sleep(2**attempt)

items = []
url = f"{base}?api-version={version}"
while url:
    page = get(url)
    items.extend(page["value"])
    url = page.get("nextLink")
names = [item["name"] for item in items]
if not names or len(names) != len(set(names)):
    raise ValueError("The managed API list is empty or contains duplicate IDs")

def export(name):
    encoded = urllib.parse.quote(name, safe="")
    swagger = get(f"{base}/{encoded}?api-version={version}&export=true")
    paths = swagger.get("paths")
    if not isinstance(paths, dict):
        raise ValueError(f"{name}: Swagger has no paths object")
    operations = {}
    for path in paths.values():
        for operation in path.values():
            if not isinstance(operation, dict) or not operation.get("x-ms-trigger"):
                continue
            kind = operation["x-ms-trigger"]
            if kind not in ("single", "batch"):
                raise ValueError(f"{name}: unexpected trigger type {kind!r}")
            operation_id = operation["operationId"]
            summary = operation.get("summary", "")
            tags = operation.get("tags", [])
            text = " ".join([summary, operation_id, *tags]).lower()
            if operation.get("x-ms-visibility") == "internal":
                continue
            if operation_id in operations:
                raise ValueError(f"{name}: duplicate operation {operation_id}")
            deprecated = operation.get("deprecated") is True or operation.get("isDeprecated") is True or "deprecated" in text
            operations[operation_id] = {"summary": summary, "type": kind, "docs": operation.get("externalDocs", {}).get("url"), "deprecated": deprecated}
    return name, operations

with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
    results = dict(pool.map(export, names))
inventory = {name: operations for name, operations in results.items() if operations}
output.write_text(json.dumps(inventory, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"{len(names)} exports; {len(inventory)} connectors; {sum(map(len, inventory.values()))} triggers")
```

Create the final inventory from the candidates after checking each flagged candidate's Connector Namespace support. Record the evidence and any unverified candidates in the session workspace; do not claim complete portal coverage if checks remain unresolved. Match portal entries to exact operation IDs using the current Swagger, since similarly named subscription and admin-only triggers can be different operations.

## 2. Map released payload types

Use the user's configured package feeds and existing credentials when provided. If the user directs that one configured proxy serve all three ecosystems, identify its NuGet, Python, and npm endpoints and use it for both release metadata and package artifacts. Do not silently substitute a different source if the requested feed is unavailable; report the problem and ask before switching. Keep credentials and private feed URLs out of the documentation, and describe a feed-limited snapshot as such rather than assuming it includes every public release.

- **.NET:** Find the latest available [`Azure.Connectors.Sdk` NuGet package](https://www.nuget.org/packages/Azure.Connectors.Sdk), including prereleases, and inspect its assembly and XML documentation. Match each operation to the package's generated trigger payload registry and obtain the payload class, model namespace, and `TriggerCallbackPayload<T>` item type from the corresponding release sources. Where no generated wrapper exists, check whether a released model matches the notification schema and verify the binding and delivered event shape. Do not present a manually composed `TriggerCallbackPayload<T>` as a confirmed operation-specific mapping based only on synthetic deserialization: it is a generic SDK envelope that expects a top-level `body`, and an unwrapped record leaves `Body` null. Until the delivered shape and binding are confirmed, document raw input and any applicable item model instead. Do not mistake a subscription response for the notification body. Verify classes against the package itself; do not list types that are absent from it. Check real signatures in [`functions-connectors-net`](https://github.com/Azure-Samples/functions-connectors-net).
- **.NET LSP:** [`Connectors-NET-LSP`](https://github.com/Azure/Connectors-NET-LSP) demonstrates SDK discovery in `Server/SdkIndex.cs`: it loads assembly metadata, reads connector and trigger constants, and resolves payload classes. Also inspect generated connector clients and registries directly: `ConnectorNames` can omit a connector whose generated types are present. Do not equate 100+ typed connector clients with 100+ connectors that have API triggers.
- **Python:** Inspect the latest available [`azurefunctions-extensions-connectors` wheel](https://pypi.org/project/azurefunctions-extensions-connectors/) and [`azure-connectors` SDK](https://pypi.org/project/azure-connectors/). Match extension module annotations to API operations, then check what their converters actually return for direct records and callback envelopes, including `body` with other top-level metadata such as `headers`. Document any shape limitations and the raw-input alternative. Dataverse `SubscribeWebhookTrigger` and `BusinessEventsTrigger` use `RowChangeNotification` and `ActionEventNotification`, respectively, and return SDK `ObjectEntity` records; do not assume those bindings apply to `SubscribeOn*`. Teams membership returns dictionaries. Webhook binding annotations do not by themselves determine Poll batch cardinality; use the repository's Python Poll sample for that case. Check [`functions-connectors-python`](https://github.com/Azure-Samples/functions-connectors-python) for example applications.
- **TypeScript:** Inspect the latest available [`@azure/connectors`](https://www.npmjs.com/package/@azure/connectors) and [`@azure/functions-extensions-connectors`](https://www.npmjs.com/package/@azure/functions-extensions-connectors) declarations. Record both each trigger payload alias and its item model. Check named helper and context-property names in the extension declarations; present helpers first when available, and use the item model as `T` in `connectorTrigger<T>` for generic registration. Some aliases, such as Dataflows, are exported by the extension instead of the SDK. Check [`functions-connectors-typescript`](https://github.com/Azure-Samples/functions-connectors-typescript) for examples, and identify any that use older helper names.

Intersect each language's mapping with the verified portal-supported inventory by connector ID and exact operation ID. A missing mapping means only that no corresponding type was found in the checked packages; raw input may be JSON or file content. For example, OneDrive single-file triggers return file content, while properties-only triggers use metadata models.

## 3. Add Microsoft Learn links

Swagger operations often include `externalDocs.url` with a connector reference and operation fragment. Check the current Microsoft Learn page and its operation ID and heading anchor: some exported fragments are stale, and some connector pages do not exist. Link the operation ID to the verified matching heading, percent-encoding parentheses and other Markdown-sensitive fragment characters. If no matching Learn operation is available, leave the ID unlinked rather than inventing a URL. Record API and Learn deprecation flags as source evidence; do not infer Connector Namespace deprecation from them without independent confirmation.

## 4. Update and validate the reference

- For a full reference refresh, update the API inventory, check the latest package versions available from the selected feeds, and verify mappings against those exact artifacts. Set `Generated On` to the UTC time when the package-version snapshot was captured (`YYYY-MM-DD HH:MM UTC`), not the time of a later edit. Preserve that timestamp for edits that do not recheck the package releases and mappings; later releases are not accounted for. Link to unversioned package pages instead of hardcoding package versions.
- Keep the introduction concise and place the table of contents immediately after it. Do not hard-wrap prose across source lines or insert forced line breaks in table cells.
- Put connector-specific dynamic-field and binding guidance beside that connector's table rather than in a cross-connector summary.
- Put top connectors with triggers in `docs/operations-functions-match.md`, followed by the alphabetical connector index; put remaining connector details in `docs/connector-triggers/numbers-a-f.md`, `g-l.md`, `m-r.md`, and `s-z.md`. Link directly to the repository sample in `test/poll/README.md`.
- Include the connector ID and linked operation ID where available; sort operation IDs alphabetically and omit operation descriptions and `single`/`batch` labels from the tables. Use separate .NET, Python, and TypeScript columns for released payload types, item types, and applicable helpers; list TypeScript helpers first and show imports above the table. Use GitHub-generated Markdown heading links and account for duplicate headings.
- Compare all connector IDs and operation IDs with the verified portal-supported inventory; verify there are no duplicates or missing connectors. Check every local link and anchor, all Learn operation links, and every listed package symbol.
- Link directly to per-connector sample source files on the sample repositories' default branches instead of duplicating code. Use the Poll variants for .NET and Python, and for TypeScript when an appropriate Poll sample is available; otherwise use an available TypeScript sample. Do not mention delivery modes in customer-facing sample link text.
- Confirm example syntax, type names, helper properties, raw input cases, Poll guidance, snapshot date, and package versions. Run any applicable documentation checks and `git diff --check`.

### Validate sample links

On every refresh, discover candidates in the current default branches of all three sample repositories, and validate every sample link in the main reference and connector detail pages:

1. Check each repository's current default branch and enumerate its source files. For every link, verify the language repository, default branch, and source-file path; .NET and Python links must use the Poll variant, and TypeScript links must use it when an appropriate variant exists. Check the source and deployment configuration rather than relying only on a directory name. Fetch the actual file contents, not just its GitHub page, and require a successful response with nonempty source. Investigate access, rate-limit, and transient errors rather than treating them as missing files.
2. Read the source and verify it contains a registered connector-trigger function and its input handling. Trace its function name or trigger configuration into the associated deployment files, such as Bicep, JSON, or deployment scripts. Confirm the configured connector ID exactly matches the reference section, using `connectorName`, `--connector-name`, or the equivalent; do not infer identity from a directory or helper name. Never link a sample for a different connector as a match.
3. Use the deployment configuration to identify the operation IDs used by the linked functions and compare them with the refreshed API inventory and the section's operation IDs. Do not assume a function name or payload class is an operation ID. Prefer examples of listed operations and record any differences in the session workspace. Keep customer-facing sample link labels to the language and "sample" without operation or input annotations.
4. Compare the sample's helper names, type names, and payload access with the latest available package declarations from the selected feeds and the actual binding behavior checked in section 2. Record raw-input handling, older helpers, and compatibility findings in the session workspace rather than annotating the sample links. Report compatibility that cannot be established, and do not present an outdated helper or payload type as supported by the current package.
5. Record the URL check, connector ID, operations, and package compatibility for each link in the session workspace. Resolve or explicitly report failed or inconclusive checks before claiming the sample links are validated.
