// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { connectors } from '@azure/functions-extensions-connectors';

connectors.office365.onNewEmail('OnNewEmailPoll', {
    deliveryMode: 'Poll',
    connection: 'ConnectorNamespace',
    triggerConfigName: '%OnNewEmailTriggerConfigName%',
    cardinality: 'many',
    maxBatchSize: 4,
    targetPendingEventThreshold: 16,
    handler: async (emails, context) => {
        if (!Array.isArray(emails.rawPayload)) {
            throw new TypeError('Expected an array of Poll connector payloads.');
        }
        if (emails.rawPayload.some(payload =>
            typeof payload !== 'object' || payload === null || Array.isArray(payload) ||
            !('body' in payload) || typeof payload.body !== 'object' || payload.body === null || Array.isArray(payload.body)
        )) {
            throw new TypeError('Expected each Poll connector payload to contain an email body.');
        }
        context.log(`Received ${emails.rawPayload.length} Poll connector payload(s) with ${emails.emails.length} email(s).`);
    },
});
