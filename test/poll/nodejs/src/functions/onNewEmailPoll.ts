// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { app, InvocationContext } from '@azure/functions';

app.connectorTrigger('OnNewEmailPoll', {
    deliveryMode: 'Poll',
    connection: 'ConnectorNamespace',
    pollingEndpoint: '%OnNewEmailEndpoint%',
    cardinality: 'many',
    maxBatchSize: 4,
    concurrency: 4,
    handler: async (inputs: unknown[], context: InvocationContext) => {
        for (const input of inputs) {
            const payload = typeof input === 'string' ? JSON.parse(input) : input;
            context.log(`Poll connector payload: ${JSON.stringify(payload)}`);
        }
    },
});
