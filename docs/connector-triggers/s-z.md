# Connector trigger payloads: S–Z

[Connector index and type guide](../operations-functions-match.md)

## S

### SAS Decisioning

**Connector ID:** `sasdecisioning`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`createGenericWebhook`](https://learn.microsoft.com/connectors/sasdecisioning/#when-a-pre-defined-event-happened) | — | — | — |
| [`createPublishWebhook`](https://learn.microsoft.com/connectors/sasdecisioning/#when-a-resource-is-published) | — | — | — |
| [`createValidateWebhook`](https://learn.microsoft.com/connectors/sasdecisioning/#when-a-resource-is-validated) | — | — | — |

### Scrive eSign

**Connector ID:** `scriveesign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`PollSignedDocuments`](https://learn.microsoft.com/connectors/scriveesign/#when-a-document-is-signed) | — | — | — |
| [`StartAndOnDocumentSign`](https://learn.microsoft.com/connectors/scriveesign/#when-document-is-signed-by-all-signatories-%28start-signing%29) | — | — | — |
| [`WebhookFromTemplateSign`](https://learn.microsoft.com/connectors/scriveesign/#when-a-document-from-a-template-is-signed-by-all-signatories) | — | — | — |

### SECIB

**Connector ID:** `secib`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`DossierCreated`](https://learn.microsoft.com/connectors/secib/#when-a-case-file-is-created) | — | — | — |

### Seismic Configuration

**Connector ID:** `seismicconfiguration`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscribeToEvents`](https://learn.microsoft.com/connectors/seismicconfiguration/#create-a-webhook-subscription) | — | — | — |

### Seismic Programs

**Connector ID:** `seismicprograms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerProgramCreateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-program-is-created) | — | — | — |
| [`TriggerProgramDeleteV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-program-is-deleted) | — | — | — |
| [`TriggerProgramRequestCreateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-request-is-created) | — | — | — |
| [`TriggerProgramRequestDeleteV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-request-is-deleted) | — | — | — |
| [`TriggerProgramRequestUpdateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-request-is-updated) | — | — | — |
| [`TriggerProgramTaskCreateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-task-is-created) | — | — | — |
| [`TriggerProgramTaskDeleteV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-task-is-deleted) | — | — | — |
| [`TriggerProgramTaskUpdateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-task-is-updated) | — | — | — |
| [`TriggerProgramUpdateV1`](https://learn.microsoft.com/connectors/seismicprograms/#when-a-program-is-updated) | — | — | — |

### Service Bus

**Connector ID:** `servicebus`

**Type imports:** .NET `Azure.Connectors.Sdk.Servicebus.Models`; TypeScript `@azure/connectors/generated/ServicebusExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetMessageFromQueue`](https://learn.microsoft.com/connectors/servicebus/#when-a-message-is-received-in-a-queue-%28auto-complete%29) | — | — | — |
| [`GetMessageFromTopic`](https://learn.microsoft.com/connectors/servicebus/#when-a-message-is-received-in-a-topic-subscription-%28auto-complete%29) | — | — | — |
| [`GetMessagesFromQueue`](https://learn.microsoft.com/connectors/servicebus/#when-one-or-more-messages-arrive-in-a-queue-%28auto-complete%29) | `ServicebusOnGetMessagesFromQueueTriggerPayload` (item: `ServiceBusMessage`) | — | `ServicebusOnGetMessagesFromQueueTriggerPayload` (item: `ServiceBusMessage`) |
| [`GetMessagesFromTopic`](https://learn.microsoft.com/connectors/servicebus/#when-one-or-more-messages-arrive-in-a-topic-%28auto-complete%29) | `ServicebusOnGetMessagesFromTopicTriggerPayload` (item: `ServiceBusMessage`) | — | `ServicebusOnGetMessagesFromTopicTriggerPayload` (item: `ServiceBusMessage`) |
| [`GetNewMessageFromQueueWithPeekLock`](https://learn.microsoft.com/connectors/servicebus/#when-a-message-is-received-in-a-queue-%28peek-lock%29) | — | — | — |
| [`GetNewMessageFromTopicWithPeekLock`](https://learn.microsoft.com/connectors/servicebus/#when-a-message-is-received-in-a-topic-subscription-%28peek-lock%29) | — | — | — |
| [`GetNewMessagesFromQueueWithPeekLock`](https://learn.microsoft.com/connectors/servicebus/#when-one-or-more-messages-arrive-in-a-queue-%28peek-lock%29) | `ServicebusOnGetNewMessagesFromQueueWithPeekLockTriggerPayload` (item: `ServiceBusMessage`) | — | `ServicebusOnGetNewMessagesFromQueueWithPeekLockTriggerPayload` (item: `ServiceBusMessage`) |
| [`GetNewMessagesFromTopicWithPeekLock`](https://learn.microsoft.com/connectors/servicebus/#when-one-or-more-messages-arrive-in-a-topic-%28peek-lock%29) | `ServicebusOnGetNewMessagesFromTopicWithPeekLockTriggerPayload` (item: `ServiceBusMessage`) | — | `ServicebusOnGetNewMessagesFromTopicWithPeekLockTriggerPayload` (item: `ServiceBusMessage`) |

### SerwerSMS

**Connector ID:** `serwersms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`new_answer`](https://learn.microsoft.com/connectors/serwersms/#when-new-sms-message-is-received) | — | — | — |

### SFTP - SSH

**Connector ID:** `sftpwithssh`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnUpdatedFile`](https://learn.microsoft.com/connectors/sftpwithssh/#when-a-file-is-added-or-modified) | — | — | — |
| [`OnUpdatedFiles`](https://learn.microsoft.com/connectors/sftpwithssh/#when-files-are-added-or-modified-%28properties-only%29) | — | — | — |

### SharePoint Embedded

**Connector ID:** `sharepointembedded`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetChangesContainer`](https://learn.microsoft.com/connectors/sharepointembedded/#monitor-item-changes-in-a-container) | — | — | — |

### Shifts for Microsoft Teams

**Connector ID:** `shifts`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerForOfferShiftRequests`](https://learn.microsoft.com/connectors/shifts/#when-an-offer-shift-request-is-created%2C-updated-or-deleted) | — | — | — |
| [`TriggerForOpenShiftChangeRequests`](https://learn.microsoft.com/connectors/shifts/#when-an-open-shift-request-is-created%2C-updated-or-deleted) | — | — | — |
| [`TriggerForShifts`](https://learn.microsoft.com/connectors/shifts/#when-a-shift-is-created%2C-updated-or-deleted) | — | — | — |
| [`TriggerForSwapShiftsChangeRequests`](https://learn.microsoft.com/connectors/shifts/#when-a-swap-shifts-request-is-created%2C-updated-or-deleted) | — | — | — |
| [`TriggerForTimeOffRequests`](https://learn.microsoft.com/connectors/shifts/#when-a-time-off-request-is-created%2C-updated-or-deleted) | — | — | — |

### Shopranos

**Connector ID:** `shopranos`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AttributeCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attribute-is-created) | — | — | — |
| [`AttributeDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attibute-is-deleted) | — | — | — |
| [`AttributeSetCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attributeset-is-created) | — | — | — |
| [`AttributeSetDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attibuteset-is-deleted) | — | — | — |
| [`AttributeSetUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attributeset-is-updated) | — | — | — |
| [`AttributeUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-attribute-is-updated) | — | — | — |
| [`BrandCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-brand-is-created) | — | — | — |
| [`BrandDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-brand-is-deleted) | — | — | — |
| [`BrandUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-brand-is-updated) | — | — | — |
| [`CategoryCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-category-is-created) | — | — | — |
| [`CategoryDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-category-is-deleted) | — | — | — |
| [`CategoryUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-category-is-updated) | — | — | — |
| [`CheckoutCompletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-checkout-is-completed) | — | — | — |
| [`CheckoutCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-checkout-is-created) | — | — | — |
| [`CustomerCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-customer-is-created) | — | — | — |
| [`CustomerDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-customer-is-deleted) | — | — | — |
| [`CustomerUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-customer-is-updated) | — | — | — |
| [`InventoryLevelCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-inventory-level-is-created) | — | — | — |
| [`InventoryLevelDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-inventory-level-is-deleted) | — | — | — |
| [`InventoryLevelUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-inventory-level-is-updated) | — | — | — |
| [`OrderCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-order-is-created) | — | — | — |
| [`OrderDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-order-is-deleted) | — | — | — |
| [`OrderUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-an-order-is-updated) | — | — | — |
| [`PaymentCompletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-payment-is-completed) | — | — | — |
| [`PaymentInitiatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-payment-is-initiated) | — | — | — |
| [`ProductCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-product-is-created) | — | — | — |
| [`ProductDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-product-is-deleted) | — | — | — |
| [`ProductUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-product-is-updated) | — | — | — |
| [`UnitCreatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-unit-is-created) | — | — | — |
| [`UnitDeletedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-unit-is-deleted) | — | — | — |
| [`UnitUpdatedTrigger`](https://learn.microsoft.com/connectors/shopranos/#when-a-unit-is-updated) | — | — | — |

### Showcase Workshop

**Connector ID:** `showcaseworkshop`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ShowcaseSharedPageDownload`](https://learn.microsoft.com/connectors/showcaseworkshop/#trigger-when-file-shared-via-email-is-downloaded) | — | — | — |
| [`ShowcaseSharedPageView`](https://learn.microsoft.com/connectors/showcaseworkshop/#triggers-when-shared-content-page-is-viewed) | — | — | — |
| [`ShowcaseShareSendEmail`](https://learn.microsoft.com/connectors/showcaseworkshop/#triggers-when-a-sharing-email-is-sent) | — | — | — |

### SignatureAPI

**Connector ID:** `signatureapi`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateEndpointForDeliverableFailed`](https://learn.microsoft.com/connectors/signatureapi/#when-a-deliverable-fails) | — | — | — |
| [`CreateEndpointForDeliverableGenerated`](https://learn.microsoft.com/connectors/signatureapi/#when-a-deliverable-is-generated) | — | — | — |
| [`CreateEndpointForEnvelopeCanceled`](https://learn.microsoft.com/connectors/signatureapi/#when-an-envelope-is-canceled) | — | — | — |
| [`CreateEndpointForEnvelopeCompleted`](https://learn.microsoft.com/connectors/signatureapi/#when-an-envelope-is-completed) | — | — | — |
| [`CreateEndpointForEnvelopeCreated`](https://learn.microsoft.com/connectors/signatureapi/#when-an-envelope-is-created) | — | — | — |
| [`CreateEndpointForEnvelopeFailed`](https://learn.microsoft.com/connectors/signatureapi/#when-an-envelope-fails) | — | — | — |
| [`CreateEndpointForEnvelopeStarted`](https://learn.microsoft.com/connectors/signatureapi/#when-an-envelope-has-started) | — | — | — |
| [`CreateEndpointForRecipientBounced`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-bounces) | — | — | — |
| [`CreateEndpointForRecipientCompleted`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-completed) | — | — | — |
| [`CreateEndpointForRecipientFailed`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-fails) | — | — | — |
| [`CreateEndpointForRecipientRejected`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-rejected) | — | — | — |
| [`CreateEndpointForRecipientReleased`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-released) | — | — | — |
| [`CreateEndpointForRecipientReplaced`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-replaced) | — | — | — |
| [`CreateEndpointForRecipientResent`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-resent) | — | — | — |
| [`CreateEndpointForRecipientSent`](https://learn.microsoft.com/connectors/signatureapi/#when-a-recipient-is-sent) | — | — | — |

### SigningHub Webhooks

**Connector ID:** `signinghubwebhooks`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Webhook_SubscribeTrigger`](https://learn.microsoft.com/connectors/signinghubwebhooks/#webhook-subscribe-trigger) | — | — | — |

### SignNow

**Connector ID:** `signnow`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Triggers_V2`](https://learn.microsoft.com/connectors/signnow/#start-flow-on-document-event) | — | — | — |

### SignNow EU

**Connector ID:** `signnoweu`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Triggers_V2`](https://learn.microsoft.com/connectors/signnoweu/#start-flow-on-document-event-%28preview%29) | — | — | — |

### SignRequest

**Connector ID:** `signrequest`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhooksCreate`](https://learn.microsoft.com/connectors/signrequest/#when-a-document-status-changes) | — | — | — |

### SimpleSurvey

**Connector ID:** `simplesurvey`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ResponseChanged`](https://learn.microsoft.com/connectors/simplesurvey/#when-a-response-is-changed) | — | — | — |
| [`ResponseSubmitted`](https://learn.microsoft.com/connectors/simplesurvey/#when-a-response-is-submitted) | — | — | — |

### Sinch

**Connector ID:** `sinch`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetDeliveryReceipt`](https://learn.microsoft.com/connectors/sinch/#when-delivery-receipt-is-received) | — | — | — |
| [`MessageArrived`](https://learn.microsoft.com/connectors/sinch/#when-a-message-is-received) | — | — | — |

### SkyPoint Cloud

**Connector ID:** `skypointcloud`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnDataflowRefreshComplete`](https://learn.microsoft.com/connectors/skypointcloud/#when-a-dataflow-refresh-is-completed) | — | — | — |
| [`GetOnDataflowRefreshFail`](https://learn.microsoft.com/connectors/skypointcloud/#when-a-dataflow-refresh-is-failed) | — | — | — |

### Slack

**Connector ID:** `slack`

**Type imports:** .NET `Azure.Connectors.Sdk.Slack.Models`; TypeScript `@azure/connectors/generated/SlackExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewFile`](https://learn.microsoft.com/connectors/slack/#when-a-file-is-created) | `SlackOnNewFileTriggerPayload` (item: `JsonElement?`) | — | `SlackOnNewFileTriggerPayload` (item: `Record<string, unknown>`) |

### Slascone

**Connector ID:** `slascone`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Create-Customer-Trigger`](https://learn.microsoft.com/connectors/slascone/#create-customer) | — | — | — |

### SmartDialog

**Connector ID:** `smartdialog`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewMessage`](https://learn.microsoft.com/connectors/smartdialog/#new-message) | — | — | — |

### Smartsheet

**Connector ID:** `smartsheet`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCommentAdded`](https://learn.microsoft.com/connectors/smartsheet/#when-a-comment-is-added-to-a-discussion) | — | — | — |
| [`OnDiscussionCreated`](https://learn.microsoft.com/connectors/smartsheet/#when-a-discussion-is-added-to-a-sheet) | — | — | — |
| [`OnNewComment`](https://learn.microsoft.com/connectors/smartsheet/#when-a-comment-is-added-to-a-sheet) | — | — | — |
| [`OnNewSheet`](https://learn.microsoft.com/connectors/smartsheet/#when-a-new-sheet-is-created) | — | — | — |
| [`OnRowCreated`](https://learn.microsoft.com/connectors/smartsheet/#when-a-new-row-is-created) | — | — | — |
| [`OnUpdatedSheet`](https://learn.microsoft.com/connectors/smartsheet/#when-any-sheet-is-updated) | — | — | — |
| [`OnUpdatedSpecificSheet`](https://learn.microsoft.com/connectors/smartsheet/#when-a-sheet-is-updated) | — | — | — |

### SmileBack

**Connector ID:** `smileback`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CSAT_received`](https://learn.microsoft.com/connectors/smileback/#csat-review-received) | — | — | — |
| [`NPS_received`](https://learn.microsoft.com/connectors/smileback/#nps-response-received) | — | — | — |
| [`PRJ_received`](https://learn.microsoft.com/connectors/smileback/#projects-survey-result-received) | — | — | — |

### Snapforms

**Connector ID:** `snapforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookTrigger`](https://learn.microsoft.com/connectors/snapforms/#when-a-form-response-is-submitted) | — | — | — |

### Soft1

**Connector ID:** `soft1`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Webhook`](https://learn.microsoft.com/connectors/soft1/#when-a-record-is-created-or-modified) | — | — | — |
| [`WebhookOnDelete`](https://learn.microsoft.com/connectors/soft1/#when-a-record-is-deleted) | — | — | — |
| [`WebhookOnInsert`](https://learn.microsoft.com/connectors/soft1/#when-a-record-is-created) | — | — | — |
| [`WebhookOnUpdate`](https://learn.microsoft.com/connectors/soft1/#when-a-record-is-modified) | — | — | — |

### Softone Web CRM

**Connector ID:** `softonewebcrm`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Call_Created`](https://learn.microsoft.com/connectors/softonewebcrm/#call-created-trigger) | — | — | — |
| [`Call_Deleted`](https://learn.microsoft.com/connectors/softonewebcrm/#call-deleted-trigger) | — | — | — |
| [`Call_Updated`](https://learn.microsoft.com/connectors/softonewebcrm/#call-updated-trigger) | — | — | — |
| [`Event_Created`](https://learn.microsoft.com/connectors/softonewebcrm/#event-created-trigger) | — | — | — |
| [`Event_Deleted`](https://learn.microsoft.com/connectors/softonewebcrm/#event-deleted-trigger) | — | — | — |
| [`Event_Updated`](https://learn.microsoft.com/connectors/softonewebcrm/#event-updated-trigger) | — | — | — |
| [`Lead_Created`](https://learn.microsoft.com/connectors/softonewebcrm/#lead-created-trigger) | — | — | — |
| [`Lead_Deleted`](https://learn.microsoft.com/connectors/softonewebcrm/#lead-deleted-trigger) | — | — | — |
| [`Lead_Updated`](https://learn.microsoft.com/connectors/softonewebcrm/#lead-updated-trigger) | — | — | — |
| [`Opportunity_Created`](https://learn.microsoft.com/connectors/softonewebcrm/#opportunity-created-trigger) | — | — | — |
| [`Opportunity_Deleted`](https://learn.microsoft.com/connectors/softonewebcrm/#opportunity-deleted-trigger) | — | — | — |
| [`Opportunity_Updated`](https://learn.microsoft.com/connectors/softonewebcrm/#opportunity-updated-trigger) | — | — | — |
| [`Task_Created`](https://learn.microsoft.com/connectors/softonewebcrm/#task-created-trigger) | — | — | — |
| [`Task_Deleted`](https://learn.microsoft.com/connectors/softonewebcrm/#task-deleted-trigger) | — | — | — |
| [`Task_Updated`](https://learn.microsoft.com/connectors/softonewebcrm/#task-updated-trigger) | — | — | — |

### SparkPost

**Connector ID:** `sparkpost`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewRecipientList`](https://learn.microsoft.com/connectors/sparkpost/#when-a-new-recipient-list-is-created) | — | — | — |

### Spring Global

**Connector ID:** `springglobal`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnSurveyExecution`](https://learn.microsoft.com/connectors/springglobal/#on-survey-execution) | — | — | — |

### SQL Server

**Connector ID:** `sql`

**Type imports:** .NET `Azure.Connectors.Sdk.Sql.Models`; TypeScript `@azure/connectors/generated/SqlExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnNewItems_V2`](https://learn.microsoft.com/connectors/sql/#when-an-item-is-created-%28v2%29) | `SqlOnNewItemsTriggerPayload` (item: `SqlItem`) | — | `SqlOnNewItemsTriggerPayload` (item: `SqlItem`) |
| [`GetOnUpdatedItems_V2`](https://learn.microsoft.com/connectors/sql/#when-an-item-is-modified-%28v2%29) | `SqlOnUpdatedItemsTriggerPayload` (item: `SqlItem`) | — | `SqlOnUpdatedItemsTriggerPayload` (item: `SqlItem`) |

### StaffCircle

**Connector ID:** `staffcircle`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewAbsence`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-absence-is-added) | — | — | — |
| [`NewObjective`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-objective-is-added) | — | — | — |
| [`NewPerson`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-person-is-added) | — | — | — |
| [`NewReview`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-review-is-added) | — | — | — |
| [`NewTask`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-task-is-added) | — | — | — |
| [`PublishedArticle`](https://learn.microsoft.com/connectors/staffcircle/#when-a-new-article-is-published) | — | — | — |
| [`UpdateObjective`](https://learn.microsoft.com/connectors/staffcircle/#when-an-objective-is-updated) | — | — | — |

### Stormboard

**Connector ID:** `stormboard`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CommentCreated`](https://learn.microsoft.com/connectors/stormboard/#comment-created) | — | — | — |
| [`IdeaCreated`](https://learn.microsoft.com/connectors/stormboard/#idea-created) | — | — | — |
| [`IdeaDeleted`](https://learn.microsoft.com/connectors/stormboard/#idea-deleted) | — | — | — |
| [`IdeaSection`](https://learn.microsoft.com/connectors/stormboard/#idea-moved) | — | — | — |
| [`LegendChange`](https://learn.microsoft.com/connectors/stormboard/#legend-change) | — | — | — |

### Strategic Portfolio Manager

**Connector ID:** `strategicportfolioma`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `EventsAddActualsApprovalWorkflowStartedHook` | — | — | — |
| `EventsAddActualsPeriodStatusChangedHook` | — | — | — |
| `EventsAddBreakdownValuesChangedHook` | — | — | — |
| `EventsAddChangeRequestCreatedHook` | — | — | — |
| `EventsAddChangeRequestDeletedHook` | — | — | — |
| `EventsAddChangeRequestStatusChangedHook` | — | — | — |
| `EventsAddChangeRequestUpdatedHook` | — | — | — |
| `EventsAddEntityCreatedHook` | — | — | — |
| `EventsAddEntityDeletedHook` | — | — | — |
| `EventsAddEntityUpdatedHook` | — | — | — |
| `EventsAddFinancialValuesChangedHook` | — | — | — |
| `EventsAddMilestoneCreatedHook` | — | — | — |
| `EventsAddMilestoneDeletedHook` | — | — | — |
| `EventsAddMilestoneUpdatedHook` | — | — | — |
| `EventsAddRelationshipCreatedHook` | — | — | — |
| `EventsAddRelationshipDeletedHook` | — | — | — |
| `EventsAddRelationshipUpdatedHook` | — | — | — |
| `EventsAddResourceAssignmentAddedHook` | — | — | — |
| `EventsAddResourceAssignmentRemovedHook` | — | — | — |
| `EventsAddResourceAssignmentUpdatedHook` | — | — | — |
| `EventsAddStageTransitionHook` | — | — | — |

### Stripe

**Connector ID:** `stripe`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewCharge`](https://learn.microsoft.com/connectors/stripe/#when-a-charge-is-added) | — | — | — |
| [`OnNewCustomer`](https://learn.microsoft.com/connectors/stripe/#when-a-customer-is-added) | — | — | — |
| [`OnNewInvoiceItem`](https://learn.microsoft.com/connectors/stripe/#when-an-invoice-item-is-added) | — | — | — |
| [`OnNewOrder`](https://learn.microsoft.com/connectors/stripe/#when-an-order-is-added) | — | — | — |

### Survey123

**Connector ID:** `survey123`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AddData`](https://learn.microsoft.com/connectors/survey123/#when-a-survey-response-is-submitted) | — | — | — |

### Survey123 (Test)

**Connector ID:** `survey123test`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `AddData` | — | — | — |

### SurveyMonkey

**Connector ID:** `surveymonkey`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewResponseAddedCollector`](https://learn.microsoft.com/connectors/surveymonkey/#when-a-new-collector-response-is-added) | — | — | — |
| [`OnNewResponseAddedSurvey`](https://learn.microsoft.com/connectors/surveymonkey/#when-a-new-survey-response-is-added) | — | — | — |
| [`OnNewResponseToQuestionAdded`](https://learn.microsoft.com/connectors/surveymonkey/#when-a-new-survey-response-to-a-question-is-added) | — | — | — |
| [`OnSurveyCollectorCreated`](https://learn.microsoft.com/connectors/surveymonkey/#when-a-new-collector-is-created) | — | — | — |
| [`OnSurveyCreated`](https://learn.microsoft.com/connectors/surveymonkey/#when-a-new-survey-is-created) | — | — | — |

### SurveyMonkey Canada

**Connector ID:** `surveymonkeycanada`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewResponseAddedCollector`](https://learn.microsoft.com/connectors/surveymonkeycanada/#when-a-new-collector-response-is-added) | — | — | — |
| [`OnNewResponseAddedSurvey`](https://learn.microsoft.com/connectors/surveymonkeycanada/#when-a-new-survey-response-is-added) | — | — | — |
| [`OnNewResponseToQuestionAdded`](https://learn.microsoft.com/connectors/surveymonkeycanada/#when-a-new-survey-response-to-a-question-is-added) | — | — | — |
| [`OnSurveyCollectorCreated`](https://learn.microsoft.com/connectors/surveymonkeycanada/#when-a-new-collector-is-created) | — | — | — |
| [`OnSurveyCreated`](https://learn.microsoft.com/connectors/surveymonkeycanada/#when-a-new-survey-is-created) | — | — | — |

## T

### Tago

**Connector ID:** `tago`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`DataTrigger`](https://learn.microsoft.com/connectors/tago/#when-a-condition-is-matched) | — | — | — |

### Talkdesk

**Connector ID:** `talkdesk`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AgentLogIn`](https://learn.microsoft.com/connectors/talkdesk/#when-agent-logs-in) | — | — | — |
| [`AgentLogOut`](https://learn.microsoft.com/connectors/talkdesk/#when-agent-logs-out) | — | — | — |
| [`ContactCreated`](https://learn.microsoft.com/connectors/talkdesk/#when-a-contact-is-created) | — | — | — |
| [`ContactUpdated`](https://learn.microsoft.com/connectors/talkdesk/#when-a-contact-is-updated) | — | — | — |
| [`InboundCallEnds`](https://learn.microsoft.com/connectors/talkdesk/#when-inbound-call-ends) | — | — | — |
| [`InboundCallReachesContactCenter`](https://learn.microsoft.com/connectors/talkdesk/#when-inbound-call-reaches-contact-center) | — | — | — |
| [`InboundCallStarts`](https://learn.microsoft.com/connectors/talkdesk/#when-inbound-call-starts) | — | — | — |
| [`NoteCreated`](https://learn.microsoft.com/connectors/talkdesk/#when-a-note-is-created) | — | — | — |
| [`OutboundCallEnds`](https://learn.microsoft.com/connectors/talkdesk/#when-outbound-call-ends) | — | — | — |

### Team Forms

**Connector ID:** `teamforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscribeResponse`](https://learn.microsoft.com/connectors/teamforms/#when-a-new-form-response-is-submitted) | — | — | — |
| [`SubscribeResponseDeletion`](https://learn.microsoft.com/connectors/teamforms/#when-a-form-response-is-deleted) | — | — | — |

### Teams-Spirit

**Connector ID:** `teamsspirit`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ActionTrigger`](https://learn.microsoft.com/connectors/teamsspirit/#action-trigger) | — | — | — |

### TeamWherx

**Connector ID:** `actsoft`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormController_Poll`](https://learn.microsoft.com/connectors/actsoft/#form-data-poll) | — | — | — |
| [`OrderController_Poll`](https://learn.microsoft.com/connectors/actsoft/#orders-poll) | — | — | — |
| [`OrderController_StatusPoll`](https://learn.microsoft.com/connectors/actsoft/#order-statuses-poll) | — | — | — |
| [`TimekeepingController_Poll`](https://learn.microsoft.com/connectors/actsoft/#timekeeping-poll) | — | — | — |

### Teamwork Projects

**Connector ID:** `teamwork`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookCreateProject`](https://learn.microsoft.com/connectors/teamwork/#when-a-project-is-created) | — | — | — |
| [`WebhookCreateTask`](https://learn.microsoft.com/connectors/teamwork/#when-a-task-is-created) | — | — | — |
| [`WebhookCreateUser`](https://learn.microsoft.com/connectors/teamwork/#when-a-user-is-created) | — | — | — |

### tegolySIGN

**Connector ID:** `tegolysign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CompletelySigned`](https://learn.microsoft.com/connectors/tegolysign/#when-a-document-is-completely-signed) | — | — | — |

### Telephony Xtended Serv Interf

**Connector ID:** `telephonyxtendedsrv`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`events`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#raw-event-subscription) | — | — | — |
| [`events.call`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-call-event) | — | — | — |
| [`events.callCenterAgent`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-call-center-agent-acd-state-change) | — | — | — |
| [`events.callCenterMonitoring`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-call-center-monitoring-change) | — | — | — |
| [`events.callCenterQueue`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-call-center-queue-change) | — | — | — |
| [`events.doNotDisturb`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-do-not-disturb-change) | — | — | — |
| [`events.voicemail`](https://learn.microsoft.com/connectors/telephonyxtendedsrv/#on-voice-mail-message-summary) | — | — | — |

### Telstra SMS

**Connector ID:** `telstrasms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `ReceiveSMS` | — | — | — |

### Text Request

**Connector ID:** `textrequest`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TextingWebhook`](https://learn.microsoft.com/connectors/textrequest/#triggers-when-a-text-is-sent-or-received) | — | — | — |

### The Events Calendar

**Connector ID:** `theeventscalendar`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CanceledEventTrigger`](https://learn.microsoft.com/connectors/theeventscalendar/#when-an-event-is-canceled-on-your-website) | — | — | — |
| [`NewEventTrigger`](https://learn.microsoft.com/connectors/theeventscalendar/#when-an-event-is-created-on-your-website) | — | — | — |
| [`UpdatedEventTrigger`](https://learn.microsoft.com/connectors/theeventscalendar/#when-an-event-is-updated-on-your-website) | — | — | — |

### Tikit

**Connector ID:** `tikit`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ActivatePowerAutomateTaskWebhook`](https://learn.microsoft.com/connectors/tikit/#when-a-lifecycle-pa-task-becomes-active) | — | — | — |
| [`AddCommentTicketWebhook`](https://learn.microsoft.com/connectors/tikit/#when-a-comment-is-left-on-a-ticket) | — | — | — |
| [`AddTicketWebhookTrigger`](https://learn.microsoft.com/connectors/tikit/#when-a-ticket-is-created) | — | — | — |
| [`ChangeLifecyclePhaseWebhook`](https://learn.microsoft.com/connectors/tikit/#when-a-lifecycle-transitions-to-a-phase) | — | — | — |
| [`EngageLifecycleTransitionWebhook`](https://learn.microsoft.com/connectors/tikit/#when-a-lifecycle-transition-engages) | — | — | — |
| [`UpdateTicketWebhookTrigger`](https://learn.microsoft.com/connectors/tikit/#when-a-ticket-is-updated) | — | — | — |

### Tilkee

**Connector ID:** `tilkee`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnTilkeeEvent`](https://learn.microsoft.com/connectors/tilkee/#subscribe-to-a-tilkee-event) | — | — | — |
| [`OnTilkeeEventEnded`](https://learn.microsoft.com/connectors/tilkee/#subscribe-to-end-of-connexion-tilkee-event) | — | — | — |
| [`OnTilkeeEventSigned`](https://learn.microsoft.com/connectors/tilkee/#subscribe-to-signature-tilkee-event) | — | — | — |

### timeghost

**Connector ID:** `timeghost`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ClientChanges`](https://learn.microsoft.com/connectors/timeghost/#client-trigger) | — | — | — |
| [`ProjectChanges`](https://learn.microsoft.com/connectors/timeghost/#project-trigger) | — | — | — |
| [`TagChanges`](https://learn.microsoft.com/connectors/timeghost/#tag-trigger) | — | — | — |
| [`TaskChanges`](https://learn.microsoft.com/connectors/timeghost/#task-trigger) | — | — | — |
| [`TimeChanges`](https://learn.microsoft.com/connectors/timeghost/#time-trigger) | — | — | — |

### Todoist

**Connector ID:** `todoist`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnItemCompletedV4`](https://learn.microsoft.com/connectors/todoist/#when-a-task-is-completed-%28v4%29) | — | — | — |
| [`OnItemCreatedV4`](https://learn.microsoft.com/connectors/todoist/#when-a-task-is-created-%28v4%29) | — | — | — |

### Toggl Track (Independent Publisher)

**Connector ID:** `toggltrack`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnTimeEntryCreated`](https://learn.microsoft.com/connectors/toggltrack/#when-a-time-entry-is-created) | — | — | — |
| [`OnTimeEntryDeleted`](https://learn.microsoft.com/connectors/toggltrack/#when-a-time-entry-is-deleted) | — | — | — |
| [`OnTimeEntryStarted`](https://learn.microsoft.com/connectors/toggltrack/#when-a-time-entry-is-started) | — | — | — |
| [`OnTimeEntryStopped`](https://learn.microsoft.com/connectors/toggltrack/#when-a-time-entry-is-stopped) | — | — | — |
| [`OnTimeEntryUpdated`](https://learn.microsoft.com/connectors/toggltrack/#when-a-time-entry-is-updated) | — | — | — |

### Toodledo

**Connector ID:** `toodledo`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigOnNewTask`](https://learn.microsoft.com/connectors/toodledo/#when-a-new-task-is-created-or-updated) | — | — | — |

### touchSMS

**Connector ID:** `touchsmsv2documentat`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`InboundMessage`](https://learn.microsoft.com/connectors/touchsmsv2documentat/#inbound-message) | — | — | — |

### TPC Portal

**Connector ID:** `tpcportal`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TpcTriggerCommentCreated`](https://learn.microsoft.com/connectors/tpcportal/#comment-created) | — | — | — |
| [`TpcTriggerCommentDeleted`](https://learn.microsoft.com/connectors/tpcportal/#comment-deleted) | — | — | — |
| [`TpcTriggerCommentUpdated`](https://learn.microsoft.com/connectors/tpcportal/#comment-updated) | — | — | — |
| [`TpcTriggerContentCreated`](https://learn.microsoft.com/connectors/tpcportal/#content-created) | — | — | — |
| [`TpcTriggerContentDeleted`](https://learn.microsoft.com/connectors/tpcportal/#content-deleted) | — | — | — |
| [`TpcTriggerContentUpdated`](https://learn.microsoft.com/connectors/tpcportal/#content-updated) | — | — | — |
| [`TpcTriggerFormEntryCreated`](https://learn.microsoft.com/connectors/tpcportal/#form-entry-created) | — | — | — |
| [`TpcTriggerFormEntryUpdated`](https://learn.microsoft.com/connectors/tpcportal/#form-entry-updated) | — | — | — |
| [`TpcTriggerFormSubmission`](https://learn.microsoft.com/connectors/tpcportal/#tpc-form-submission) | — | — | — |
| [`TpcTriggerKnowledgeSearch`](https://learn.microsoft.com/connectors/tpcportal/#tpc-knowledge-search) | — | — | — |
| [`TpcTriggerLogin`](https://learn.microsoft.com/connectors/tpcportal/#login) | — | — | — |
| [`TpcTriggerLogout`](https://learn.microsoft.com/connectors/tpcportal/#logout) | — | — | — |
| [`TpcTriggerPageNavigation`](https://learn.microsoft.com/connectors/tpcportal/#page-navigation) | — | — | — |
| [`TpcTriggerPortalUserCreated`](https://learn.microsoft.com/connectors/tpcportal/#portal-user-created) | — | — | — |
| [`TpcTriggerPortalUserDeleted`](https://learn.microsoft.com/connectors/tpcportal/#portal-user-deleted) | — | — | — |
| [`TpcTriggerPortalUserUpdated`](https://learn.microsoft.com/connectors/tpcportal/#portal-user-updated) | — | — | — |
| [`TpcTriggerRoleAssigned`](https://learn.microsoft.com/connectors/tpcportal/#role-assigned) | — | — | — |
| [`TpcTriggerRoleUnassigned`](https://learn.microsoft.com/connectors/tpcportal/#role-unassigned) | — | — | — |
| [`TpcTriggerSearch`](https://learn.microsoft.com/connectors/tpcportal/#search) | — | — | — |
| [`TpcTriggerUserCreated`](https://learn.microsoft.com/connectors/tpcportal/#user-created) | — | — | — |
| [`TpcTriggerUserDeleted`](https://learn.microsoft.com/connectors/tpcportal/#user-deleted) | — | — | — |
| [`TpcTriggerUserUpdated`](https://learn.microsoft.com/connectors/tpcportal/#user-updated) | — | — | — |

### Traction Guest

**Connector ID:** `tractionguest`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateInviteWebhook`](https://learn.microsoft.com/connectors/tractionguest/#when-a-guest-is-invited) | — | — | — |
| [`CreateSigninWebhook`](https://learn.microsoft.com/connectors/tractionguest/#when-a-guest-signs-in) | — | — | — |
| [`CreateSignoutWebhook`](https://learn.microsoft.com/connectors/tractionguest/#when-a-guest-signs-out) | — | — | — |
| [`CreateWatchlistWebhook`](https://learn.microsoft.com/connectors/tractionguest/#when-a-guest-triggers-a-watchlist) | — | — | — |

### Trello

**Connector ID:** `trello`

**Type imports:** .NET `Azure.Connectors.Sdk.Trello.Models`; TypeScript `@azure/connectors/generated/TrelloExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewCardInBoardV2`](https://learn.microsoft.com/connectors/trello/#when-a-new-card-is-added-to-a-board-%28v2%29) | — | — | — |
| [`OnNewCardInBoardV3`](https://learn.microsoft.com/connectors/trello/#when-a-new-card-is-added-to-a-board-%28v3%29-%28preview%29) | `TrelloOnNewCardInBoardTriggerPayload` (item: `CardInAction`) | — | `TrelloOnNewCardInBoardTriggerPayload` (item: `CardInAction`) |
| [`OnNewCardInListV2`](https://learn.microsoft.com/connectors/trello/#when-a-new-card-is-added-to-a-list-%28v2%29) | — | — | — |
| [`OnNewCardInListV3`](https://learn.microsoft.com/connectors/trello/#when-a-new-card-is-added-to-a-list-%28v3%29-%28preview%29) | `TrelloOnNewCardInListTriggerPayload` (item: `CardInAction`) | — | `TrelloOnNewCardInListTriggerPayload` (item: `CardInAction`) |

### Tribal - Maytas

**Connector ID:** `tribalmaytas`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenEventHappens`](https://learn.microsoft.com/connectors/tribalmaytas/#when-an-event-happens) | — | — | — |

### Tribal - Platform

**Connector ID:** `tribal`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreatedEvents`](https://learn.microsoft.com/connectors/tribal/#when-an-entity-is-created) | — | — | — |
| [`CustomEvents`](https://learn.microsoft.com/connectors/tribal/#when-an-event-is-triggered-on-an-entity) | — | — | — |
| [`DeletedEvents`](https://learn.microsoft.com/connectors/tribal/#when-an-entity-is-deleted) | — | — | — |
| [`LongRunningEvents`](https://learn.microsoft.com/connectors/tribal/#when-a-long-running-operation-is-performed-on-an-entity) | — | — | — |
| [`UpdatedEvents`](https://learn.microsoft.com/connectors/tribal/#when-an-entity-is-updated) | — | — | — |

### Tribal - SITS

**Connector ID:** `tribalsits`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenEventHappens`](https://learn.microsoft.com/connectors/tribalsits/#when-an-event-happens) | — | — | — |

### Trovve

**Connector ID:** `vineforce`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenTaskIsCompleted`](https://learn.microsoft.com/connectors/vineforce/#when-task-is-completed) | — | — | — |
| [`WhenTaskIsCreated`](https://learn.microsoft.com/connectors/vineforce/#when-a-task-is-created) | — | — | — |
| [`WhenTaskIsDeleted`](https://learn.microsoft.com/connectors/vineforce/#when-task-is-deleted) | — | — | — |
| [`WhenTaskIsUpdated`](https://learn.microsoft.com/connectors/vineforce/#when-a-task-is-updated) | — | — | — |
| [`WhenTaskSectionIsChanged`](https://learn.microsoft.com/connectors/vineforce/#when-task-section-is-changed) | — | — | — |

### TrueDialog SMS

**Connector ID:** `truedialogsms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Delivery_Notice_received`](https://learn.microsoft.com/connectors/truedialogsms/#delivery-notice-received) | — | — | — |
| [`Incoming_SMS_Received`](https://learn.microsoft.com/connectors/truedialogsms/#incoming-message-received) | — | — | — |
| [`Invalid_Targets`](https://learn.microsoft.com/connectors/truedialogsms/#invalid-targets) | — | — | — |
| [`Keyword_received`](https://learn.microsoft.com/connectors/truedialogsms/#keyword-received) | — | — | — |
| [`Stop_received`](https://learn.microsoft.com/connectors/truedialogsms/#stop-message-received) | — | — | — |

### TxtSync

**Connector ID:** `txtsync`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`InboundSMS`](https://learn.microsoft.com/connectors/txtsync/#inbound-sms-notification) | — | — | — |
| [`OutboundSMS`](https://learn.microsoft.com/connectors/txtsync/#outbound-sms-notification) | — | — | — |

### tyntec SMS Business

**Connector ID:** `tyntecsms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`incomingV3`](https://learn.microsoft.com/connectors/tyntecsms/#incoming-message) | — | — | — |

### tyntec Viber Business

**Connector ID:** `tyntecviber`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IncomingV3`](https://learn.microsoft.com/connectors/tyntecviber/#incoming-message) | — | — | — |

### tyntec WhatsApp Business

**Connector ID:** `tyntecwa`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IncomingV2`](https://learn.microsoft.com/connectors/tyntecwa/#incoming-message) | — | — | — |

### Typeform

**Connector ID:** `typeform`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewResponseWebhook`](https://learn.microsoft.com/connectors/typeform/#when-a-response-is-submitted-%28preview%29) | — | — | — |
| [`NewResponseWebhook_V2`](https://learn.microsoft.com/connectors/typeform/#when-a-response-is-submitted-%28preview%29) | — | — | — |

## U

### Ubiqod by Skiply

**Connector ID:** `ubiqodbyskiply`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`DataIn`](https://learn.microsoft.com/connectors/ubiqodbyskiply/#when-data-is-received-from-iot-device-or-qod) | — | — | — |

### Ubiqod by Taqt

**Connector ID:** `ubiqodbyskiplyv2`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ReceiveDataFromTrackers`](https://learn.microsoft.com/connectors/ubiqodbyskiplyv2/#receive-data-from-trackers) | — | — | — |

### Unique

**Connector ID:** `unique`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `registerTranscriptionReadyWebhook` | — | — | — |

### Updates App (Microsoft 365)

**Connector ID:** `m365updatesapp`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ListReceivedReports`](https://learn.microsoft.com/connectors/m365updatesapp/#when-i-receive-a-new-update) | — | — | — |
| [`ListReceivedReportsByReportDefinition`](https://learn.microsoft.com/connectors/m365updatesapp/#when-i-receive-a-new-update-tied-to-a-specific-request) | — | — | — |

### UserVoice

**Connector ID:** `uservoice`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnNewItems`](https://learn.microsoft.com/connectors/uservoice/#when-an-item-is-created) | — | — | — |
| [`GetOnUpdatedItems`](https://learn.microsoft.com/connectors/uservoice/#when-an-item-is-modified) | — | — | — |

## V

### Varuna

**Connector ID:** `varuna`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscribeTrigger`](https://learn.microsoft.com/connectors/varuna/#send-notification-when-an-event-occurs) | — | — | — |

### Vimeo

**Connector ID:** `vimeo`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewVideoInChannel`](https://learn.microsoft.com/connectors/vimeo/#when-a-video-is-added-to-a-channel) | — | — | — |
| [`OnVideoUpload`](https://learn.microsoft.com/connectors/vimeo/#when-i-upload-a-new-video) | — | — | — |

### Viva Engage

**Connector ID:** `yammer`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewMessagesFollowingV2`](https://learn.microsoft.com/connectors/yammer/#when-there-is-a-new-message-in-my-followed-feed-%28v2%29) | — | — | — |
| [`OnNewMessagesInGroupV2`](https://learn.microsoft.com/connectors/yammer/#when-there-is-a-new-message-in-a-group-%28v2%29) | — | — | — |

### Vocean

**Connector ID:** `vocean`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IdeaTrigger`](https://learn.microsoft.com/connectors/vocean/#when-an-idea-is-added-or-interacted-with) | — | — | — |
| [`UserActivityTrigger`](https://learn.microsoft.com/connectors/vocean/#when-an-activity%27s%2C-or-a-workboard%27s%2C-availability-changes-for-the-current-user) | — | — | — |

### Vome

**Connector ID:** `vome`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ArchivedProfile`](https://learn.microsoft.com/connectors/vome/#archived-profile) | — | — | — |
| [`DeletedFormSubmission`](https://learn.microsoft.com/connectors/vome/#deleted-form-submission) | — | — | — |
| [`DeletedProfile`](https://learn.microsoft.com/connectors/vome/#deleted-profile) | — | — | — |
| [`DeletedReservation`](https://learn.microsoft.com/connectors/vome/#deleted-reservation) | — | — | — |
| [`DeletedSequence`](https://learn.microsoft.com/connectors/vome/#deleted-sequence-assigment) | — | — | — |
| [`NewFormSubmission`](https://learn.microsoft.com/connectors/vome/#new-form-submission) | — | — | — |
| [`NewProfile`](https://learn.microsoft.com/connectors/vome/#new-profile) | — | — | — |
| [`NewReservation`](https://learn.microsoft.com/connectors/vome/#new-reservation) | — | — | — |
| [`NewSequence`](https://learn.microsoft.com/connectors/vome/#new-sequence-assigned) | — | — | — |
| [`UpdatedFormSubmission`](https://learn.microsoft.com/connectors/vome/#updated-form-submission) | — | — | — |
| [`UpdatedProfile`](https://learn.microsoft.com/connectors/vome/#updatedprofile) | — | — | — |
| [`UpdatedReservation`](https://learn.microsoft.com/connectors/vome/#updated-reservation) | — | — | — |
| [`UpdatedSequence`](https://learn.microsoft.com/connectors/vome/#updated-sequence) | — | — | — |

## W

### Wauld

**Connector ID:** `wauld`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhookTrigger`](https://learn.microsoft.com/connectors/wauld/#when-a-new-credential-is-issued) | — | — | — |

### Wavelength

**Connector ID:** `wavelength`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnFlowChanged`](https://learn.microsoft.com/connectors/wavelength/#when-a-flow-is-created-or-updated) | — | — | — |

### Way We Do

**Connector ID:** `waywedo`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Checklist_Create_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-checklist-instance-is-started) | — | — | — |
| [`Checklist_Step_Completed_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-checklist-step-is-completed) | — | — | — |
| [`Finish_Checklist_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-checklist-is-finished) | — | — | — |
| [`Generate_Acceptance_PDF_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-procedure-is-accepted) | — | — | — |
| [`Invite_Supervisor_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-supervisor-is-invited) | — | — | — |
| [`New_Comment_WebHook`](https://learn.microsoft.com/connectors/waywedo/#when-a-comment-is-added-to-a-checklist) | — | — | — |

### Webex

**Connector ID:** `webex`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`MembershipsCreated`](https://learn.microsoft.com/connectors/webex/#when-a-membership-is-created) | — | — | — |
| [`MembershipsDeleted`](https://learn.microsoft.com/connectors/webex/#when-a-membership-is-deleted) | — | — | — |
| [`MembershipsUpdated`](https://learn.microsoft.com/connectors/webex/#when-a-membership-is-updated) | — | — | — |
| [`MessagesCreated`](https://learn.microsoft.com/connectors/webex/#when-a-message-is-created) | — | — | — |
| [`MessagesDeleted`](https://learn.microsoft.com/connectors/webex/#when-a-message-is-deleted) | — | — | — |
| [`SpaceCreated`](https://learn.microsoft.com/connectors/webex/#when-a-space-is-created) | — | — | — |
| [`SpaceUpdated`](https://learn.microsoft.com/connectors/webex/#when-a-space-is-updated) | — | — | — |

### Windows 365

**Connector ID:** `windows365`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookCloudPcProvisioned`](https://learn.microsoft.com/connectors/windows365/#when-a-cloud-pc-is-created) | — | — | — |
| [`WebhookCloudPcRemoteActionCompleted`](https://learn.microsoft.com/connectors/windows365/#when-a-remote-action-on-a-cloud-pc-completes) | — | — | — |
| [`WebhookCloudPcRemoteActionTriggered`](https://learn.microsoft.com/connectors/windows365/#when-a-remote-action-on-a-cloud-pc-is-triggered) | — | — | — |
| [`WebhookProvisioningPolicyCreated`](https://learn.microsoft.com/connectors/windows365/#when-a-new-provisioning-policy-is-created) | — | — | — |
| [`WebhookProvisioningPolicyUpdated`](https://learn.microsoft.com/connectors/windows365/#when-a-provisioning-policy-is-updated) | — | — | — |

### Witivio

**Connector ID:** `witivio`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookEscalationTrigger`](https://learn.microsoft.com/connectors/witivio/#when-the-chatbot-launch-an-escalation) | — | — | — |
| [`WebhookTrigger`](https://learn.microsoft.com/connectors/witivio/#when-the-chatbot-understand) | — | — | — |

### WooCommerce

**Connector ID:** `woocommerce`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/woocommerce/#when-a-woocommerce-event-happens) | — | — | — |

### WordPress

**Connector ID:** `wordpress`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnTriggerNewPost`](https://learn.microsoft.com/connectors/wordpress/#when-a-post-is-created) | — | — | — |

### Workday HCM

**Connector ID:** `workdayhcm`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`When_an_Employee_is_Added_or_Updated`](https://learn.microsoft.com/connectors/workdayhcm/#when-an-employee-is-added-or-updated) | — | — | — |

### Workfront Event Subscription (Independent Publisher)

**Connector ID:** `workfronteventsubscr`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnWorkfrontEvent`](https://learn.microsoft.com/connectors/workfronteventsubscr/#when-a-workfront-event-occurs) | — | — | — |

### WorkMobile

**Connector ID:** `workmobile`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormDataHook`](https://learn.microsoft.com/connectors/workmobile/#when-data-is-submitted-for-a-form) | — | — | — |

### WorkPoint

**Connector ID:** `workpoint`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebHookRegistrations_Post`](https://learn.microsoft.com/connectors/workpoint/#triggers-when-a-workpoint-event-occurs) | — | — | — |

### WorkPoint 365

**Connector ID:** `workpoint365`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebHookRegistrations_Post`](https://learn.microsoft.com/connectors/workpoint365/#triggers-when-a-workpoint-event-occurs) | — | — | — |

### WorkSpan

**Connector ID:** `workspan`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubmitToPartnerCenterObjectEvent`](https://learn.microsoft.com/connectors/workspan/#submit-referral-to-microsoft-partner-center-from-workspan) | — | — | — |
| [`Subscribe`](https://learn.microsoft.com/connectors/workspan/#subscribe-to-workspan-collaboration-events) | — | — | — |
| [`SubscribeObjectEvent`](https://learn.microsoft.com/connectors/workspan/#subscribe-to-workspan-object-events) | — | — | — |

### WP Connectr for WordPress

**Connector ID:** `wpconnectrforwordpre`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateTrigger`](https://learn.microsoft.com/connectors/wpconnectrforwordpre/#when-a-wordpress-event-happens) | — | — | — |

### WPForms by Reenhanced LLC

**Connector ID:** `wpformsbyreenhancedl`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateFlow`](https://learn.microsoft.com/connectors/wpformsbyreenhancedl/#when-a-new-wpforms-entry-is-submitted) | — | — | — |

## X

### X

**Connector ID:** `twitter`

**Type imports:** .NET `Azure.Connectors.Sdk.Twitter.Models`; TypeScript `@azure/connectors/generated/TwitterExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewTweet`](https://learn.microsoft.com/connectors/twitter/#when-a-new-tweet-is-posted) | `TwitterOnNewTweetTriggerPayload` (item: `TweetModel`) | — | `TwitterOnNewTweetTriggerPayload` (item: `TweetModel`) |

### X12

**Connector ID:** `x12`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnModifiedControlNumber`](https://learn.microsoft.com/connectors/x12/#when-a-control-number-is-modified) | — | — | — |

### Xero Accounting - Magnetism

**Connector ID:** `xeroaccountingmagnet`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`trigger-xero`](https://learn.microsoft.com/connectors/xeroaccountingmagnet/#trigger-when-a-xero-record-is-created-or-updated) | — | — | — |

## Y

### YakChat

**Connector ID:** `yakchat`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`BidirectionalMessage`](https://learn.microsoft.com/connectors/yakchat/#when-a-message-is-sent-or-received-%28preview%29) | — | — | — |
| [`InboundMessage`](https://learn.microsoft.com/connectors/yakchat/#when-a-message-is-received) | — | — | — |
| [`InboundMessage_V2`](https://learn.microsoft.com/connectors/yakchat/#when-a-message-is-received-%28v2%29-%28preview%29) | — | — | — |
| [`OutboundMessage`](https://learn.microsoft.com/connectors/yakchat/#when-a-message-is-sent) | — | — | — |
| [`OutboundMessage_V2`](https://learn.microsoft.com/connectors/yakchat/#when-a-message-is-sent-%28v2%29-%28preview%29) | — | — | — |

### Yeeflow

**Connector ID:** `yeeflow`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnItemCreated`](https://learn.microsoft.com/connectors/yeeflow/#when-an-item-is-created) | — | — | — |
| [`OnItemCreatedModified`](https://learn.microsoft.com/connectors/yeeflow/#when-an-item-is-created-or-modified) | — | — | — |
| [`OnItemDeleted`](https://learn.microsoft.com/connectors/yeeflow/#when-an-item-is-deleted) | — | — | — |
| [`OnItemModified`](https://learn.microsoft.com/connectors/yeeflow/#when-an-item-is-modified) | — | — | — |

### YouTube

**Connector ID:** `youtube`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnMyNewVideo`](https://learn.microsoft.com/connectors/youtube/#when-i-upload-a-video) | — | — | — |
| [`OnNewVideoInChannel`](https://learn.microsoft.com/connectors/youtube/#when-a-video-is-uploaded-by-a-channel) | — | — | — |
| [`OnNewVideoMatchingSearch`](https://learn.microsoft.com/connectors/youtube/#when-a-new-video-matches-a-search) | — | — | — |

## Z

### Zahara

**Connector ID:** `zahara`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewApprovalComment`](https://learn.microsoft.com/connectors/zahara/#new-approval-comment) | — | — | — |
| [`NewCostCode`](https://learn.microsoft.com/connectors/zahara/#new-cost-code) | — | — | — |
| [`NewDocumentApproved`](https://learn.microsoft.com/connectors/zahara/#new-document-approved) | — | — | — |
| [`NewInvoice`](https://learn.microsoft.com/connectors/zahara/#new-invoice) | — | — | — |
| [`NewInvoiceSetAsExported`](https://learn.microsoft.com/connectors/zahara/#new-invoice-set-as-exported) | — | — | — |
| [`NewNominalCode`](https://learn.microsoft.com/connectors/zahara/#new-nominal-code) | — | — | — |
| [`NewProject`](https://learn.microsoft.com/connectors/zahara/#new-project) | — | — | — |
| [`NewPurchaseOrder`](https://learn.microsoft.com/connectors/zahara/#new-purchase-order) | — | — | — |
| [`NewPurchaseOrderSentToSupplier`](https://learn.microsoft.com/connectors/zahara/#new-purchase-order-sent-to-supplier) | — | — | — |
| [`NewPurchaseRequsition`](https://learn.microsoft.com/connectors/zahara/#new-purchase-requsition) | — | — | — |
| [`NewSupplier`](https://learn.microsoft.com/connectors/zahara/#new-supplier) | — | — | — |
| [`NewTaxCode`](https://learn.microsoft.com/connectors/zahara/#new-tax-code) | — | — | — |
| [`SupplierAmended`](https://learn.microsoft.com/connectors/zahara/#supplier-amended) | — | — | — |

### Zellis

**Connector ID:** `zellis`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CRUDEntitiy`](https://learn.microsoft.com/connectors/zellis/#entity-trigger) | — | — | — |

### Zendesk

**Connector ID:** `zendesk`

**Type imports:** .NET `Azure.Connectors.Sdk.Zendesk.Models`; TypeScript `@azure/connectors/generated/ZendeskExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`GetOnNewItems`](https://learn.microsoft.com/connectors/zendesk/#when-an-item-is-created) | `ZendeskOnNewItemsTriggerPayload` (item: `Item`) | — | `ZendeskOnNewItemsTriggerPayload` (item: `Item`) |
| [`GetOnUpdatedItems`](https://learn.microsoft.com/connectors/zendesk/#when-an-item-is-modified) | — | — | — |
| [`GetOnUpdatedItemsV2`](https://learn.microsoft.com/connectors/zendesk/#when-an-item-is-modified) | `ZendeskOnUpdatedItemsTriggerPayload` (item: `Item`) | — | `ZendeskOnUpdatedItemsTriggerPayload` (item: `Item`) |

### ZIPPYDOC

**Connector ID:** `zippydoc`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnTableChange`](https://learn.microsoft.com/connectors/zippydoc/#on-table-changed) | — | — | — |

### Zoho Calendar

**Connector ID:** `zohocalendar`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Delete_Event_Notification`](https://learn.microsoft.com/connectors/zohocalendar/#delete-event-notification) | — | — | — |
| [`Edit_Event_Notification`](https://learn.microsoft.com/connectors/zohocalendar/#edit-event-notification) | — | — | — |
| [`New_Event_Notification`](https://learn.microsoft.com/connectors/zohocalendar/#new-event-notification) | — | — | — |

### Zoho Forms

**Connector ID:** `zforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormSubmitted`](https://learn.microsoft.com/connectors/zforms/#when-form-gets-a-response) | — | — | — |

### Zoho Mail

**Connector ID:** `zohomail`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NEW_CONDITIONAL_MAIL`](https://learn.microsoft.com/connectors/zohomail/#get-new-mail-by-condition) | — | — | — |
| [`New_Mail_Trigger`](https://learn.microsoft.com/connectors/zohomail/#get-mail-notificaiton-from-specified-user) | — | — | — |

### Zoho Sign

**Connector ID:** `zohosign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`zoho-sign-triggers`](https://learn.microsoft.com/connectors/zohosign/#zoho-sign-triggers) | — | — | — |

### zReports

**Connector ID:** `zreports`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewDispatchAdvice`](https://learn.microsoft.com/connectors/zreports/#when-a-despatch-advice-is-received) | — | — | — |
| [`NewInvoice`](https://learn.microsoft.com/connectors/zreports/#when-an-invoice-is-received) | — | — | — |

### Zvanu Parvaldnieks

**Connector ID:** `zvanuparvaldnieks`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CallCompleted`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#callcompleted-webhook-callback) | — | — | — |
| [`CallConnected`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#callconnected-webhook-callback) | — | — | — |
| [`CallRecorded`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#callrecorded-webhook-callback) | — | — | — |
| [`CallStarted`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#callstarted-webhook-callback) | — | — | — |
| [`CallTransfered`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#calltransfered-webhook-callback) | — | — | — |
| [`GetAudioPrompts`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-audio-prompts) | — | — | — |
| [`GetCallHistory`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-call-history) | — | — | — |
| [`GetCallRecordings`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-call-recordings) | — | — | — |
| [`GetContacts`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-contacts) | — | — | — |
| [`GetGroups`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-groups) | — | — | — |
| [`GetVoicemailBoxes`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-voicemail-boxes) | — | — | — |
| [`GetVoicemails`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-voicemails) | — | — | — |
| [`GetVoipEndpoints`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#get-voip-endpoints) | — | — | — |
| [`LostCallerAdded`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#lostcalleradded-webhook-callback) | — | — | — |
| [`LostCallerRemoved`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#lostcallerremoved-webhook-callback) | — | — | — |
| [`LostCallerUpdated`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#lostcallerupdated-webhook-callback) | — | — | — |
| [`VoicemailCreated`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#voicemailcreated-webhook-callback) | — | — | — |
| [`VoicemailDeleted`](https://learn.microsoft.com/connectors/zvanuparvaldnieks/#voicemaildeleted-webhook-callback) | — | — | — |

[Back to connector index](../operations-functions-match.md)
