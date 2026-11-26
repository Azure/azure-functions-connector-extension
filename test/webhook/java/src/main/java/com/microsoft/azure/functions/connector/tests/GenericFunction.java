// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

package com.microsoft.azure.functions.connector.tests;

import com.microsoft.azure.functions.ExecutionContext;
import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.annotation.FunctionName;

public class GenericFunction {
    @FunctionName("OnGenericWebhook")
    public void run(
            @CustomBinding(name = "payload", type = "connectorTrigger", direction = "in") String payload,
            final ExecutionContext context) {
        if (payload == null || payload.isBlank()) {
            throw new IllegalArgumentException("Expected a non-empty connector payload.");
        }
        context.getLogger().info("Received generic Webhook connector payload.");
    }
}
