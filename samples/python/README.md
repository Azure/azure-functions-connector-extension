# Python Sample App

This sample demonstrates how to use the Connector Extension with the Python v2 programming model.

When AI Gateway detects a connector event (e.g., a new Office 365 email arrives), it sends a webhook callback to your function. The function receives the JSON payload via the `connectorTrigger` binding, logs key fields, and persists the raw payload to Azure Blob Storage using a blob output binding. The blob output provides a simple way to archive every incoming event for auditing, replay, or downstream processing.

## Prerequisites

- [Python 3.10+](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-python#python-version)
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
@app.generic_trigger(arg_name="payload", type="connectorTrigger")
@app.blob_output(
    arg_name="output",
    path="connector-messages/{rand-guid}.json",
    connection="BlobStoreConnection")
def on_new_email(payload: str, output: func.Out[str]) -> None:
    data = json.loads(payload)
    emails = data.get("body", {}).get("value", [])
    # ... process emails
    output.set(payload)
```
