# Operations to Azure Functions Signature Mapping

This document maps connector trigger operation names to their corresponding Azure Functions signatures across .NET, Python, and TypeScript SDKs.

> [!CAUTION]
> **Python Connectors Extension Package temporary limitation:** Until [Handle dual shaped payload (#28)](https://github.com/Azure/connectors-python-sdk/issues/28) is resolved, use `str` as the parameter type. The typed SDK models are not yet reliable for single type payload.

## Python: Choosing a Decorator

| Python Version | Decorator | Package Requirement |
| --- | --- | --- |
| 3.13+ | `@app.connector_trigger` | `azure-functions>=2.2.0b4` |
| < 3.13 | `@app.generic_trigger(type="connectorTrigger", arg_name="payload")` | `azure-functions` |

## Python: Choosing Packages by Trigger Operation

| Scenario | Extra Package(s) | Parameter Type |
| --- | --- | --- |
| Office 365 `OnNewEmail` | `azurefunctions-extensions-connectors` (pulls in `azure-connectors`) | Typed class (`ClientReceiveMessage`) |
| Other connector with a typed SDK model (see Python Type column) | `azure-connectors` | Typed class |
| No typed model available | None beyond `azure-functions` | `str` |

> [!NOTE]
> **Typed Payloads:** Where columns show `string` / `str` / `unknown` (raw JSON), typed SDK models are in development. Use the raw type and parse JSON manually in your function.
>
> **Trigger Types:** Operations are classified as `batch` (returns multiple items) or `single` (returns one item). The SDKs always return a list — for `single` triggers, the list contains one element.

## Table of Contents

- [Office 365 Connector](#office-365-connector)
- [SharePoint Online Connector](#sharepoint-online-connector)
- [Microsoft Teams Connector](#microsoft-teams-connector)
- [OneDrive for Business Connector](#onedrive-for-business-connector)
- [Azure Blob Storage Connector](#azure-blob-storage-connector)
- [Event Hubs Connector](#event-hubs-connector)
- [Service Bus Connector](#service-bus-connector)
- [Outlook Connector](#outlook-connector)
- [SQL Connector](#sql-connector)
- [FTP Connector](#ftp-connector)
- [Dropbox Connector](#dropbox-connector)
- [OneDrive (Personal) Connector](#onedrive-personal-connector)
- [Salesforce Connector](#salesforce-connector)
- [GitHub Connector](#github-connector)
- [Twitter Connector](#twitter-connector)
- [RSS Connector](#rss-connector)
- [Box Connector](#box-connector)
- [Slack Connector](#slack-connector)
- [Azure Queues Connector](#azure-queues-connector)
- [Azure Event Grid Connector](#azure-event-grid-connector)
- [Yammer (Viva Engage) Connector](#yammer-viva-engage-connector)
- [Microsoft Defender ATP (WDATP) Connector](#microsoft-defender-atp-wdatp-connector)
- [Google Calendar Connector](#google-calendar-connector)
- [Dynamics AX Connector](#dynamics-ax-connector)
- [Source References](#source-references)

---

## Office 365 Connector

### Email Triggers

| Operation ID | Description | Type | .NET Payload Type | Python SDK Type | TypeScript SDK Return Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewEmailV3` | When a new email arrives (V3) | batch | `Office365OnNewEmailTriggerPayload` | `ClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `OnFlaggedEmailV4` | When an email is flagged (V4) | batch | `Office365OnFlaggedEmailTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `OnNewMentionMeEmailV3` | When a new email mentioning me arrives (V3) | batch | `Office365OnNewEmailMentioningMeTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `SharedMailboxOnNewEmailV2` | When a new email arrives in a shared mailbox (V2) | batch | `Office365OnSharedMailboxNewEmailTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |

### Calendar Triggers

| Operation ID | Description | Type | .NET Payload Type | Python SDK Type | TypeScript SDK Return Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `CalendarGetOnChangedItemsV3` | When an event is added, updated or deleted (V3) | batch | `Office365OnCalendarChangedItemsTriggerPayload` | `GraphCalendarEventClientWithActionType` | `GraphCalendarEventListWithActionType` |
| `CalendarGetOnNewItemsV3` | When a new event is created (V3) | batch | `Office365OnCalendarNewItemsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |
| `CalendarGetOnUpdatedItemsV3` | When an event is modified (V3) | batch | `Office365OnCalendarUpdatedItemsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |
| `OnUpcomingEventsV3` | When an upcoming event is starting soon (V3) | batch | `Office365OnUpcomingEventsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |

### O365 Azure Functions Signatures

#### O365 .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Office365.Models;

// OnNewEmailV3
[Function("OnNewEmail")]
public string OnNewEmail(
    [ConnectorTrigger]
    Office365OnNewEmailTriggerPayload payload)
{
    return System.Text.Json.JsonSerializer.Serialize(payload);
}

// OnFlaggedEmailV4
[Function("OnFlaggedEmail")]
public void OnFlaggedEmail(
    [ConnectorTrigger]
    Office365OnFlaggedEmailTriggerPayload payload) { }

// CalendarGetOnChangedItemsV3
[Function("OnCalendarChangedItems")]
public void OnCalendarChangedItems(
    [ConnectorTrigger]
    Office365OnCalendarChangedItemsTriggerPayload payload) { }

// CalendarGetOnNewItemsV3
[Function("OnCalendarNewItems")]
public void OnCalendarNewItems(
    [ConnectorTrigger]
    Office365OnCalendarNewItemsTriggerPayload payload) { }
```

#### O365 Python

```python
import azure.functions as func
import azurefunctions.extensions.connectors.office365 as office365
from typing import List
import json
import logging

app = func.FunctionApp()

# OnNewEmailV3 — Uses typed ClientReceiveMessage payload
@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="emails")
def on_new_email(emails: List[office365.ClientReceiveMessage]) -> None:
    for email in emails:
        logging.info(f"Subject: {email.subject}, From: {email.from_}")
```

For all other O365 triggers (or when typed payload is not available), use connector trigger with `string` payload:

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

# OnFlaggedEmailV4 — string payload
@app.function_name(name="OnFlaggedEmail")
@app.connector_trigger(arg_name="payload")
def on_flagged_email(payload: str) -> None:
    logging.info("OnFlaggedEmail trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for email in items:
        logging.info(f"Subject: {email.get('subject')}")
        logging.info(f"From: {email.get('from')}")
```

#### O365 TypeScript

```typescript
import { InvocationContext } from '@azure/functions';
import { connectors, EmailTriggerContext } from '@azure/functions-extensions-connectors';

// OnNewEmailV3
connectors.office365.onNewEmail('OnNewEmail', {
    handler: async (context: EmailTriggerContext, invocationContext: InvocationContext) => {
        for (const email of context.emails) {
            invocationContext.log(`Subject: '${email.subject}', From: '${email.from}'.`);
        }
    },
});

// OnFlaggedEmailV4
connectors.office365.onFlaggedEmail('OnFlaggedEmail', {
    handler: async (context: EmailTriggerContext, invocationContext: InvocationContext) => {
        for (const email of context.emails) {
            invocationContext.log(`Flagged: '${email.subject}'.`);
        }
    },
});

// CalendarGetOnChangedItemsV3
connectors.office365.onCalendarChangedItems('OnCalendarChangedItems', {
    handler: async (context, invocationContext: InvocationContext) => {
        for (const event of context.items) {
            invocationContext.log(`Event changed: '${event.subject}', action: '${event.ActionType}'.`);
        }
    },
});

// CalendarGetOnNewItemsV3
connectors.office365.onCalendarNewItems('OnCalendarNewItems', {
    handler: async (context, invocationContext: InvocationContext) => {
        for (const event of context.items) {
            invocationContext.log(`New event: '${event.subject}'.`);
        }
    },
});
```

---

## SharePoint Online Connector

### SharePoint Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems` | When an item is created | batch | `SharePointOnlineOnNewItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnUpdatedItems` | When an item is created or modified | batch | `SharePointOnlineOnUpdatedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnChangedItems` | When an item or a file is modified | batch | `SharePointOnlineOnChangedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnNewFileItems` | When a file is created (properties only) | batch | `SharePointOnlineOnNewFileItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnUpdatedFileItems` | When a file is created or modified (properties only) | batch | `SharePointOnlineOnUpdatedFileItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnDeletedItems` | When an item is deleted | batch | `SharePointOnlineOnDeletedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnDeletedFileItems` | When a file is deleted | batch | `SharePointOnlineOnDeletedFileItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnUpdatedFileClassifiedTimes` | When a file is classified by a Microsoft Syntex model | batch | `SharePointOnlineOnUpdatedFileClassifiedTimesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnNewItemsFromForm` | When a form is submitted (preview) | batch | — | `str` (raw JSON) | `unknown` (raw JSON) |

### SharePoint Azure Functions Signatures

#### SharePoint .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.SharePointOnline.Models;

// GetOnNewItems
[Function("OnNewSharePointItem")]
public void OnNewSharePointItem(
    [ConnectorTrigger]
    SharePointOnlineOnNewItemsTriggerPayload payload) { }

// GetOnUpdatedFileItems
[Function("OnUpdatedSharePointFile")]
public void OnUpdatedSharePointFile(
    [ConnectorTrigger]
    SharePointOnlineOnUpdatedFileItemsTriggerPayload payload) { }

// GetOnDeletedItems
[Function("OnDeletedSharePointItem")]
public void OnDeletedSharePointItem(
    [ConnectorTrigger]
    SharePointOnlineOnDeletedItemsTriggerPayload payload) { }
```

#### SharePoint Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewSharePointItem")
@app.connector_trigger(arg_name="payload")
def on_new_sharepoint_item(payload: str) -> None:
    logging.info("OnNewSharePointItem trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for item in items:
        logging.info(f"Item ID: {item.get('Id')}")
```

#### SharePoint TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewSharePointItem', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewSharePointItem trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const item of items) {
            context.log(`Item ID: ${item.Id}`);
        }
    },
});
```

---

## Microsoft Teams Connector

### Teams Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `WebhookChatMessageTrigger` | When a new chat message is added | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebhookNewMessageTrigger` | When a new message is added to a chat or channel | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebhookAtMentionTrigger` | When I'm @mentioned | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebhookKeywordTrigger` | When keywords are mentioned | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebhookMessageReactionTrigger` | When someone reacted to a message in chat | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewChannelMessage` | When a new channel message is added | batch | `TeamsOnNewChannelMessageTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewChannelMessageMentioningMe` | When I am mentioned in a channel message | batch | `TeamsOnNewChannelMessageMentioningMeTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnGroupMembershipAdd` | When a new team member is added | batch | `TeamsOnGroupMembershipAddTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnGroupMembershipRemoval` | When a new team member is removed | batch | `TeamsOnGroupMembershipRemovalTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `RecordingTrigger` | When a recording is available | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `TranscriptTrigger` | When a transcript is available | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

### Teams Azure Functions Signatures

#### Teams .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Teams.Models;

// When a new channel message is added
[Function("OnNewChannelMessage")]
public void OnNewChannelMessage(
    [ConnectorTrigger]
    TeamsOnNewChannelMessageTriggerPayload payload) { }

// When a new team member is added
[Function("OnGroupMembershipAdd")]
public void OnGroupMembershipAdd(
    [ConnectorTrigger]
    TeamsOnGroupMembershipAddTriggerPayload payload) { }
```

#### Teams Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewChannelMessage")
@app.connector_trigger(arg_name="payload")
def on_new_channel_message(payload: str) -> None:
    logging.info("OnNewChannelMessage trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for message in items:
        logging.info(f"From: {message.get('from')}")
```

#### Teams TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewChannelMessage', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewChannelMessage trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const message of items) {
            context.log(`From: ${message.from}`);
        }
    },
});
```

---

## OneDrive for Business Connector

### OneDrive Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFileV2` | When a file is created | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewFilesV2` | When a file is created (properties only) | batch | `OneDriveForBusinessOnNewFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFileV2` | When a file is modified | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFilesV2` | When a file is modified (properties only) | batch | `OneDriveForBusinessOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

### OneDrive Azure Functions Signatures

#### OneDrive .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.OneDriveForBusiness.Models;

[Function("OnNewOneDriveFile")]
public void OnNewOneDriveFile(
    [ConnectorTrigger]
    OneDriveForBusinessOnNewFileTriggerPayload payload) { }
```

#### OneDrive Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewOneDriveFile")
@app.connector_trigger(arg_name="payload")
def on_new_onedrive_file(payload: str) -> None:
    logging.info("OnNewOneDriveFile trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for file in items:
        logging.info(f"File: {file.get('name')}, Size: {file.get('size')} bytes")
```

#### OneDrive TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewOneDriveFile', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewOneDriveFile trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const file of items) {
            context.log(`File: ${file.name}, Size: ${file.size} bytes`);
        }
    },
});
```

---

## Azure Blob Storage Connector

### Blob Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnUpdatedFiles_V2` | When a blob is added or modified (properties only) (V2) | batch | `AzureBlobOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

### Blob Azure Functions Signatures

#### Blob .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.AzureBlob.Models;

[Function("OnBlobUpdated")]
public void OnBlobUpdated(
    [ConnectorTrigger]
    AzureBlobOnUpdatedFilesTriggerPayload payload) { }
```

#### Blob Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnBlobUpdated")
@app.connector_trigger(arg_name="payload")
def on_blob_updated(payload: str) -> None:
    logging.info("OnBlobUpdated trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for blob in items:
        logging.info(f"Blob: {blob.get('Name')}, Path: {blob.get('Path')}")
```

#### Blob TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnBlobUpdated', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnBlobUpdated trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const blob of items) {
            context.log(`Blob: ${blob.Name}, Path: ${blob.Path}`);
        }
    },
});
```

---

## Event Hubs Connector

### EventHubs Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewEvents` | When events are available in Event Hub | batch | `EventHubsOnNewEventsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

### EventHubs Azure Functions Signatures

#### EventHubs .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Eventhubs.Models;

[Function("OnNewEvents")]
public void OnNewEvents(
    [ConnectorTrigger]
    EventHubsOnNewEventsTriggerPayload payload) { }
```

#### EventHubs Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewEvents")
@app.connector_trigger(arg_name="payload")
def on_new_events(payload: str) -> None:
    logging.info("OnNewEvents trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for event in items:
        logging.info(f"Event: {event}")
```

#### EventHubs TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewEvents', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewEvents trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const event of items) {
            context.log(`Event: ${JSON.stringify(event)}`);
        }
    },
});
```

---

## Service Bus Connector

### ServiceBus Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `GetMessageFromQueue` | When a message is received in a queue (auto-complete) | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetNewMessageFromQueueWithPeekLock` | When a message is received in a queue (peek-lock) | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetMessageFromTopic` | When a message is received in a topic subscription (auto-complete) | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetNewMessageFromTopicWithPeekLock` | When a message is received in a topic subscription (peek-lock) | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetMessagesFromQueue` | When one or more messages arrive in a queue (auto-complete) | batch | `ServicebusOnGetMessagesFromQueueTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetMessagesFromTopic` | When one or more messages arrive in a topic (auto-complete) | batch | `ServicebusOnGetMessagesFromTopicTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetNewMessagesFromQueueWithPeekLock` | When one or more messages arrive in a queue (peek-lock) | batch | `ServicebusOnGetNewMessagesFromQueueWithPeekLockTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetNewMessagesFromTopicWithPeekLock` | When one or more messages arrive in a topic (peek-lock) | batch | `ServicebusOnGetNewMessagesFromTopicWithPeekLockTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

> Single-message triggers have no typed payload — use `string` in .NET, `str` in Python, or `unknown` in TypeScript to receive raw JSON.

### ServiceBus Azure Functions Signatures

#### ServiceBus .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Servicebus.Models;

// Batch - queue (auto-complete)
[Function("OnQueueMessages")]
public void OnQueueMessages(
    [ConnectorTrigger]
    ServicebusOnGetMessagesFromQueueTriggerPayload payload) { }

// Batch - queue (peek-lock)
[Function("OnQueueMessagesPeekLock")]
public void OnQueueMessagesPeekLock(
    [ConnectorTrigger]
    ServicebusOnGetNewMessagesFromQueueWithPeekLockTriggerPayload payload) { }

// Single message - no typed payload, receive as string
[Function("OnQueueMessage")]
public void OnQueueMessage(
    [ConnectorTrigger]
    string payload)
{
    var json = System.Text.Json.JsonDocument.Parse(payload);
}
```

#### ServiceBus Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnQueueMessages")
@app.connector_trigger(arg_name="payload")
def on_queue_messages(payload: str) -> None:
    logging.info("OnQueueMessages trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for message in items:
        logging.info(f"Message: {message}")
```

#### ServiceBus TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnQueueMessages', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnQueueMessages trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const message of items) {
            context.log(`Message: ${JSON.stringify(message)}`);
        }
    },
});
```

---

## Outlook Connector

### Outlook Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewEmailV2` | When a new email arrives (V2) | batch | `OutlookOnNewEmailTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnFlaggedEmailV2` | When an email is flagged (V2) | batch | `OutlookOnFlaggedEmailTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewMentionMeEmailV2` | When a new email mentioning me arrives (V2) | batch | `OutlookOnNewMentionMeEmailTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `CalendarGetOnChangedItemsV2` | When an event is added, updated or deleted (V2) | batch | `OutlookOnCalendarGetOnChangedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `CalendarGetOnNewItemsV2` | When a new event is created (V2) | batch | `OutlookOnCalendarGetOnNewItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `CalendarGetOnUpdatedItemsV2` | When an event is modified (V2) | batch | `OutlookOnCalendarGetOnUpdatedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpcomingEventsV2` | When an upcoming event is starting soon (V2) | batch | `OutlookOnUpcomingEventsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

### Outlook Azure Functions Signatures

#### Outlook .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Outlook.Models;

[Function("OnNewOutlookEmail")]
public void OnNewOutlookEmail(
    [ConnectorTrigger]
    OutlookOnNewEmailTriggerPayload payload) { }

[Function("OnOutlookCalendarChanged")]
public void OnOutlookCalendarChanged(
    [ConnectorTrigger]
    OutlookOnCalendarGetOnChangedItemsTriggerPayload payload) { }
```

#### Outlook Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewOutlookEmail")
@app.connector_trigger(arg_name="payload")
def on_new_outlook_email(payload: str) -> None:
    logging.info("OnNewOutlookEmail trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for email in items:
        logging.info(f"Subject: {email.get('Subject')}")
        logging.info(f"From: {email.get('From')}")
```

#### Outlook TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewOutlookEmail', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewOutlookEmail trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const email of items) {
            context.log(`Subject: ${email.Subject}`);
            context.log(`From: ${email.From}`);
        }
    },
});
```

---

## SQL Connector

### SQL Triggers

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems_V2` | When an item is created (V2) | batch | `SqlOnNewItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnUpdatedItems_V2` | When an item is modified (V2) | batch | `SqlOnUpdatedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

### SQL Azure Functions Signatures

#### SQL .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.Sql.Models;

[Function("OnNewSqlItem")]
public void OnNewSqlItem(
    [ConnectorTrigger]
    SqlOnNewItemsTriggerPayload payload) { }

[Function("OnUpdatedSqlItem")]
public void OnUpdatedSqlItem(
    [ConnectorTrigger]
    SqlOnUpdatedItemsTriggerPayload payload) { }
```

#### SQL Python

```python
import azure.functions as func
import json
import logging

app = func.FunctionApp()

@app.function_name(name="OnNewSqlItem")
@app.connector_trigger(arg_name="payload")
def on_new_sql_item(payload: str) -> None:
    logging.info("OnNewSqlItem trigger received")
    data = json.loads(payload)
    items = data.get("body", {}).get("value", [])
    for item in items:
        logging.info(f"Item: {item}")
```

#### SQL TypeScript

```typescript
import { app, InvocationContext } from '@azure/functions';

app.generic('OnNewSqlItem', {
    trigger: { type: 'connectorTrigger', name: 'payload' },
    handler: async (payload: unknown, context: InvocationContext) => {
        context.log('OnNewSqlItem trigger received');
        const data = typeof payload === 'string' ? JSON.parse(payload) : payload;
        const items: Record<string, unknown>[] = (data as any)?.body?.value ?? [];
        for (const item of items) {
            context.log(`Item: ${JSON.stringify(item)}`);
        }
    },
});
```

---

## FTP Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnUpdatedFiles` | When a file is added or modified (properties only) | batch | `FtpOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Dropbox Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFile` | When a file is created | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewFiles` | When a file is created (properties only) | batch | `DropboxOnNewFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFile` | When a file is modified | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFiles` | When a file is modified (properties only) | batch | `DropboxOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## OneDrive (Personal) Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFileV2` | When a file is created | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewFilesV2` | When a file is created (properties only) | batch | `OneDriveOnNewFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnDeletedFiles` | When a file is deleted (properties only) | batch | `OneDriveOnDeletedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFilesV2` | When a file is modified (properties only) | batch | `OneDriveOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFileV2` | When a file is modified | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Salesforce Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems` | When a record is created | batch | `SalesforceOnNewItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `GetOnUpdatedItems` | When a record is modified | batch | `SalesforceOnUpdatedItemsTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## GitHub Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `IssueOpened` | When a new issue is opened and assigned to me | batch | `GitHubOnIssueOpenedTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `IssueClosed` | When an issue assigned to me is closed | batch | `GitHubOnIssueClosedTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `IssueAssigned` | When an issue is assigned to me | batch | `GitHubOnIssueAssignedTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebhookPullRequestTrigger` | When a pull request is created or modified | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Twitter Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewTweet` | When a new tweet is posted | batch | `TwitterOnNewTweetTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## RSS Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFeed` | When a feed item is published | batch | `RssOnNewFeedTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Box Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFilesV2` | When a file is created (properties only) (V2) | batch | `BoxOnNewFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFiles` | When a file is modified (properties only) | batch | — | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedFilesV2` | When a file is modified (properties only) (V2) | batch | `BoxOnUpdatedFilesTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Slack Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewFile` | When a file is created | batch | `SlackOnNewFileTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Azure Queues Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnMessages_V2` | When there are messages in a queue (V2) | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnMessageThresholdReached_V2` | When a specified number of messages are in a given queue (V2) | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Azure Event Grid Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `CreateSubscription` | When a resource event occurs | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Yammer (Viva Engage) Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewMessagesInGroupV2` | When there is a new message in a group (V2) | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnNewMessagesFollowingV2` | When there is a new message in my followed feed (V2) | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Microsoft Defender ATP (WDATP) Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewRemediationActivity` | When a new remediation activity is created (Preview) | batch | `WdatpOnNewRemediationActivityTriggerPayload` | `str` (raw JSON) | `unknown` (raw JSON) |
| `WebHooks_CreateWebHook` | When a new WDATP alert occurs | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Google Calendar Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `OnNewEventInCalendar` | When an event is added to a calendar | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnChangedEventInCalendar` | When an event is added, updated or deleted from a calendar | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnDeletedEventInCalendar` | When an event is deleted from a calendar | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnUpdatedEventInCalendar` | When an event is updated in a calendar | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |
| `OnEventStarted` | When an event starts | batch | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Dynamics AX Connector

| Operation ID | Description | Type | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- | ----- |
| `SubscribeOnABusinessEvent` | When a Business Event occurs | single | `string` (raw JSON) | `str` (raw JSON) | `unknown` (raw JSON) |

---

## Source References

- **.NET SDK**: [Azure/Connectors-NET-SDK](https://github.com/Azure/Connectors-NET-SDK) — `src/Azure.Connectors.Sdk/Generated/`
- **Python SDK**: [Azure/Connectors-python-SDK](https://github.com/Azure/Connectors-python-SDK) — `src/azure/connectors/`
- **TypeScript SDK**: [Azure/Connectors-nodejs-SDK](https://github.com/Azure/Connectors-nodejs-SDK) — `src/generated/`
- **Azure Managed APIs**: `GET /subscriptions/{id}/providers/Microsoft.Web/locations/{location}/managedApis/{connector}/apiOperations?api-version=2016-06-01`
