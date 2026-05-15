// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License.

import { InvocationContext, output } from '@azure/functions';
import { connectors, EmailTriggerContext } from '@azure/functions-extensions-connectors';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'BlobStoreConnection',
});

connectors.office365.onNewEmail('OnNewEmail', {
    extraOutputs: [blobOutput],
    handler: async (context: EmailTriggerContext, invocationContext: InvocationContext) => {
        invocationContext.log('OnNewEmail trigger received.');

        // context.emails is typed as GraphClientReceiveMessage[] — full IntelliSense
        for (const email of context.emails) {
            invocationContext.log(`Subject: '${email.subject}'.`);
            invocationContext.log(`From: '${email.from}'.`);
            invocationContext.log(`Importance: '${email.importance}'.`);
            invocationContext.log(`Has attachments: '${email.hasAttachments}'.`);
        }

        invocationContext.log(`Batch contains '${context.items.length}' item(s).`);

        // Persist the raw payload to blob storage for auditing/replay
        invocationContext.extraOutputs.set(blobOutput, context.toJSON());
    },
});
