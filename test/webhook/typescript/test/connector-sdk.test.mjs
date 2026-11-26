// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import assert from 'node:assert/strict';
import { mock, test } from 'node:test';
import { app } from '@azure/functions';

test('the typed trigger normalizes released SDK callback shapes and writes raw output', async () => {
    const registrations = [];
    const registration = mock.method(app, 'connectorTrigger', (name, options) => {
        registrations.push({ name, options });
    });

    try {
        await import('../dist/functions/onNewEmail.js');
    } finally {
        registration.mock.restore();
    }

    assert.equal(registrations.length, 1);
    const { name, options } = registrations[0];
    assert.equal(name, 'OnNewEmail');
    assert.equal(options.extraOutputs.length, 1);

    const email = {
        id: 'synthetic-id',
        subject: 'Synthetic email',
        from: 'sender@example.invalid',
        importance: 'high',
        hasAttachments: false,
    };
    for (const payload of [
        { body: email },
        { body: { value: [email] }, headers: { 'x-test': 'value' } },
    ]) {
        const logs = [];
        const outputs = [];
        await options.handler(payload, {
            log: (message) => logs.push(message),
            extraOutputs: { set: (binding, value) => outputs.push({ binding, value }) },
        });
        assert.ok(logs.includes(`Subject: '${email.subject}'.`));
        assert.ok(logs.includes("Batch contains '1' item(s)."));
        assert.equal(outputs.length, 1);
        assert.equal(outputs[0].binding, options.extraOutputs[0]);
        assert.deepEqual(outputs[0].value, payload);
    }
});

test('the generic extension trigger preserves the parsed payload in blob output', async () => {
    const registrations = [];
    const registration = mock.method(app, 'connectorTrigger', (name, options) => {
        registrations.push({ name, options });
    });
    try {
        await import('../dist/functions/onNewEmailDirect.js');
    } finally {
        registration.mock.restore();
    }

    assert.equal(registrations.length, 1);
    const { name, options } = registrations[0];
    assert.equal(name, 'OnNewEmailDirect');
    assert.equal(options.extraOutputs.length, 1);

    const payload = { body: { id: 'synthetic-id', subject: 'Synthetic email' }, headers: { 'x-test': 'value' } };
    for (const input of [payload, JSON.stringify(payload)]) {
        const outputs = [];
        await options.handler(input, {
            log() {},
            extraOutputs: { set: (binding, value) => outputs.push({ binding, value }) },
        });
        assert.deepEqual(outputs, [{ binding: options.extraOutputs[0], value: payload }]);
    }

    const warnings = [];
    await assert.rejects(
        () => options.handler('not JSON', { log() {}, warn: (message) => warnings.push(message) }),
        /Expected a JSON connector trigger payload object or array/,
    );
    assert.equal(warnings.length, 1);
});
