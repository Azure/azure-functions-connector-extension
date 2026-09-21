// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { app, InvocationContext } from '@azure/functions';

app.connectorTrigger('OnNewEmailPoll', {
    deliveryMode: 'Poll',
    connection: 'ConnectorNamespace',
    triggerConfigName: '%ConnectorTriggerConfigName%',
    maxBatchSize: 1,
    concurrency: 4,
    handler: async (input: unknown, context: InvocationContext) => {
        const payload = typeof input === 'string' ? JSON.parse(input) : input;
        context.log(`Poll connector payload: ${JSON.stringify(payload)}`);
    },
});
