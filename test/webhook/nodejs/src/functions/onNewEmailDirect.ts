// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License.

import { app, InvocationContext, output } from '@azure/functions';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'BlobStoreConnection',
});

/**
 * Sample: Direct app.connectorTrigger() usage without the extensions package.
 *
 * This demonstrates using the first-class connectorTrigger binding
 * from the core @azure/functions library. The raw trigger payload is
 * passed directly to the handler as-is (string or object).
 */
app.connectorTrigger('OnNewEmailDirect', {
    extraOutputs: [blobOutput],
    handler: async (triggerInput: unknown, invocationContext: InvocationContext) => {
        invocationContext.log('OnNewEmailDirect trigger received via app.connectorTrigger().');

        const parsed = typeof triggerInput === 'string'
            ? JSON.parse(triggerInput) as Record<string, unknown>
            : (triggerInput ?? {}) as Record<string, unknown>;

        invocationContext.log(`Raw payload: '${JSON.stringify(parsed)}'.`);

        // Persist the raw payload to blob storage for auditing/replay
        invocationContext.extraOutputs.set(blobOutput, parsed);
    },
});
