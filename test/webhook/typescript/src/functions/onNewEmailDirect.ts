// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License.

import { InvocationContext, output } from '@azure/functions';
import { connectorTrigger, ConnectorTriggerContext, GraphClientReceiveMessage } from '@azure/functions-extensions-connectors';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'BlobStoreConnection',
});

/**
 * Test: Generic registration with the connector extension.
 */
connectorTrigger<GraphClientReceiveMessage>('OnNewEmailDirect', {
    extraOutputs: [blobOutput],
    handler: async (context: ConnectorTriggerContext<GraphClientReceiveMessage>, invocationContext: InvocationContext) => {
        invocationContext.log('OnNewEmailDirect trigger received via connectorTrigger().');

        if (typeof context.rawPayload !== 'object' || context.rawPayload === null) {
            throw new TypeError('Expected a JSON connector trigger payload object or array.');
        }

        invocationContext.log(`Raw payload: '${JSON.stringify(context.rawPayload)}'.`);

        // Persist the raw payload to blob storage for auditing/replay
        invocationContext.extraOutputs.set(blobOutput, context.rawPayload);
    },
});
