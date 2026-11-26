# TypeScript Webhook Test App

This test app demonstrates the Connector Extension with the Node.js v4 TypeScript programming model using `@azure/functions-extensions-connectors`.

When Connector Namespace detects an event, it sends a Webhook callback to the function. The extension normalizes the payload into a typed context; the app logs key fields and writes the raw payload to Blob Storage.

## Prerequisites

- [Node.js 24+](https://learn.microsoft.com/azure/azure-functions/supported-languages?pivots=programming-language-typescript#languages-by-runtime-version), required by the 1.0.0 connector extension
- .NET SDK 10+ only to build the repository-local development host extension project (`extensions.csproj`); it is not required by the TypeScript app itself
- Azure Functions Core Tools v4+
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

Copy `local.settings.example.json` to the gitignored `local.settings.json` before starting the host.

1. **Build the local binding extensions:**

   ```bash
   cd test/webhook/typescript
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

Run `npm test` from this test app directory to validate callback normalization and blob output handling without connecting to Azure.

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
