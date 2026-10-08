// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { app, InvocationContext } from '@azure/functions';
import type { TriggerCallbackPayload } from '@azure/connectors';
import type { GraphClientReceiveMessage } from '@azure/connectors/generated/Office365Extensions';

type EmailPollPayload =
    | TriggerCallbackPayload<GraphClientReceiveMessage>
    | { body: GraphClientReceiveMessage };

app.connectorTrigger('OnNewEmailPoll', {
    deliveryMode: 'Poll',
    connection: 'ConnectorNamespace',
    triggerConfigName: '%OnNewEmailTriggerConfigName%',
    cardinality: 'many',
    maxBatchSize: 4,
    targetPendingEventThreshold: 16,
    handler: async (inputs: EmailPollPayload[], context: InvocationContext) => {
        context.log(`Received ${inputs.length} Poll connector payload(s).`);
    },
});
