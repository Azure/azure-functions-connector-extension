# Python Sample App

This sample demonstrates how to use the Connector Extension with the Python v2 programming model.

When AI Gateway detects a connector event (e.g., a new Office 365 email arrives), it sends a webhook callback to your function. The function receives the JSON payload via the `connectorTrigger` binding, logs key fields, and persists the raw payload to Azure Blob Storage using a blob output binding. The blob output provides a simple way to archive every incoming event for auditing, replay, or downstream processing.

## Prerequisites

- [Python 3.13+](https://learn.microsoft.com/azure/azure-functions/supported-languages?pivots=programming-language-python#languages-by-runtime-version)
- Use `generic_trigger` for lower Python version support.
- Azure Functions Core Tools v4
- Azure Storage Emulator (Azurite) or Azure Storage account

## Setup

1. **Build the extension** (from repo root):

   ```bash
   cd samples/python
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

```python
@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="email")
def on_new_email(email: office365.ClientReceiveMessage) -> None:
    """
    Receives Office 365 email trigger callbacks from Connector Namespace managed connectors
    and saves to blob storage.
    """
    logging.info("OnNewEmail trigger received. Payload: %s", email)

    logging.info(f"Subject: {email.subject}")
    logging.info(f"From: {email.from_}")
```
