# Node.js Sample App

This sample demonstrates how to use the Connector Extension with the Node.js v4 programming model (TypeScript).

When AI Gateway detects a connector event (e.g., a new Office 365 email arrives), it sends a webhook callback to your function. The function receives the JSON payload via the `connectorTrigger` binding, logs key fields, and persists the raw payload to Azure Blob Storage using a blob output binding. The blob output provides a simple way to archive every incoming event for auditing, replay, or downstream processing.

## Prerequisites

- [Node.js 20+](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-node#supported-versions)
- Azure Functions Core Tools v4
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Build the extension** (from repo root):

   ```bash
   cd samples/nodejs
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

4. **Start Azurite** (in another terminal):

   ```bash
   azurite --silent
   ```

5. **Run the function app:**

   ```bash
   npm start
   ```

## Available Functions

| Function     | Description                          | Example Use Case       |
| ------------ | ------------------------------------ | ---------------------- |
| `OnNewEmail` | Office 365 email trigger via AI Gateway | O365 mailbox monitoring |

## Testing

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnNewEmail" \
     -H "Content-Type: application/json" \
     -d '{
       "body": {
         "value": [{
           "subject": "URGENT: Action Required",
           "from": "john@contoso.com"
         }]
       }
     }'
```

## Code Structure

```typescript
import { app, InvocationContext, output } from "@azure/functions";

const blobOutput = output.storageBlob({
  path: "connector-messages/{rand-guid}.json",
  connection: "BlobStoreConnection",
});

app.generic("OnNewEmail", {
  trigger: {
    type: "connectorTrigger",
    name: "payload",
  },
  extraOutputs: [blobOutput],
  handler: async (payload: unknown, context: InvocationContext) => {
    const data = typeof payload === "string" ? JSON.parse(payload) : payload;
    // ... process emails
    context.extraOutputs.set(blobOutput, JSON.stringify(data));
  },
});
```
