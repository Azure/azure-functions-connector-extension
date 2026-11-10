# Connector trigger payloads for Azure Functions

**Generated On:** 2026-11-10 07:57 UTC

Find the strongly typed .NET, Python, and TypeScript payloads for Connector Namespace triggers. The mappings reflect the latest package versions available in the checked feeds at the Generated On time.

## Table of contents

- [How to read the tables](#how-to-read-the-tables)
- [Top connectors with triggers](#top-connectors-with-triggers)
  - [Office 365 Outlook](#office-365-outlook)
  - [SharePoint Online](#sharepoint-online)
  - [OneDrive for Business](#onedrive-for-business)
  - [Microsoft Teams](#microsoft-teams)
  - [Microsoft Dataverse](#microsoft-dataverse)
  - [Planner](#planner)
  - [Salesforce](#salesforce)
  - [SAP](#sap)
  - [Azure DevOps](#azure-devops)
  - [Power Query Dataflows](#power-query-dataflows)
- [Other connectors](#other-connectors): [0–9](#09) · [A](#a) · [B](#b) · [C](#c) · [D](#d) · [E](#e) · [F](#f) · [G](#g) · [H](#h) · [I](#i) · [J](#j) · [K](#k) · [L](#l) · [M](#m) · [N](#n) · [O](#o) · [P](#p) · [Q](#q) · [R](#r) · [S](#s) · [T](#t) · [U](#u) · [V](#v) · [W](#w) · [X](#x) · [Y](#y) · [Z](#z)
- [Sources and versions](#sources-and-versions)

## How to read the tables

Find the trigger operation ID; linked IDs open the matching Microsoft Learn operation. Each language column shows its payload type; `—` means no operation-specific type was identified in the checked SDKs. Use the listed type when available, but remember that a typed envelope can contain runtime-shaped fields: values exposed as `JsonElement`, `AdditionalProperties`, `dynamic_properties`, `Record<string, unknown>`, or by a model named `Dynamic...` are not statically described. When no type is listed, start with raw input (`string` in .NET, `str` in Python, or `unknown` in TypeScript); use `JsonElement` in .NET when the input is JSON, and handle file content as content rather than JSON.

- **.NET:** The first type is the trigger callback payload; the item in parentheses is the item type inside its body. Types are from `Azure.Connectors.Sdk`.
- **Python:** Types are binding annotations from `azurefunctions-extensions-connectors`; the binding result is described in the row or connector note. Some converters unwrap a callback only when `body` is its sole top-level property. If a callback also has top-level metadata, such as `headers`, use raw input and parse the record inside `body`. For batched input, see the [Python sample](../test/poll/README.md), which uses `List[str]` and parses each event.
- **TypeScript:** Import `connectors` from `@azure/functions-extensions-connectors` and use the listed helper when available; the value in parentheses is its context property. For generic registration, use the listed item type as `T` in `connectorTrigger<T>`. A helper does not select the operation; configure the trigger separately.

Trigger availability can vary by namespace and over time. Microsoft Learn and API status labels do not by themselves determine Connector Namespace support. Operations marked `x-ms-visibility: internal` are excluded; that rule is inferred from designer behavior.

## Top connectors with triggers

### Office 365 Outlook

**Connector ID:** `office365`

**Type imports:** .NET `Azure.Connectors.Sdk.Office365.Models`; Python `azurefunctions.extensions.connectors.office365`; TypeScript `@azure/connectors/generated/Office365Extensions`

[.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/office365App/polling/O365Functions.cs) · [Python sample](https://github.com/Azure-Samples/functions-connectors-python/blob/main/office365App/polling/function_app.py) · [TypeScript sample](https://github.com/Azure-Samples/functions-connectors-typescript/blob/main/office365App/src/functions/onNewEmail.ts)

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CalendarGetOnChangedItemsV3`](https://learn.microsoft.com/connectors/office365/#when-an-event-is-added%2C-updated-or-deleted-%28v3%29) | `Office365OnCalendarChangedItemsTriggerPayload` (item: `GraphCalendarEventClientWithActionType`) | `List[office365.GraphCalendarEventClientWithActionType]` | `connectors.office365.onCalendarChangedItems` (`calendarEvents`); `Office365OnCalendarChangedItemsTriggerPayload` (item: `GraphCalendarEventClientWithActionType`) |
| [`CalendarGetOnNewItemsV3`](https://learn.microsoft.com/connectors/office365/#when-a-new-event-is-created-%28v3%29) | `Office365OnCalendarNewItemsTriggerPayload` (item: `GraphCalendarEventClientReceive`) | `List[office365.GraphCalendarEventClientReceive]` | `connectors.office365.onCalendarNewItems` (`calendarEvents`); `Office365OnCalendarNewItemsTriggerPayload` (item: `GraphCalendarEventClientReceive`) |
| [`CalendarGetOnUpdatedItemsV3`](https://learn.microsoft.com/connectors/office365/#when-an-event-is-modified-%28v3%29) | `Office365OnCalendarUpdatedItemsTriggerPayload` (item: `GraphCalendarEventClientReceive`) | `List[office365.GraphCalendarEventClientReceive]` | `connectors.office365.onCalendarUpdatedItems` (`calendarEvents`); `Office365OnCalendarUpdatedItemsTriggerPayload` (item: `GraphCalendarEventClientReceive`) |
| [`OnFlaggedEmailV4`](https://learn.microsoft.com/connectors/office365/#when-an-email-is-flagged-%28v4%29-%28preview%29) | `Office365OnFlaggedEmailTriggerPayload` (item: `GraphClientReceiveMessage`) | `List[office365.GraphClientReceiveMessage]` | `connectors.office365.onFlaggedEmail` (`emails`); `Office365OnFlaggedEmailTriggerPayload` (item: `GraphClientReceiveMessage`) |
| [`OnNewEmailV3`](https://learn.microsoft.com/connectors/office365/#when-a-new-email-arrives-%28v3%29) | `Office365OnNewEmailTriggerPayload` (item: `GraphClientReceiveMessage`) | `List[office365.GraphClientReceiveMessage]` | `connectors.office365.onNewEmail` (`emails`); `Office365OnNewEmailTriggerPayload` (item: `GraphClientReceiveMessage`) |
| [`OnNewMentionMeEmailV3`](https://learn.microsoft.com/connectors/office365/#when-a-new-email-mentioning-me-arrives-%28v3%29) | `Office365OnNewEmailMentioningMeTriggerPayload` (item: `GraphClientReceiveMessage`) | `List[office365.GraphClientReceiveMessage]` | `connectors.office365.onNewEmailMentioningMe` (`emails`); `Office365OnNewEmailMentioningMeTriggerPayload` (item: `GraphClientReceiveMessage`) |
| [`OnUpcomingEventsV3`](https://learn.microsoft.com/connectors/office365/#when-an-upcoming-event-is-starting-soon-%28v3%29) | `Office365OnUpcomingEventsTriggerPayload` (item: `GraphCalendarEventClientReceive`) | `List[office365.GraphCalendarEventClientReceive]` | `connectors.office365.onUpcomingEvents` (`calendarEvents`); `Office365OnUpcomingEventsTriggerPayload` (item: `GraphCalendarEventClientReceive`) |
| [`SharedMailboxOnNewEmailV2`](https://learn.microsoft.com/connectors/office365/#when-a-new-email-arrives-in-a-shared-mailbox-%28v2%29) | `Office365OnSharedMailboxNewEmailTriggerPayload` (item: `GraphClientReceiveMessage`) | `List[office365.GraphClientReceiveMessage]` | `connectors.office365.onSharedMailboxNewEmail` (`emails`); `Office365OnSharedMailboxNewEmailTriggerPayload` (item: `GraphClientReceiveMessage`) |

### SharePoint Online

**Connector ID:** `sharepointonline`

**Type imports:** .NET `Azure.Connectors.Sdk.SharePointOnline.Models`; Python `azurefunctions.extensions.connectors.sharepointonline`; TypeScript `@azure/connectors/generated/SharepointonlineExtensions`

[.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/sharepointApp/polling/SharePointFunctions.cs) · [Python sample](https://github.com/Azure-Samples/functions-connectors-python/blob/main/sharepointApp/polling/function_app.py) · [TypeScript sample](https://github.com/Azure-Samples/functions-connectors-typescript/blob/main/sharepointApp/src/functions/onSharepointNewFile.ts)

The `Item` model contains dynamic fields from the selected list. In .NET, inspect `AdditionalProperties` or `DynamicProperties`; in Python, use `dynamic_properties`. `DeletedItem` is a separate model.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnChangedItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-an-item-or-a-file-is-modified) | `SharePointOnlineOnChangedItemsTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onChangedItems` (`records`); `SharepointonlineOnChangedItemsTriggerPayload` (item: `Item`) |
| [`GetOnDeletedFileItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-a-file-is-deleted) | `SharePointOnlineOnDeletedFileItemsTriggerPayload` (item: `DeletedItem`) | `List[sharepointonline.DeletedItem]` | `connectors.sharepoint.onDeletedFileItems` (`deletedItems`); `SharepointonlineOnDeletedFileItemsTriggerPayload` (item: `DeletedItem`) |
| [`GetOnDeletedItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-an-item-is-deleted) | `SharePointOnlineOnDeletedItemsTriggerPayload` (item: `DeletedItem`) | `List[sharepointonline.DeletedItem]` | `connectors.sharepoint.onDeletedItems` (`deletedItems`); `SharepointonlineOnDeletedItemsTriggerPayload` (item: `DeletedItem`) |
| [`GetOnNewFileItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-a-file-is-created-%28properties-only%29) | `SharePointOnlineOnNewFileItemsTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onNewFileItems` (`records`); `SharepointonlineOnNewFileItemsTriggerPayload` (item: `Item`) |
| [`GetOnNewItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-an-item-is-created) | `SharePointOnlineOnNewItemsTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onNewItems` (`records`); `SharepointonlineOnNewItemsTriggerPayload` (item: `Item`) |
| [`GetOnNewItemsFromForm`](https://learn.microsoft.com/connectors/sharepointonline/#when-a-form-is-submitted-%28preview%29) | `SharePointOnlineOnNewItemsFromFormTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onNewItemsFromForm` (`records`); `SharepointonlineOnNewItemsFromFormTriggerPayload` (item: `Item`) |
| [`GetOnRecurrenceDigest`](https://learn.microsoft.com/connectors/sharepointonline/#recurring-digest-of-updates-%28preview%29) | `SharePointOnlineOnRecurrenceDigestTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onRecurrenceDigest` (`records`); `SharepointonlineOnRecurrenceDigestTriggerPayload` (item: `Item`) |
| [`GetOnUpdatedFileClassifiedTimes`](https://learn.microsoft.com/connectors/sharepointonline/#when-a-file-is-classified-by-a-microsoft-syntex-model) | `SharePointOnlineOnUpdatedFileClassifiedTimesTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onUpdatedFileClassifiedTimes` (`records`); `SharepointonlineOnUpdatedFileClassifiedTimesTriggerPayload` (item: `Item`) |
| [`GetOnUpdatedFileItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-a-file-is-created-or-modified-%28properties-only%29) | `SharePointOnlineOnUpdatedFileItemsTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onUpdatedFileItems` (`records`); `SharepointonlineOnUpdatedFileItemsTriggerPayload` (item: `Item`) |
| [`GetOnUpdatedItems`](https://learn.microsoft.com/connectors/sharepointonline/#when-an-item-is-created-or-modified) | `SharePointOnlineOnUpdatedItemsTriggerPayload` (item: `Item`) | `List[sharepointonline.Item]` | `connectors.sharepoint.onUpdatedItems` (`records`); `SharepointonlineOnUpdatedItemsTriggerPayload` (item: `Item`) |

### OneDrive for Business

**Connector ID:** `onedriveforbusiness`

**Type imports:** .NET `Azure.Connectors.Sdk.OneDriveForBusiness.Models`; Python `azurefunctions.extensions.connectors.onedriveforbusiness`; TypeScript `@azure/connectors/generated/OnedriveforbusinessExtensions`

[.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/onedriveApp/polling/OneDriveFunctions.cs) · [Python sample](https://github.com/Azure-Samples/functions-connectors-python/blob/main/onedriveApp/polling/function_app.py)

The single-file triggers return file content; the typed metadata models apply to the properties-only triggers.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewFilesV2`](https://learn.microsoft.com/connectors/onedriveforbusiness/#when-a-file-is-created-%28properties-only%29) | `OneDriveForBusinessOnNewFilesTriggerPayload` (item: `BlobMetadata`) | `List[onedriveforbusiness.BlobMetadata]` | `connectors.onedriveforbusiness.onNewFiles` (`files`); `OnedriveforbusinessOnNewFilesTriggerPayload` (item: `BlobMetadata`) |
| [`OnNewFileV2`](https://learn.microsoft.com/connectors/onedriveforbusiness/#when-a-file-is-created) | — | — | — |
| [`OnUpdatedFilesV2`](https://learn.microsoft.com/connectors/onedriveforbusiness/#when-a-file-is-modified-%28properties-only%29) | `OneDriveForBusinessOnUpdatedFilesTriggerPayload` (item: `BlobMetadata`) | `List[onedriveforbusiness.BlobMetadata]` | `connectors.onedriveforbusiness.onUpdatedFiles` (`files`); `OnedriveforbusinessOnUpdatedFilesTriggerPayload` (item: `BlobMetadata`) |
| [`OnUpdatedFileV2`](https://learn.microsoft.com/connectors/onedriveforbusiness/#when-a-file-is-modified) | — | — | — |

### Microsoft Teams

**Connector ID:** `teams`

**Type imports:** .NET `Azure.Connectors.Sdk.Teams.Models`; Python `azurefunctions.extensions.connectors.teams`; TypeScript `@azure/connectors/generated/TeamsExtensions`

[.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/teamsApp/polling/TeamsFunctions.cs) · [.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/teamsApp/polling/AdditionalTeamsFunctions.cs) · [Python sample](https://github.com/Azure-Samples/functions-connectors-python/blob/main/teamsApp/polling/function_app.py) · [TypeScript sample](https://github.com/Azure-Samples/functions-connectors-typescript/blob/main/teamsApp/src/functions/onNewChannelMessage.ts)

The .NET `ChatMessage` model exposes nested values such as `Body` and `From` as `JsonElement?`. The Python `Dynamic...` webhook annotations describe runtime-shaped payloads, not fixed message schemas.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnGroupMembershipAdd`](https://learn.microsoft.com/connectors/teams/#when-a-new-team-member-is-added) | `TeamsOnTeamMemberAddedTriggerPayload` (item: `JsonElement?`) | `teams.OnGroupMemberChangeResponse` (returns dictionaries) | `connectors.teams.onTeamMemberAdded` (`members`); `TeamsOnTeamMemberAddedTriggerPayload` (item: `Record<string, unknown>`) |
| [`OnGroupMembershipRemoval`](https://learn.microsoft.com/connectors/teams/#when-a-new-team-member-is-removed) | `TeamsOnTeamMemberRemovedTriggerPayload` (item: `JsonElement?`) | `teams.OnGroupMemberChangeResponse` (returns dictionaries) | `connectors.teams.onTeamMemberRemoved` (`members`); `TeamsOnTeamMemberRemovedTriggerPayload` (item: `Record<string, unknown>`) |
| [`OnNewChannelMessage`](https://learn.microsoft.com/connectors/teams/#when-a-new-channel-message-is-added) | `TeamsOnNewChannelMessageTriggerPayload` (item: `ChatMessage`) | `List[teams.ChatMessage]` | `connectors.teams.onNewChannelMessage` (`messages`); `TeamsOnNewChannelMessageTriggerPayload` (item: `ChatMessage`) |
| [`OnNewChannelMessageMentioningMe`](https://learn.microsoft.com/connectors/teams/#when-i-am-mentioned-in-a-channel-message) | `TeamsOnNewChannelMessageMentioningMeTriggerPayload` (item: `ChatMessage`) | `List[teams.ChatMessage]` | `connectors.teams.onNewChannelMessageMentioningMe` (`messages`); `TeamsOnNewChannelMessageMentioningMeTriggerPayload` (item: `ChatMessage`) |
| [`WebhookAtMentionTrigger`](https://learn.microsoft.com/connectors/teams/#when-i%27m-%40mentioned) | — | `List[teams.DynamicAtMentionWebhookTriggerResponseSchema]` | — |
| [`WebhookChatMessageTrigger`](https://learn.microsoft.com/connectors/teams/#when-a-new-chat-message-is-added) | — | `List[teams.ChatMessageWebhookResponseSchema]` | — |
| [`WebhookKeywordTrigger`](https://learn.microsoft.com/connectors/teams/#when-keywords-are-mentioned) | — | `List[teams.DynamicKeywordWebhookTriggerResponseSchema]` | — |
| [`WebhookMessageReactionTrigger`](https://learn.microsoft.com/connectors/teams/#when-someone-reacted-to-a-message-in-chat) | — | `List[teams.MessageReactionWebhookResponseSchema]` | — |
| [`WebhookNewMessageTrigger`](https://learn.microsoft.com/connectors/teams/#when-a-new-message-is-added-to-a-chat-or-channel) | — | `List[teams.DynamicNewMessageWebhookTriggerResponseSchema]` | — |

### Microsoft Dataverse

**Connector ID:** `commondataservice`

**Type imports:** Python `azurefunctions.extensions.connectors.commondataservice`

[.NET sample](https://github.com/Azure-Samples/functions-connectors-net/blob/main/dataverseApp/polling/DataverseFunctions.cs) · [Python sample](https://github.com/Azure-Samples/functions-connectors-python/blob/main/dataverseApp/polling/function_app.py)

`SubscribeOnDeletedItems`, `SubscribeOnNewItems`, and `SubscribeOnUpdatedItems` are supported by Connector Namespace and fully supported for Logic Apps and Connectors. Their payload schemas are resolved at runtime for the selected Dataverse table, so there is no single fixed row class. Use raw JSON and parse the fields for your table.

For `SubscribeWebhookTrigger` and `BusinessEventsTrigger`, the Python annotations (`RowChangeNotification` and `ActionEventNotification`) return SDK `ObjectEntity` records with dynamic fields rather than fixed row or action classes.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`BusinessEventsTrigger`](https://learn.microsoft.com/connectors/commondataservice/#when-an-action-is-performed) | — | `List[commondataservice.ActionEventNotification]` (returns SDK `ObjectEntity` records) | — |
| [`SubscribeOnDeletedItems`](https://learn.microsoft.com/connectors/commondataservice/#when-a-row-is-deleted-%5Bdeprecated%5D) | — | — | — |
| [`SubscribeOnNewItems`](https://learn.microsoft.com/connectors/commondataservice/#when-a-row-is-added-%5Bdeprecated%5D) | — | — | — |
| [`SubscribeOnUpdatedItems`](https://learn.microsoft.com/connectors/commondataservice/#when-a-row-is-modified-%5Bdeprecated%5D) | — | — | — |
| [`SubscribeWebhookTrigger`](https://learn.microsoft.com/connectors/commondataservice/#when-a-row-is-added%2C-modified-or-deleted) | — | `List[commondataservice.RowChangeNotification]` (returns SDK `ObjectEntity` records) | — |

### Planner

**Connector ID:** `planner`

**Type imports:** .NET `Azure.Connectors.Sdk.Planner.Models`; Python `azurefunctions.extensions.connectors.planner`; TypeScript `@azure/connectors/generated/PlannerExtensions`

In .NET, `GetTaskResponseV2.CreatedBy` and entries in `Assignments` are `JsonElement?` values; read them as JSON at runtime.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCompleteTask_V3`](https://learn.microsoft.com/connectors/planner/#when-a-task-is-completed) | `PlannerOnCompleteTaskTriggerPayload` (item: `GetTaskResponseV2`) | `List[planner.GetTaskResponseV2]` | `connectors.planner.onCompleteTask` (`tasks`); `PlannerOnCompleteTaskTriggerPayload` (item: `GetTaskResponseV2`) |
| [`OnNewTask_V3`](https://learn.microsoft.com/connectors/planner/#when-a-new-task-is-created) | `PlannerOnNewTaskTriggerPayload` (item: `GetTaskResponseV2`) | `List[planner.GetTaskResponseV2]` | `connectors.planner.onNewTask` (`tasks`); `PlannerOnNewTaskTriggerPayload` (item: `GetTaskResponseV2`) |
| [`OnTaskAssignedToMe_V2`](https://learn.microsoft.com/connectors/planner/#when-a-task-is-assigned-to-me) | `PlannerOnTaskAssignedToMeTriggerPayload` (item: `GetTaskResponseV2`) | `List[planner.GetTaskResponseV2]` | `connectors.planner.onTaskAssignedToMe` (`tasks`); `PlannerOnTaskAssignedToMeTriggerPayload` (item: `GetTaskResponseV2`) |

### Salesforce

**Connector ID:** `salesforce`

**Type imports:** .NET `Azure.Connectors.Sdk.Salesforce.Models`; Python `azurefunctions.extensions.connectors.salesforce`; TypeScript `@azure/connectors/generated/SalesforceExtensions`

The `Item` model contains dynamic fields from the selected Salesforce object. In .NET, inspect `AdditionalProperties` or `DynamicProperties`; in Python, use `dynamic_properties`.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnNewItems`](https://learn.microsoft.com/connectors/salesforce/#when-a-record-is-created) | `SalesforceOnNewItemsTriggerPayload` (item: `Item`) | `List[salesforce.Item]` | `connectors.salesforce.onNewItems` (`records`); `SalesforceOnNewItemsTriggerPayload` (item: `Item`) |
| [`GetOnUpdatedItems`](https://learn.microsoft.com/connectors/salesforce/#when-a-record-is-modified) | `SalesforceOnUpdatedItemsTriggerPayload` (item: `Item`) | `List[salesforce.Item]` | `connectors.salesforce.onUpdatedItems` (`records`); `SalesforceOnUpdatedItemsTriggerPayload` (item: `Item`) |

### SAP

**Connector ID:** `sap`

The SAP message shape depends on the configured SAP action and IDoc format. Bind the raw input and parse it using the schema for the configured SAP message.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Subscribe`](https://learn.microsoft.com/connectors/sap/#when-a-message-is-received) | — | — | — |

### Azure DevOps

**Connector ID:** `visualstudioteamservices`

**Type imports:** .NET `Azure.Connectors.Sdk.VisualStudioTeamServices.Models`; Python `azurefunctions.extensions.connectors.visualstudioteamservices`; TypeScript `@azure/connectors/generated/VisualstudioteamservicesExtensions`

In .NET, `TriggerWorkItemResponse.Fields` is a `JsonElement?`; read the work-item fields as JSON at runtime.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnBuildCompleted`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-build-completes) | `VisualStudioTeamServicesOnBuildCompletedTriggerPayload` (item: `BuildResult`) | `List[visualstudioteamservices.BuildResult]` | `connectors.vsts.onBuildCompleted` (`builds`); `VisualstudioteamservicesOnBuildCompletedTriggerPayload` (item: `BuildResult`) |
| [`OnGitPullClosed`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-pull-request-is-closed-%28git%29) | `VisualStudioTeamServicesOnGitPullClosedTriggerPayload` (item: `GitPullRequest`) | `List[visualstudioteamservices.GitPullRequest]` | `connectors.vsts.onGitPullClosed` (`pullRequests`); `VisualstudioteamservicesOnGitPullClosedTriggerPayload` (item: `GitPullRequest`) |
| [`OnGitPullCreated`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-pull-request-is-created-%28git%29) | `VisualStudioTeamServicesOnGitPullCreatedTriggerPayload` (item: `GitPullRequest`) | `List[visualstudioteamservices.GitPullRequest]` | `connectors.vsts.onGitPullCreated` (`pullRequests`); `VisualstudioteamservicesOnGitPullCreatedTriggerPayload` (item: `GitPullRequest`) |
| [`OnGitPush`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-code-is-pushed-%28git%29) | `VisualStudioTeamServicesOnGitPushTriggerPayload` (item: `GitPush`) | `List[visualstudioteamservices.GitPush]` | `connectors.vsts.onGitPush` (`pushes`); `VisualstudioteamservicesOnGitPushTriggerPayload` (item: `GitPush`) |
| [`OnTfvcCheckIn`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-code-is-checked-in-%28tfvc%29) | `VisualStudioTeamServicesOnTfvcCheckInTriggerPayload` (item: `TfvcChangeset`) | `List[visualstudioteamservices.TfvcChangeset]` | `connectors.vsts.onTfvcCheckIn` (`changesets`); `VisualstudioteamservicesOnTfvcCheckInTriggerPayload` (item: `TfvcChangeset`) |
| [`OnWorkItemAssignedV2`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-work-item-is-assigned) | `VisualStudioTeamServicesOnWorkItemAssignedTriggerPayload` (item: `TriggerWorkItemResponse`) | `List[visualstudioteamservices.TriggerWorkItemResponse]` | `connectors.vsts.onWorkItemAssigned` (`workItems`); `VisualstudioteamservicesOnWorkItemAssignedTriggerPayload` (item: `TriggerWorkItemResponse`) |
| [`OnWorkItemClosedV2`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-work-item-is-closed) | `VisualStudioTeamServicesOnWorkItemClosedTriggerPayload` (item: `TriggerWorkItemResponse`) | `List[visualstudioteamservices.TriggerWorkItemResponse]` | `connectors.vsts.onWorkItemClosed` (`workItems`); `VisualstudioteamservicesOnWorkItemClosedTriggerPayload` (item: `TriggerWorkItemResponse`) |
| [`OnWorkItemCreatedV2`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-work-item-is-created) | `VisualStudioTeamServicesOnWorkItemCreatedTriggerPayload` (item: `TriggerWorkItemResponse`) | `List[visualstudioteamservices.TriggerWorkItemResponse]` | `connectors.vsts.onWorkItemCreated` (`workItems`); `VisualstudioteamservicesOnWorkItemCreatedTriggerPayload` (item: `TriggerWorkItemResponse`) |
| [`OnWorkItemUpdatedV2`](https://learn.microsoft.com/connectors/visualstudioteamservices/#when-a-work-item-is-updated) | `VisualStudioTeamServicesOnWorkItemUpdatedTriggerPayload` (item: `TriggerWorkItemResponse`) | `List[visualstudioteamservices.TriggerWorkItemResponse]` | `connectors.vsts.onWorkItemUpdated` (`workItems`); `VisualstudioteamservicesOnWorkItemUpdatedTriggerPayload` (item: `TriggerWorkItemResponse`) |

### Power Query Dataflows

**Connector ID:** `dataflows`

**Type imports:** .NET `Azure.Connectors.Sdk.Dataflows.Models`; Python `azurefunctions.extensions.connectors.dataflows`; TypeScript `@azure/connectors/generated/DataflowsExtensions`; TypeScript payload `@azure/functions-extensions-connectors`

For .NET, start with raw JSON. After confirming the event structure, deserialize the refresh record into `RefreshModel`.

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnRefreshComplete`](https://learn.microsoft.com/connectors/dataflows/#when-a-dataflow-refresh-completes) | — | `List[dataflows.RefreshModel]` | `connectors.dataflows.onRefreshComplete` (`refreshes`); `DataflowsOnRefreshCompleteTriggerPayload` (item: `RefreshModel`) |

## Other connectors

Connectors are listed alphabetically. The language column shows where at least one typed mapping was found. Select a connector for individual operations and types.

[0–9](#09) · [A](#a) · [B](#b) · [C](#c) · [D](#d) · [E](#e) · [F](#f) · [G](#g) · [H](#h) · [I](#i) · [J](#j) · [K](#k) · [L](#l) · [M](#m) · [N](#n) · [O](#o) · [P](#p) · [Q](#q) · [R](#r) · [S](#s) · [T](#t) · [U](#u) · [V](#v) · [W](#w) · [X](#x) · [Y](#y) · [Z](#z)

### 0–9

| Connector | Typed languages |
| --- | --- |
| [10to8 Appointment Scheduling](connector-triggers/numbers-a-f.md#10to8-appointment-scheduling) (`10to8`) | — |
| [365 Training](connector-triggers/numbers-a-f.md#365-training) (`365training`) | — |
| [3E Events](connector-triggers/numbers-a-f.md#3e-events) (`3eevents`) | — |

### A

| Connector | Typed languages |
| --- | --- |
| [absentify](connector-triggers/numbers-a-f.md#absentify) (`absentify`) | — |
| [AcceptMission](connector-triggers/numbers-a-f.md#acceptmission) (`acceptmission`) | — |
| [Act!](connector-triggers/numbers-a-f.md#act) (`act`) | — |
| [ActivityInfo](connector-triggers/numbers-a-f.md#activityinfo) (`activityinfo`) | — |
| [Adobe Acrobat Sign](connector-triggers/numbers-a-f.md#adobe-acrobat-sign) (`adobesign`) | — |
| [Adobe Acrobat Sign Sandbox](connector-triggers/numbers-a-f.md#adobe-acrobat-sign-sandbox) (`adobeacrobatsignsand`) | — |
| [Adobe Creative Cloud](connector-triggers/numbers-a-f.md#adobe-creative-cloud) (`adobecreativecloud`) | — |
| [Africa's Talking Airtime](connector-triggers/numbers-a-f.md#africas-talking-airtime) (`africastalkingairtime`) | — |
| [Africa's Talking SMS](connector-triggers/numbers-a-f.md#africas-talking-sms) (`africastalkingsms`) | — |
| [AgilePoint NX](connector-triggers/numbers-a-f.md#agilepoint-nx) (`agilepointnx`) | — |
| [AI TeamsMaestro](connector-triggers/numbers-a-f.md#ai-teamsmaestro) (`aiteamsmaestro`) | — |
| [Airmeet](connector-triggers/numbers-a-f.md#airmeet) (`airmeet`) | — |
| [airSlate](connector-triggers/numbers-a-f.md#airslate) (`airslate`) | — |
| [Alemba ITSM](connector-triggers/numbers-a-f.md#alemba-itsm) (`alembaitsm`) | — |
| [Aletheia](connector-triggers/numbers-a-f.md#aletheia) (`aletheia`) | — |
| [Alkymi](connector-triggers/numbers-a-f.md#alkymi) (`alkymi`) | — |
| [Almabase](connector-triggers/numbers-a-f.md#almabase) (`almabase`) | — |
| [ALVAO](connector-triggers/numbers-a-f.md#alvao) (`alvao`) | — |
| [Amazon S3](connector-triggers/numbers-a-f.md#amazon-s3) (`amazons3`) | — |
| [Amazon SQS](connector-triggers/numbers-a-f.md#amazon-sqs) (`amazonsqs`) | — |
| [Annature (Independent Publisher)](connector-triggers/numbers-a-f.md#annature-independent-publisher) (`annatureip`) | — |
| [Apify](connector-triggers/numbers-a-f.md#apify) (`apify`) | — |
| [App Power Forms](connector-triggers/numbers-a-f.md#app-power-forms) (`apppowerforms`) | — |
| [Appfigures](connector-triggers/numbers-a-f.md#appfigures) (`appfigures`) | — |
| [ArcGIS](connector-triggers/numbers-a-f.md#arcgis) (`arcgis`) | — |
| [ArcGIS Enterprise](connector-triggers/numbers-a-f.md#arcgis-enterprise) (`arcgisenterprise`) | — |
| [AS2](connector-triggers/numbers-a-f.md#as2) (`as2`) | — |
| [Asana](connector-triggers/numbers-a-f.md#asana) (`asana`) | — |
| [Asite](connector-triggers/numbers-a-f.md#asite) (`asite`) | — |
| [Asite (Canada)](connector-triggers/numbers-a-f.md#asite-canada) (`asitecanada`) | — |
| [Asite (Hong Kong)](connector-triggers/numbers-a-f.md#asite-hong-kong) (`asitehongkong`) | — |
| [Asite (KSA)](connector-triggers/numbers-a-f.md#asite-ksa) (`asiteksa`) | — |
| [Asite (UAE)](connector-triggers/numbers-a-f.md#asite-uae) (`asiteuae`) | — |
| [Asite (US Gov.)](connector-triggers/numbers-a-f.md#asite-us-gov) (`asiteusgov`) | — |
| [Assently E-Sign](connector-triggers/numbers-a-f.md#assently-e-sign) (`assentlyesign`) | — |
| [AtBot Admin](connector-triggers/numbers-a-f.md#atbot-admin) (`bizzyadmin`) | — |
| [AtBot Logic](connector-triggers/numbers-a-f.md#atbot-logic) (`bizzy`) | — |
| [Autenti E-Signature Workflow](connector-triggers/numbers-a-f.md#autenti-e-signature-workflow) (`autentiesignature`) | — |
| [Autenti E-Signature Workflow](connector-triggers/numbers-a-f.md#autenti-e-signature-workflow-1) (`autentiesignaturewor`) | — |
| [Autodesk Data Exchange](connector-triggers/numbers-a-f.md#autodesk-data-exchange) (`autodeskforgedataexc`) | — |
| [AvePoint Cloud Governance](connector-triggers/numbers-a-f.md#avepoint-cloud-governance) (`avepointcloudgovernance`) | — |
| [Azure Blob Storage](connector-triggers/numbers-a-f.md#azure-blob-storage) (`azureblob`) | .NET, TypeScript |
| [Azure Communication Services SMS Events](connector-triggers/numbers-a-f.md#azure-communication-services-sms-events) (`acssmsevents`) | — |
| [Azure Database for MySQL](connector-triggers/numbers-a-f.md#azure-database-for-mysql) (`azuremysql`) | — |
| [Azure Event Grid](connector-triggers/numbers-a-f.md#azure-event-grid) (`azureeventgrid`) | — |
| [Azure IoT Central V2](connector-triggers/numbers-a-f.md#azure-iot-central-v2) (`iotcentral`) | — |
| [Azure IoT Central V3](connector-triggers/numbers-a-f.md#azure-iot-central-v3) (`azureiotcentral`) | — |
| [Azure Queues](connector-triggers/numbers-a-f.md#azure-queues) (`azurequeues`) | — |

### B

| Connector | Typed languages |
| --- | --- |
| [Basecamp 2](connector-triggers/numbers-a-f.md#basecamp-2) (`basecamp2`) | — |
| [Basecamp 3](connector-triggers/numbers-a-f.md#basecamp-3) (`basecamp`) | — |
| [Bing Search](connector-triggers/numbers-a-f.md#bing-search) (`bingsearch`) | — |
| [Bitbucket](connector-triggers/numbers-a-f.md#bitbucket) (`bitbucket`) | — |
| [Bitly](connector-triggers/numbers-a-f.md#bitly) (`bitly`) | — |
| [Blogger](connector-triggers/numbers-a-f.md#blogger) (`blogger`) | — |
| [Bloomflow](connector-triggers/numbers-a-f.md#bloomflow) (`bloomflow`) | — |
| [BoldSign](connector-triggers/numbers-a-f.md#boldsign) (`boldsign`) | — |
| [boomapp connect](connector-triggers/numbers-a-f.md#boomapp-connect) (`boomappconnect`) | — |
| [Box](connector-triggers/numbers-a-f.md#box) (`box`) | .NET, TypeScript |
| [bttn](connector-triggers/numbers-a-f.md#bttn) (`bttn`) | — |
| [Bttn ONE](connector-triggers/numbers-a-f.md#bttn-one) (`bttnone`) | — |
| [Buffer](connector-triggers/numbers-a-f.md#buffer) (`buffer`) | — |
| [Businessmap](connector-triggers/numbers-a-f.md#businessmap) (`kanbanize`) | — |

### C

| Connector | Typed languages |
| --- | --- |
| [C TWO](connector-triggers/numbers-a-f.md#c-two) (`ctwo`) | — |
| [Calendly](connector-triggers/numbers-a-f.md#calendly) (`calendlyv2`) | — |
| [Calendly (legacy)](connector-triggers/numbers-a-f.md#calendly-legacy) (`calendly`) | — |
| [Campfire](connector-triggers/numbers-a-f.md#campfire) (`campfire`) | — |
| [Capsule CRM](connector-triggers/numbers-a-f.md#capsule-crm) (`capsulecrm`) | — |
| [Captisa Forms](connector-triggers/numbers-a-f.md#captisa-forms) (`captisaforms`) | — |
| [Certinal eSign](connector-triggers/numbers-a-f.md#certinal-esign) (`certinalesign`) | — |
| [Certyneo](connector-triggers/numbers-a-f.md#certyneo) (`certyneo`) | — |
| [Chatter](connector-triggers/numbers-a-f.md#chatter) (`chatter`) | — |
| [Cireson Service Manager Portal](connector-triggers/numbers-a-f.md#cireson-service-manager-portal) (`ciresonservicemanage`) | — |
| [Cireson Service Manager Portal](connector-triggers/numbers-a-f.md#cireson-service-manager-portal-1) (`ciresonservicemanagerportal`) | — |
| [CivicPlus Transform](connector-triggers/numbers-a-f.md#civicplus-transform) (`civicplustransform`) | — |
| [CiviQ](connector-triggers/numbers-a-f.md#civiq) (`civiq`) | — |
| [ClickSend](connector-triggers/numbers-a-f.md#clicksend) (`clicksendsms`) | — |
| [Cloud BOT](connector-triggers/numbers-a-f.md#cloud-bot) (`cloudbot`) | — |
| [Cloud Connect Studio](connector-triggers/numbers-a-f.md#cloud-connect-studio) (`appstudioapi`) | — |
| [Cloud PKI Management](connector-triggers/numbers-a-f.md#cloud-pki-management) (`cloudpki`) | — |
| [CluedIn](connector-triggers/numbers-a-f.md#cluedin) (`cluedin`) | — |
| [Cognito Forms](connector-triggers/numbers-a-f.md#cognito-forms) (`cognitoforms`) | — |
| [Connect2All](connector-triggers/numbers-a-f.md#connect2all) (`connect2all`) | — |
| [Connect2All on-premises](connector-triggers/numbers-a-f.md#connect2all-on-premises) (`connect2allonpremises`) | — |
| [Content Gate](connector-triggers/numbers-a-f.md#content-gate) (`axtensioncontentgate`) | — |
| [Cornerstone Learning vILT](connector-triggers/numbers-a-f.md#cornerstone-learning-vilt) (`cornerstonelearningv`) | — |
| [COSMO Bot](connector-triggers/numbers-a-f.md#cosmo-bot) (`cosmobot`) | — |
| [CPQSync](connector-triggers/numbers-a-f.md#cpqsync) (`cpqsync`) | — |
| [Cradl AI](connector-triggers/numbers-a-f.md#cradl-ai) (`cradlai`) | — |
| [CRM Bot](connector-triggers/numbers-a-f.md#crm-bot) (`crmbot`) | — |

### D

| Connector | Typed languages |
| --- | --- |
| [d.velop](connector-triggers/numbers-a-f.md#dvelop) (`dvelop`) | — |
| [Data Activator Early Access](connector-triggers/numbers-a-f.md#data-activator-early-access) (`dataactivatorpreview`) | — |
| [DataFlows SMS](connector-triggers/numbers-a-f.md#dataflows-sms) (`dataflowssms`) | — |
| [DataScope Forms](connector-triggers/numbers-a-f.md#datascope-forms) (`datascopeforms`) | — |
| [Datelist](connector-triggers/numbers-a-f.md#datelist) (`datelist`) | — |
| [Desk365](connector-triggers/numbers-a-f.md#desk365) (`desk365`) | — |
| [DeskDirector](connector-triggers/numbers-a-f.md#deskdirector) (`deskdirector`) | — |
| [Dime.Scheduler](connector-triggers/numbers-a-f.md#dimescheduler) (`dimeschedulerv2`) | — |
| [Disqus](connector-triggers/numbers-a-f.md#disqus) (`disqus`) | — |
| [DocJuris](connector-triggers/numbers-a-f.md#docjuris) (`docjuris`) | — |
| [Docparser](connector-triggers/numbers-a-f.md#docparser) (`docparser`) | — |
| [Docugami](connector-triggers/numbers-a-f.md#docugami) (`docugami`) | — |
| [Document Drafter](connector-triggers/numbers-a-f.md#document-drafter) (`documentdrafter`) | — |
| [Docusign](connector-triggers/numbers-a-f.md#docusign) (`docusign`) | — |
| [Docusign Demo](connector-triggers/numbers-a-f.md#docusign-demo) (`docusigndemo`) | — |
| [Dropbox](connector-triggers/numbers-a-f.md#dropbox) (`dropbox`) | .NET, TypeScript |
| [Dynamics 365 Business Central](connector-triggers/numbers-a-f.md#dynamics-365-business-central) (`dynamicssmbsaas`) | — |
| [Dynamics 365 Customer Insights](connector-triggers/numbers-a-f.md#dynamics-365-customer-insights) (`customerinsights`) | — |

### E

| Connector | Typed languages |
| --- | --- |
| [E-Sign](connector-triggers/numbers-a-f.md#e-sign) (`esign`) | — |
| [Easy8](connector-triggers/numbers-a-f.md#easy8) (`easyredmine`) | — |
| [Edgility](connector-triggers/numbers-a-f.md#edgility) (`edgility`) | — |
| [Edifact](connector-triggers/numbers-a-f.md#edifact) (`edifact`) | — |
| [Eduframe](connector-triggers/numbers-a-f.md#eduframe) (`eduframe`) | — |
| [Egnyte](connector-triggers/numbers-a-f.md#egnyte) (`egnyte`) | — |
| [Eigen Events](connector-triggers/numbers-a-f.md#eigen-events) (`eigenevents`) | — |
| [Elfsquad Data](connector-triggers/numbers-a-f.md#elfsquad-data) (`elfsquaddata`) | — |
| [Elfsquad Product Configurator](connector-triggers/numbers-a-f.md#elfsquad-product-configurator) (`elfsquad`) | — |
| [Emigo](connector-triggers/numbers-a-f.md#emigo) (`emigo`) | — |
| [Encodian](connector-triggers/numbers-a-f.md#encodian) (`encodian`) | — |
| [Encodian - General](connector-triggers/numbers-a-f.md#encodian---general) (`encodiangeneral`) | — |
| [Entegrations.io](connector-triggers/numbers-a-f.md#entegrationsio) (`entegrations`) | — |
| [Entersoft](connector-triggers/numbers-a-f.md#entersoft) (`entersoft`) | — |
| [Envoy](connector-triggers/numbers-a-f.md#envoy) (`envoy`) | — |
| [Event Hubs](connector-triggers/numbers-a-f.md#event-hubs) (`eventhubs`) | .NET, TypeScript |
| [Event Tickets](connector-triggers/numbers-a-f.md#event-tickets) (`eventtickets`) | — |
| [Eventbrite](connector-triggers/numbers-a-f.md#eventbrite) (`eventbrite`) | .NET, TypeScript |
| [Evocom](connector-triggers/numbers-a-f.md#evocom) (`evocom`) | — |
| [eWay-CRM](connector-triggers/numbers-a-f.md#eway-crm) (`ewaycrm`) | — |

### F

| Connector | Typed languages |
| --- | --- |
| [Feathery Forms](connector-triggers/numbers-a-f.md#feathery-forms) (`featheryforms`) | — |
| [File System](connector-triggers/numbers-a-f.md#file-system) (`filesystem`) | — |
| [Fin &amp; Ops Apps (Dynamics 365)](connector-triggers/numbers-a-f.md#fin--ops-apps-dynamics-365) (`dynamicsax`) | — |
| [Finalcad One Connect 4.0](connector-triggers/numbers-a-f.md#finalcad-one-connect-40) (`finalcadoneconnect`) | — |
| [Fluid Kinnectorz for Procore](connector-triggers/numbers-a-f.md#fluid-kinnectorz-for-procore) (`fluidkinnectorzforpr`) | — |
| [Fluid Kinnectorz for Procore (Test)](connector-triggers/numbers-a-f.md#fluid-kinnectorz-for-procore-test) (`fluidkinnectorztest`) | — |
| [FORCAM FORCE Bridge](connector-triggers/numbers-a-f.md#forcam-force-bridge) (`forcamforcebridge`) | — |
| [ForceManager CRM](connector-triggers/numbers-a-f.md#forcemanager-crm) (`forcemanagercrm`) | — |
| [Formspace](connector-triggers/numbers-a-f.md#formspace) (`formspace`) | — |
| [Formstack Forms](connector-triggers/numbers-a-f.md#formstack-forms) (`formstackforms`) | — |
| [Foxit eSign](connector-triggers/numbers-a-f.md#foxit-esign) (`foxitesign`) | — |
| [FreshBooks](connector-triggers/numbers-a-f.md#freshbooks) (`freshbooks`) | — |
| [Freshdesk](connector-triggers/numbers-a-f.md#freshdesk) (`freshdesk`) | — |
| [Freshservice](connector-triggers/numbers-a-f.md#freshservice) (`freshservice`) | .NET, TypeScript |
| [FTP](connector-triggers/numbers-a-f.md#ftp) (`ftp`) | .NET, TypeScript |
| [Fulcrum](connector-triggers/numbers-a-f.md#fulcrum) (`fulcrum`) | — |

### G

| Connector | Typed languages |
| --- | --- |
| [GetAccept](connector-triggers/g-l.md#getaccept) (`getaccept`) | — |
| [GitHub](connector-triggers/g-l.md#github) (`github`) | .NET, TypeScript |
| [Glaass Pro](connector-triggers/g-l.md#glaass-pro) (`glaasspro`) | — |
| [Gmail](connector-triggers/g-l.md#gmail) (`gmail`) | — |
| [GMO Sign](connector-triggers/g-l.md#gmo-sign) (`gmosign`) | — |
| [GoFormz](connector-triggers/g-l.md#goformz) (`goformz`) | — |
| [Google Calendar](connector-triggers/g-l.md#google-calendar) (`googlecalendar`) | — |
| [Google Contacts](connector-triggers/g-l.md#google-contacts) (`googlecontacts`) | — |
| [Google Tasks](connector-triggers/g-l.md#google-tasks) (`googletasks`) | — |
| [GoToMeeting](connector-triggers/g-l.md#gotomeeting) (`gotomeeting`) | — |
| [GoToTraining](connector-triggers/g-l.md#gototraining) (`gototraining`) | — |
| [GoToWebinar](connector-triggers/g-l.md#gotowebinar) (`gotowebinar`) | — |
| [Gratavid](connector-triggers/g-l.md#gratavid) (`gratavid`) | — |
| [Gravity Forms by reenhanced](connector-triggers/g-l.md#gravity-forms-by-reenhanced) (`gravityformsbyreenhanced`) | — |
| [Gravity Forms Professional](connector-triggers/g-l.md#gravity-forms-professional) (`gravityformsprofessi`) | — |
| [Gravity SMS](connector-triggers/g-l.md#gravity-sms) (`gravitysms`) | — |
| [Groopit](connector-triggers/g-l.md#groopit) (`groopit`) | — |

### H

| Connector | Typed languages |
| --- | --- |
| [Harvest](connector-triggers/g-l.md#harvest) (`harvest`) | — |
| [HelloSign](connector-triggers/g-l.md#hellosign) (`hellosign`) | — |
| [HighGear Workflow](connector-triggers/g-l.md#highgear-workflow) (`highgearworkflow`) | — |
| [HipChat](connector-triggers/g-l.md#hipchat) (`hipchat`) | — |
| [Honeywell Forge](connector-triggers/g-l.md#honeywell-forge) (`honeywellforge`) | — |
| [HouseRater QA](connector-triggers/g-l.md#houserater-qa) (`houseraterqa`) | — |
| [Huddle](connector-triggers/g-l.md#huddle) (`huddle`) | — |
| [Huddle for US Gov &amp; Healthcare](connector-triggers/g-l.md#huddle-for-us-gov--healthcare) (`huddleforusgovhealth`) | — |
| [Huddo Boards](connector-triggers/g-l.md#huddo-boards) (`huddoboards`) | — |

### I

| Connector | Typed languages |
| --- | --- |
| [IcM](connector-triggers/g-l.md#icm) (`icm`) | — |
| [Ideanote](connector-triggers/g-l.md#ideanote) (`ideanote`) | — |
| [iManage Work](connector-triggers/g-l.md#imanage-work) (`imanagework`) | — |
| [iMIS](connector-triggers/g-l.md#imis) (`imis`) | — |
| [Impexium](connector-triggers/g-l.md#impexium) (`impexium`) | — |
| [Impower ERP](connector-triggers/g-l.md#impower-erp) (`impower`) | — |
| [Imprezian360-CRM](connector-triggers/g-l.md#imprezian360-crm) (`imprezian`) | — |
| [Infobip](connector-triggers/g-l.md#infobip) (`infobip`) | — |
| [Infusionsoft](connector-triggers/g-l.md#infusionsoft) (`infusionsoft`) | .NET, TypeScript |
| [Inoreader](connector-triggers/g-l.md#inoreader) (`inoreader`) | — |
| [Insightly](connector-triggers/g-l.md#insightly) (`insightly`) | — |
| [Instapaper](connector-triggers/g-l.md#instapaper) (`instapaper`) | — |
| [intelliHR](connector-triggers/g-l.md#intellihr) (`intellihr`) | — |
| [Intercom](connector-triggers/g-l.md#intercom) (`intercom`) | — |
| [ISOPlanner](connector-triggers/g-l.md#isoplanner) (`isoplanner`) | — |

### J

| Connector | Typed languages |
| --- | --- |
| [Jira](connector-triggers/g-l.md#jira) (`jira`) | .NET, TypeScript |
| [JotForm](connector-triggers/g-l.md#jotform) (`jotform`) | — |
| [Jotform Enterprise](connector-triggers/g-l.md#jotform-enterprise) (`jotformenterprise`) | — |
| [JungleMail 365](connector-triggers/g-l.md#junglemail-365) (`junglemail365`) | — |

### K

| Connector | Typed languages |
| --- | --- |
| [kintone](connector-triggers/g-l.md#kintone) (`kintone`) | — |

### L

| Connector | Typed languages |
| --- | --- |
| [Lawlift](connector-triggers/g-l.md#lawlift) (`lawlift`) | — |
| [LawVu](connector-triggers/g-l.md#lawvu) (`lawvu`) | — |
| [LCP - iCordis](connector-triggers/g-l.md#lcp---icordis) (`lcpicordis`) | — |
| [Leadinfo](connector-triggers/g-l.md#leadinfo) (`leadinfo`) | — |
| [LeanKit](connector-triggers/g-l.md#leankit) (`leankit`) | — |
| [Legalesign](connector-triggers/g-l.md#legalesign) (`legalesign`) | — |
| [Lexoffice (Independent Publisher)](connector-triggers/g-l.md#lexoffice-independent-publisher) (`lexoffice`) | — |
| [LiveTiles Bots](connector-triggers/g-l.md#livetiles-bots) (`livetilesbots`) | — |
| [LMS365](connector-triggers/g-l.md#lms365) (`lms365`) | — |
| [Loopio](connector-triggers/g-l.md#loopio) (`loopio`) | — |
| [Loopio-EU](connector-triggers/g-l.md#loopio-eu) (`loopioeu`) | — |
| [Loopio-Int01](connector-triggers/g-l.md#loopio-int01) (`loopioint01`) | — |
| [Luware Nimbus](connector-triggers/g-l.md#luware-nimbus) (`luwarenimbus`) | — |

### M

| Connector | Typed languages |
| --- | --- |
| [MailChimp](connector-triggers/m-r.md#mailchimp) (`mailchimp`) | — |
| [MailParser](connector-triggers/m-r.md#mailparser) (`mailparser`) | — |
| [Maximizer CRM](connector-triggers/m-r.md#maximizer-crm) (`maximizercrm`) | — |
| [Medium](connector-triggers/m-r.md#medium) (`medium`) | — |
| [Meisterplan](connector-triggers/m-r.md#meisterplan) (`meisterplan`) | — |
| [Microsoft Bookings](connector-triggers/m-r.md#microsoft-bookings) (`microsoftbookings`) | — |
| [Microsoft Defender ATP](connector-triggers/m-r.md#microsoft-defender-atp) (`wdatp`) | .NET, TypeScript |
| [Microsoft Defender for Cloud Alert](connector-triggers/m-r.md#microsoft-defender-for-cloud-alert) (`ascalert`) | — |
| [Microsoft Defender for Cloud Recommendation](connector-triggers/m-r.md#microsoft-defender-for-cloud-recommendation) (`ascassessment`) | — |
| [Microsoft Defender for Cloud Regulatory Compliance](connector-triggers/m-r.md#microsoft-defender-for-cloud-regulatory-compliance) (`ascregulatorycomplianceassessment`) | — |
| [Microsoft Defender SOC](connector-triggers/m-r.md#microsoft-defender-soc) (`defendersoc`) | — |
| [Microsoft Forms](connector-triggers/m-r.md#microsoft-forms) (`microsoftforms`) | — |
| [Microsoft Kaizala](connector-triggers/m-r.md#microsoft-kaizala) (`kaizala`) | — |
| [Microsoft Sentinel](connector-triggers/m-r.md#microsoft-sentinel) (`azuresentinel`) | — |
| [Microsoft To-Do (Business)](connector-triggers/m-r.md#microsoft-to-do-business) (`todo`) | .NET, TypeScript |
| [Microsoft To-Do (Consumer)](connector-triggers/m-r.md#microsoft-to-do-consumer) (`todoconsumer`) | — |
| [Mobili Stotele](connector-triggers/m-r.md#mobili-stotele) (`mobilistotele`) | — |
| [monday](connector-triggers/m-r.md#monday) (`monday`) | — |
| [MoreApp Forms](connector-triggers/m-r.md#moreapp-forms) (`moreappforms`) | — |
| [MotaWord Translations](connector-triggers/m-r.md#motaword-translations) (`motawordtranslations`) | — |
| [MSN Weather](connector-triggers/m-r.md#msn-weather) (`msnweather`) | — |
| [My Hours](connector-triggers/m-r.md#my-hours) (`myhours`) | — |

### N

| Connector | Typed languages |
| --- | --- |
| [nBold](connector-triggers/m-r.md#nbold) (`nbold`) | — |
| [NetDocuments](connector-triggers/m-r.md#netdocuments) (`netdocuments`) | — |
| [Netvolution](connector-triggers/m-r.md#netvolution) (`netvolution`) | — |
| [NH360 Portfolio Insights](connector-triggers/m-r.md#nh360-portfolio-insights) (`strategicportfoliomanager`) | — |
| [Nimflow](connector-triggers/m-r.md#nimflow) (`nimflow`) | — |
| [Nitro](connector-triggers/m-r.md#nitro) (`nitro`) | — |
| [Nitro Sign](connector-triggers/m-r.md#nitro-sign) (`nitrosign`) | — |
| [Nosco](connector-triggers/m-r.md#nosco) (`nosco`) | — |
| [Nozbe](connector-triggers/m-r.md#nozbe) (`nozbe`) | — |
| [nps.today](connector-triggers/m-r.md#npstoday) (`npstoday`) | — |
| [nunify](connector-triggers/m-r.md#nunify) (`nunify`) | — |

### O

| Connector | Typed languages |
| --- | --- |
| [Office 365 Groups](connector-triggers/m-r.md#office-365-groups) (`office365groups`) | .NET, TypeScript |
| [Office 365 Groups Mail](connector-triggers/m-r.md#office-365-groups-mail) (`office365groupsmail`) | .NET, TypeScript |
| [OK dokument (Independent Publisher)](connector-triggers/m-r.md#ok-dokument-independent-publisher) (`okdokumentip`) | — |
| [OneBlink](connector-triggers/m-r.md#oneblink) (`oneblink`) | — |
| [Oneflow](connector-triggers/m-r.md#oneflow) (`oneflow`) | — |
| [OneNote (Business)](connector-triggers/m-r.md#onenote-business) (`onenote`) | .NET, TypeScript |
| [OnePlan](connector-triggers/m-r.md#oneplan) (`oneplan`) | — |
| [Oodrive Sign](connector-triggers/m-r.md#oodrive-sign) (`oodrivesign`) | — |
| [OpenText Core Signature](connector-triggers/m-r.md#opentext-core-signature) (`opentextcoresignatur`) | — |
| [OpenText eDOCS](connector-triggers/m-r.md#opentext-edocs) (`opentextedocsbyonefox`) | — |
| [OpenText Extended ECM](connector-triggers/m-r.md#opentext-extended-ecm) (`opentextcsbyonefox`) | — |
| [OrbusInfinity](connector-triggers/m-r.md#orbusinfinity) (`orbusinfinity`) | — |
| [Outlook.com](connector-triggers/m-r.md#outlookcom) (`outlook`) | .NET, TypeScript |

### P

| Connector | Typed languages |
| --- | --- |
| [PagerDuty](connector-triggers/m-r.md#pagerduty) (`pagerduty`) | — |
| [Parserr](connector-triggers/m-r.md#parserr) (`parserr`) | — |
| [Parseur](connector-triggers/m-r.md#parseur) (`parseur`) | — |
| [Passlet](connector-triggers/m-r.md#passlet) (`passlet`) | — |
| [Paylocity](connector-triggers/m-r.md#paylocity) (`paylocity`) | — |
| [PDF4me](connector-triggers/m-r.md#pdf4me) (`pdf4me`) | — |
| [PDF4me Connect](connector-triggers/m-r.md#pdf4me-connect) (`pdf4meconnect`) | — |
| [PDF4me PDF](connector-triggers/m-r.md#pdf4me-pdf) (`pdf4mepdf`) | — |
| [Peakboard](connector-triggers/m-r.md#peakboard) (`peakboard`) | — |
| [Pinterest](connector-triggers/m-r.md#pinterest) (`pinterest`) | — |
| [Pipedrive](connector-triggers/m-r.md#pipedrive) (`pipedrive`) | .NET, TypeScript |
| [Pivotal Tracker](connector-triggers/m-r.md#pivotal-tracker) (`pivotaltracker`) | — |
| [PKIsigning](connector-triggers/m-r.md#pkisigning) (`pkisigning`) | — |
| [Plumsail Documents](connector-triggers/m-r.md#plumsail-documents) (`plumsail`) | — |
| [Plumsail Forms](connector-triggers/m-r.md#plumsail-forms) (`plumsailforms`) | — |
| [Poka](connector-triggers/m-r.md#poka) (`poka`) | — |
| [Polaris PSA](connector-triggers/m-r.md#polaris-psa) (`polarispsa`) | — |
| [Power BI](connector-triggers/m-r.md#power-bi) (`powerbi`) | — |
| [Power Form 7](connector-triggers/m-r.md#power-form-7) (`powerform7`) | — |
| [Power Platform for Admins V2](connector-triggers/m-r.md#power-platform-for-admins-v2) (`powerplatformadminv2`) | — |
| [Power Textor](connector-triggers/m-r.md#power-textor) (`powertextor`) | — |
| [PPM Express](connector-triggers/m-r.md#ppm-express) (`ppmexpress`) | — |
| [Priority Matrix](connector-triggers/m-r.md#priority-matrix) (`prioritymatrix`) | — |
| [Priority Matrix HIPAA](connector-triggers/m-r.md#priority-matrix-hipaa) (`prioritymatrixhipaa`) | — |
| [Process Street](connector-triggers/m-r.md#process-street) (`processstreet`) | — |
| [Project Online](connector-triggers/m-r.md#project-online) (`projectonline`) | — |
| [ProjectPlace](connector-triggers/m-r.md#projectplace) (`projectplace`) | — |
| [Pure Leads](connector-triggers/m-r.md#pure-leads) (`pureleads`) | — |
| [Pushcut](connector-triggers/m-r.md#pushcut) (`pushcut`) | — |

### Q

| Connector | Typed languages |
| --- | --- |
| [QPP NextGen](connector-triggers/m-r.md#qpp-nextgen) (`qppngintegrationhub`) | — |

### R

| Connector | Typed languages |
| --- | --- |
| [RamQuest Events](connector-triggers/m-r.md#ramquest-events) (`ramquestevents`) | — |
| [RAPID Platform](connector-triggers/m-r.md#rapid-platform) (`rapidplatform`) | — |
| [Receptful](connector-triggers/m-r.md#receptful) (`receptful`) | — |
| [Redmine](connector-triggers/m-r.md#redmine) (`redmine`) | — |
| [Rencore Governance](connector-triggers/m-r.md#rencore-governance) (`rencoregovernance`) | — |
| [Rencore Oversight](connector-triggers/m-r.md#rencore-oversight) (`rencoreoversight`) | — |
| [Replicon](connector-triggers/m-r.md#replicon) (`replicon`) | — |
| [Requestor](connector-triggers/m-r.md#requestor) (`requestor`) | — |
| [Resco Cloud](connector-triggers/m-r.md#resco-cloud) (`rescocloud`) | — |
| [Revelation helpdesk](connector-triggers/m-r.md#revelation-helpdesk) (`revelationhelpdesk`) | — |
| [RowShare](connector-triggers/m-r.md#rowshare) (`rowshare`) | — |
| [RSS](connector-triggers/m-r.md#rss) (`rss`) | .NET, TypeScript |

### S

| Connector | Typed languages |
| --- | --- |
| [SAS Decisioning](connector-triggers/s-z.md#sas-decisioning) (`sasdecisioning`) | — |
| [Scrive eSign](connector-triggers/s-z.md#scrive-esign) (`scriveesign`) | — |
| [SECIB](connector-triggers/s-z.md#secib) (`secib`) | — |
| [Seismic Configuration](connector-triggers/s-z.md#seismic-configuration) (`seismicconfiguration`) | — |
| [Seismic Programs](connector-triggers/s-z.md#seismic-programs) (`seismicprograms`) | — |
| [Service Bus](connector-triggers/s-z.md#service-bus) (`servicebus`) | .NET, TypeScript |
| [SerwerSMS](connector-triggers/s-z.md#serwersms) (`serwersms`) | — |
| [SFTP - SSH](connector-triggers/s-z.md#sftp---ssh) (`sftpwithssh`) | — |
| [SharePoint Embedded](connector-triggers/s-z.md#sharepoint-embedded) (`sharepointembedded`) | — |
| [Shifts for Microsoft Teams](connector-triggers/s-z.md#shifts-for-microsoft-teams) (`shifts`) | — |
| [Shopranos](connector-triggers/s-z.md#shopranos) (`shopranos`) | — |
| [Showcase Workshop](connector-triggers/s-z.md#showcase-workshop) (`showcaseworkshop`) | — |
| [SignatureAPI](connector-triggers/s-z.md#signatureapi) (`signatureapi`) | — |
| [SigningHub Webhooks](connector-triggers/s-z.md#signinghub-webhooks) (`signinghubwebhooks`) | — |
| [SignNow](connector-triggers/s-z.md#signnow) (`signnow`) | — |
| [SignNow EU](connector-triggers/s-z.md#signnow-eu) (`signnoweu`) | — |
| [SignRequest](connector-triggers/s-z.md#signrequest) (`signrequest`) | — |
| [SimpleSurvey](connector-triggers/s-z.md#simplesurvey) (`simplesurvey`) | — |
| [Sinch](connector-triggers/s-z.md#sinch) (`sinch`) | — |
| [SkyPoint Cloud](connector-triggers/s-z.md#skypoint-cloud) (`skypointcloud`) | — |
| [Slack](connector-triggers/s-z.md#slack) (`slack`) | .NET, TypeScript |
| [Slascone](connector-triggers/s-z.md#slascone) (`slascone`) | — |
| [SmartDialog](connector-triggers/s-z.md#smartdialog) (`smartdialog`) | — |
| [Smartsheet](connector-triggers/s-z.md#smartsheet) (`smartsheet`) | — |
| [SmileBack](connector-triggers/s-z.md#smileback) (`smileback`) | — |
| [Snapforms](connector-triggers/s-z.md#snapforms) (`snapforms`) | — |
| [Soft1](connector-triggers/s-z.md#soft1) (`soft1`) | — |
| [Softone Web CRM](connector-triggers/s-z.md#softone-web-crm) (`softonewebcrm`) | — |
| [SparkPost](connector-triggers/s-z.md#sparkpost) (`sparkpost`) | — |
| [Spring Global](connector-triggers/s-z.md#spring-global) (`springglobal`) | — |
| [SQL Server](connector-triggers/s-z.md#sql-server) (`sql`) | .NET, TypeScript |
| [StaffCircle](connector-triggers/s-z.md#staffcircle) (`staffcircle`) | — |
| [Stormboard](connector-triggers/s-z.md#stormboard) (`stormboard`) | — |
| [Strategic Portfolio Manager](connector-triggers/s-z.md#strategic-portfolio-manager) (`strategicportfolioma`) | — |
| [Stripe](connector-triggers/s-z.md#stripe) (`stripe`) | — |
| [Survey123](connector-triggers/s-z.md#survey123) (`survey123`) | — |
| [Survey123 (Test)](connector-triggers/s-z.md#survey123-test) (`survey123test`) | — |
| [SurveyMonkey](connector-triggers/s-z.md#surveymonkey) (`surveymonkey`) | — |
| [SurveyMonkey Canada](connector-triggers/s-z.md#surveymonkey-canada) (`surveymonkeycanada`) | — |

### T

| Connector | Typed languages |
| --- | --- |
| [Tago](connector-triggers/s-z.md#tago) (`tago`) | — |
| [Talkdesk](connector-triggers/s-z.md#talkdesk) (`talkdesk`) | — |
| [Team Forms](connector-triggers/s-z.md#team-forms) (`teamforms`) | — |
| [Teams-Spirit](connector-triggers/s-z.md#teams-spirit) (`teamsspirit`) | — |
| [TeamWherx](connector-triggers/s-z.md#teamwherx) (`actsoft`) | — |
| [Teamwork Projects](connector-triggers/s-z.md#teamwork-projects) (`teamwork`) | — |
| [tegolySIGN](connector-triggers/s-z.md#tegolysign) (`tegolysign`) | — |
| [Telephony Xtended Serv Interf](connector-triggers/s-z.md#telephony-xtended-serv-interf) (`telephonyxtendedsrv`) | — |
| [Telstra SMS](connector-triggers/s-z.md#telstra-sms) (`telstrasms`) | — |
| [Text Request](connector-triggers/s-z.md#text-request) (`textrequest`) | — |
| [The Events Calendar](connector-triggers/s-z.md#the-events-calendar) (`theeventscalendar`) | — |
| [Tikit](connector-triggers/s-z.md#tikit) (`tikit`) | — |
| [Tilkee](connector-triggers/s-z.md#tilkee) (`tilkee`) | — |
| [timeghost](connector-triggers/s-z.md#timeghost) (`timeghost`) | — |
| [Todoist](connector-triggers/s-z.md#todoist) (`todoist`) | — |
| [Toggl Track (Independent Publisher)](connector-triggers/s-z.md#toggl-track-independent-publisher) (`toggltrack`) | — |
| [Toodledo](connector-triggers/s-z.md#toodledo) (`toodledo`) | — |
| [touchSMS](connector-triggers/s-z.md#touchsms) (`touchsmsv2documentat`) | — |
| [TPC Portal](connector-triggers/s-z.md#tpc-portal) (`tpcportal`) | — |
| [Traction Guest](connector-triggers/s-z.md#traction-guest) (`tractionguest`) | — |
| [Trello](connector-triggers/s-z.md#trello) (`trello`) | .NET, TypeScript |
| [Tribal - Maytas](connector-triggers/s-z.md#tribal---maytas) (`tribalmaytas`) | — |
| [Tribal - Platform](connector-triggers/s-z.md#tribal---platform) (`tribal`) | — |
| [Tribal - SITS](connector-triggers/s-z.md#tribal---sits) (`tribalsits`) | — |
| [Trovve](connector-triggers/s-z.md#trovve) (`vineforce`) | — |
| [TrueDialog SMS](connector-triggers/s-z.md#truedialog-sms) (`truedialogsms`) | — |
| [TxtSync](connector-triggers/s-z.md#txtsync) (`txtsync`) | — |
| [tyntec SMS Business](connector-triggers/s-z.md#tyntec-sms-business) (`tyntecsms`) | — |
| [tyntec Viber Business](connector-triggers/s-z.md#tyntec-viber-business) (`tyntecviber`) | — |
| [tyntec WhatsApp Business](connector-triggers/s-z.md#tyntec-whatsapp-business) (`tyntecwa`) | — |
| [Typeform](connector-triggers/s-z.md#typeform) (`typeform`) | — |

### U

| Connector | Typed languages |
| --- | --- |
| [Ubiqod by Skiply](connector-triggers/s-z.md#ubiqod-by-skiply) (`ubiqodbyskiply`) | — |
| [Ubiqod by Taqt](connector-triggers/s-z.md#ubiqod-by-taqt) (`ubiqodbyskiplyv2`) | — |
| [Unique](connector-triggers/s-z.md#unique) (`unique`) | — |
| [Updates App (Microsoft 365)](connector-triggers/s-z.md#updates-app-microsoft-365) (`m365updatesapp`) | — |
| [UserVoice](connector-triggers/s-z.md#uservoice) (`uservoice`) | — |

### V

| Connector | Typed languages |
| --- | --- |
| [Varuna](connector-triggers/s-z.md#varuna) (`varuna`) | — |
| [Vimeo](connector-triggers/s-z.md#vimeo) (`vimeo`) | — |
| [Viva Engage](connector-triggers/s-z.md#viva-engage) (`yammer`) | — |
| [Vocean](connector-triggers/s-z.md#vocean) (`vocean`) | — |
| [Vome](connector-triggers/s-z.md#vome) (`vome`) | — |

### W

| Connector | Typed languages |
| --- | --- |
| [Wauld](connector-triggers/s-z.md#wauld) (`wauld`) | — |
| [Wavelength](connector-triggers/s-z.md#wavelength) (`wavelength`) | — |
| [Way We Do](connector-triggers/s-z.md#way-we-do) (`waywedo`) | — |
| [Webex](connector-triggers/s-z.md#webex) (`webex`) | — |
| [Windows 365](connector-triggers/s-z.md#windows-365) (`windows365`) | — |
| [Witivio](connector-triggers/s-z.md#witivio) (`witivio`) | — |
| [WooCommerce](connector-triggers/s-z.md#woocommerce) (`woocommerce`) | — |
| [WordPress](connector-triggers/s-z.md#wordpress) (`wordpress`) | — |
| [Workday HCM](connector-triggers/s-z.md#workday-hcm) (`workdayhcm`) | — |
| [Workfront Event Subscription (Independent Publisher)](connector-triggers/s-z.md#workfront-event-subscription-independent-publisher) (`workfronteventsubscr`) | — |
| [WorkMobile](connector-triggers/s-z.md#workmobile) (`workmobile`) | — |
| [WorkPoint](connector-triggers/s-z.md#workpoint) (`workpoint`) | — |
| [WorkPoint 365](connector-triggers/s-z.md#workpoint-365) (`workpoint365`) | — |
| [WorkSpan](connector-triggers/s-z.md#workspan) (`workspan`) | — |
| [WP Connectr for WordPress](connector-triggers/s-z.md#wp-connectr-for-wordpress) (`wpconnectrforwordpre`) | — |
| [WPForms by Reenhanced LLC](connector-triggers/s-z.md#wpforms-by-reenhanced-llc) (`wpformsbyreenhancedl`) | — |

### X

| Connector | Typed languages |
| --- | --- |
| [X](connector-triggers/s-z.md#x-1) (`twitter`) | .NET, TypeScript |
| [X12](connector-triggers/s-z.md#x12) (`x12`) | — |
| [Xero Accounting - Magnetism](connector-triggers/s-z.md#xero-accounting---magnetism) (`xeroaccountingmagnet`) | — |

### Y

| Connector | Typed languages |
| --- | --- |
| [YakChat](connector-triggers/s-z.md#yakchat) (`yakchat`) | — |
| [Yeeflow](connector-triggers/s-z.md#yeeflow) (`yeeflow`) | — |
| [YouTube](connector-triggers/s-z.md#youtube) (`youtube`) | — |

### Z

| Connector | Typed languages |
| --- | --- |
| [Zahara](connector-triggers/s-z.md#zahara) (`zahara`) | — |
| [Zellis](connector-triggers/s-z.md#zellis) (`zellis`) | — |
| [Zendesk](connector-triggers/s-z.md#zendesk) (`zendesk`) | .NET, TypeScript |
| [ZIPPYDOC](connector-triggers/s-z.md#zippydoc) (`zippydoc`) | — |
| [Zoho Calendar](connector-triggers/s-z.md#zoho-calendar) (`zohocalendar`) | — |
| [Zoho Forms](connector-triggers/s-z.md#zoho-forms) (`zforms`) | — |
| [Zoho Mail](connector-triggers/s-z.md#zoho-mail) (`zohomail`) | — |
| [Zoho Sign](connector-triggers/s-z.md#zoho-sign) (`zohosign`) | — |
| [zReports](connector-triggers/s-z.md#zreports) (`zreports`) | — |
| [Zvanu Parvaldnieks](connector-triggers/s-z.md#zvanu-parvaldnieks) (`zvanuparvaldnieks`) | — |

## Sources and versions

- Connector Namespace API: `GET /subscriptions/{id}/resourceGroups/{rg}/providers/Microsoft.Web/connectorGateways/{namespace}/managedApis/{connector}?api-version=2026-05-01-preview&export=true`.
- [.NET Connector SDK](https://www.nuget.org/packages/Azure.Connectors.Sdk).
- [Python Functions connector extension](https://pypi.org/project/azurefunctions-extensions-connectors/) and [Python Connector SDK](https://pypi.org/project/azure-connectors/).
- [Node.js Connector SDK](https://www.npmjs.com/package/@azure/connectors) and [Functions connector extension](https://www.npmjs.com/package/@azure/functions-extensions-connectors).
- [How to update this reference](../.github/skills/operations-doc-update/SKILL.md), including [.NET SDK discovery](https://github.com/Azure/Connectors-NET-LSP).
