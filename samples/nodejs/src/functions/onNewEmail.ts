/**
 * Azure Functions Connector Extension - Node.js Sample
 *
 * Receives trigger callbacks from Connector Namespace managed connectors
 * and saves the payload to blob storage.
 */

import { app, InvocationContext, output } from "@azure/functions";

const blobOutput = output.storageBlob({
  path: "connector-messages/{rand-guid}.json",
  connection: "AzureWebJobsStorage",
});

app.generic("OnNewEmail", {
  trigger: {
    type: "connectorTrigger",
    name: "payload",
  },
  extraOutputs: [blobOutput],
  handler: async (payload: unknown, context: InvocationContext) => {
    context.log("OnNewEmail trigger received");

    const data = typeof payload === "string" ? JSON.parse(payload) : payload;
    const emails: Record<string, unknown>[] = data?.body?.value ?? [];

    for (const email of emails) {
      context.log(`Subject: ${email.subject}`);
      context.log(`From: ${email.from}`);
    }

    context.extraOutputs.set(blobOutput, JSON.stringify(data));
  },
});
