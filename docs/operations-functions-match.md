# Operations to Azure Functions Signature Mapping

This document maps connector trigger operation names to their corresponding Azure Functions signatures across .NET, Python, and TypeScript SDKs.

## Python: Choosing a Decorator

| Python Version | Decorator | Package Requirement | Required App Settings |
| --- | --- | --- |  --- |
| 3.13+ | `@app.connector_trigger` | `azure-functions>=2.2.0b4` | - |
| < 3.13 | `@app.connector_trigger` | `azure-functions>=1.26.0b3` | `PYTHON_ISOLATE_WORKER_DEPENDENCIES=1` |

## Python: Choosing Packages by Trigger Operation

| Scenario | Extra Package(s) | Parameter Type |
| --- | --- | --- |
| Office 365 `OnNewEmail` | `azurefunctions-extensions-connectors` (pulls in `azure-connectors`) | Typed class (`ClientReceiveMessage`) |
| Other connector with a typed SDK model (see Python Type column) | `azure-connectors` | Typed class |
| No typed model available | None beyond `azure-functions` | `str` |

> [!NOTE]
> **Typed Payloads:** The **.NET Payload Type**, **Python Type**, and **TypeScript Type** columns list the strongly-typed model each SDK generates for the operation. Coverage differs per language and the type names are not identical across SDKs.
>
> When a cell shows the **default raw-JSON binding type** instead of a model class, that SDK has no typed model for the operation — bind the trigger payload as that type and parse the JSON manually. The default per language is:
>
> - **.NET:** `string`
> - **Python:** `str`
> - **TypeScript:** `unknown`
>
> **Trigger Types:** The SDKs always deliver trigger items as a list. When the Connector Namespace callback contains a single item, the list contains one element, so your handler can iterate uniformly in all cases.

## Table of Contents

