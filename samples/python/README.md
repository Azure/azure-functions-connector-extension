# Python Sample App

This sample demonstrates how to use the Connector Extension with Python v4 programming model.

## Prerequisites

- Python 3.10+
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

| Function | Description | Example Use Case |
| -------- | ----------- | ---------------- |
| `OnEmail` | Microsoft Graph email notifications | O365 mailbox monitoring |
| `OnTeamsMessage` | Teams bot messages | Chat commands, notifications |
| `OnSharePointItem` | SharePoint list changes | Document workflows |
| `OnGitHubPush` | GitHub webhooks | CI/CD triggers |
| `Echo` | Simple test function | Debugging |

## Testing

### Echo (Simple Test)

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=Echo" \
     -H "Content-Type: application/json" \
     -d '{"test": "Hello!"}'
```

### O365 Email Notification

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnEmail" \
     -H "Content-Type: application/json" \
     -d '{
       "value": [{
         "changeType": "created",
         "resourceData": {
           "@odata.type": "#Microsoft.Graph.Message",
           "subject": "URGENT: Action Required",
           "from": {"emailAddress": {"name": "John", "address": "john@contoso.com"}}
         }
       }]
     }'
```

### Teams Message

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnTeamsMessage" \
     -H "Content-Type: application/json" \
     -d '{
       "type": "message",
       "channelId": "msteams",
       "from": {"name": "Jane Smith"},
       "conversation": {"name": "Project Channel"},
       "text": "@bot help"
     }'
```

### SharePoint List Item

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnSharePointItem" \
     -H "Content-Type: application/json" \
     -d '{
       "value": [{
         "changeType": "created",
         "siteUrl": "https://contoso.sharepoint.com",
         "resourceData": {
           "fields": {"Title": "New Task", "Status": "Active"}
         }
       }]
     }'
```

### GitHub Push

```bash
curl -X POST "http://localhost:7071/runtime/webhooks/connector?functionName=OnGitHubPush" \
     -H "Content-Type: application/json" \
     -H "X-GitHub-Event: push" \
     -d '{
       "ref": "refs/heads/main",
       "repository": {"full_name": "owner/repo"},
       "pusher": {"name": "developer"},
       "commits": [{"message": "feat: new feature"}]
     }'
```

## Code Structure

```python
@app.function_name(name="OnEmail")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_email(context: str) -> None:
    # context is JSON-serialized ConnectorContext
    ctx = json.loads(context)
    
    body = ctx.get('Body')        # Request body as string
    headers = ctx.get('Headers')  # Dict of headers
    query = ctx.get('Query')      # Dict of query params
    method = ctx.get('Method')    # HTTP method
```

## Integrating with Microsoft Graph

To receive real webhooks from Microsoft Graph:

1. Deploy your function to Azure
2. Create a Graph subscription pointing to your endpoint
3. Handle the validation handshake (return `validationToken`)

```python
@app.function_name(name="OnEmail")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_email(context: str) -> None:
    ctx = json.loads(context)
    
    # Graph sends validationToken during subscription creation
    validation_token = ctx.get('Query', {}).get('validationToken')
    if validation_token:
        # In production, return this token with 200 OK
        logging.info(f"Validation: {validation_token}")
        return
    
    # Process actual notifications
    notification = json.loads(ctx.get('Body', '{}'))
    # ... handle notification
```
