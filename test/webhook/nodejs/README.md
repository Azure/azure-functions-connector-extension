# Node.js Sample App

This sample demonstrates how to use the Connector Extension with the Node.js v4 programming model (TypeScript) using `@azure/functions-extensions-connectors`.

When Connector Namespace detects a connector event (for example, a new Office
365 email), it sends a Webhook callback to the function. The extension package
normalizes the payload into a strongly typed context. The sample logs key
fields and persists the raw payload to Azure Blob Storage.

## Prerequisites

- [Node.js 22+](https://learn.microsoft.com/azure/azure-functions/supported-languages?pivots=programming-language-typescript#languages-by-runtime-version)
- Azure Functions Core Tools v4+
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Install dependencies:**

   ```bash
   cd test/webhook/nodejs
   npm install
   ```

2. **Build TypeScript:**

   ```bash
   npm run build
   ```

3. **Start Azurite** in another terminal if it is not already running:

   ```bash
   azurite --silent
   ```

4. **Run the function app:**

   ```bash
   npm start
   ```

## Available Functions

| Function | Approach | Description |
| ---------- | ---------- | ------------- |
| `OnNewEmail` | `connectors.office365.onNewEmail()` | Typed email trigger with `EmailTriggerContext` and blob output |
| `OnNewEmailDirect` | `app.connectorTrigger()` | Raw trigger with manual payload parsing and blob output |

## Testing

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnNewEmail" \
     -H "Content-Type: application/json" \
     -d '{
       "body": {
         "value": [{
           "subject": "URGENT: Action Required",
           "from": "john@contoso.com",
           "importance": "high",
           "hasAttachments": false
         }]
       }
     }'
```

## Code Structure

### Using `@azure/functions-extensions-connectors` (recommended)

```typescript
import { InvocationContext, output } from '@azure/functions';
import { connectors, EmailTriggerContext } from '@azure/functions-extensions-connectors';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'AzureWebJobsStorage',
});

connectors.office365.onNewEmail('OnNewEmail', {
    extraOutputs: [blobOutput],
    handler: async (context: EmailTriggerContext, invocationContext: InvocationContext) => {
        // context.emails is GraphClientReceiveMessage[] — full IntelliSense
        for (const email of context.emails) {
            invocationContext.log(`Subject: '${email.subject}'.`);
        }

        invocationContext.extraOutputs.set(blobOutput, context.toJSON());
    },
});
```

### Using `app.connectorTrigger()` directly (for comparison)

```typescript
import { app, InvocationContext, output } from '@azure/functions';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'AzureWebJobsStorage',
});

app.connectorTrigger('OnNewEmailDirect', {
    extraOutputs: [blobOutput],
    handler: async (triggerInput: unknown, invocationContext: InvocationContext) => {
        const parsed = typeof triggerInput === 'string'
            ? JSON.parse(triggerInput) : (triggerInput ?? {});

        invocationContext.extraOutputs.set(blobOutput, JSON.stringify(parsed));
    },
});
```
