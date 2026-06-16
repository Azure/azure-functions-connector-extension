# Phase 2 API Contracts — Connector Trigger Scaling for Azure Functions

## Summary

This document defines the API contracts for Phase 2 of the Connector Trigger Scaling project. Phase 1 implemented a working `ITargetScaler` with a mock backlog count. Phase 2 replaces the mock with real backlog data by introducing a system-managed event buffer within the Connector Namespace, with three new data plane endpoints for the Function App to consume events and retrieve scaling metrics.

**What Phase 1 delivered:**
- `ITargetScaler` implementation in the Connector Extension (mock backlog count)
- Scale Monitor onboarding for the `connectorTrigger` trigger type
- 107 unit tests passing, Docker-validated end-to-end
- Extension PR: https://github.com/Azure/azure-functions-connector-extension/pull/26
- Scale Monitor PR: https://msazure.visualstudio.com/One/_git/AAPT-Antares-ScaleController/pullrequest/15950498
- Phase 1 Design Doc: https://msazure.visualstudio.com/One/_git/AAPT-Antares-ScaleController?path=/docs/design/phase1-design-doc.md&version=GBuser%2Fanabansal%2Fconnector-scaler-onboarding&_a=preview


---

## Table of Contents

1. [Problem Statement](#1-problem-statement)
2. [Contract 1 — PUT TriggerConfig (Modified Control Plane)](#2-contract-1--put-triggerconfig-modified-control-plane)
3. [Contract 2 — GET TriggerConfig (Modified Control Plane)](#3-contract-2--get-triggerconfig-modified-control-plane)
4. [Contract 3 — GET /events (New Data Plane)](#4-contract-3--get-events-new-data-plane)
5. [Contract 4 — POST /events/acknowledge (New Data Plane)](#5-contract-4--post-eventsacknowledge-new-data-plane)
6. [Contract 5 — GET /metrics (New Data Plane)](#6-contract-5--get-metrics-new-data-plane)
7. [Message Envelope Schema](#7-message-envelope-schema)
8. [Authentication Model](#8-authentication-model)


---

## 1. Problem Statement

The Connector Extension for Azure Functions implements `ITargetScaler` to enable scaling via the Scale Monitor (SM). The SM calls `ConnectorMetricsProvider.GetMetricsAsync()` every 10 seconds to ask: "how many events are pending?"

**Today, that answer is a mock number.** No component in the system knows the real pending count because:

- BPM polls connectors every 3 minutes and immediately delivers events via HTTP POST to the customer's webhook callback URL
- But now **Two independent consumers** (Function Host + SM) need access to the same event data through different interfaces (consume vs. count).


## 2. Contract 1 — PUT TriggerConfig (Modified Control Plane)

### Overview

The PUT TriggerConfig API is an existing ARM control plane endpoint. Phase 2 adds a new `eventSubscription` section as an alternative to `notificationDetails`. When `eventSubscription` is present, events are buffered in a system-managed queue and exposed via data plane endpoints. The data plane endpoint URL is returned in the response.

### Endpoint

```
PUT https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/connectorGateways/{gatewayName}/triggerConfigs/{triggerConfigName}?api-version=2026-03-01-preview
```

### Authentication

ARM Bearer token (existing, no change).

### Request Body — Complete Schema

```diff
{
  "properties": {
    "description": "string | null",
    "state": "Enabled | Disabled",

    "connectionDetails": {
      "connectorName": "string",
      "connectionName": "string"
    },

    "operationName": "string",

    "parameters": [
      {
        "name": "string",
        "value": "<any JSON value>"
      }
    ],

    "recurrence": {
      "frequency": "Month | Week | Day | Hour | Minute | Second",
      "interval": "<integer>"
    },

    "type": "NotSpecified | Recurrence | SlidingWindow",

    "delay": "string | null",

    "conditions": [
      {
        "expression": "string"
      }
    ],

    "settings": {
      "disableSplitOn": "<boolean>"
    },

    "metadata": "<any JSON value> | null",

    "notificationDetails": {                              // existing — unchanged
      "callbackUrl": "string",
      "httpMethod": "Post | Get | Put | Patch | Delete | null",
      "authentication": "<FlowAuthentication object> | null",
      "body": "<any JSON value> | null"
    },

+   "eventSubscription": {                                // NEW — alternative to notificationDetails
+     "allowedConsumers": [
+       {
+         "objectId": "string",
+         "tenantId": "string",
+         "displayName": "string | null"
+       }
+     ]
+   }
  }
}
```

The customer provides **either** `notificationDetails` (webhook delivery) **or** `eventSubscription` (managed queue delivery). Not both.

- `notificationDetails` — **unchanged from today**. Events are delivered via HTTP POST to the customer's callbackUrl.
- `eventSubscription` — **new**. Events are buffered in a system-managed queue and exposed via data plane endpoints for the extension to consume. Authorization to the data plane endpoints is handled via Azure RBAC role assignment on the Connector Namespace or TriggerConfig resource (role definition and exact scope to be finalized during implementation).

### Field-by-Field Reference

**`eventSubscription` (new):**

| Field | Type | Required | Description |
|---|---|---|---|
| `allowedConsumers` | array | Yes (min 1) | Managed Identities authorized to call the data plane endpoints for this TriggerConfig. |
| `allowedConsumers[].objectId` | string (GUID) | Yes | AAD object ID of the Managed Identity (e.g., Function App's system-assigned MI). |
| `allowedConsumers[].tenantId` | string (GUID) | Yes | AAD tenant ID the identity belongs to. |
| `allowedConsumers[].displayName` | string | No | Optional human-readable label for the identity. |

### Validation Rules

```
IF eventSubscription is present:
  - notificationDetails MUST NOT be present → 400 if both are set
    Reason: events are delivered via managed queue, not webhook. Can't have both.

IF notificationDetails is present:
  - callbackUrl MUST be set → 400 if absent
  - callbackUrl MUST be a valid absolute HTTPS URI → 400 if not
  - eventSubscription MUST NOT be present → 400 if both are set
  - All existing validation rules apply unchanged

IF neither is present:
  - 400 — one delivery mechanism must be specified
```

### Example — Event Subscription Mode (new)

```diff
PUT https://management.azure.com/subscriptions/268065b9-dd0a-4246-b686-516f30c2c91d/resourceGroups/t-anabansal-rg-1/providers/Microsoft.Web/connectorGateways/t-anabansal-connector-ns/triggerConfigs/on-new-email?api-version=2026-03-01-preview

Authorization: Bearer <ARM token>
Content-Type: application/json

{
  "properties": {
    "description": "Notify my Function App when new emails arrive in Inbox",
    "connectionDetails": {
      "connectorName": "office365",
      "connectionName": "office365-conn"
    },
    "operationName": "OnNewEmail",
    "parameters": [
      { "name": "folderPath", "value": "Inbox" },
      { "name": "importance", "value": "Any" },
      { "name": "fetchOnlyWithAttachment", "value": false }
    ],
    "recurrence": {
      "frequency": "Minute",
      "interval": 3
    },
+   "eventSubscription": {                                        // NEW — instead of notificationDetails
+     "allowedConsumers": [
+       {
+         "objectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
+         "tenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
+         "displayName": "my-connector-func-app (System Assigned MI)"
+       }
+     ]
+   },
    "settings": {
      "disableSplitOn": false
    }
  }
}
```

### Response — 201 Created (Event Subscription Mode)

```
HTTP/1.1 201 Created
Content-Type: application/json
```

```diff
{
  "id": "/subscriptions/268065b9-dd0a-4246-b686-516f30c2c91d/resourceGroups/t-anabansal-rg-1/providers/Microsoft.Web/connectorGateways/t-anabansal-connector-ns/triggerConfigs/on-new-email",
  "name": "on-new-email",
  "type": "Microsoft.Web/connectorGateways/triggerConfigs",
  "properties": {
    "provisioningState": "Succeeded",
    "state": "Enabled",
    "description": "Notify my Function App when new emails arrive in Inbox",
    "connectionDetails": {
      "connectorName": "office365",
      "connectionName": "office365-conn"
    },
    "operationName": "OnNewEmail",
    "parameters": [
      { "name": "folderPath", "value": "Inbox" },
      { "name": "importance", "value": "Any" },
      { "name": "fetchOnlyWithAttachment", "value": false }
    ],
    "recurrence": {
      "frequency": "Minute",
      "interval": 3
    },
    "type": "NotSpecified",
    "conditions": [],
    "settings": {
      "disableSplitOn": false
    },
+   "eventSubscription": {                                        // NEW
+     "allowedConsumers": [
+       {
+         "objectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
+         "tenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
+         "displayName": "my-connector-func-app (System Assigned MI)"
+       }
+     ]
+   },
+   "dataPlaneEndpoint": {                                        // NEW
+     "baseUri": "<data plane base URL — structure determined by implementation>"
+   }
  },
  "systemData": {
    "createdBy": "t-anabansal@microsoft.com",
    "createdByType": "User",
    "createdAt": "2026-06-14T10:00:00Z",
    "lastModifiedBy": "t-anabansal@microsoft.com",
    "lastModifiedByType": "User",
    "lastModifiedAt": "2026-06-14T10:00:00Z"
  }
}
```

### Example — Webhook Mode (today's behavior, unchanged)

```
PUT https://management.azure.com/subscriptions/268065b9-dd0a-4246-b686-516f30c2c91d/resourceGroups/t-anabansal-rg-1/providers/Microsoft.Web/connectorGateways/t-anabansal-connector-ns/triggerConfigs/on-new-email?api-version=2026-03-01-preview

Authorization: Bearer <ARM token>
Content-Type: application/json

{
  "properties": {
    "description": "POST new emails to my dev tunnel",
    "connectionDetails": {
      "connectorName": "office365",
      "connectionName": "office365-conn"
    },
    "operationName": "OnNewEmail",
    "parameters": [
      { "name": "folderPath", "value": "Inbox" },
      { "name": "importance", "value": "Any" },
      { "name": "fetchOnlyWithAttachment", "value": false }
    ],
    "recurrence": {
      "frequency": "Minute",
      "interval": 3
    },
    "notificationDetails": {
      "callbackUrl": "https://n35m4fsv-8080.inc1.devtunnels.ms/api/HandleNewEmail",
      "httpMethod": "Post",
      "authentication": {
        "type": "ManagedServiceIdentity",
        "audience": "https://my-func-app.azurewebsites.net"
      },
      "body": {
        "body": "@triggerBody()",
        "headers": "@triggerOutputs()['headers']"
      }
    },
    "settings": {
      "disableSplitOn": false
    }
  }
}
```

---

## 3. Contract 2 — GET TriggerConfig (Modified Control Plane)

### Overview

The GET response is modified to include a `dataPlaneEndpoint` section when `eventSubscription` is present. This tells the customer (and the Connector Extension) where to poll for events and metrics.

### Endpoint

```
GET https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/connectorGateways/{gatewayName}/triggerConfigs/{triggerConfigName}?api-version=2026-03-01-preview
```

### Response — Event Subscription Mode

```diff
{
  "id": "/subscriptions/268065b9-dd0a-4246-b686-516f30c2c91d/resourceGroups/t-anabansal-rg-1/providers/Microsoft.Web/connectorGateways/t-anabansal-connector-ns/triggerConfigs/on-new-email",
  "name": "on-new-email",
  "type": "Microsoft.Web/connectorGateways/triggerConfigs",
  "properties": {
    "provisioningState": "Succeeded",
    "state": "Enabled",
    "description": "Notify my Function App when new emails arrive in Inbox",
    "connectionDetails": {
      "connectorName": "office365",
      "connectionName": "office365-conn"
    },
    "operationName": "OnNewEmail",
    "parameters": [
      { "name": "folderPath", "value": "Inbox" },
      { "name": "importance", "value": "Any" },
      { "name": "fetchOnlyWithAttachment", "value": false }
    ],
    "recurrence": {
      "frequency": "Minute",
      "interval": 3
    },
    "type": "NotSpecified",
    "conditions": [],
    "settings": {
      "disableSplitOn": false
    },
+   "eventSubscription": {                                        // NEW
+     "allowedConsumers": [
+       {
+         "objectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
+         "tenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47",
+         "displayName": "my-connector-func-app (System Assigned MI)"
+       }
+     ]
+   },
+   "dataPlaneEndpoint": {                                        // NEW
+     "baseUri": "<data plane base URL — structure determined by implementation>"
+   }
  },
  "systemData": {
    "createdBy": "t-anabansal@microsoft.com",
    "createdByType": "User",
    "createdAt": "2026-06-13T17:00:00Z",
    "lastModifiedBy": "t-anabansal@microsoft.com",
    "lastModifiedByType": "User",
    "lastModifiedAt": "2026-06-13T17:00:00Z"
  }
}
```

### New fields in GET response

| Field | Type | Present when | Purpose |
|---|---|---|---|
| `eventSubscription` | object | Present if managed queue delivery was configured | Echoed back — presence indicates managed queue delivery |
| `dataPlaneEndpoint` | object | `eventSubscription` is present | Container for data plane URL |
| `dataPlaneEndpoint.baseUri` | string | `eventSubscription` is present | Base URL for data plane calls. Append `/events`, `/events/acknowledge`, `/metrics`. |

The `dataPlaneEndpoint.baseUri` URL structure is determined by the implementation team. The extension reads this URL from the response and appends the path suffixes for each data plane call.

### Response — Webhook Mode (unchanged from today)

```json
{
  "id": "...",
  "name": "on-new-email",
  "type": "Microsoft.Web/connectorGateways/triggerConfigs",
  "properties": {
    "provisioningState": "Succeeded",
    "state": "Enabled",
    "connectionDetails": { "connectorName": "office365", "connectionName": "office365-conn" },
    "operationName": "OnNewEmail",
    "parameters": [ ... ],
    "recurrence": { "frequency": "Minute", "interval": 3 },
    "notificationDetails": {
      "callbackUrl": "https://n35m4fsv-8080.inc1.devtunnels.ms/api/HandleNewEmail",
      "httpMethod": "Post",
      "authentication": { "type": "ManagedServiceIdentity", "audience": "..." },
      "body": { "body": "@triggerBody()" }
    },
    "settings": { "disableSplitOn": false }
  }
}
```

No `dataPlaneEndpoint` field. No `eventSubscription` field. Fully backward compatible.

---

## 4. Contract 3 — GET /events (New Data Plane)

### Overview

Returns pending events from the queue using peek-ack semantics. Called by the Function Host to consume events. Each returned event includes an `ackToken` that must be sent back via POST /events/acknowledge within the acknowledgment window. If not acknowledged in time, the event reappears in the queue for retry. Acknowledgment window to be determined during implementation.

### Endpoint

```
GET {dataPlaneEndpoint.baseUri}/events
```

### Authentication

```
Authorization: Bearer <token from DefaultAzureCredential>
```

Same as /metrics — caller must be authorized to access this TriggerConfig's data plane.

### Request

```
GET {dataPlaneEndpoint.baseUri}/events?maxMessages={integer}&maxWaitSeconds={integer}
```

**Query Parameters (all optional):**

| Parameter | Type | Description |
|-----------|------|-------------|
| `maxMessages` | integer | Max events to return. Default and range TBD during implementation. |
| `maxWaitSeconds` | integer | Long-poll timeout in seconds. Returns empty array on timeout. Set to `0` for immediate response. Default and range TBD during implementation. |

> **Note:** Parameters are passed as query string values, not in a request body. GET requests with bodies are not reliably supported across proxies, load balancers, and HTTP clients.

### Response — 200 OK (events available)

```json
{
  "value": [
    {
      "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "ackToken": "ack-a1b2c3d4-e5f6-7890",
      "deliveryCount": 1,
      "enqueuedTimeUtc": "2026-06-13T17:00:05.123Z",
      "ackDeadlineUtc": "2026-06-13T17:05:05.123Z",

      "trackingId": "55555555-5555-5555-5555-555555555555",
      "workflowRunId": "08585432109876543210",
      "splitOnEnabled": true,

      "data": {
        "id": "AAMkAGQ5...",
        "subject": "Meeting tomorrow",
        "from": "alice@contoso.com",
        "toRecipients": "t-anabansal@microsoft.com",
        "receivedDateTime": "2026-06-13T16:59:45Z",
        "importance": "normal",
        "hasAttachments": false,
        "body": {
          "contentType": "html",
          "content": "<html><body>Let's meet at 3pm...</body></html>"
        },
        "isRead": false
      }
    }
  ]
}
```

### Response — 200 OK (no events, long-poll timeout)

```json
{
  "value": []
}
```

### Response field reference for each event in `value[]`

| Field | Type | Description |
|---|---|---|
| `eventId` | string | Unique identifier for this event. Stable across retries — callers can use it for idempotency and deduplication. |
| `ackToken` | string | Acknowledgment receipt. Must be sent back in POST /events/acknowledge within the acknowledgment window. |
| `deliveryCount` | integer | How many times this event has been delivered. 1 = first delivery. Increments each time the lock expires without acknowledgment. After exceeding the max delivery count (configured on the queue), the event is dead-lettered. |
| `enqueuedTimeUtc` | string (ISO 8601) | When the event was enqueued into the buffer. |
| `ackDeadlineUtc` | string (ISO 8601) | When the peek-ack expires for this event. If the extension hasn't acknowledged by this time, the event reappears for retry. The extension can use this to decide whether to continue processing or skip. |
| `trackingId` | string (GUID) | Primary correlation ID for log stitching across BPM → ApiHub → Connector. Unique per event. Use this to search Kusto logs for the full execution trace. |
| `workflowRunId` | string | Links back to the hidden workflow's run history in BPM. Unique per event. Use this to look up the full run details (trigger output, action result, timing). |
| `splitOnEnabled` | boolean | Whether splitOn was applied to this trigger output. When `true`, `data` is a single item from the array. When `false`, `data` is the full trigger output (may be an array). Extension uses this to know how to deserialize `data`. |
| `data` | any JSON | **The actual trigger payload from the connector.** This is the raw connector response body (what `@triggerBody()` evaluates to). For splitOn triggers, this is a single element from the array. The extension deserializes this into the customer's POCO type (e.g., `Office365OnNewEmailTriggerPayload`). |

### Behavior

- **peek-ack semantics**: Returned events are invisible to other receivers for the acknowledgment window (configured on the queue). If not acknowledged, they reappear in the queue.
- **At-least-once delivery**: Events may be delivered more than once if the caller crashes between receive and acknowledge. The `eventId` is stable across retries — callers can use it for idempotency.
- **Long polling**: If no events are available and `maxWaitSeconds > 0`, the request blocks waiting for events. Returns empty array on timeout.


---

## 5. Contract 4 — POST /events/acknowledge (New Data Plane)

### Overview

Acknowledges (completes) previously received events. Called by the Function Host after successfully processing events. Acknowledged events are permanently removed from the queue.

### Endpoint

```
POST {dataPlaneEndpoint.baseUri}/events/acknowledge
```

### Authentication

```
Authorization: Bearer <token from DefaultAzureCredential>
Content-Type: application/json
```

### Request Body

```json
{
  "events": [
    { "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "ackToken": "ack-a1b2c3d4-e5f6-7890" },
    { "eventId": "b2c3d4e5-f6a7-8901-bcde-f12345678901", "ackToken": "ack-b2c3d4e5-f6a7-8901" },
    { "eventId": "c3d4e5f6-a7b8-9012-cdef-123456789012", "ackToken": "ack-c3d4e5f6-a7b8-9012" }
  ]
}
```

### Request field reference

| Field | Type | Required | Description |
|---|---|---|---|
| `events` | array | Yes | Events to acknowledge. Each entry pairs the event's ID with its acknowledgment token. |
| `events[].eventId` | string | Yes | The event ID from the GET /events response. Included for traceability and logging. |
| `events[].ackToken` | string | Yes | The acknowledgment token from the GET /events response. |

### Response — 200 OK

> **Example:** 3 events submitted — 2 acknowledged successfully, 1 acknowledgment window had expired.

```json
{
  "acknowledged": 2,
  "expired": 1,
  "failed": 0,
  "results": [
    { "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "ackToken": "ack-a1b2c3d4-e5f6-7890", "status": "Acknowledged" },
    { "eventId": "b2c3d4e5-f6a7-8901-bcde-f12345678901", "ackToken": "ack-b2c3d4e5-f6a7-8901", "status": "AckExpired" },
    { "eventId": "c3d4e5f6-a7b8-9012-cdef-123456789012", "ackToken": "ack-c3d4e5f6-a7b8-9012", "status": "Acknowledged" }
  ]
}
```

### Response field reference

| Field | Type | Description |
|---|---|---|
| `acknowledged` | integer | Number of events successfully completed (permanently removed from queue). |
| `expired` | integer | Number of events whose acknowledgment window had expired. The events reappeared in the queue. |
| `failed` | integer | Number of events that failed for other reasons (e.g., backend error). |
| `results` | array | Per-event status. |
| `results[].eventId` | string | The event ID that was submitted. |
| `results[].ackToken` | string | The acknowledgment token that was submitted. |
| `results[].status` | enum | `Acknowledged` — completed successfully. `AckExpired` — acknowledgment window expired, event reappeared. `NotFound` — token doesn't match any pending event (already acknowledged or invalid). `Failed` — unexpected error. |

### Per-ack-token status values

| Status | Meaning | Action needed |
|---|---|---|
| `Acknowledged` | Event permanently removed from queue. | None. |
| `AckExpired` | Lock expired before acknowledge arrived. Event reappeared in queue and will be redelivered on the next GET /events call. | None — automatic retry via redelivery. Log a warning if frequent. |
| `NotFound` | acknowledgment token is invalid or event was already acknowledged. | None — likely a duplicate ack. Idempotent. |
| `Failed` | Backend error (e.g., transient failure). | Retry the acknowledge call. |


---

## 6. Contract 5 — GET /metrics (New Data Plane)

### Overview

Returns the current queue depth and scaling metrics. Called by the Scale Monitor every 10 seconds. This is the **primary contract for enabling target based scaling** — it provides the `pendingEventCount` that feeds into `ConnectorTargetScaler.GetScaleResultAsync()`.

### Endpoint

```
GET {dataPlaneEndpoint.baseUri}/metrics
```

### Authentication

```
Authorization: Bearer <token from DefaultAzureCredential>
```

The token's `oid` claim must be authorized to access this TriggerConfig's data plane (via resource-level authentication — mechanism TBD).

### Request

No query parameters. No request body. Simple GET.

### Response — 200 OK

```json
{
  "pendingEventCount": 47,
  "oldestEventAgeSeconds": 323,
  "lastEnqueuedTimeUtc": "2026-06-13T17:28:12.456Z",
  "lastDequeuedTimeUtc": "2026-06-13T17:27:45.123Z"
}
```

### Response field reference

| Field | Type | Description | Used by |
|---|---|---|---|
| `pendingEventCount` | long | Number of active events. **This is the backlog.** | `ConnectorMetricsProvider` → `ConnectorTargetScaler` math |
| `oldestEventAgeSeconds` | long | Age of the oldest pending event in seconds. Useful for throttle-down decisions — if events are old, don't scale down. | `ConnectorTargetScaler` throttle logic |
| `lastEnqueuedTimeUtc` | string (ISO 8601) | Timestamp of the most recently enqueued event. SM can skip scale math entirely if no new events have arrived since the last poll cycle. | Staleness detection |
| `lastDequeuedTimeUtc` | string (ISO 8601) \| null | When the most recent event was acknowledged. Null if never consumed. | Staleness detection |

### How the Scale Monitor uses this

```
SM calls ConnectorTargetScaler.GetScaleResultAsync()
  → ConnectorMetricsProvider.GetMetricsAsync()
    → HTTP GET /metrics → { pendingEventCount: 47 }
  → math: ceil(47 / (concurrency × batchSize))
    → e.g., ceil(47 / (16 × 1)) = 3
  → return TargetScalerResult { TargetWorkerCount = 3 }
```

The `ConnectorTargetScaler` math is unchanged from Phase 1. Only the data source changes (real count vs. mock).

---

## 7. Message Envelope Schema

The message envelope is the JSON structure stored in the system-managed event buffer and returned to the consumer by the GET /events data plane endpoint.

### Design principle: slim envelope

The envelope carries **only fields that vary per event or are required by the consume/scale contracts**. Static fields (resource identity, trigger type, connection name, etc.) are excluded.

### Top-level structure

| Field | Purpose | Source in BPM | Per-event? |
|---|---|---|---|
| `eventId` | Unique message identifier | Auto-assigned by the event buffer | ✅ Yes |
| `ackToken` | Acknowledgment receipt for acknowledge | Event buffer metadata | ✅ Yes |
| `deliveryCount` | Retry attempt counter | Event buffer metadata | ✅ Yes |
| `enqueuedTimeUtc` | When the event was enqueued | Event buffer metadata | ✅ Yes |
| `ackDeadlineUtc` | When the peek-ack expires | Event buffer metadata | ✅ Yes |
| `trackingId` | Primary correlation ID | `RequestCorrelationContext.CurrentActivityId` | ✅ Yes |
| `workflowRunId` | Links to BPM run history | `metadata.FlowRunSequenceId` | ✅ Yes |
| `splitOnEnabled` | Whether data is single item or array | `trigger.SplitOn != null` | ✅ Per poll cycle |
| `data` | Raw trigger payload from connector | `@triggerBody()` resolved value | ✅ Yes |

### Complete envelope example

```json
{
  "eventId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "ackToken": "ack-a1b2c3d4-e5f6-7890",
  "deliveryCount": 1,
  "enqueuedTimeUtc": "2026-06-13T17:00:05.123Z",
  "ackDeadlineUtc": "2026-06-13T17:05:05.123Z",

  "trackingId": "55555555-5555-5555-5555-555555555555",
  "workflowRunId": "08585432109876543210",
  "splitOnEnabled": true,

  "data": {
    "id": "AAMkAGQ5...",
    "subject": "Meeting tomorrow",
    "from": "alice@contoso.com",
    "toRecipients": "t-anabansal@microsoft.com",
    "receivedDateTime": "2026-06-13T16:59:45Z",
    "importance": "normal",
    "hasAttachments": false,
    "body": {
      "contentType": "html",
      "content": "<html><body>Let's meet at 3pm...</body></html>"
    },
    "isRead": false
  }
}
```

### `data` field — trigger payload

The raw trigger payload from the connector. This is exactly what `@triggerBody()` evaluates to. The Connector Extension deserializes this into the customer's POCO type (e.g., `Office365OnNewEmailTriggerPayload`).


---

## 8. Authentication Model

### Control plane (existing, no change)

| API | Auth method | Who validates |
|---|---|---|
| PUT/GET/DELETE TriggerConfig | ARM Bearer token | Azure Resource Manager |

### Data plane (new)

| API | Auth method | Who validates | How |
|---|---|---|---|
| GET /metrics | MI Bearer token | Data plane host | Azure RBAC role assignment on the TriggerConfig resource or the NameSpace resource (role to be defined during implementation) |
| GET /events | MI Bearer token | Data plane host | Same |
| POST /events/acknowledge | MI Bearer token | Data plane host | Same |

### Token validation flow

```
1. Function App (or SM pod) calls data plane endpoint
   with: Authorization: Bearer <token from DefaultAzureCredential>

2. Data plane host receives request
   → Validates Bearer token (standard AAD validation)
   → Extracts claims: oid (object ID), tid (tenant ID), exp (expiry)

3. Checks authorization:
   Does this identity have permission to access this TriggerConfig's data plane?
   Does this identity have an Azure RBAC role assignment on this TriggerConfig resource or the NameSpace resource?
   (Role definition to be finalized during implementation)

4. Yes → request proceeds
   No → 403 Forbidden
```

### Why MI works for both Function Host and Scale Monitor

Both run from the same Function App deployment:

```
Function App (MI objectId: a1b2c3d4-e5f6-7890-abcd-ef1234567890)
  ├── Worker Pod: Function Host → calls GET /events → Bearer oid = a1b2c3d4...  ✅
  └── SM Pod: Scale Monitor → calls GET /metrics → Bearer oid = a1b2c3d4...    ✅
```

One RBAC role assignment covers both because they share the same Function App Managed Identity.


---
