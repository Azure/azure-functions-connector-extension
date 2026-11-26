// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { output } from '@azure/functions';
import { connectors } from '@azure/functions-extensions-connectors';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'AzureWebJobsStorage',
});

connectors.office365.onNewEmail('OnNewEmail', {
    extraOutputs: [blobOutput],
    handler: async (context, invocationContext) => {
        invocationContext.log('OnNewEmail trigger received.');
        for (const email of context.emails) {
            invocationContext.log(`Subject: '${email.subject}'.`);
            invocationContext.log(`From: '${email.from}'.`);
            invocationContext.log(`Importance: '${email.importance}'.`);
            invocationContext.log(`Has attachments: '${email.hasAttachments}'.`);
        }
        const batch = context.payload.body?.value ?? [];
        invocationContext.log(`Batch contains '${batch.length}' item(s).`);
        invocationContext.extraOutputs.set(blobOutput, context.rawPayload);
    },
});
