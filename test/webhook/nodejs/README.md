# Node.js Sample App

This sample demonstrates how to use the Connector Extension with the Node.js v4 programming model (TypeScript) using `@azure/functions-extensions-connectors`.

When Connector Namespace detects a connector event (for example, a new Office 365 email), it sends a Webhook callback to the function. The extension package normalizes the payload into a strongly typed context. The sample logs key fields and persists the raw payload to Azure Blob Storage.

## Prerequisites

- [Node.js 24+](https://learn.microsoft.com/azure/azure-functions/supported-languages?pivots=programming-language-typescript#languages-by-runtime-version), required by the 1.0.0 connector extension
- .NET 10 SDK to build the local binding extensions
- Azure Functions Core Tools v4+
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Build the local binding extensions:**

   ```bash
   cd test/webhook/nodejs
   dotnet build extensions.csproj
   ```

2. **Install dependencies:**

   ```bash
   npm install
   ```

3. **Build TypeScript:**

   ```bash
   npm run build
   ```

4. **Start Azurite** in another terminal if it is not already running:

   ```bash
   azurite --silent
   ```

5. **Run the function app:**

   ```bash
   npm start
   ```

## Available Functions

| Function | Approach | Description |
| ---------- | ---------- | ------------- |
| `OnNewEmail` | `connectors.office365.onNewEmail()` | Typed email trigger with `EmailTriggerContext` and blob output |
| `OnNewEmailDirect` | `connectorTrigger()` | Generic typed trigger with raw payload and blob output |

## Testing

Run `npm test` from this sample directory to validate typed callback normalization and blob output handling without connecting to Azure.

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

### Using the extension's generic `connectorTrigger()`

```typescript
import { InvocationContext, output } from '@azure/functions';
import { connectorTrigger, ConnectorTriggerContext, GraphClientReceiveMessage } from '@azure/functions-extensions-connectors';

const blobOutput = output.storageBlob({
    path: 'connector-messages/{rand-guid}.json',
    connection: 'BlobStoreConnection',
});

connectorTrigger<GraphClientReceiveMessage>('OnNewEmailDirect', {
    extraOutputs: [blobOutput],
    handler: async (context: ConnectorTriggerContext<GraphClientReceiveMessage>, invocationContext: InvocationContext) => {
        if (typeof context.rawPayload !== 'object' || context.rawPayload === null) {
            throw new TypeError('Expected a JSON connector trigger payload object or array.');
        }

        invocationContext.extraOutputs.set(blobOutput, context.rawPayload);
    },
});
```
