# Connector trigger payloads: M–R

[Connector index and type guide](../operations-functions-match.md)

## M

### MailChimp

**Connector ID:** `mailchimp`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCreateList`](https://learn.microsoft.com/connectors/mailchimp/#when-a-new-list-is-created) | — | — | — |
| [`OnMemberSubscribed`](https://learn.microsoft.com/connectors/mailchimp/#when-a-member-has-been-added-to-a-list) | — | — | — |

### MailParser

**Connector ID:** `mailparser`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookCreate`](https://learn.microsoft.com/connectors/mailparser/#new-parsed-email-is-available) | — | — | — |

### Maximizer CRM

**Connector ID:** `maximizercrm`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerAbEntryCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-address-book-entry) | — | — | — |
| [`TriggerAbEntryDateNotification`](https://learn.microsoft.com/connectors/maximizercrm/#date-notification-for-address-book-entry) | — | — | — |
| [`TriggerAbEntryUpdated`](https://learn.microsoft.com/connectors/maximizercrm/#new-or-updated-address-book-entry) | — | — | — |
| [`TriggerAppointmentCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-appointment) | — | — | — |
| [`TriggerAppointmentDateNotification`](https://learn.microsoft.com/connectors/maximizercrm/#date-notification-for-appointment) | — | — | — |
| [`TriggerAppointmentUpdated`](https://learn.microsoft.com/connectors/maximizercrm/#new-or-updated-appointment) | — | — | — |
| [`TriggerCaseCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-case) | — | — | — |
| [`TriggerCaseDateNotification`](https://learn.microsoft.com/connectors/maximizercrm/#date-notification-for-case) | — | — | — |
| [`TriggerCaseUpdated`](https://learn.microsoft.com/connectors/maximizercrm/#new-or-updated-case) | — | — | — |
| [`TriggerHTaskCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-task) | — | — | — |
| [`TriggerLeadCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-lead) | — | — | — |
| [`TriggerLeadDateNotification`](https://learn.microsoft.com/connectors/maximizercrm/#date-notification-for-lead) | — | — | — |
| [`TriggerLeadUpdated`](https://learn.microsoft.com/connectors/maximizercrm/#updated-lead) | — | — | — |
| [`TriggerOppCreated`](https://learn.microsoft.com/connectors/maximizercrm/#new-opportunity) | — | — | — |
| [`TriggerOpportunityDateNotification`](https://learn.microsoft.com/connectors/maximizercrm/#date-notification-for-opportunity) | — | — | — |
| [`TriggerOppUpdated`](https://learn.microsoft.com/connectors/maximizercrm/#new-or-updated-opportunity) | — | — | — |
| [`WebhookOppStageChanged`](https://learn.microsoft.com/connectors/maximizercrm/#opportunity-stage-changed) | — | — | — |

### Medium

**Connector ID:** `medium`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerPublicationAdded`](https://learn.microsoft.com/connectors/medium/#when-a-publication-is-created-or-subscribed-to) | — | — | — |

### Meisterplan

**Connector ID:** `meisterplan`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/meisterplan/#create-webhook) | — | — | — |

### Microsoft Bookings

**Connector ID:** `microsoftbookings`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CancelAppointment`](https://learn.microsoft.com/connectors/microsoftbookings/#when-an-appointment-is-cancelled) | — | — | — |
| [`CreateAppointment`](https://learn.microsoft.com/connectors/microsoftbookings/#when-a-appointment-is-created) | — | — | — |
| [`UpdateAppointment`](https://learn.microsoft.com/connectors/microsoftbookings/#when-an-appointment-is-updated) | — | — | — |

### Microsoft Defender ATP

**Connector ID:** `wdatp`

**Type imports:** .NET `Azure.Connectors.Sdk.Wdatp.Models`; TypeScript `@azure/connectors/generated/WdatpExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewRemediationActivity`](https://learn.microsoft.com/connectors/wdatp/#triggers-when-a-new-remediation-activity-is-created-%28preview%29) | `WdatpOnNewRemediationActivityTriggerPayload` (item: `RemediationActivity`) | — | `WdatpOnNewRemediationActivityTriggerPayload` (item: `RemediationActivity`) |
| [`WebHooks_CreateWebHook`](https://learn.microsoft.com/connectors/wdatp/#triggers---trigger-when-new-wdatp-alert-occurs) | — | — | — |

### Microsoft Defender for Cloud Alert

**Connector ID:** `ascalert`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ASCAlert_trigger_subscribe`](https://learn.microsoft.com/connectors/ascalert/#when-a-microsoft-defender-for-cloud-alert-is-created-or-triggered) | — | — | — |

### Microsoft Defender for Cloud Recommendation

**Connector ID:** `ascassessment`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ASCAssessment_trigger_subscribe`](https://learn.microsoft.com/connectors/ascassessment/#when-a-microsoft-defender-for-cloud-recommendation-is-created-or-triggered) | — | — | — |

### Microsoft Defender for Cloud Regulatory Compliance

**Connector ID:** `ascregulatorycomplianceassessment`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ASCRegulatoryComplianceAssessment_trigger_subscribe`](https://learn.microsoft.com/connectors/ascregulatorycomplianceassessment/#when-a-defender-for-cloud-regulatory-compliance-assessment-is-created-or-runs) | — | — | — |

### Microsoft Defender SOC

**Connector ID:** `defendersoc`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenDefenderCaseCreatedOrUpdated`](https://learn.microsoft.com/connectors/defendersoc/#microsoft-defender-soc-case) | — | — | — |
| [`WhenDefenderOrSentinelAlertCreated`](https://learn.microsoft.com/connectors/defendersoc/#microsoft-defender-soc-alert) | — | — | — |

### Microsoft Forms

**Connector ID:** `microsoftforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateFormWebhook`](https://learn.microsoft.com/connectors/microsoftforms/#when-a-new-response-is-submitted) | — | — | — |

### Microsoft Kaizala

**Connector ID:** `kaizala`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ActionCreatedOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-custom-action-is-sent-in-a-group) | — | — | — |
| [`ActionResponseOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-someone-responds-to-a-custom-action) | — | — | — |
| [`AnnouncementOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-there-is-an-announcement-sent-in-a-group) | — | — | — |
| [`AttachmentOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-there-is-an-attachment-sent-in-a-group) | — | — | — |
| [`GroupAddedToGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-group-is-added-as-a-member-to-a-group) | — | — | — |
| [`GroupRemovedFromGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-group-is-removed-as-a-member-to-a-group) | — | — | — |
| [`MemberAddedToGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-member-is-added-to-a-group) | — | — | — |
| [`MemberRemovedFromGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-member-is-removed-from-a-group) | — | — | — |
| [`SurveyCreatedOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-survey-is-sent-in-a-group) | — | — | — |
| [`SurveyResponseOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-someone-responds-to-a-survey) | — | — | — |
| [`TextMessageCreatedOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-a-text-message-is-sent-on-a-group) | — | — | — |
| [`UserJoinedOnGroup`](https://learn.microsoft.com/connectors/kaizala/#when-an-existing-group-member-registers-on-kaizala-for-the-first-time) | — | — | — |

### Microsoft Sentinel

**Connector ID:** `azuresentinel`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ASI_entity_trigger_subscribe`](https://learn.microsoft.com/connectors/azuresentinel/#microsoft-sentinel-entity) | — | — | — |
| [`ASI_incident_trigger_subscribe`](https://learn.microsoft.com/connectors/azuresentinel/#microsoft-sentinel-incident) | — | — | — |
| [`ASI_trigger_subscribe`](https://learn.microsoft.com/connectors/azuresentinel/#microsoft-sentinel-alert) | — | — | — |

### Microsoft To-Do (Business)

**Connector ID:** `todo`

**Type imports:** .NET `Azure.Connectors.Sdk.Todo.Models`; TypeScript `@azure/connectors/generated/TodoExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewToDoInFolderV2`](https://learn.microsoft.com/connectors/todo/#when-a-new-to-do-in-a-specific-folder-is-created-%28v2%29) | `TodoOnNewToDoInFolderTriggerPayload` (item: `ToDo`) | — | `TodoOnNewToDoInFolderTriggerPayload` (item: `ToDo`) |
| [`OnUpdateToDoInFolderV2`](https://learn.microsoft.com/connectors/todo/#when-a-to-do-in-a-specific-folder-is-updated-%28v2%29) | `TodoOnUpdateToDoInFolderTriggerPayload` (item: `ToDo`) | — | `TodoOnUpdateToDoInFolderTriggerPayload` (item: `ToDo`) |

### Microsoft To-Do (Consumer)

**Connector ID:** `todoconsumer`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewToDoInFolder`](https://learn.microsoft.com/connectors/todoconsumer/#when-a-new-to-do-in-a-specific-folder-is-created) | — | — | — |
| [`OnUpdateToDoInFolder`](https://learn.microsoft.com/connectors/todoconsumer/#when-a-to-do-in-a-specific-folder-is-updated) | — | — | — |

### Mobili Stotele

**Connector ID:** `mobilistotele`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CallCompleted`](https://learn.microsoft.com/connectors/mobilistotele/#callcompleted-webhook-callback) | — | — | — |
| [`CallConnected`](https://learn.microsoft.com/connectors/mobilistotele/#callconnected-webhook-callback) | — | — | — |
| [`CallRecorded`](https://learn.microsoft.com/connectors/mobilistotele/#callrecorded-webhook-callback) | — | — | — |
| [`CallStarted`](https://learn.microsoft.com/connectors/mobilistotele/#callstarted-webhook-callback) | — | — | — |
| [`CallTransfered`](https://learn.microsoft.com/connectors/mobilistotele/#calltransfered-webhook-callback) | — | — | — |
| [`GetAudioPrompts`](https://learn.microsoft.com/connectors/mobilistotele/#get-audio-prompts) | — | — | — |
| [`GetCallHistory`](https://learn.microsoft.com/connectors/mobilistotele/#get-call-history) | — | — | — |
| [`GetCallRecordings`](https://learn.microsoft.com/connectors/mobilistotele/#get-call-recordings) | — | — | — |
| [`GetContacts`](https://learn.microsoft.com/connectors/mobilistotele/#get-contacts) | — | — | — |
| [`GetGroups`](https://learn.microsoft.com/connectors/mobilistotele/#get-groups) | — | — | — |
| [`GetVoicemailBoxes`](https://learn.microsoft.com/connectors/mobilistotele/#get-voicemail-boxes) | — | — | — |
| [`GetVoicemails`](https://learn.microsoft.com/connectors/mobilistotele/#get-voicemails) | — | — | — |
| [`GetVoipEndpoints`](https://learn.microsoft.com/connectors/mobilistotele/#get-voip-endpoints) | — | — | — |
| [`LostCallerAdded`](https://learn.microsoft.com/connectors/mobilistotele/#lostcalleradded-webhook-callback) | — | — | — |
| [`LostCallerRemoved`](https://learn.microsoft.com/connectors/mobilistotele/#lostcallerremoved-webhook-callback) | — | — | — |
| [`LostCallerUpdated`](https://learn.microsoft.com/connectors/mobilistotele/#lostcallerupdated-webhook-callback) | — | — | — |
| [`VoicemailCreated`](https://learn.microsoft.com/connectors/mobilistotele/#voicemailcreated-webhook-callback) | — | — | — |
| [`VoicemailDeleted`](https://learn.microsoft.com/connectors/mobilistotele/#voicemaildeleted-webhook-callback) | — | — | — |

### monday

**Connector ID:** `monday`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookAnyColumnChanges`](https://learn.microsoft.com/connectors/monday/#when-any-column-changes) | — | — | — |
| [`WebhookChangeName`](https://learn.microsoft.com/connectors/monday/#when-an-item%27s-name-changes) | — | — | — |
| [`WebhookChangeSubitemName`](https://learn.microsoft.com/connectors/monday/#when-a-subitem%27s-name-changes) | — | — | — |
| [`WebhookColumnChanges`](https://learn.microsoft.com/connectors/monday/#when-a-column-changes) | — | — | — |
| [`WebhookCreateItem`](https://learn.microsoft.com/connectors/monday/#when-an-item-is-created) | — | — | — |
| [`WebhookCreateSubitem`](https://learn.microsoft.com/connectors/monday/#when-a-subitem-is-created) | — | — | — |
| [`WebhookCreateUpdate`](https://learn.microsoft.com/connectors/monday/#when-a-new-update-is-posted) | — | — | — |
| [`WebhookSubitemColumnChanges`](https://learn.microsoft.com/connectors/monday/#when-any-subitem-column-changes) | — | — | — |

### MoreApp Forms

**Connector ID:** `moreappforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewSubmission`](https://learn.microsoft.com/connectors/moreappforms/#new-submission) | — | — | — |
| [`TaskFulfilled`](https://learn.microsoft.com/connectors/moreappforms/#task-fulfilled) | — | — | — |

### MotaWord Translations

**Connector ID:** `motawordtranslations`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenTranslationProjectCompleted`](https://learn.microsoft.com/connectors/motawordtranslations/#when-translation-project-completed) | — | — | — |

### MSN Weather

**Connector ID:** `msnweather`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnCurrentConditionsChange`](https://learn.microsoft.com/connectors/msnweather/#when-the-current-conditions-change) | — | — | — |
| [`OnCurrentWeatherChange`](https://learn.microsoft.com/connectors/msnweather/#when-the-current-weather-changes) | — | — | — |

### My Hours

**Connector ID:** `myhours`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewProject`](https://learn.microsoft.com/connectors/myhours/#when-a-new-project-is-created) | — | — | — |
| [`NewTask`](https://learn.microsoft.com/connectors/myhours/#when-a-new-task-is-created) | — | — | — |
| [`NewTimeLog`](https://learn.microsoft.com/connectors/myhours/#when-a-new-time-log-is-created) | — | — | — |

## N

### nBold

**Connector ID:** `nbold`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TeamCreationApprovalApprovedHook`](https://learn.microsoft.com/connectors/nbold/#when-a-team-creation-is-approved-%28preview%29) | — | — | — |
| [`TeamCreationApprovalRejectedHook`](https://learn.microsoft.com/connectors/nbold/#when-a-team-creation-is-rejected-%28preview%29) | — | — | — |
| [`TeamCreationApprovalRequestedHook`](https://learn.microsoft.com/connectors/nbold/#when-a-team-creation-approval-is-requested-%28preview%29) | — | — | — |
| [`TeamProvisioningCompletedHook`](https://learn.microsoft.com/connectors/nbold/#when-a-team-is-created-using-a-collaboration-template-%28preview%29) | — | — | — |

### NetDocuments

**Connector ID:** `netdocuments`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SearchCab`](https://learn.microsoft.com/connectors/netdocuments/#search-cabinet) | — | — | — |

### Netvolution

**Connector ID:** `netvolution`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewEvent`](https://learn.microsoft.com/connectors/netvolution/#when-a-new-event-happens) | — | — | — |
| [`OnNewUserInSegment`](https://learn.microsoft.com/connectors/netvolution/#when-a-contact-is-added-to-a-segment) | — | — | — |

### NH360 Portfolio Insights

**Connector ID:** `strategicportfoliomanager`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`EventsAddActualsApprovalWorkflowStartedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-actuals-approval-workflow-started) | — | — | — |
| [`EventsAddActualsPeriodStatusChangedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-actuals-period-status-changed) | — | — | — |
| [`EventsAddApprovalRequestChangedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-lifecycle-approval-request-changed) | — | — | — |
| [`EventsAddApprovalRequestCreatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-lifecycle-approval-request-created) | — | — | — |
| [`EventsAddChangeRequestCreatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-change-request-created) | — | — | — |
| [`EventsAddChangeRequestDeletedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-change-request-deleted) | — | — | — |
| [`EventsAddChangeRequestStatusChangedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-change-request-status-changed) | — | — | — |
| [`EventsAddChangeRequestUpdatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-change-request-updated) | — | — | — |
| [`EventsAddEntityCreatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-entity-created) | — | — | — |
| [`EventsAddEntityDeletedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-entity-deleted) | — | — | — |
| [`EventsAddEntityUpdatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-entity-updated) | — | — | — |
| [`EventsAddFinancialValuesChangedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-financial-values-change) | — | — | — |
| [`EventsAddMilestoneCreatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-milestone-created) | — | — | — |
| [`EventsAddMilestoneDeletedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-milestone-deleted) | — | — | — |
| [`EventsAddMilestoneUpdatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-milestone-updated) | — | — | — |
| [`EventsAddRelationshipCreatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-relationship-created) | — | — | — |
| [`EventsAddRelationshipDeletedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-relationship-deleted) | — | — | — |
| [`EventsAddRelationshipUpdatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-relationship-updated) | — | — | — |
| [`EventsAddResourceAssignmentAddedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-resource-assignment-created) | — | — | — |
| [`EventsAddResourceAssignmentRemovedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-resource-assignment-removed) | — | — | — |
| [`EventsAddResourceAssignmentUpdatedHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-resource-assignment-updated) | — | — | — |
| [`EventsAddStageTransitionHook`](https://learn.microsoft.com/connectors/strategicportfoliomanager/#on-process-stage-transition) | — | — | — |

### Nimflow

**Connector ID:** `nimflow`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenMilestoneCleared_Post`](https://learn.microsoft.com/connectors/nimflow/#when-a-milestone-is-cleared) | — | — | — |
| [`WhenMilestoneReached_Post`](https://learn.microsoft.com/connectors/nimflow/#when-a-milestone-is-reached) | — | — | — |
| [`WhenTaskArchived_Post`](https://learn.microsoft.com/connectors/nimflow/#when-a-task-is-archived) | — | — | — |
| [`WhenTaskCreated_Post`](https://learn.microsoft.com/connectors/nimflow/#when-a-task-is-created) | — | — | — |
| [`WhenTaskUpdated_Post`](https://learn.microsoft.com/connectors/nimflow/#when-a-task-is-updated) | — | — | — |

### Nitro

**Connector ID:** `nitro`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`webhookDocumentSignedTriggerV2`](https://learn.microsoft.com/connectors/nitro/#when-a-signature-request-is-completed) | — | — | — |

### Nitro Sign

**Connector ID:** `nitrosign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`SubscribeEnvelopeCancelled`](https://learn.microsoft.com/connectors/nitrosign/#when-an-envelope-is-cancelled) | — | — | — |
| [`SubscribeEnvelopeCreated`](https://learn.microsoft.com/connectors/nitrosign/#when-an-envelope-is-created) | — | — | — |
| [`SubscribeEnvelopeSealed`](https://learn.microsoft.com/connectors/nitrosign/#when-an-envelope-is-sealed) | — | — | — |
| [`SubscribeEnvelopeSentForSignature`](https://learn.microsoft.com/connectors/nitrosign/#when-an-envelope-is-sent-for-signature) | — | — | — |
| [`SubscribeEnvelopeSigningCompleted`](https://learn.microsoft.com/connectors/nitrosign/#when-envelope-signing-is-completed) | — | — | — |
| [`SubscribeSignatureRequestDeclined`](https://learn.microsoft.com/connectors/nitrosign/#when-a-signature-request-is-declined) | — | — | — |
| [`SubscribeSignatureRequestSigned`](https://learn.microsoft.com/connectors/nitrosign/#when-a-signature-request-is-signed) | — | — | — |

### Nosco

**Connector ID:** `nosco`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`IdeaEditedTrigger`](https://learn.microsoft.com/connectors/nosco/#when-a-post-is-edited) | — | — | — |
| [`IdeaPublishedTrigger`](https://learn.microsoft.com/connectors/nosco/#when-a-new-post-is-published) | — | — | — |
| [`IdeaReachedStageTrigger`](https://learn.microsoft.com/connectors/nosco/#when-a-post-reaches-a-stage) | — | — | — |
| [`IdeaStatusChangedTrigger`](https://learn.microsoft.com/connectors/nosco/#when-a-post-changes-status) | — | — | — |

### Nozbe

**Connector ID:** `nozbe`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`pollNewTasks`](https://learn.microsoft.com/connectors/nozbe/#trigger-when-new-nozbe-task-appears) | — | — | — |
| [`pollUpdatedTasks`](https://learn.microsoft.com/connectors/nozbe/#trigger-when-updated-nozbe-task-appears) | — | — | — |

### nps.today

**Connector ID:** `npstoday`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewCampaignMember`](https://learn.microsoft.com/connectors/npstoday/#subscribe-to-new-campaign-members-being-added) | — | — | — |
| [`NewResponse`](https://learn.microsoft.com/connectors/npstoday/#subscribe-to-new-responses-being-added) | — | — | — |

### nunify

**Connector ID:** `nunify`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NEW_CHECKIN`](https://learn.microsoft.com/connectors/nunify/#attendee-check-in) | — | — | — |
| [`NEW_REGISTRATION`](https://learn.microsoft.com/connectors/nunify/#new-registration) | — | — | — |

## O

### Office 365 Groups

**Connector ID:** `office365groups`

**Type imports:** .NET `Azure.Connectors.Sdk.Office365Groups.Models`; TypeScript `@azure/connectors/generated/Office365groupsExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnGroupMembershipChange`](https://learn.microsoft.com/connectors/office365groups/#when-a-group-member-is-added-or-removed) | `Office365GroupsOnGroupMembershipChangeTriggerPayload` (item: `JsonElement?`) | — | `Office365groupsOnGroupMembershipChangeTriggerPayload` (item: `Record<string, unknown>`) |
| [`OnNewEvent`](https://learn.microsoft.com/connectors/office365groups/#when-there-is-a-new-event) | `Office365GroupsOnNewEventTriggerPayload` (item: `JsonElement?`) | — | `Office365groupsOnNewEventTriggerPayload` (item: `Record<string, unknown>`) |

### Office 365 Groups Mail

**Connector ID:** `office365groupsmail`

**Type imports:** .NET `Azure.Connectors.Sdk.Office365GroupsMail.Models`; TypeScript `@azure/connectors/generated/Office365groupsmailExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewEmailInGroup`](https://learn.microsoft.com/connectors/office365groupsmail/#when-a-new-email-arrives-to-a-group) | `Office365GroupsMailOnNewEmailInGroupTriggerPayload` (item: `ConversationTriggerResponse`) | — | `Office365groupsmailOnNewEmailInGroupTriggerPayload` (item: `ConversationTriggerResponse`) |

### OK dokument (Independent Publisher)

**Connector ID:** `okdokumentip`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WaitForSignature`](https://learn.microsoft.com/connectors/okdokumentip/#wait-for-document-signature) | — | — | — |

### OneBlink

**Connector ID:** `oneblink`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormSubmissionMetaWebhookTrigger`](https://learn.microsoft.com/connectors/oneblink/#triggers-when-a-form-submission-occurs) | — | — | — |

### Oneflow

**Connector ID:** `oneflow`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`webhookRegister`](https://learn.microsoft.com/connectors/oneflow/#when-a-contract-is-updated) | — | — | — |

### OneNote (Business)

**Connector ID:** `onenote`

**Type imports:** .NET `Azure.Connectors.Sdk.Onenote.Models`; TypeScript `@azure/connectors/generated/OnenoteExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewPageInSection`](https://learn.microsoft.com/connectors/onenote/#when-a-new-page-is-created-in-a-section) | `OnenoteOnNewPageInSectionTriggerPayload` (item: `Page`) | — | `OnenoteOnNewPageInSectionTriggerPayload` (item: `Page`) |
| [`OnNewSectionGroupInNotebook`](https://learn.microsoft.com/connectors/onenote/#when-a-new-section-group-is-created) | `OnenoteOnNewSectionGroupInNotebookTriggerPayload` (item: `SectionGroupResponse`) | — | `OnenoteOnNewSectionGroupInNotebookTriggerPayload` (item: `SectionGroupResponse`) |
| [`OnNewSectionInNotebook`](https://learn.microsoft.com/connectors/onenote/#when-a-new-section-is-created) | `OnenoteOnNewSectionInNotebookTriggerPayload` (item: `SectionResponse`) | — | `OnenoteOnNewSectionInNotebookTriggerPayload` (item: `SectionResponse`) |

### OnePlan

**Connector ID:** `oneplan`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AutomationTrigger`](https://learn.microsoft.com/connectors/oneplan/#when-an-automation-event-fires) | — | — | — |
| [`PlanUpdateTrigger`](https://learn.microsoft.com/connectors/oneplan/#when-a-plan-is-updated) | — | — | — |
| [`StepAddTrigger`](https://learn.microsoft.com/connectors/oneplan/#when-a-plan-is-added) | — | — | — |
| [`StepUpdateTrigger`](https://learn.microsoft.com/connectors/oneplan/#when-a-process-step-changes) | — | — | — |

### Oodrive Sign

**Connector ID:** `oodrivesign`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenContractStatusChanged`](https://learn.microsoft.com/connectors/oodrivesign/#when-a-contract%27s-status-change) | — | — | — |

### OpenText Core Signature

**Connector ID:** `opentextcoresignatur`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `CreateSignatureRequest` | — | — | — |
| `OnCancel` | — | — | — |
| `OnDecline` | — | — | — |
| `OnExpire` | — | — | — |
| `OnSign` | — | — | — |

### OpenText eDOCS

**Connector ID:** `opentextedocsbyonefox`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`DocumentContentUpdated`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-document-content-is-updated) | — | — | — |
| [`DocumentCreated`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-a-document-is-created) | — | — | — |
| [`DocumentDeleted`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-a-document-is-deleted) | — | — | — |
| [`DocumentPropertiesUpdated`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-document-properties-are-updated) | — | — | — |
| [`FolderCreated`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-a-folder-is-created) | — | — | — |
| [`FolderDeleted`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-a-folder-is-deleted) | — | — | — |
| [`FolderUpdated`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-a-folder-is-updated) | — | — | — |
| [`ItemDeleted`](https://learn.microsoft.com/connectors/opentextedocsbyonefox/#when-an-item-is-deleted) | — | — | — |

### OpenText Extended ECM

**Connector ID:** `opentextcsbyonefox`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`BusinessWorkspaceCreated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-business-workspace-is-created) | — | — | — |
| [`BusinessWorkspaceDeleted`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-business-workspace-is-deleted) | — | — | — |
| [`BusinessWorkspaceUpdated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-business-workspace-is-updated) | — | — | — |
| [`DocumentCreated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-document-is-created) | — | — | — |
| [`DocumentDeleted`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-document-is-deleted) | — | — | — |
| [`DocumentUpdated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-document-is-updated) | — | — | — |
| [`FolderCreated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-folder-is-created) | — | — | — |
| [`FolderDeleted`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-folder-is-deleted) | — | — | — |
| [`FolderUpdated`](https://learn.microsoft.com/connectors/opentextcsbyonefox/#when-a-folder-is-updated) | — | — | — |

### OrbusInfinity

**Connector ID:** `orbusinfinity`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Post_Webhooks`](https://learn.microsoft.com/connectors/orbusinfinity/#when-an-event-occurs) | — | — | — |

### Outlook.com

**Connector ID:** `outlook`

**Type imports:** .NET `Azure.Connectors.Sdk.Outlook.Models`; TypeScript `@azure/connectors/generated/OutlookExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CalendarGetOnChangedItemsV2`](https://learn.microsoft.com/connectors/outlook/#when-an-event-is-added%2C-updated-or-deleted-%28v2%29) | `OutlookOnCalendarGetOnChangedItemsTriggerPayload` (item: `CalendarEventClientWithActionType`) | — | `OutlookOnCalendarGetOnChangedItemsTriggerPayload` (item: `CalendarEventClientWithActionType`) |
| [`CalendarGetOnNewItemsV2`](https://learn.microsoft.com/connectors/outlook/#when-a-new-event-is-created-%28v2%29) | `OutlookOnCalendarGetOnNewItemsTriggerPayload` (item: `CalendarEventClientReceive`) | — | `OutlookOnCalendarGetOnNewItemsTriggerPayload` (item: `CalendarEventClientReceive`) |
| [`CalendarGetOnUpdatedItemsV2`](https://learn.microsoft.com/connectors/outlook/#when-an-event-is-modified-%28v2%29) | `OutlookOnCalendarGetOnUpdatedItemsTriggerPayload` (item: `CalendarEventClientReceive`) | — | `OutlookOnCalendarGetOnUpdatedItemsTriggerPayload` (item: `CalendarEventClientReceive`) |
| [`OnFlaggedEmailV2`](https://learn.microsoft.com/connectors/outlook/#when-an-email-is-flagged-%28v2%29) | `OutlookOnFlaggedEmailTriggerPayload` (item: `ClientReceiveMessage`) | — | `OutlookOnFlaggedEmailTriggerPayload` (item: `ClientReceiveMessage`) |
| [`OnNewEmailV2`](https://learn.microsoft.com/connectors/outlook/#when-a-new-email-arrives-%28v2%29) | `OutlookOnNewEmailTriggerPayload` (item: `ClientReceiveMessage`) | — | `OutlookOnNewEmailTriggerPayload` (item: `ClientReceiveMessage`) |
| [`OnNewMentionMeEmailV2`](https://learn.microsoft.com/connectors/outlook/#when-a-new-email-mentioning-me-arrives-%28v2%29) | `OutlookOnNewMentionMeEmailTriggerPayload` (item: `ClientReceiveMessage`) | — | `OutlookOnNewMentionMeEmailTriggerPayload` (item: `ClientReceiveMessage`) |
| [`OnUpcomingEventsV2`](https://learn.microsoft.com/connectors/outlook/#when-an-upcoming-event-is-starting-soon-%28v2%29) | `OutlookOnUpcomingEventsTriggerPayload` (item: `CalendarEventClientReceive`) | — | `OutlookOnUpcomingEventsTriggerPayload` (item: `CalendarEventClientReceive`) |

## P

### PagerDuty

**Connector ID:** `pagerduty`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnIncidentAcknowledged`](https://learn.microsoft.com/connectors/pagerduty/#when-an-incident-is-acknowledged) | — | — | — |
| [`OnIncidentAssigned`](https://learn.microsoft.com/connectors/pagerduty/#when-an-incident-is-assigned-to-a-user) | — | — | — |
| [`OnIncidentResolved`](https://learn.microsoft.com/connectors/pagerduty/#when-an-incident-is-resolved) | — | — | — |
| [`OnNewIncidentCreated`](https://learn.microsoft.com/connectors/pagerduty/#when-an-incident-is-created) | — | — | — |
| [`OnNewIncidentNote`](https://learn.microsoft.com/connectors/pagerduty/#when-a-note-is-added) | — | — | — |

### Parserr

**Connector ID:** `parserr`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookEmailReceived`](https://learn.microsoft.com/connectors/parserr/#when-an-email-is-received) | — | — | — |

### Parseur

**Connector ID:** `parseur`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`NewDocumentExpanded`](https://learn.microsoft.com/connectors/parseur/#new-document-processed) | — | — | — |
| [`TableProcessed`](https://learn.microsoft.com/connectors/parseur/#table-processed) | — | — | — |
| [`TemplateNeeded`](https://learn.microsoft.com/connectors/parseur/#document-failed-to-process) | — | — | — |

### Passlet

**Connector ID:** `passlet`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `PassFailedTrigger` | — | — | — |
| `PassIssuedTrigger` | — | — | — |
| `PassScannedTrigger` | — | — | — |
| `PassUpdatedTrigger` | — | — | — |
| `PassVoidedTrigger` | — | — | — |

### Paylocity

**Connector ID:** `paylocity`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookTrigger`](https://learn.microsoft.com/connectors/paylocity/#when-a-time-off-request-is-approved) | — | — | — |

### PDF4me

**Connector ID:** `pdf4me`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Get-Document-From-PDF4me`](https://learn.microsoft.com/connectors/pdf4me/#get-document-from-pdf4me) | — | — | — |

### PDF4me Connect

**Connector ID:** `pdf4meconnect`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Get-Document-From-PDF4me`](https://learn.microsoft.com/connectors/pdf4meconnect/#get-document-from-pdf4me) | — | — | — |

### PDF4me PDF

**Connector ID:** `pdf4mepdf`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Get-Document-From-PDF4me`](https://learn.microsoft.com/connectors/pdf4mepdf/#get-document-from-pdf4me) | — | — | — |

### Peakboard

**Connector ID:** `peakboard`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WhenAlertIsSent`](https://learn.microsoft.com/connectors/peakboard/#when-alert-is-sent) | — | — | — |

### Pinterest

**Connector ID:** `pinterest`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnPinAddedToFollowedBoard`](https://learn.microsoft.com/connectors/pinterest/#when-pin-is-added-to-followed-board-%28preview%29) | — | — | — |
| [`OnPinAddedToMyBoard`](https://learn.microsoft.com/connectors/pinterest/#when-pin-is-added-to-my-board-%28preview%29) | — | — | — |
| [`OnSomeoneFollowsMe_V2`](https://learn.microsoft.com/connectors/pinterest/#when-someone-follows-me-%28v2%29-%28preview%29) | — | — | — |

### Pipedrive

**Connector ID:** `pipedrive`

**Type imports:** .NET `Azure.Connectors.Sdk.Pipedrive.Models`; TypeScript `@azure/connectors/generated/PipedriveExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigNewActivity`](https://learn.microsoft.com/connectors/pipedrive/#when-a-new-activity-is-added) | `PipedriveOnTrigNewActivityTriggerPayload` (item: `ActivityResponse`) | — | `PipedriveOnTrigNewActivityTriggerPayload` (item: `ActivityResponse`) |
| [`TrigNewDealV2`](https://learn.microsoft.com/connectors/pipedrive/#when-a-new-deal-is-added-%28v2%29-%28preview%29) | `PipedriveOnTrigNewDealTriggerPayload` (item: `DealResponseV2`) | — | `PipedriveOnTrigNewDealTriggerPayload` (item: `DealResponseV2`) |

### Pivotal Tracker

**Connector ID:** `pivotaltracker`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TrigStoryCompleted`](https://learn.microsoft.com/connectors/pivotaltracker/#when-a-story-is-completed) | — | — | — |
| [`TrigStoryCreated`](https://learn.microsoft.com/connectors/pivotaltracker/#when-a-new-story-is-created) | — | — | — |
| [`TrigStoryUpdated`](https://learn.microsoft.com/connectors/pivotaltracker/#when-a-story-is-updated) | — | — | — |

### PKIsigning

**Connector ID:** `pkisigning`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Webhooks_CreateWebhook`](https://learn.microsoft.com/connectors/pkisigning/#when-an-event-occurs-on-a-request) | — | — | — |

### Plumsail Documents

**Connector ID:** `plumsail`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FlowV1ProcessesFlowTriggersPost`](https://learn.microsoft.com/connectors/plumsail/#process-finished-%28deprecated%29-%5Bdeprecated%5D) | — | — | — |

`FlowV1ProcessesFlowTriggersPost` is marked deprecated in Microsoft Learn but not in the API snapshot.

### Plumsail Forms

**Connector ID:** `plumsailforms`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`FormIsSubmitted`](https://learn.microsoft.com/connectors/plumsailforms/#form-is-submitted) | — | — | — |

### Poka

**Connector ID:** `poka`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/poka/#add-a-poka-trigger) | — | — | — |

### Polaris PSA

**Connector ID:** `polarispsa`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookSubscriptionsRestAPI`](https://learn.microsoft.com/connectors/polarispsa/#subscribe-to-event-triggers) | — | — | — |

### Power BI

**Connector ID:** `powerbi`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CheckAlertStatus`](https://learn.microsoft.com/connectors/powerbi/#when-a-data-driven-alert-is-triggered) | — | — | — |
| [`GoalChangeTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-a-goal-changes-%28preview%29) | — | — | — |
| [`GoalRefreshFailedTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-a-data-refresh-for-a-goal-fails-%28preview%29) | — | — | — |
| [`GoalsAssignedTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-someone-assigns-a-new-owner-to-a-goal-%28preview%29) | — | — | — |
| [`GoalStatusChangeTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-status-of-a-goal-changes-%28preview%29) | — | — | — |
| [`GoalValueChangeTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-current-value-of-a-goal-changes-%28preview%29) | — | — | — |
| [`GoalValueOrNoteUpsertTrigger`](https://learn.microsoft.com/connectors/powerbi/#when-someone-adds-or-edits-a-goal-check-in-%28preview%29) | — | — | — |

### Power Form 7

**Connector ID:** `powerform7`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateWebhook`](https://learn.microsoft.com/connectors/powerform7/#when-a-contact-form-7-form-is-submitted) | — | — | — |

### Power Platform for Admins V2

**Connector ID:** `powerplatformadminv2`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnAlertTriggered`](https://learn.microsoft.com/connectors/powerplatformadminv2/#when-an-alert-triggers-%28preview%29) | — | — | — |

### Power Textor

**Connector ID:** `powertextor`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`MMSWebhook`](https://learn.microsoft.com/connectors/powertextor/#when-a-text-%28mms%29-message-is-received) | — | — | — |
| [`ProductionWebhook`](https://learn.microsoft.com/connectors/powertextor/#when-a-text-%28sms%29-message-is-received) | — | — | — |

### PPM Express

**Connector ID:** `ppmexpress`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookTrigger`](https://learn.microsoft.com/connectors/ppmexpress/#webhook-trigger) | — | — | — |

### Priority Matrix

**Connector ID:** `prioritymatrix`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ItemCompletedHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-was-completed) | — | — | — |
| [`ItemCreatedHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-was-created) | — | — | — |
| [`ItemDelegatedHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-was-delegated) | — | — | — |
| [`ItemDeletedHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-was-deleted) | — | — | — |
| [`ItemDueHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-is-due) | — | — | — |
| [`ItemStartHook`](https://learn.microsoft.com/connectors/prioritymatrix/#item-starts-now) | — | — | — |

### Priority Matrix HIPAA

**Connector ID:** `prioritymatrixhipaa`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ItemCompletedHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-was-completed) | — | — | — |
| [`ItemCreatedHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-was-created) | — | — | — |
| [`ItemDelegatedHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-was-delegated) | — | — | — |
| [`ItemDeletedHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-was-deleted) | — | — | — |
| [`ItemDueHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-is-due) | — | — | — |
| [`ItemStartHook`](https://learn.microsoft.com/connectors/prioritymatrixhipaa/#item-starts-now) | — | — | — |

### Process Street

**Connector ID:** `processstreet`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CreateTaskStateChangedTrigger`](https://learn.microsoft.com/connectors/processstreet/#when-a-task-is-checked%2C-unchecked-or-ready) | — | — | — |
| [`CreateWorkflowRunCompletedTrigger`](https://learn.microsoft.com/connectors/processstreet/#when-a-workflow-run-is-completed) | — | — | — |
| [`CreateWorkflowRunCreatedTrigger`](https://learn.microsoft.com/connectors/processstreet/#when-a-workflow-is-run) | — | — | — |

### Project Online

**Connector ID:** `projectonline`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewProject`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-project-is-created) | — | — | — |
| [`OnNewProjectV2`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-project-is-created-%28v2%29-%28preview%29) | — | — | — |
| [`OnNewResource`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-resource-is-created) | — | — | — |
| [`OnNewResourceV2`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-resource-is-created-%28v2%29-%28preview%29) | — | — | — |
| [`OnNewTask`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-task-is-created) | — | — | — |
| [`OnNewTaskV2`](https://learn.microsoft.com/connectors/projectonline/#when-a-new-task-is-created-%28v2%29-%28preview%29) | — | — | — |
| [`OnProjectPublished`](https://learn.microsoft.com/connectors/projectonline/#when-a-project-is-published) | — | — | — |
| [`OnProjectPublishedV2`](https://learn.microsoft.com/connectors/projectonline/#when-a-project-is-published-%28v2%29-%28preview%29) | — | — | — |

### ProjectPlace

**Connector ID:** `projectplace`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`set_webhook_card_create`](https://learn.microsoft.com/connectors/projectplace/#when-a-card-is-created) | — | — | — |
| [`set_webhook_card_due_date`](https://learn.microsoft.com/connectors/projectplace/#when-a-card-is-due) | — | — | — |
| [`set_webhook_properties_change`](https://learn.microsoft.com/connectors/projectplace/#when-a-card%27s-properties-are-changed) | — | — | — |

### Pure Leads

**Connector ID:** `pureleads`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Created-Lead-Submission`](https://learn.microsoft.com/connectors/pureleads/#new-lead-submission) | — | — | — |

### Pushcut

**Connector ID:** `pushcut`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ActionExecuted`](https://learn.microsoft.com/connectors/pushcut/#when-an-action-is-executed) | — | — | — |

## Q

### QPP NextGen

**Connector ID:** `qppngintegrationhub`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`Webhook-trigger`](https://learn.microsoft.com/connectors/qppngintegrationhub/#triggers-when-an-event-occurs-for-an-already-registered-webhook) | — | — | — |

## R

### RamQuest Events

**Connector ID:** `ramquestevents`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CCEEventTrigger`](https://learn.microsoft.com/connectors/ramquestevents/#cce-event-trigger) | — | — | — |
| [`HorizonEventTrigger`](https://learn.microsoft.com/connectors/ramquestevents/#horizon-event-trigger) | — | — | — |

### RAPID Platform

**Connector ID:** `rapidplatform`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ListHookTrigger`](https://learn.microsoft.com/connectors/rapidplatform/#when-something-happens-to-an-item-in-your-rapid-site) | — | — | — |

### Receptful

**Connector ID:** `receptful`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`VisitEvents`](https://learn.microsoft.com/connectors/receptful/#create-a-webhook-for-visit-events) | — | — | — |

### Redmine

**Connector ID:** `redmine`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewIssue`](https://learn.microsoft.com/connectors/redmine/#when-an-issue-is-created) | — | — | — |
| [`OnNewProject`](https://learn.microsoft.com/connectors/redmine/#when-a-project-is-created) | — | — | — |
| [`OnUpdatedIssue`](https://learn.microsoft.com/connectors/redmine/#when-an-issue-is-updated) | — | — | — |

### Rencore Governance

**Connector ID:** `rencoregovernance`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CheckNotificationTrigger`](https://learn.microsoft.com/connectors/rencoregovernance/#when-a-check-in-rencore-governance-is-violated) | — | — | — |

### Rencore Oversight

**Connector ID:** `rencoreoversight`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| `PolicyNotificationTrigger` | — | — | — |

### Replicon

**Connector ID:** `replicon`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`WebhookSubscriptionsRestAPI`](https://learn.microsoft.com/connectors/replicon/#subscribe-to-event-triggers) | — | — | — |

### Requestor

**Connector ID:** `requestor`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`CompanyCreated`](https://learn.microsoft.com/connectors/requestor/#when-a-company-is-created) | — | — | — |
| [`CompanyDeleted`](https://learn.microsoft.com/connectors/requestor/#when-a-company-is-deleted) | — | — | — |
| [`CompanyUpdated`](https://learn.microsoft.com/connectors/requestor/#when-a-company-is-updated) | — | — | — |
| [`TicketCategoryChange`](https://learn.microsoft.com/connectors/requestor/#when-a-ticket-category-is-changed) | — | — | — |
| [`TicketCreated`](https://learn.microsoft.com/connectors/requestor/#when-a-ticket-is-created) | — | — | — |
| [`TicketCustomFormChange`](https://learn.microsoft.com/connectors/requestor/#ticket-custom-form-change) | — | — | — |
| [`TicketMonitoring`](https://learn.microsoft.com/connectors/requestor/#ticket-monitoring) | — | — | — |
| [`TicketStateChange`](https://learn.microsoft.com/connectors/requestor/#when-a-ticket-state-changes) | — | — | — |
| [`UserCreated`](https://learn.microsoft.com/connectors/requestor/#when-a-user-is-created) | — | — | — |
| [`UserDeleted`](https://learn.microsoft.com/connectors/requestor/#when-a-user-is-deleted) | — | — | — |
| [`UserUpdated`](https://learn.microsoft.com/connectors/requestor/#when-a-user-is-updated) | — | — | — |

### Resco Cloud

**Connector ID:** `rescocloud`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`TriggerCreate`](https://learn.microsoft.com/connectors/rescocloud/#when-an-item-is-created-updated-deleted) | — | — | — |

### Revelation helpdesk

**Connector ID:** `revelationhelpdesk`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`AssetCreatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-asset-is-created) | — | — | — |
| [`AssetUpdatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-asset-is-updated) | — | — | — |
| [`ClientCreatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-client-is-created) | — | — | — |
| [`ClientUpdatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-client-is-updated) | — | — | — |
| [`OfficeCreatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-office-is-created) | — | — | — |
| [`OfficeUpdatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-office-is-updated) | — | — | — |
| [`TicketAtRiskWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-at-risk) | — | — | — |
| [`TicketClosedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-closed) | — | — | — |
| [`TicketDeletedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-deleted) | — | — | — |
| [`TicketDueWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-due) | — | — | — |
| [`TicketLoggedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-logged) | — | — | — |
| [`TicketReopenedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-reopened) | — | — | — |
| [`TicketUpdatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-ticket-is-updated) | — | — | — |
| [`UserCreatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-user-is-created) | — | — | — |
| [`UserUpdatedWebhook`](https://learn.microsoft.com/connectors/revelationhelpdesk/#when-a-user-is-updated) | — | — | — |

### RowShare

**Connector ID:** `rowshare`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`ColumnChanged`](https://learn.microsoft.com/connectors/rowshare/#column-changed) | — | — | — |
| [`NewOrUpdatedRow`](https://learn.microsoft.com/connectors/rowshare/#new-or-updated-row-%28polling%29) | — | — | — |
| [`NewRow`](https://learn.microsoft.com/connectors/rowshare/#new-row-%28polling%29) | — | — | — |
| [`RowStatusChanged`](https://learn.microsoft.com/connectors/rowshare/#row-status-changed) | — | — | — |

### RSS

**Connector ID:** `rss`

**Type imports:** .NET `Azure.Connectors.Sdk.Rss.Models`; TypeScript `@azure/connectors/generated/RssExtensions`

| Trigger operation ID | .NET payload | Python binding | TypeScript helper / payload |
| --- | --- | --- | --- |
| [`OnNewFeed`](https://learn.microsoft.com/connectors/rss/#when-a-feed-item-is-published) | `RssOnNewFeedTriggerPayload` (item: `FeedItem`) | — | `RssOnNewFeedTriggerPayload` (item: `FeedItem`) |

[Back to connector index](../operations-functions-match.md)
