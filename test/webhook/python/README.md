# Python Sample App

This sample demonstrates how to use the Connector Extension with the Python v2 programming model.

When Connector Namespace detects a connector event (for example, a new Office 365 email), it sends a Webhook callback to the function. The Connector binding converts the callback into typed `ClientReceiveMessage` values. The sample logs key fields and persists the serialized batch to Azure Blob Storage.

## Prerequisites

- [Supported Python version](https://learn.microsoft.com/azure/azure-functions/supported-languages?pivots=programming-language-python#languages-by-runtime-version)
- Azure Functions Core Tools v4+
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Build the extension** (from repo root):

   ```bash
   cd test/webhook/python
   dotnet build extensions.csproj
   ```

2. **Create virtual environment:**

   ```bash
   python -m venv .venv
   source .venv/bin/activate  # Linux/macOS
   .venv\Scripts\activate     # Windows
   pip install -r requirements.txt
   ```

3. **Start Azurite** (in another terminal):

   ```bash
   azurite --silent
   ```

4. **Run the function app:**

   ```bash
   func start
   ```

## Available Functions

| Function     | Description                             | Example Use Case        |
| ------------ | --------------------------------------- | ----------------------- |
| `OnNewEmail` | Office 365 email Webhook from Connector Namespace | O365 mailbox monitoring |

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

```python
import json
from typing import List

import azure.functions as func
import azurefunctions.extensions.connectors.office365 as office365

@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="emails")
@app.blob_output(
    arg_name="outputblob",
    path="connector-messages/{rand-guid}.json",
    connection="BlobStoreConnection",
)
def on_new_email(
    emails: List[office365.ClientReceiveMessage],
    outputblob: func.Out[str],
) -> None:
    """
    Receives Office 365 email trigger callbacks from Connector Namespace managed connectors
    and saves to blob storage.
    """
    for email in emails:
        logging.info("Subject: %s", email.subject)
        logging.info("From: %s", email.from_)

    outputblob.set(json.dumps([vars(email) for email in emails], default=str))
```
