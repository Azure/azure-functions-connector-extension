// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import assert from 'node:assert/strict';
import { mock, test } from 'node:test';
import { app } from '@azure/functions';

test('the SDK sample normalizes callbacks and writes the raw payload to blob output', async () => {
    const registrations = [];
    const registration = mock.method(app, 'connectorTrigger', (name, options) => {
        registrations.push({ name, options });
    });
    try {
        await import('../src/functions/onNewEmail.js');
    } finally {
        registration.mock.restore();
    }
    assert.equal(registrations.length, 1);
    const { name, options } = registrations[0];
    assert.equal(name, 'OnNewEmail');
    assert.equal(options.extraOutputs.length, 1);

    const email = {
        id: 'synthetic-id', subject: 'Synthetic email', from: 'sender@example.invalid',
        importance: 'high', hasAttachments: false,
    };
    for (const payload of [
        { body: email },
        { body: { value: [email] }, headers: { 'x-test': 'value' } },
    ]) {
        const logs = [];
        const outputs = [];
        await options.handler(payload, {
            log: message => logs.push(message),
            extraOutputs: { set: (binding, value) => outputs.push({ binding, value }) },
        });
        assert.ok(logs.includes(`Subject: '${email.subject}'.`));
        assert.ok(logs.includes("Batch contains '1' item(s)."));
        assert.deepEqual(outputs, [{ binding: options.extraOutputs[0], value: payload }]);
    }
});
