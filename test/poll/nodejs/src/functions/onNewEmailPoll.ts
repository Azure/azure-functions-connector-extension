// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { app, InvocationContext } from '@azure/functions';

app.connectorTrigger('OnNewEmailPoll', {
    deliveryMode: 'Poll',
    connection: 'ConnectorNamespace',
    triggerConfigName: '%OnNewEmailTriggerConfigName%',
    cardinality: 'many',
    maxBatchSize: 4,
    concurrency: 4,
    handler: async (inputs: unknown[], context: InvocationContext) => {
        context.log(`Received ${inputs.length} Poll connector payload(s).`);
    },
});