- [Azure Blob Storage Connector](#azure-blob-storage-connector)
- [Azure Event Grid Connector](#azure-event-grid-connector)
- [Azure IoT Central Connector](#azure-iot-central-connector)
- [Azure Queues Connector](#azure-queues-connector)
- [Box Connector](#box-connector)
- [Campfire Connector](#campfire-connector)
- [ClickSend SMS Connector](#clicksend-sms-connector)
- [Common Data Service (Dataverse) Connector](#common-data-service-dataverse-connector)
- [DocuSign Connector](#docusign-connector)
- [Dropbox Connector](#dropbox-connector)
- [Dynamics AX Connector](#dynamics-ax-connector)
- [Elfsquad Connector](#elfsquad-connector)
- [Event Hubs Connector](#event-hubs-connector)
- [Eventbrite Connector](#eventbrite-connector)
- [Formstack Forms Connector](#formstack-forms-connector)
- [FreshService Connector](#freshservice-connector)
- [FTP Connector](#ftp-connector)
- [GitHub Connector](#github-connector)
- [Google Calendar Connector](#google-calendar-connector)
- [Google Tasks Connector](#google-tasks-connector)
- [Impexium Connector](#impexium-connector)
- [Infusionsoft Connector](#infusionsoft-connector)
- [Insightly Connector](#insightly-connector)
- [Jira Connector](#jira-connector)
- [Mailchimp Connector](#mailchimp-connector)
- [Microsoft Bookings Connector](#microsoft-bookings-connector)
- [Microsoft Defender ATP (WDATP) Connector](#microsoft-defender-atp-wdatp-connector)
- [Microsoft Forms Connector](#microsoft-forms-connector)
- [Microsoft Teams Connector](#microsoft-teams-connector)
- [Microsoft To Do Connector](#microsoft-to-do-connector)
- [Monday.com Connector](#mondaycom-connector)
- [Office 365 Connector](#office-365-connector)
- [Office 365 Groups Connector](#office-365-groups-connector)
- [Office 365 Groups Mail Connector](#office-365-groups-mail-connector)
- [OneDrive (Personal) Connector](#onedrive-personal-connector)
- [OneDrive for Business Connector](#onedrive-for-business-connector)
- [OneNote Connector](#onenote-connector)
- [Orderful Connector](#orderful-connector)
- [Outlook Connector](#outlook-connector)
- [Pipedrive Connector](#pipedrive-connector)
- [Planner Connector](#planner-connector)
- [Plumsail Connector](#plumsail-connector)
- [Power BI Connector](#power-bi-connector)
- [Projectplace Connector](#projectplace-connector)
- [Replicon Connector](#replicon-connector)
- [RSS Connector](#rss-connector)
- [Salesforce Connector](#salesforce-connector)
- [Service Bus Connector](#service-bus-connector)
- [SharePoint Online Connector](#sharepoint-online-connector)
- [Shifts Connector](#shifts-connector)
- [Slack Connector](#slack-connector)
- [SQL Connector](#sql-connector)
- [Text Request Connector](#text-request-connector)
- [Trello Connector](#trello-connector)
- [Twitter Connector](#twitter-connector)
- [Typeform Connector](#typeform-connector)
- [Way We Do Connector](#way-we-do-connector)
- [Webex Connector](#webex-connector)
- [WordPress Connector](#wordpress-connector)
- [Yammer (Viva Engage) Connector](#yammer-viva-engage-connector)
- [Zendesk Connector](#zendesk-connector)
- [Zoho Sign Connector](#zoho-sign-connector)
- [Source References](#source-references)

---

## Azure Blob Storage Connector

_📖 Connector reference: [learn.microsoft.com/connectors/azureblob](https://learn.microsoft.com/connectors/azureblob/)_

### Blob Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnUpdatedFiles_V2` | When a blob is added or modified (properties only) (V2) | `AzureBlobOnUpdatedFilesTriggerPayload` | `str` | `unknown` |

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

## Azure Event Grid Connector

_📖 Connector reference: [learn.microsoft.com/connectors/azureeventgrid](https://learn.microsoft.com/connectors/azureeventgrid/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `CreateSubscription` | When a resource event occurs | `string` | `str` | `unknown` |

---

## Azure IoT Central Connector

_📖 Connector reference: [learn.microsoft.com/connectors/azureiotcentral](https://learn.microsoft.com/connectors/azureiotcentral/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `Workflow_CreateTrigger` | When a rule is fired | `string` | `str` | `unknown` |

---

## Azure Queues Connector

_📖 Connector reference: [learn.microsoft.com/connectors/azurequeues](https://learn.microsoft.com/connectors/azurequeues/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnMessages_V2` | When there are messages in a queue (V2) | `string` | `str` | `unknown` |
| `OnMessageThresholdReached_V2` | When a specified number of messages are in a given queue (V2) | `string` | `str` | `unknown` |

---

## Box Connector

_📖 Connector reference: [learn.microsoft.com/connectors/box](https://learn.microsoft.com/connectors/box/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFilesV2` | When a file is created (properties only) (V2) | `BoxOnNewFilesTriggerPayload` | `str` | `unknown` |
| `OnUpdatedFiles` | When a file is modified (properties only) | `string` | `str` | `unknown` |
| `OnUpdatedFilesV2` | When a file is modified (properties only) (V2) | `BoxOnUpdatedFilesTriggerPayload` | `str` | `unknown` |

---

## Campfire Connector

_📖 Connector reference: [learn.microsoft.com/connectors/campfire](https://learn.microsoft.com/connectors/campfire/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewRoom` | When a room is created | `string` | `str` | `unknown` |
| `OnNewMessage` | When a new message is received | `string` | `str` | `unknown` |
| `OnNewUpload` | When a file is uploaded | `string` | `str` | `unknown` |

---

## ClickSend SMS Connector

_📖 Connector reference: [learn.microsoft.com/connectors/clicksendsms](https://learn.microsoft.com/connectors/clicksendsms/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `sms_inbound_automation` | SMS Inbound Automation | `string` | `str` | `unknown` |

---

## Common Data Service (Dataverse) Connector

_📖 Connector reference: [learn.microsoft.com/connectors/commondataserviceforapps](https://learn.microsoft.com/connectors/commondataserviceforapps/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `SubscribeWebhookTrigger` | When a row is added, modified or deleted | `string` | `str` | `unknown` |
| `BusinessEventsTrigger` | When an action is performed | `string` | `str` | `unknown` |

---

## DocuSign Connector

_📖 Connector reference: [learn.microsoft.com/connectors/docusign](https://learn.microsoft.com/connectors/docusign/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `CreateHookEnvelopeV3` | When an envelope status changes (Connect) (V3) | `string` | `str` | `unknown` |

---

## Dropbox Connector

_📖 Connector reference: [learn.microsoft.com/connectors/dropbox](https://learn.microsoft.com/connectors/dropbox/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFile` | When a file is created | `string` | `str` | `unknown` |
| `OnNewFiles` | When a file is created (properties only) | `DropboxOnNewFilesTriggerPayload` | `str` | `unknown` |
| `OnUpdatedFile` | When a file is modified | `string` | `str` | `unknown` |
| `OnUpdatedFiles` | When a file is modified (properties only) | `DropboxOnUpdatedFilesTriggerPayload` | `str` | `unknown` |

---

## Dynamics AX Connector

_📖 Connector reference: [learn.microsoft.com/connectors/dynamicsax](https://learn.microsoft.com/connectors/dynamicsax/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `SubscribeOnABusinessEvent` | When a Business Event occurs | `string` | `str` | `unknown` |

---

## Elfsquad Connector

_📖 Connector reference: [learn.microsoft.com/connectors/elfsquaddata](https://learn.microsoft.com/connectors/elfsquaddata/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `create_trigger` | Custom trigger | `string` | `str` | `unknown` |

---

## Event Hubs Connector

_📖 Connector reference: [learn.microsoft.com/connectors/eventhubs](https://learn.microsoft.com/connectors/eventhubs/)_

### EventHubs Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEvents` | When events are available in Event Hub | `EventHubsOnNewEventsTriggerPayload` | `str` | `unknown` |

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

## Eventbrite Connector

_📖 Connector reference: [learn.microsoft.com/connectors/eventbrite](https://learn.microsoft.com/connectors/eventbrite/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEventV2` | When an event is created (V2) | `EventbriteOnNewEventTriggerPayload` | `str` | `unknown` |
| `OnOrderChangedV2` | When an order changes (V2) | `EventbriteOnOrderChangedTriggerPayload` | `str` | `unknown` |

---

## Formstack Forms Connector

_📖 Connector reference: [learn.microsoft.com/connectors/formstackforms](https://learn.microsoft.com/connectors/formstackforms/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `FormstackFormSubmitted` | Triggers when a form is submitted | `string` | `str` | `unknown` |

---

## FreshService Connector

_📖 Connector reference: [learn.microsoft.com/connectors/freshservice](https://learn.microsoft.com/connectors/freshservice/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnTicketCreatedV2` | When a ticket is created (V2) | `FreshServiceOnTicketCreatedTriggerPayload` | `str` | `unknown` |

---

## FTP Connector

_📖 Connector reference: [learn.microsoft.com/connectors/ftp](https://learn.microsoft.com/connectors/ftp/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnUpdatedFiles` | When a file is added or modified (properties only) | `FtpOnUpdatedFilesTriggerPayload` | `str` | `unknown` |

---

## GitHub Connector

_📖 Connector reference: [learn.microsoft.com/connectors/github](https://learn.microsoft.com/connectors/github/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `IssueOpened` | When a new issue is opened and assigned to me | `GitHubOnIssueOpenedTriggerPayload` | `str` | `unknown` |
| `IssueClosed` | When an issue assigned to me is closed | `GitHubOnIssueClosedTriggerPayload` | `str` | `unknown` |
| `IssueAssigned` | When an issue is assigned to me | `GitHubOnIssueAssignedTriggerPayload` | `str` | `unknown` |
| `WebhookPullRequestTrigger` | When a pull request is created or modified | `string` | `str` | `unknown` |

---

## Google Calendar Connector

_📖 Connector reference: [learn.microsoft.com/connectors/googlecalendar](https://learn.microsoft.com/connectors/googlecalendar/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEventInCalendar` | When an event is added to a calendar | `string` | `str` | `unknown` |
| `OnChangedEventInCalendar` | When an event is added, updated or deleted from a calendar | `string` | `str` | `unknown` |
| `OnDeletedEventInCalendar` | When an event is deleted from a calendar | `string` | `str` | `unknown` |
| `OnUpdatedEventInCalendar` | When an event is updated in a calendar | `string` | `str` | `unknown` |
| `OnEventStarted` | When an event starts | `string` | `str` | `unknown` |

---

## Google Tasks Connector

_📖 Connector reference: [learn.microsoft.com/connectors/googletasks](https://learn.microsoft.com/connectors/googletasks/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewTaskList` | When a new task list is created | `string` | `str` | `unknown` |
| `OnNewTaskInList` | When a task is added to a task list | `string` | `str` | `unknown` |
| `OnDueTaskInList` | When a task is due in a task list | `string` | `str` | `unknown` |
| `OnCompletedTaskInListV2` | When a task is completed in a task list (V2) | `string` | `str` | `unknown` |

---

## Impexium Connector

_📖 Connector reference: [learn.microsoft.com/connectors/impexium](https://learn.microsoft.com/connectors/impexium/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `When-Individual-Created` | When an individual is created | `string` | `str` | `unknown` |
| `When-Individual-Deleted` | When an individual is deleted | `string` | `str` | `unknown` |
| `When-Individual-Request-Forgotten` | When an individual requests to be forgotten | `string` | `str` | `unknown` |
| `When-product-purchased` | When a product is purchased | `string` | `str` | `unknown` |
| `When-Committee-Member-Updated` | When a committee member is updated | `string` | `str` | `unknown` |
| `When-purchase-cancelled` | When a purchase is canceled | `string` | `str` | `unknown` |
| `When-Request-Updated` | When a customer request is updated | `string` | `str` | `unknown` |
| `When-Email-Updated` | When a customer email is updated | `string` | `str` | `unknown` |
| `When-Customer-Custom-Field-Value-Updated` | When a customer custom field value is updated | `string` | `str` | `unknown` |
| `When-Customer-Is-Merged` | When a customer is merged | `string` | `str` | `unknown` |
| `When-Customer-Relationship-Updated` | When a customer relationship is updated | `string` | `str` | `unknown` |
| `When-Customer-Phone-Updated` | When a customer phone is updated | `string` | `str` | `unknown` |
| `When-Customer-Address-Updated` | When a customer address is updated | `string` | `str` | `unknown` |
| `When-Event-Registration-Substituted` | When an event registration is substituted | `string` | `str` | `unknown` |
| `When-Purchase-Paid` | When a purchase is paid | `string` | `str` | `unknown` |
| `When-Membership-Terminated` | When a membership is terminated | `string` | `str` | `unknown` |

---

## Infusionsoft Connector

_📖 Connector reference: [learn.microsoft.com/connectors/infusionsoft](https://learn.microsoft.com/connectors/infusionsoft/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewTask` | When a new task is created | `InfusionsoftOnNewTaskTriggerPayload` | `str` | `unknown` |
| `OnNewOrder` | When there is a new order | `InfusionsoftOnNewOrderTriggerPayload` | `str` | `unknown` |

---

## Insightly Connector

_📖 Connector reference: [learn.microsoft.com/connectors/insightly](https://learn.microsoft.com/connectors/insightly/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnTaskAssignedToMe` | When a task is assigned to me | `string` | `str` | `unknown` |
| `OnTaskCreated` | When a task is created | `string` | `str` | `unknown` |
| `OnTaskUpdated` | When a task is updated | `string` | `str` | `unknown` |
| `OnProjectCreated` | When a project is created | `string` | `str` | `unknown` |
| `OnProjectUpdated` | When a project is updated | `string` | `str` | `unknown` |
| `OnLeadCreated` | When a lead is created | `string` | `str` | `unknown` |
| `OnLeadUpdated` | When a lead is updated | `string` | `str` | `unknown` |
| `OnContactCreated` | When a contact is created | `string` | `str` | `unknown` |
| `OnContactUpdated` | When a contact is updated | `string` | `str` | `unknown` |
| `OnEventCreated` | When an event is created | `string` | `str` | `unknown` |
| `OnEventUpdated` | When an event is updated | `string` | `str` | `unknown` |

---

## Jira Connector

_📖 Connector reference: [learn.microsoft.com/connectors/jira](https://learn.microsoft.com/connectors/jira/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewIssue_V2` | When a new issue is created (V2) | `JiraOnNewIssueTriggerPayload` | `str` | `unknown` |
| `OnCloseIssue_V2` | When an issue is closed (V2) | `JiraOnCloseIssueTriggerPayload` | `str` | `unknown` |
| `OnNewIssueJQL_V2` | When a new issue is returned by a JQL query (V2) | `JiraOnNewIssueJQLTriggerPayload` | `str` | `unknown` |
| `OnNewIssue_Datacenter` | When a new issue is created (Datacenter) | `JiraOnNewIssueDatacenterTriggerPayload` | `str` | `unknown` |
| `OnCloseIssue_Datacenter` | When an issue is closed (Datacenter) | `JiraOnCloseIssueDatacenterTriggerPayload` | `str` | `unknown` |
| `OnNewIssueJQL_Datacenter` | When a new issue is returned by a JQL query (Datacenter) | `JiraOnNewIssueJQLDatacenterTriggerPayload` | `str` | `unknown` |

---

## Mailchimp Connector

_📖 Connector reference: [learn.microsoft.com/connectors/mailchimp](https://learn.microsoft.com/connectors/mailchimp/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnMemberSubscribed` | When a Member has been added to a list | `string` | `str` | `unknown` |
| `OnCreateList` | When a new list is created | `string` | `str` | `unknown` |

---

## Microsoft Bookings Connector

_📖 Connector reference: [learn.microsoft.com/connectors/microsoftbookings](https://learn.microsoft.com/connectors/microsoftbookings/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `CreateAppointment` | When an appointment is created | `string` | `str` | `unknown` |
| `UpdateAppointment` | When an appointment is updated | `string` | `str` | `unknown` |
| `CancelAppointment` | When an appointment is cancelled | `string` | `str` | `unknown` |

---

## Microsoft Defender ATP (WDATP) Connector

_📖 Connector reference: [learn.microsoft.com/connectors/wdatp](https://learn.microsoft.com/connectors/wdatp/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewRemediationActivity` | When a new remediation activity is created (Preview) | `WdatpOnNewRemediationActivityTriggerPayload` | `str` | `unknown` |
| `WebHooks_CreateWebHook` | When a new WDATP alert occurs | `string` | `str` | `unknown` |

---

## Microsoft Forms Connector

_📖 Connector reference: [learn.microsoft.com/connectors/microsoftforms](https://learn.microsoft.com/connectors/microsoftforms/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `CreateFormWebhook` | When a new response is submitted | `string` | `str` | `unknown` |

---

## Microsoft Teams Connector

_📖 Connector reference: [learn.microsoft.com/connectors/teams](https://learn.microsoft.com/connectors/teams/)_

### Teams Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `WebhookChatMessageTrigger` | When a new chat message is added | `string` | `str` | `unknown` |
| `WebhookNewMessageTrigger` | When a new message is added to a chat or channel | `string` | `str` | `unknown` |
| `WebhookAtMentionTrigger` | When I'm @mentioned | `string` | `str` | `unknown` |
| `WebhookKeywordTrigger` | When keywords are mentioned | `string` | `str` | `unknown` |
| `WebhookMessageReactionTrigger` | When someone reacted to a message in chat | `string` | `str` | `unknown` |
| `OnNewChannelMessage` | When a new channel message is added | `TeamsOnNewChannelMessageTriggerPayload` | `str` | `unknown` |
| `OnNewChannelMessageMentioningMe` | When I am mentioned in a channel message | `TeamsOnNewChannelMessageMentioningMeTriggerPayload` | `str` | `unknown` |
| `OnGroupMembershipAdd` | When a new team member is added | `TeamsOnTeamMemberAddedTriggerPayload` | `str` | `unknown` |
| `OnGroupMembershipRemoval` | When a new team member is removed | `TeamsOnTeamMemberRemovedTriggerPayload` | `str` | `unknown` |
| `RecordingTrigger` | When a recording is available | `string` | `str` | `unknown` |
| `TranscriptTrigger` | When a transcript is available | `string` | `str` | `unknown` |

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
    TeamsOnTeamMemberAddedTriggerPayload payload) { }
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

## Microsoft To Do Connector

_📖 Connector reference: [learn.microsoft.com/connectors/todo](https://learn.microsoft.com/connectors/todo/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewToDoInFolderV2` | When a new to-do in a specific folder is created (V2) | `TodoOnNewToDoInFolderTriggerPayload` | `str` | `unknown` |
| `OnUpdateToDoInFolderV2` | When a to-do in a specific folder is updated (V2) | `TodoOnUpdateToDoInFolderTriggerPayload` | `str` | `unknown` |

---

## Monday.com Connector

_📖 Connector reference: [learn.microsoft.com/connectors/monday](https://learn.microsoft.com/connectors/monday/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `WebhookCreateItem` | When an item is created | `string` | `str` | `unknown` |
| `WebhookCreateUpdate` | When a new update is posted | `string` | `str` | `unknown` |
| `WebhookChangeName` | When an item&apos;s name changes | `string` | `str` | `unknown` |
| `WebhookChangeSubitemName` | When a subitem&apos;s name changes | `string` | `str` | `unknown` |
| `WebhookCreateSubitem` | When a subitem is created | `string` | `str` | `unknown` |
| `WebhookColumnChanges` | When a column changes | `string` | `str` | `unknown` |
| `WebhookAnyColumnChanges` | When any column changes | `string` | `str` | `unknown` |
| `WebhookSubitemColumnChanges` | When any subitem column changes | `string` | `str` | `unknown` |

---

## Office 365 Connector

_📖 Connector reference: [learn.microsoft.com/connectors/office365](https://learn.microsoft.com/connectors/office365/)_

### Email Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEmailV3` | When a new email arrives (V3) | `Office365OnNewEmailTriggerPayload` | `ClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `OnFlaggedEmailV4` | When an email is flagged (V4) | `Office365OnFlaggedEmailTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `OnNewMentionMeEmailV3` | When a new email mentioning me arrives (V3) | `Office365OnNewEmailMentioningMeTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |
| `SharedMailboxOnNewEmailV2` | When a new email arrives in a shared mailbox (V2) | `Office365OnSharedMailboxNewEmailTriggerPayload` | `GraphClientReceiveMessage` | `TriggerBatchResponseGraphClientReceiveMessage` |

### Calendar Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `CalendarGetOnChangedItemsV3` | When an event is added, updated or deleted (V3) | `Office365OnCalendarChangedItemsTriggerPayload` | `GraphCalendarEventClientWithActionType` | `GraphCalendarEventListWithActionType` |
| `CalendarGetOnNewItemsV3` | When a new event is created (V3) | `Office365OnCalendarNewItemsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |
| `CalendarGetOnUpdatedItemsV3` | When an event is modified (V3) | `Office365OnCalendarUpdatedItemsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |
| `OnUpcomingEventsV3` | When an upcoming event is starting soon (V3) | `Office365OnUpcomingEventsTriggerPayload` | `GraphCalendarEventClientReceive` | `GraphCalendarEventListClientReceive` |

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

## Office 365 Groups Connector

_📖 Connector reference: [learn.microsoft.com/connectors/office365groups](https://learn.microsoft.com/connectors/office365groups/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnGroupMembershipChange` | When a group member is added or removed | `Office365GroupsOnGroupMembershipChangeTriggerPayload` | `str` | `unknown` |
| `OnNewEvent` | When there is a new event | `Office365GroupsOnNewEventTriggerPayload` | `str` | `unknown` |

---

## Office 365 Groups Mail Connector

_📖 Connector reference: [learn.microsoft.com/connectors/office365groupsmail](https://learn.microsoft.com/connectors/office365groupsmail/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEmailInGroup` | When a new email arrives to a group | `Office365GroupsMailOnNewEmailInGroupTriggerPayload` | `str` | `unknown` |

---

## OneDrive (Personal) Connector

_📖 Connector reference: [learn.microsoft.com/connectors/onedrive](https://learn.microsoft.com/connectors/onedrive/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFileV2` | When a file is created | `string` | `str` | `unknown` |
| `OnNewFilesV2` | When a file is created (properties only) | `OneDriveOnNewFilesTriggerPayload` | `str` | `unknown` |
| `OnDeletedFiles` | When a file is deleted (properties only) | `OneDriveOnDeletedFilesTriggerPayload` | `str` | `unknown` |
| `OnUpdatedFilesV2` | When a file is modified (properties only) | `OneDriveOnUpdatedFilesTriggerPayload` | `str` | `unknown` |
| `OnUpdatedFileV2` | When a file is modified | `string` | `str` | `unknown` |

---

## OneDrive for Business Connector

_📖 Connector reference: [learn.microsoft.com/connectors/onedriveforbusiness](https://learn.microsoft.com/connectors/onedriveforbusiness/)_

### OneDrive Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFileV2` | When a file is created | `string` | `str` | `unknown` |
| `OnNewFilesV2` | When a file is created (properties only) | `OneDriveForBusinessOnNewFilesTriggerPayload` | `str` | `unknown` |
| `OnUpdatedFileV2` | When a file is modified | `string` | `str` | `unknown` |
| `OnUpdatedFilesV2` | When a file is modified (properties only) | `OneDriveForBusinessOnUpdatedFilesTriggerPayload` | `str` | `unknown` |

### OneDrive Azure Functions Signatures

#### OneDrive .NET

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Connector;
using Azure.Connectors.Sdk.OneDriveForBusiness.Models;

[Function("OnNewOneDriveFile")]
public void OnNewOneDriveFile(
    [ConnectorTrigger]
    OneDriveForBusinessOnNewFilesTriggerPayload payload) { }
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

## OneNote Connector

_📖 Connector reference: [learn.microsoft.com/connectors/onenote](https://learn.microsoft.com/connectors/onenote/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewSectionInNotebook` | When a new section is created | `OnenoteOnNewSectionInNotebookTriggerPayload` | `str` | `unknown` |
| `OnNewSectionGroupInNotebook` | When a new section group is created | `OnenoteOnNewSectionGroupInNotebookTriggerPayload` | `str` | `unknown` |
| `OnNewPageInSection` | When a new page is created in a section | `OnenoteOnNewPageInSectionTriggerPayload` | `str` | `unknown` |

---

## Orderful Connector

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `Communication-Channel` | Listen to Communication Channel | `string` | `str` | `unknown` |

---

## Outlook Connector

_📖 Connector reference: [learn.microsoft.com/connectors/outlook](https://learn.microsoft.com/connectors/outlook/)_

### Outlook Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewEmailV2` | When a new email arrives (V2) | `OutlookOnNewEmailTriggerPayload` | `str` | `unknown` |
| `OnFlaggedEmailV2` | When an email is flagged (V2) | `OutlookOnFlaggedEmailTriggerPayload` | `str` | `unknown` |
| `OnNewMentionMeEmailV2` | When a new email mentioning me arrives (V2) | `OutlookOnNewMentionMeEmailTriggerPayload` | `str` | `unknown` |
| `CalendarGetOnChangedItemsV2` | When an event is added, updated or deleted (V2) | `OutlookOnCalendarGetOnChangedItemsTriggerPayload` | `str` | `unknown` |
| `CalendarGetOnNewItemsV2` | When a new event is created (V2) | `OutlookOnCalendarGetOnNewItemsTriggerPayload` | `str` | `unknown` |
| `CalendarGetOnUpdatedItemsV2` | When an event is modified (V2) | `OutlookOnCalendarGetOnUpdatedItemsTriggerPayload` | `str` | `unknown` |
| `OnUpcomingEventsV2` | When an upcoming event is starting soon (V2) | `OutlookOnUpcomingEventsTriggerPayload` | `str` | `unknown` |

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

## Pipedrive Connector

_📖 Connector reference: [learn.microsoft.com/connectors/pipedrive](https://learn.microsoft.com/connectors/pipedrive/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `TrigNewActivity` | When a new activity is added | `PipedriveOnTrigNewActivityTriggerPayload` | `str` | `unknown` |
| `TrigNewDealV2` | When a new deal is added (V2) | `PipedriveOnTrigNewDealTriggerPayload` | `str` | `unknown` |

---

## Planner Connector

_📖 Connector reference: [learn.microsoft.com/connectors/planner](https://learn.microsoft.com/connectors/planner/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnCompleteTask_V3` | When a task is completed | `PlannerOnCompleteTaskTriggerPayload` | `str` | `unknown` |
| `OnNewTask_V3` | When a new task is created | `PlannerOnNewTaskTriggerPayload` | `str` | `unknown` |
| `OnTaskAssignedToMe_V2` | When a task is assigned to me | `PlannerOnTaskAssignedToMeTriggerPayload` | `str` | `unknown` |

---

## Plumsail Connector

_📖 Connector reference: [learn.microsoft.com/connectors/plumsail](https://learn.microsoft.com/connectors/plumsail/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `FlowV1ProcessesFlowTriggersPost` | Process finished | `string` | `str` | `unknown` |

---

## Power BI Connector

_📖 Connector reference: [learn.microsoft.com/connectors/powerbi](https://learn.microsoft.com/connectors/powerbi/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GoalsAssignedTrigger` | When someone assigns a new owner to a goal | `string` | `str` | `unknown` |
| `GoalChangeTrigger` | When a goal changes | `string` | `str` | `unknown` |
| `GoalStatusChangeTrigger` | When status of a goal changes | `string` | `str` | `unknown` |
| `GoalValueChangeTrigger` | When current value of a goal changes | `string` | `str` | `unknown` |
| `GoalRefreshFailedTrigger` | When a data refresh for a goal fails | `string` | `str` | `unknown` |
| `GoalValueOrNoteUpsertTrigger` | When someone adds or edits a goal check-in | `string` | `str` | `unknown` |
| `CheckAlertStatus` | When a data driven alert is triggered | `string` | `str` | `unknown` |

---

## Projectplace Connector

_📖 Connector reference: [learn.microsoft.com/connectors/projectplace](https://learn.microsoft.com/connectors/projectplace/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `set_webhook_card_create` | When a card is created | `string` | `str` | `unknown` |
| `set_webhook_properties_change` | When a card&apos;s properties are changed | `string` | `str` | `unknown` |
| `set_webhook_card_due_date` | When a card is due | `string` | `str` | `unknown` |

---

## Replicon Connector

_📖 Connector reference: [learn.microsoft.com/connectors/replicon](https://learn.microsoft.com/connectors/replicon/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `WebhookSubscriptionsRestAPI` | Subscribe to Event triggers | `string` | `str` | `unknown` |

---

## RSS Connector

_📖 Connector reference: [learn.microsoft.com/connectors/rss](https://learn.microsoft.com/connectors/rss/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFeed` | When a feed item is published | `RssOnNewFeedTriggerPayload` | `str` | `unknown` |

---

## Salesforce Connector

_📖 Connector reference: [learn.microsoft.com/connectors/salesforce](https://learn.microsoft.com/connectors/salesforce/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems` | When a record is created | `SalesforceOnNewItemsTriggerPayload` | `str` | `unknown` |
| `GetOnUpdatedItems` | When a record is modified | `SalesforceOnUpdatedItemsTriggerPayload` | `str` | `unknown` |

---

## Service Bus Connector

_📖 Connector reference: [learn.microsoft.com/connectors/servicebus](https://learn.microsoft.com/connectors/servicebus/)_

### ServiceBus Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GetMessageFromQueue` | When a message is received in a queue (auto-complete) | `string` | `str` | `unknown` |
| `GetNewMessageFromQueueWithPeekLock` | When a message is received in a queue (peek-lock) | `string` | `str` | `unknown` |
| `GetMessageFromTopic` | When a message is received in a topic subscription (auto-complete) | `string` | `str` | `unknown` |
| `GetNewMessageFromTopicWithPeekLock` | When a message is received in a topic subscription (peek-lock) | `string` | `str` | `unknown` |
| `GetMessagesFromQueue` | When one or more messages arrive in a queue (auto-complete) | `ServicebusOnGetMessagesFromQueueTriggerPayload` | `str` | `unknown` |
| `GetMessagesFromTopic` | When one or more messages arrive in a topic (auto-complete) | `ServicebusOnGetMessagesFromTopicTriggerPayload` | `str` | `unknown` |
| `GetNewMessagesFromQueueWithPeekLock` | When one or more messages arrive in a queue (peek-lock) | `ServicebusOnGetNewMessagesFromQueueWithPeekLockTriggerPayload` | `str` | `unknown` |
| `GetNewMessagesFromTopicWithPeekLock` | When one or more messages arrive in a topic (peek-lock) | `ServicebusOnGetNewMessagesFromTopicWithPeekLockTriggerPayload` | `str` | `unknown` |

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

## SharePoint Online Connector

_📖 Connector reference: [learn.microsoft.com/connectors/sharepointonline](https://learn.microsoft.com/connectors/sharepointonline/)_

### SharePoint Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems` | When an item is created | `SharePointOnlineOnNewItemsTriggerPayload` | `str` | `unknown` |
| `GetOnUpdatedItems` | When an item is created or modified | `SharePointOnlineOnUpdatedItemsTriggerPayload` | `str` | `unknown` |
| `GetOnChangedItems` | When an item or a file is modified | `SharePointOnlineOnChangedItemsTriggerPayload` | `str` | `unknown` |
| `GetOnNewFileItems` | When a file is created (properties only) | `SharePointOnlineOnNewFileItemsTriggerPayload` | `str` | `unknown` |
| `GetOnUpdatedFileItems` | When a file is created or modified (properties only) | `SharePointOnlineOnUpdatedFileItemsTriggerPayload` | `str` | `unknown` |
| `GetOnDeletedItems` | When an item is deleted | `SharePointOnlineOnDeletedItemsTriggerPayload` | `str` | `unknown` |
| `GetOnDeletedFileItems` | When a file is deleted | `SharePointOnlineOnDeletedFileItemsTriggerPayload` | `str` | `unknown` |
| `GetOnUpdatedFileClassifiedTimes` | When a file is classified by a Microsoft Syntex model | `SharePointOnlineOnUpdatedFileClassifiedTimesTriggerPayload` | `str` | `unknown` |
| `GetOnNewItemsFromForm` | When a form is submitted (preview) | `SharePointOnlineOnNewItemsFromFormTriggerPayload` | `str` | `unknown` |

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

## Shifts Connector

_📖 Connector reference: [learn.microsoft.com/connectors/shifts](https://learn.microsoft.com/connectors/shifts/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `TriggerForOpenShiftChangeRequests` | When an Open Shift request is created, updated or deleted | `string` | `str` | `unknown` |
| `TriggerForSwapShiftsChangeRequests` | When a Swap Shifts request is created, updated or deleted | `string` | `str` | `unknown` |
| `TriggerForOfferShiftRequests` | When an Offer Shift request is created, updated or deleted | `string` | `str` | `unknown` |
| `TriggerForTimeOffRequests` | When a Time Off request is created, updated or deleted | `string` | `str` | `unknown` |
| `TriggerForShifts` | When a Shift is created, updated or deleted | `string` | `str` | `unknown` |

---

## Slack Connector

_📖 Connector reference: [learn.microsoft.com/connectors/slack](https://learn.microsoft.com/connectors/slack/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewFile` | When a file is created | `SlackOnNewFileTriggerPayload` | `str` | `unknown` |

---

## SQL Connector

_📖 Connector reference: [learn.microsoft.com/connectors/sql](https://learn.microsoft.com/connectors/sql/)_

### SQL Triggers

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GetOnNewItems_V2` | When an item is created (V2) | `SqlOnNewItemsTriggerPayload` | `str` | `unknown` |
| `GetOnUpdatedItems_V2` | When an item is modified (V2) | `SqlOnUpdatedItemsTriggerPayload` | `str` | `unknown` |

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

## Text Request Connector

_📖 Connector reference: [learn.microsoft.com/connectors/textrequest](https://learn.microsoft.com/connectors/textrequest/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `TextingWebhook` | Triggers when a text is sent or received | `string` | `str` | `unknown` |

---

## Trello Connector

_📖 Connector reference: [learn.microsoft.com/connectors/trello](https://learn.microsoft.com/connectors/trello/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewCardInBoardV3` | When a new card is added to a board (V3) | `TrelloOnNewCardInBoardTriggerPayload` | `str` | `unknown` |
| `OnNewCardInListV3` | When a new card is added to a list (V3) | `TrelloOnNewCardInListTriggerPayload` | `str` | `unknown` |

---

## Twitter Connector

_📖 Connector reference: [learn.microsoft.com/connectors/twitter](https://learn.microsoft.com/connectors/twitter/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewTweet` | When a new tweet is posted | `TwitterOnNewTweetTriggerPayload` | `str` | `unknown` |

---

## Typeform Connector

_📖 Connector reference: [learn.microsoft.com/connectors/typeform](https://learn.microsoft.com/connectors/typeform/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `NewResponseWebhook_V2` | When a response is submitted | `string` | `str` | `unknown` |

---

## Way We Do Connector

_📖 Connector reference: [learn.microsoft.com/connectors/waywedo](https://learn.microsoft.com/connectors/waywedo/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `Checklist_Create_WebHook` | When a checklist instance is started | `string` | `str` | `unknown` |
| `New_Comment_WebHook` | When a comment is added to a checklist | `string` | `str` | `unknown` |
| `Finish_Checklist_WebHook` | When a checklist is finished | `string` | `str` | `unknown` |
| `Invite_Supervisor_WebHook` | When a supervisor is invited | `string` | `str` | `unknown` |
| `Generate_Acceptance_PDF_WebHook` | When a procedure is accepted | `string` | `str` | `unknown` |
| `Checklist_Step_Completed_WebHook` | When a checklist step is completed | `string` | `str` | `unknown` |

---

## Webex Connector

_📖 Connector reference: [learn.microsoft.com/connectors/webex](https://learn.microsoft.com/connectors/webex/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `MembershipsUpdated` | When a membership is updated | `string` | `str` | `unknown` |
| `MembershipsDeleted` | When a membership is deleted | `string` | `str` | `unknown` |
| `MembershipsCreated` | When a membership is created | `string` | `str` | `unknown` |
| `MessagesCreated` | When a message is created | `string` | `str` | `unknown` |
| `MessagesDeleted` | When a message is deleted | `string` | `str` | `unknown` |
| `SpaceCreated` | When a space is created | `string` | `str` | `unknown` |
| `SpaceUpdated` | When a space is updated | `string` | `str` | `unknown` |

---

## WordPress Connector

_📖 Connector reference: [learn.microsoft.com/connectors/wordpress](https://learn.microsoft.com/connectors/wordpress/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnTriggerNewPost` | When a post is created | `string` | `str` | `unknown` |

---

## Yammer (Viva Engage) Connector

_📖 Connector reference: [learn.microsoft.com/connectors/yammer](https://learn.microsoft.com/connectors/yammer/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `OnNewMessagesInGroupV2` | When there is a new message in a group (V2) | `string` | `str` | `unknown` |
| `OnNewMessagesFollowingV2` | When there is a new message in my followed feed (V2) | `string` | `str` | `unknown` |

---

## Zendesk Connector

_📖 Connector reference: [learn.microsoft.com/connectors/zendesk](https://learn.microsoft.com/connectors/zendesk/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `GetOnUpdatedItemsV2` | When an item is modified | `ZendeskOnUpdatedItemsTriggerPayload` | `str` | `unknown` |

---

## Zoho Sign Connector

_📖 Connector reference: [learn.microsoft.com/connectors/zohosign](https://learn.microsoft.com/connectors/zohosign/)_

| Operation ID | Description | .NET Payload Type | Python Type | TypeScript Type |
| ----- | ----- | ----- | ----- | ----- |
| `zoho-sign-triggers` | Zoho Sign Triggers | `string` | `str` | `unknown` |

---

## Source References

- **.NET SDK**: [Azure/Connectors-NET-SDK](https://github.com/Azure/Connectors-NET-SDK) — `src/Azure.Connectors.Sdk/Generated/`
- **Python SDK**: [Azure/Connectors-python-SDK](https://github.com/Azure/Connectors-python-SDK) — `src/azure/connectors/`
- **TypeScript SDK**: [Azure/Connectors-nodejs-SDK](https://github.com/Azure/Connectors-nodejs-SDK) — `src/generated/`
- **Azure Managed APIs**: `GET /subscriptions/{id}/providers/Microsoft.Web/locations/{location}/managedApis/{connector}/apiOperations?api-version=2016-06-01`
