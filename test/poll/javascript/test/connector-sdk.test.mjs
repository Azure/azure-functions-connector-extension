// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import assert from 'node:assert/strict';
import { mock, test } from 'node:test';
import { app } from '@azure/functions';

test('the Poll sample registers the SDK helper and normalizes a batch', async () => {
    const registrations = [];
    const registration = mock.method(app, 'connectorTrigger', (name, options) => {
        registrations.push({ name, options });
    });
    try {
        await import('../src/functions/onNewEmailPoll.js');
    } finally {
        registration.mock.restore();
    }
    assert.equal(registrations.length, 1);
    const { name, options } = registrations[0];
    assert.equal(name, 'OnNewEmailPoll');
    assert.equal(options.deliveryMode, 'Poll');
    assert.equal(options.connection, 'ConnectorNamespace');
    assert.equal(options.triggerConfigName, '%OnNewEmailTriggerConfigName%');
    assert.equal(options.cardinality, 'many');
    assert.equal(options.maxBatchSize, 4);
    assert.equal(options.targetPendingEventThreshold, 16);

    const batch = [
        { body: { id: 'one', subject: 'First email' } },
        { body: { value: [{ id: 'two' }, { id: 'three' }] } },
    ];
    for (const payload of [batch, JSON.stringify(batch)]) {
        const logs = [];
        await options.handler(payload, { log: message => logs.push(message) });
        assert.deepEqual(logs, ['Received 2 Poll connector payload(s) with 3 email(s).']);
    }
    await assert.rejects(
        () => options.handler({ body: { id: 'one' } }, { log() {} }),
        /Expected an array of Poll connector payloads/,
    );
    await assert.rejects(
        () => options.handler([batch[0], 'invalid JSON'], { log() {} }),
        /Expected each Poll connector payload to contain an email body/,
    );
});
