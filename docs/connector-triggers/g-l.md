# Connector trigger payloads: G–L

[Connector index and type guide](../operations-functions-match.md)

## G

### GetAccept

**Connector ID:** `getaccept`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateRejectedSubscription`](https://learn.microsoft.com/connectors/getaccept/#when-a-document-has-been-rejected) | — | — | — |
| [`CreateReviewedSubscription`](https://learn.microsoft.com/connectors/getaccept/#when-a-document-has-been-reviewed) | — | — | — |
| [`CreateSignedSubscription`](https://learn.microsoft.com/connectors/getaccept/#when-a-document-has-been-signed) | — | — | — |
| [`CreateSubscription`](https://learn.microsoft.com/connectors/getaccept/#subscribe-to-document-events) | — | — | — |

### GitHub

**Connector ID:** `github`

**Type imports:** .NET `Azure.Connectors.Sdk.GitHub.Models`; TypeScript `@azure/connectors/generated/GithubExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IssueAssigned`](https://learn.microsoft.com/connectors/github/#when-an-issue-is-assigned-to-me) | `GitHubOnIssueAssignedTriggerPayload` (item: `IssueDetailsModel`) | — | `GithubOnIssueAssignedTriggerPayload` (item: `IssueDetailsModel`) |
| [`IssueClosed`](https://learn.microsoft.com/connectors/github/#when-an-issue-assigned-to-me-is-closed) | `GitHubOnIssueClosedTriggerPayload` (item: `IssueDetailsModel`) | — | `GithubOnIssueClosedTriggerPayload` (item: `IssueDetailsModel`) |
| [`IssueOpened`](https://learn.microsoft.com/connectors/github/#when-a-new-issue-is-opened-and-assigned-to-me) | `GitHubOnIssueOpenedTriggerPayload` (item: `IssueDetailsModel`) | — | `GithubOnIssueOpenedTriggerPayload` (item: `IssueDetailsModel`) |
| [`WebhookPullRequestTrigger`](https://learn.microsoft.com/connectors/github/#when-a-pull-request-is-created-or-modified-%28preview%29) | — | — | — |

### Glaass Pro

**Connector ID:** `glaasspro`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Case_Closed_Trigger`](https://learn.microsoft.com/connectors/glaasspro/#when-a-case-is-closed) | — | — | — |
| [`Case_Created_Trigger`](https://learn.microsoft.com/connectors/glaasspro/#when-a-new-case-is-created) | — | — | — |
| [`Case_Updated_Trigger`](https://learn.microsoft.com/connectors/glaasspro/#when-an-existing-case-is-updated) | — | — | — |
| [`Metadata_Created_Trigger`](https://learn.microsoft.com/connectors/glaasspro/#when-a-metadata-is-created) | — | — | — |
| [`Metadata_Updated_Trigger`](https://learn.microsoft.com/connectors/glaasspro/#when-a-metadata-is-updated) | — | — | — |

### Gmail

**Connector ID:** `gmail`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewEmail`](https://learn.microsoft.com/connectors/gmail/#when-a-new-email-arrives) | — | — | — |

### GMO Sign

**Connector ID:** `gmosign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateHookEnvelope`](https://learn.microsoft.com/connectors/gmosign/#when-an-envelope-status-changes-%28connect%29) | — | — | — |

### GoFormz

**Connector ID:** `goformz`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormCompleted`](https://learn.microsoft.com/connectors/goformz/#form-completed-webhook) | — | — | — |

### Google Calendar

**Connector ID:** `googlecalendar`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnChangedEventInCalendar`](https://learn.microsoft.com/connectors/googlecalendar/#when-an-event-is-added%2C-updated-or-deleted-from-a-calendar) | — | — | — |
| [`OnDeletedEventInCalendar`](https://learn.microsoft.com/connectors/googlecalendar/#when-an-event-is-deleted-from-a-calendar) | — | — | — |
| [`OnEventStarted`](https://learn.microsoft.com/connectors/googlecalendar/#when-an-event-starts) | — | — | — |
| [`OnNewEventInCalendar`](https://learn.microsoft.com/connectors/googlecalendar/#when-an-event-is-added-to-a-calendar) | — | — | — |
| [`OnUpdatedEventInCalendar`](https://learn.microsoft.com/connectors/googlecalendar/#when-an-event-is-updated-in-a-calendar) | — | — | — |

### Google Contacts

**Connector ID:** `googlecontacts`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`PeopleApiOnContactUpdatedV3`](https://learn.microsoft.com/connectors/googlecontacts/#when-a-contact-changes-v3) | — | — | — |

### Google Tasks

**Connector ID:** `googletasks`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCompletedTaskInList`](https://learn.microsoft.com/connectors/googletasks/#when-a-task-is-completed-in-a-task-list) | — | — | — |
| [`OnCompletedTaskInListV2`](https://learn.microsoft.com/connectors/googletasks/#when-a-task-is-completed-in-a-task-list-%28v2%29-%28preview%29) | — | — | — |
| [`OnDueTaskInList`](https://learn.microsoft.com/connectors/googletasks/#when-a-task-is-due-in-a-task-list) | — | — | — |
| [`OnNewTaskInList`](https://learn.microsoft.com/connectors/googletasks/#when-a-task-is-added-to-a-task-list) | — | — | — |
| [`OnNewTaskList`](https://learn.microsoft.com/connectors/googletasks/#when-a-new-task-list-is-created) | — | — | — |

### GoToMeeting

**Connector ID:** `gotomeeting`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnMeetingComplete`](https://learn.microsoft.com/connectors/gotomeeting/#when-a-meeting-ends) | — | — | — |
| [`OnNewMeeting`](https://learn.microsoft.com/connectors/gotomeeting/#when-a-meeting-is-created) | — | — | — |

### GoToTraining

**Connector ID:** `gototraining`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewRegistration`](https://learn.microsoft.com/connectors/gototraining/#when-a-registrant-is-added) | — | — | — |
| [`OnNewTraining`](https://learn.microsoft.com/connectors/gototraining/#when-a-training-is-created) | — | — | — |

### GoToWebinar

**Connector ID:** `gotowebinar`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewRegistration`](https://learn.microsoft.com/connectors/gotowebinar/#when-a-registrant-is-added) | — | — | — |
| [`OnNewWebinar`](https://learn.microsoft.com/connectors/gotowebinar/#when-a-webinar-is-created) | — | — | — |

### Gratavid

**Connector ID:** `gratavid`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`newEvent`](https://learn.microsoft.com/connectors/gratavid/#when-a-new-event-%28watch%2C-reply%2C-etc.%29-takes-place-in-gratavid) | — | — | — |

### Gravity Forms by reenhanced

**Connector ID:** `gravityformsbyreenhanced`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/gravityformsbyreenhanced/#when-a-form-is-submitted) | — | — | — |

### Gravity Forms Professional

**Connector ID:** `gravityformsprofessi`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerOnEntryDelete`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-entry-is-deleted) | — | — | — |
| [`TriggerOnEntryEntryApprovalUpdate`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-entry-approval-is-changed-%28gravityview%29) | — | — | — |
| [`TriggerOnEntryUpdate`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-entry-is-updated) | — | — | — |
| [`TriggerOnEntryUpdateStatus`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-entry-status-is-updated) | — | — | — |
| [`TriggerOnFormSubmission`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-form-is-submitted) | — | — | — |
| [`TriggerOnNoteAdded`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-gravity-forms-entry-gets-a-note) | — | — | — |
| [`TriggerOnPaymentAction`](https://learn.microsoft.com/connectors/gravityformsprofessi/#when-a-payment-action-occurs-on-a-gravity-forms-entry) | — | — | — |

### Gravity SMS

**Connector ID:** `gravitysms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SmsReceived`](https://learn.microsoft.com/connectors/gravitysms/#when-an-sms-is-received) | — | — | — |

### Groopit

**Connector ID:** `groopit`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscribeToLinkedMembers`](https://learn.microsoft.com/connectors/groopit/#when-a-user-is-added-to-a-group) | — | — | — |
| [`SubscribeToReports`](https://learn.microsoft.com/connectors/groopit/#when-an-insight-is-shared-or-edited-in-a-data-model) | — | — | — |
| [`SubscribeToReports2`](https://learn.microsoft.com/connectors/groopit/#when-an-insight-is-created-or-edited-in-a-group) | — | — | — |

## H

### Harvest

**Connector ID:** `harvest`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigNewClient`](https://learn.microsoft.com/connectors/harvest/#when-a-client-is-added) | — | — | — |
| [`TrigNewContact`](https://learn.microsoft.com/connectors/harvest/#when-a-contact-is-added) | — | — | — |
| [`TrigNewProject`](https://learn.microsoft.com/connectors/harvest/#when-a-project-is-created) | — | — | — |
| [`TrigNewTimeEntry`](https://learn.microsoft.com/connectors/harvest/#when-a-time-entry-is-created-for-date) | — | — | — |
| [`TrigNewTimeEntryToday`](https://learn.microsoft.com/connectors/harvest/#when-a-time-entry-is-created-for-the-current-day) | — | — | — |
| [`TrigNewUser`](https://learn.microsoft.com/connectors/harvest/#when-a-user-is-added) | — | — | — |

### HelloSign

**Connector ID:** `hellosign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewRequest`](https://learn.microsoft.com/connectors/hellosign/#when-a-signature-request-is-created) | — | — | — |
| [`OnRequestCompleted`](https://learn.microsoft.com/connectors/hellosign/#when-a-signature-request-is-completed) | — | — | — |
| [`OnRequestDeclined`](https://learn.microsoft.com/connectors/hellosign/#when-a-signature-request-is-declined) | — | — | — |

### HighGear Workflow

**Connector ID:** `highgearworkflow`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscriptionSubscribe`](https://learn.microsoft.com/connectors/highgearworkflow/#when-a-task-enters-a-web-request-node) | — | — | — |

### HipChat

**Connector ID:** `hipchat`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewFile`](https://learn.microsoft.com/connectors/hipchat/#when-a-file-is-uploaded-to-a-room) | — | — | — |
| [`OnNewMessage`](https://learn.microsoft.com/connectors/hipchat/#when-a-message-is-posted-to-a-room) | — | — | — |
| [`OnNewRoom`](https://learn.microsoft.com/connectors/hipchat/#when-a-new-room-is-added) | — | — | — |

### Honeywell Forge

**Connector ID:** `honeywellforge`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ServiceCaseCreated`](https://learn.microsoft.com/connectors/honeywellforge/#register-a-webhook-that-allows-you-to-receive-open-service-cases) | — | — | — |

### HouseRater QA

**Connector ID:** `houseraterqa`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`InspectionStatusChange`](https://learn.microsoft.com/connectors/houseraterqa/#inspection-status-change) | — | — | — |

### Huddle

**Connector ID:** `huddle`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`PollFolderForFileUpload`](https://learn.microsoft.com/connectors/huddle/#poll-a-target-folder-for-it%27s-most-recent-upload) | — | — | — |
| [`PollWorkspaceForNewApproval`](https://learn.microsoft.com/connectors/huddle/#poll-a-target-workspace-for-it%27s-most-recent-approval-request) | — | — | — |

### Huddle for US Gov &amp; Healthcare

**Connector ID:** `huddleforusgovhealth`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`PollFolderForFileUpload`](https://learn.microsoft.com/connectors/huddleforusgovhealth/#poll-a-target-folder-for-it%27s-most-recent-upload) | — | — | — |
| [`PollWorkspaceForNewApproval`](https://learn.microsoft.com/connectors/huddleforusgovhealth/#poll-a-target-workspace-for-it%27s-most-recent-approval-request) | — | — | — |

### Huddo Boards

**Connector ID:** `huddoboards`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AddedToBoard`](https://learn.microsoft.com/connectors/huddoboards/#board-added) | — | — | — |
| [`AssignedTask`](https://learn.microsoft.com/connectors/huddoboards/#assigned-task) | — | — | — |
| [`BoardTaskCompleted`](https://learn.microsoft.com/connectors/huddoboards/#task-completed-in-board) | — | — | — |
| [`CreatedNode`](https://learn.microsoft.com/connectors/huddoboards/#card-added-to-board) | — | — | — |
| [`MyTaskCompleted`](https://learn.microsoft.com/connectors/huddoboards/#completed-task) | — | — | — |

## I

### IcM

**Connector ID:** `icm`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `When_an_IcM_incident_is_created` | — | — | — |
| `When_an_IcM_incident_is_modified` | — | — | — |

### Ideanote

**Connector ID:** `ideanote`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FinishRating`](https://learn.microsoft.com/connectors/ideanote/#when-someone-rates-an-idea) | — | — | — |
| [`NewComment`](https://learn.microsoft.com/connectors/ideanote/#when-a-comment-is-created) | — | — | — |
| [`NewIdea`](https://learn.microsoft.com/connectors/ideanote/#when-an-idea-is-created) | — | — | — |
| [`NewLike`](https://learn.microsoft.com/connectors/ideanote/#when-an-idea-or-comment-is-liked) | — | — | — |
| [`NewUser`](https://learn.microsoft.com/connectors/ideanote/#when-a-user-is-created) | — | — | — |
| [`UpdatedIdeaPhase`](https://learn.microsoft.com/connectors/ideanote/#when-the-phase-of-an-idea-is-changed) | — | — | — |
| [`UpdatedIdeaStatus`](https://learn.microsoft.com/connectors/ideanote/#when-the-status-of-an-idea-is-changed) | — | — | — |

### iManage Work

**Connector ID:** `imanagework`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`MultipleSelectedDocuments`](https://learn.microsoft.com/connectors/imanagework/#for-multiple-selected-documents) | — | — | — |
| [`MultipleSelectedWorkspaces`](https://learn.microsoft.com/connectors/imanagework/#for-multiple-selected-workspaces) | — | — | — |
| [`RegisterFormWorkflow`](https://learn.microsoft.com/connectors/imanagework/#when-a-form-response-is-submitted) | — | — | — |
| [`RegisterGenericWorkflow`](https://learn.microsoft.com/connectors/imanagework/#start-a-workflow) | — | — | — |
| [`SingleSelectedDocument`](https://learn.microsoft.com/connectors/imanagework/#for-a-selected-document) | — | — | — |
| [`SingleSelectedWorkspace`](https://learn.microsoft.com/connectors/imanagework/#for-a-selected-workspace) | — | — | — |

### iMIS

**Connector ID:** `imis`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ActivityTriggerPoll`](https://learn.microsoft.com/connectors/imis/#when-an-activity-is-added) | — | — | — |
| [`OrderTriggerPoll`](https://learn.microsoft.com/connectors/imis/#when-an-order-is-added) | — | — | — |

### Impexium

**Connector ID:** `impexium`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`When-Committee-Member-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-committee-member-is-updated) | — | — | — |
| [`When-Customer-Address-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-address-is-updated) | — | — | — |
| [`When-Customer-Custom-Field-Value-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-custom-field-value-is-updated) | — | — | — |
| [`When-Customer-Is-Merged`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-is-merged) | — | — | — |
| [`When-Customer-Phone-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-phone-is-updated) | — | — | — |
| [`When-Customer-Relationship-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-relationship-is-updated) | — | — | — |
| [`When-Email-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-email-is-updated) | — | — | — |
| [`When-Event-Registration-Substituted`](https://learn.microsoft.com/connectors/impexium/#when-an-event-registration-is-substituted) | — | — | — |
| [`When-Individual-Created`](https://learn.microsoft.com/connectors/impexium/#when-an-individual-is-created) | — | — | — |
| [`When-Individual-Deleted`](https://learn.microsoft.com/connectors/impexium/#when-an-individual-is-deleted) | — | — | — |
| [`When-Individual-Request-Forgotten`](https://learn.microsoft.com/connectors/impexium/#when-an-individual-requests-to-be-forgotten) | — | — | — |
| [`When-Membership-Terminated`](https://learn.microsoft.com/connectors/impexium/#when-a-membership-is-terminated) | — | — | — |
| [`When-product-purchased`](https://learn.microsoft.com/connectors/impexium/#when-a-product-is-purchased) | — | — | — |
| [`When-purchase-cancelled`](https://learn.microsoft.com/connectors/impexium/#when-a-purchase-is-canceled) | — | — | — |
| [`When-Purchase-Paid`](https://learn.microsoft.com/connectors/impexium/#when-a-purchase-is-paid) | — | — | — |
| [`When-Request-Updated`](https://learn.microsoft.com/connectors/impexium/#when-a-customer-request-is-updated) | — | — | — |

### Impower ERP

**Connector ID:** `impower`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`createConnectionUsingPOST`](https://learn.microsoft.com/connectors/impower/#create-an-application-connection) | — | — | — |

### Imprezian360-CRM

**Connector ID:** `imprezian`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`New-Marketing-Campaign`](https://learn.microsoft.com/connectors/imprezian/#new-marketing-campaign-created) | — | — | — |
| [`New-Members-Leads`](https://learn.microsoft.com/connectors/imprezian/#new-lead-campaign-members) | — | — | — |
| [`New-Proposal-Created`](https://learn.microsoft.com/connectors/imprezian/#new-proposal-created) | — | — | — |
| [`New-Sales-Lead`](https://learn.microsoft.com/connectors/imprezian/#new-sales-lead-created) | — | — | — |
| [`New-Sales-Order`](https://learn.microsoft.com/connectors/imprezian/#new-sales-order-created) | — | — | — |
| [`Order-In-History`](https://learn.microsoft.com/connectors/imprezian/#order-in-history) | — | — | — |
| [`Order-In-Processing`](https://learn.microsoft.com/connectors/imprezian/#order-in-processing) | — | — | — |
| [`Order-Is-Shipping`](https://learn.microsoft.com/connectors/imprezian/#order-is-shipping) | — | — | — |
| [`Order-Status-Changed`](https://learn.microsoft.com/connectors/imprezian/#order-status-has-changed) | — | — | — |
| [`Proposal-Needs-Approval`](https://learn.microsoft.com/connectors/imprezian/#proposal-needs-approval) | — | — | — |
| [`WorkOrder-Closed`](https://learn.microsoft.com/connectors/imprezian/#workorder-closed) | — | — | — |
| [`WorkOrder-Created`](https://learn.microsoft.com/connectors/imprezian/#workorder-created) | — | — | — |
| [`WorkOrder-Past-Due`](https://learn.microsoft.com/connectors/imprezian/#workorder-is-past-due) | — | — | — |
| [`WorkOrder-Status-Changed`](https://learn.microsoft.com/connectors/imprezian/#workorder-status-changed) | — | — | — |

### Infobip

**Connector ID:** `infobip`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateInfobipSMSWebhook`](https://learn.microsoft.com/connectors/infobip/#when-you-receive-an-incoming-text-message) | — | — | — |

### Infusionsoft

**Connector ID:** `infusionsoft`

**Type imports:** .NET `Azure.Connectors.Sdk.Infusionsoft.Models`; TypeScript `@azure/connectors/generated/InfusionsoftExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewOrder`](https://learn.microsoft.com/connectors/infusionsoft/#when-there-is-a-new-order) | `InfusionsoftOnNewOrderTriggerPayload` (item: `JsonElement?`) | — | `InfusionsoftOnNewOrderTriggerPayload` (item: `Record<string, unknown>`) |
| [`OnNewTask`](https://learn.microsoft.com/connectors/infusionsoft/#when-a-new-task-is-created) | `InfusionsoftOnNewTaskTriggerPayload` (item: `TaskResponse`) | — | `InfusionsoftOnNewTaskTriggerPayload` (item: `TaskResponse`) |

### Inoreader

**Connector ID:** `inoreader`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewSubscription`](https://learn.microsoft.com/connectors/inoreader/#when-a-new-subscription-is-added) | — | — | — |
| [`OnUnreadItemCountForStreamExceedsTarget`](https://learn.microsoft.com/connectors/inoreader/#when-unread-count-reaches-a-target) | — | — | — |

### Insightly

**Connector ID:** `insightly`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnContactCreated`](https://learn.microsoft.com/connectors/insightly/#when-a-contact-is-created) | — | — | — |
| [`OnContactUpdated`](https://learn.microsoft.com/connectors/insightly/#when-a-contact-is-updated) | — | — | — |
| [`OnEventCreated`](https://learn.microsoft.com/connectors/insightly/#when-an-event-is-created) | — | — | — |
| [`OnEventUpdated`](https://learn.microsoft.com/connectors/insightly/#when-an-event-is-updated) | — | — | — |
| [`OnLeadCreated`](https://learn.microsoft.com/connectors/insightly/#when-a-lead-is-created) | — | — | — |
| [`OnLeadUpdated`](https://learn.microsoft.com/connectors/insightly/#when-a-lead-is-updated) | — | — | — |
| [`OnProjectCreated`](https://learn.microsoft.com/connectors/insightly/#when-a-project-is-created) | — | — | — |
| [`OnProjectUpdated`](https://learn.microsoft.com/connectors/insightly/#when-a-project-is-updated) | — | — | — |
| [`OnTaskAssignedToMe`](https://learn.microsoft.com/connectors/insightly/#when-a-task-is-assigned-to-me) | — | — | — |
| [`OnTaskCreated`](https://learn.microsoft.com/connectors/insightly/#when-a-task-is-created) | — | — | — |
| [`OnTaskUpdated`](https://learn.microsoft.com/connectors/insightly/#when-a-task-is-updated) | — | — | — |

### Instapaper

**Connector ID:** `instapaper`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnBookmarkAdded`](https://learn.microsoft.com/connectors/instapaper/#when-a-bookmark-is-added-to-a-folder) | — | — | — |
| [`OnBookmarkArchived`](https://learn.microsoft.com/connectors/instapaper/#when-a-bookmark-is-archived) | — | — | — |
| [`OnBookmarkLiked`](https://learn.microsoft.com/connectors/instapaper/#when-a-bookmark-is-liked) | — | — | — |
| [`OnBookmarkProgressRead`](https://learn.microsoft.com/connectors/instapaper/#when-a-bookmark-is-marked-read) | — | — | — |
| [`OnBookmarkProgressUpdated`](https://learn.microsoft.com/connectors/instapaper/#when-the-read-progress-of-a-bookmark-is-updated) | — | — | — |
| [`OnBookmarkRemoved`](https://learn.microsoft.com/connectors/instapaper/#when-a-bookmark-is-removed-from-a-folder) | — | — | — |
| [`OnFolderCreated`](https://learn.microsoft.com/connectors/instapaper/#when-a-folder-is-created) | — | — | — |
| [`OnHighlightAdded`](https://learn.microsoft.com/connectors/instapaper/#when-a-highlight-is-added-to-a-bookmark) | — | — | — |

### intelliHR

**Connector ID:** `intellihr`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/intellihr/#create-a-webhook-and-subscribe-to-a-type-of-event-in-your-tenant) | — | — | — |

### Intercom

**Connector ID:** `intercom`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigNewConversation`](https://learn.microsoft.com/connectors/intercom/#when-a-new-conversation-starts) | — | — | — |
| [`TrigNewLead`](https://learn.microsoft.com/connectors/intercom/#when-a-lead-is-created) | — | — | — |
| [`TrigNewUser`](https://learn.microsoft.com/connectors/intercom/#when-a-user-is-created) | — | — | — |
| [`TrigUpdateUser`](https://learn.microsoft.com/connectors/intercom/#when-a-user-is-updated) | — | — | — |

### ISOPlanner

**Connector ID:** `isoplanner`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`EventCreated`](https://learn.microsoft.com/connectors/isoplanner/#when-an-event-is-created) | — | — | — |
| [`EventDeleted`](https://learn.microsoft.com/connectors/isoplanner/#when-an-event-is-deleted) | — | — | — |
| [`EventUpdated`](https://learn.microsoft.com/connectors/isoplanner/#when-an-event-is-updated) | — | — | — |
| [`MonitoringTaskCreated`](https://learn.microsoft.com/connectors/isoplanner/#when-an-annual-plan-task-is-started) | — | — | — |
| [`RiskCreated`](https://learn.microsoft.com/connectors/isoplanner/#when-a-risk-is-created) | — | — | — |
| [`RiskDeleted`](https://learn.microsoft.com/connectors/isoplanner/#when-a-risk-is-deleted) | — | — | — |
| [`RiskUpdated`](https://learn.microsoft.com/connectors/isoplanner/#when-a-risk-is-updated) | — | — | — |
| [`TaskCreated`](https://learn.microsoft.com/connectors/isoplanner/#when-a-task-is-created) | — | — | — |
| [`TaskDeleted`](https://learn.microsoft.com/connectors/isoplanner/#when-a-task-is-deleted) | — | — | — |
| [`TaskUpdated`](https://learn.microsoft.com/connectors/isoplanner/#when-a-task-is-updated) | — | — | — |

## J

### Jira

**Connector ID:** `jira`

**Type imports:** .NET `Azure.Connectors.Sdk.Jira.Models`; TypeScript `@azure/connectors/generated/JiraExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCloseIssue_Datacenter`](https://learn.microsoft.com/connectors/jira/#when-an-issue-is-closed-%28datacenter%29) | `JiraOnCloseIssueDatacenterTriggerPayload` (item: `FullIssue`) | — | `JiraOnCloseIssueDatacenterTriggerPayload` (item: `FullIssue`) |
| [`OnCloseIssue_V2`](https://learn.microsoft.com/connectors/jira/#when-an-issue-is-closed-%28v2%29) | `JiraOnCloseIssueTriggerPayload` (item: `FullIssue`) | — | `JiraOnCloseIssueTriggerPayload` (item: `FullIssue`) |
| [`OnNewIssue_Datacenter`](https://learn.microsoft.com/connectors/jira/#when-a-new-issue-is-created-%28datacenter%29) | `JiraOnNewIssueDatacenterTriggerPayload` (item: `FullIssue`) | — | `JiraOnNewIssueDatacenterTriggerPayload` (item: `FullIssue`) |
| [`OnNewIssue_V2`](https://learn.microsoft.com/connectors/jira/#when-a-new-issue-is-created-%28v2%29) | `JiraOnNewIssueTriggerPayload` (item: `FullIssue`) | — | `JiraOnNewIssueTriggerPayload` (item: `FullIssue`) |
| [`OnNewIssueJQL_Datacenter`](https://learn.microsoft.com/connectors/jira/#when-a-new-issue-is-returned-by-a-jql-query-%28datacenter%29) | `JiraOnNewIssueJQLDatacenterTriggerPayload` (item: `FullIssue`) | — | `JiraOnNewIssueJQLDatacenterTriggerPayload` (item: `FullIssue`) |
| [`OnNewIssueJQL_V2`](https://learn.microsoft.com/connectors/jira/#when-a-new-issue-is-returned-by-a-jql-query-%28v2%29) | `JiraOnNewIssueJQLTriggerPayload` (item: `FullIssue`) | — | `JiraOnNewIssueJQLTriggerPayload` (item: `FullIssue`) |

### JotForm

**Connector ID:** `jotform`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerNewResponse`](https://learn.microsoft.com/connectors/jotform/#when-a-response-is-submitted) | — | — | — |

### Jotform Enterprise

**Connector ID:** `jotformenterprise`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookTrigger_V2`](https://learn.microsoft.com/connectors/jotformenterprise/#when-a-response-is-submitted-%28v2%29) | — | — | — |

### JungleMail 365

**Connector ID:** `junglemail365`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Webhook_JobCompleted`](https://learn.microsoft.com/connectors/junglemail365/#when-newsletter-processing-is-completed) | — | — | — |
| [`Webhook_JobStarted`](https://learn.microsoft.com/connectors/junglemail365/#when-newsletter-processing-is-started) | — | — | — |
| [`Webhook_JobSumitted`](https://learn.microsoft.com/connectors/junglemail365/#when-a-newsletter-is-submitted) | — | — | — |

## K

### kintone

**Connector ID:** `kintone`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AddWebhookForAddingRecordComment`](https://learn.microsoft.com/connectors/kintone/#when-a-record-comment-is-posted-on-an-app) | — | — | — |
| [`AddWebhookForDeletingRecord`](https://learn.microsoft.com/connectors/kintone/#when-an-app-record-is-deleted) | — | — | — |
| [`AddWebhookForUpdatingRecord`](https://learn.microsoft.com/connectors/kintone/#when-an-app-record-is-updated) | — | — | — |
| [`AddWebhookForUpdatingStatus`](https://learn.microsoft.com/connectors/kintone/#when-a-process-management-status-is-updated) | — | — | — |
| [`webhook-trigger`](https://learn.microsoft.com/connectors/kintone/#when-a-record-is-added-to-an-app) | — | — | — |

## L

### Lawlift

**Connector ID:** `lawlift`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`LawliftExportTrigger`](https://learn.microsoft.com/connectors/lawlift/#when-documents-are-exported) | — | — | — |
| [`LawliftNotificationTrigger`](https://learn.microsoft.com/connectors/lawlift/#when-a-questionnaire-is-submitted) | — | — | — |

### LawVu

**Connector ID:** `lawvu`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Triggers_CreateSubscriptionContractCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-is-created) | — | — | — |
| [`Triggers_CreateSubscriptionContractDocumentUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-document-is-updated) | — | — | — |
| [`Triggers_CreateSubscriptionContractFileCreatedOrUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-file-is-added-or-updated-in-a-contract) | — | — | — |
| [`Triggers_CreateSubscriptionContractKeyDateCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-key-date-is-created) | — | — | — |
| [`Triggers_CreateSubscriptionContractKeyDateUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-key-date-is-updated) | — | — | — |
| [`Triggers_CreateSubscriptionContractStatusMessageCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-status-message-is-posted-to-a-contract) | — | — | — |
| [`Triggers_CreateSubscriptionContractStatusUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-state-changes) | — | — | — |
| [`Triggers_CreateSubscriptionContractUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-contract-is-updated) | — | — | — |
| [`Triggers_CreateSubscriptionMatterCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-matter-is-created) | — | — | — |
| [`Triggers_CreateSubscriptionMatterFileCreatedOrUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-file-is-added-or-updated-in-a-matter) | — | — | — |
| [`Triggers_CreateSubscriptionMatterStatusMessageCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-status-message-is-posted-to-a-matter) | — | — | — |
| [`Triggers_CreateSubscriptionMatterStatusUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-matter-state-changes) | — | — | — |
| [`Triggers_CreateSubscriptionMatterTagCreated`](https://learn.microsoft.com/connectors/lawvu/#when-a-tag-is-added-to-a-matter) | — | — | — |
| [`Triggers_CreateSubscriptionMatterUpdated`](https://learn.microsoft.com/connectors/lawvu/#when-a-matter-is-updated) | — | — | — |

### LCP - iCordis

**Connector ID:** `lcpicordis`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewEntry`](https://learn.microsoft.com/connectors/lcpicordis/#when-a-new-submission-is-created-for-the-e-loket-form) | — | — | — |

### Leadinfo

**Connector ID:** `leadinfo`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`share-company`](https://learn.microsoft.com/connectors/leadinfo/#when-company-data-is-shared) | — | — | — |
| [`share-contact`](https://learn.microsoft.com/connectors/leadinfo/#when-a-contact-is-shared) | — | — | — |
| [`trigger-company`](https://learn.microsoft.com/connectors/leadinfo/#when-a-company-is-identified) | — | — | — |
| [`trigger-contact`](https://learn.microsoft.com/connectors/leadinfo/#when-a-contact-is-identified) | — | — | — |

### LeanKit

**Connector ID:** `leankit`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigNewCard`](https://learn.microsoft.com/connectors/leankit/#when-a-new-card-is-added) | — | — | — |
| [`TrigUpdateCard`](https://learn.microsoft.com/connectors/leankit/#when-a-card-is-updated) | — | — | — |

### Legalesign

**Connector ID:** `legalesign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`DocumentTrigger`](https://learn.microsoft.com/connectors/legalesign/#document-event) | — | — | — |
| [`RecipientTrigger`](https://learn.microsoft.com/connectors/legalesign/#recipient-event) | — | — | — |

### Lexoffice (Independent Publisher)

**Connector ID:** `lexoffice`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`EventSubscription_Lexoffice`](https://learn.microsoft.com/connectors/lexoffice/#select-your-custom-trigger) | — | — | — |

### LiveTiles Bots

**Connector ID:** `livetilesbots`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IntentRecognized`](https://learn.microsoft.com/connectors/livetilesbots/#when-an-intent-is-recognized) | — | — | — |

### LMS365

**Connector ID:** `lms365`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CourseCompleted`](https://learn.microsoft.com/connectors/lms365/#when-a-learner-has-completed-training) | — | — | — |
| [`CourseCreated`](https://learn.microsoft.com/connectors/lms365/#when-training-has-been-created) | — | — | — |
| [`CourseDeleted`](https://learn.microsoft.com/connectors/lms365/#when-training-has-been-deleted-from-a-catalog) | — | — | — |
| [`CourseEnrollment`](https://learn.microsoft.com/connectors/lms365/#when-a-learner-has-enrolled-into-training) | — | — | — |
| [`CoursePublished`](https://learn.microsoft.com/connectors/lms365/#when-training-has-been-published-to-a-catalog) | — | — | — |
| [`CourseStarted`](https://learn.microsoft.com/connectors/lms365/#when-a-learner-has-started-any-of-the-course-activities) | — | — | — |
| [`CourseUnenrollment`](https://learn.microsoft.com/connectors/lms365/#when-a-learner-has-unenrolled-from-training) | — | — | — |
| [`CourseUnpublished`](https://learn.microsoft.com/connectors/lms365/#when-training-has-been-unpublished-from-a-catalog) | — | — | — |
| [`EnrollmentApprovalRequest`](https://learn.microsoft.com/connectors/lms365/#when-a-learner-has-requested-approval-for-training) | — | — | — |
| [`UserCreated`](https://learn.microsoft.com/connectors/lms365/#when-a-user-has-been-created) | — | — | — |
| [`UserDeleted`](https://learn.microsoft.com/connectors/lms365/#when-a-user-has-been-deleted) | — | — | — |

### Loopio

**Connector ID:** `loopio`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhookSubscription`](https://learn.microsoft.com/connectors/loopio/#create-a-new-event-subscription) | — | — | — |

### Loopio-EU

**Connector ID:** `loopioeu`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhookSubscription`](https://learn.microsoft.com/connectors/loopioeu/#create-a-new-event-subscription) | — | — | — |

### Loopio-Int01

**Connector ID:** `loopioint01`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhookSubscription`](https://learn.microsoft.com/connectors/loopioint01/#create-a-new-event-subscription) | — | — | — |

### Luware Nimbus

**Connector ID:** `luwarenimbus`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`RegisterSchedulerEntryTrigger`](https://learn.microsoft.com/connectors/luwarenimbus/#when-a-scheduler-entry-changes-state) | — | — | — |
| [`RegisterTaskWebHook`](https://learn.microsoft.com/connectors/luwarenimbus/#when-a-task-changes-state) | — | — | — |
| [`RegisterVirtualUserAssistantTrigger`](https://learn.microsoft.com/connectors/luwarenimbus/#when-the-virtual-user-assistant-has-an-update) | — | — | — |

[Back to connector index](../operations-functions-match.md)
