# Node.js Sample App

This sample demonstrates how to use the Connector Extension with the Node.js v4 programming model (TypeScript) using `@azure/functions-extensions-connectors`.

When AI Gateway detects a connector event (e.g., a new Office 365 email arrives), it sends a webhook callback to your function. The extension package automatically normalizes the payload into a strongly-typed context. The function logs key fields and persists the raw payload to Azure Blob Storage using a blob output binding.

## Prerequisites

- [Node.js 20+](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-node#supported-versions)
- Azure Functions Core Tools v4
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Install dependencies:**

   ```bash
   cd samples/nodejs
   npm install
   ```

2. **Build TypeScript:**

   ```bash
   npm run build
   ```

3. **Start Azurite** (in another terminal):

   ```bash
   azurite --silent
   ```

4. **Run the function app:**

   ```bash
   npm start
   ```

## Available Functions

| Function | Approach | Description |
|----------|----------|-------------|
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
    connection: 'BlobStoreConnection',
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
    connection: 'BlobStoreConnection',
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
