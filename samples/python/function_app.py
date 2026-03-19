"""
Azure Functions Connector Extension - Python v4 Sample

This sample demonstrates how to use the Connector trigger with Python
using the v4 programming model (decorator-based).

The Connector extension exposes an HTTP webhook endpoint that can receive
JSON or string payloads from external services like Microsoft Graph,
SharePoint, Teams, and other webhook providers.

Endpoint: POST http://localhost:7071/runtime/webhooks/connector?functionName={FunctionName}
"""

import azure.functions as func
import logging
import json

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)


# =============================================================================
# Example 1: O365 Email - OnEmail trigger (Microsoft Graph webhook)
# =============================================================================
@app.function_name(name="OnEmail")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_email(context: str) -> None:
    """
    Handles Microsoft Graph email notifications.
    Subscribe to mailbox changes via Graph API subscriptions.
    """
    logging.info("=== O365 OnEmail Trigger ===")
    
    ctx = json.loads(context)
    body = ctx.get('Body', '{}')
    
    # Handle Graph validation request
    query = ctx.get('Query', {})
    if 'validationToken' in query:
        logging.info(f"Graph subscription validation: {query['validationToken']}")
        return
    
    try:
        notification = json.loads(body)
        
        for change in notification.get('value', []):
            change_type = change.get('changeType', '')
            resource_data = change.get('resourceData', {})
            
            logging.info(f"Change Type: {change_type}")
            
            if resource_data.get('@odata.type') == '#Microsoft.Graph.Message':
                subject = resource_data.get('subject', 'No Subject')
                sender = resource_data.get('from', {}).get('emailAddress', {})
                sender_name = sender.get('name', 'Unknown')
                sender_email = sender.get('address', '')
                
                logging.info(f"New Email: {subject}")
                logging.info(f"From: {sender_name} <{sender_email}>")
                
                if 'urgent' in subject.lower():
                    logging.info("Flagged as URGENT")
                    
    except json.JSONDecodeError as e:
        logging.error(f"Invalid JSON payload: {e}")


# =============================================================================
# Example 2: Teams - OnMessage trigger
# =============================================================================
@app.function_name(name="OnTeamsMessage")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_teams_message(context: str) -> None:
    """
    Handles Microsoft Teams messages via Bot Framework or webhook.
    """
    logging.info("=== Teams OnMessage Trigger ===")
    
    ctx = json.loads(context)
    
    try:
        activity = json.loads(ctx.get('Body', '{}'))
        
        activity_type = activity.get('type', '')
        channel = activity.get('channelId', '')
        
        if activity_type == 'message' and channel == 'msteams':
            sender = activity.get('from', {})
            sender_name = sender.get('name', 'Unknown')
            conversation = activity.get('conversation', {})
            conv_name = conversation.get('name', 'Direct Message')
            text = activity.get('text', '')
            
            logging.info(f"Teams Message from {sender_name}")
            logging.info(f"Channel: {conv_name}")
            logging.info(f"Text: {text}")
            
            # Simple command handling
            text_lower = text.lower()
            if 'help' in text_lower:
                logging.info("Command: HELP requested")
            elif 'status' in text_lower:
                logging.info("Command: STATUS requested")
                
        elif activity_type == 'conversationUpdate':
            logging.info("Conversation update (member added/removed)")
            
    except json.JSONDecodeError as e:
        logging.error(f"Invalid Teams activity: {e}")


# =============================================================================
# Example 3: SharePoint - OnListItemCreated trigger
# =============================================================================
@app.function_name(name="OnSharePointItem")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_sharepoint_item(context: str) -> None:
    """
    Handles SharePoint list item events via Graph/webhook notifications.
    """
    logging.info("=== SharePoint OnListItemCreated Trigger ===")
    
    ctx = json.loads(context)
    
    # Handle SharePoint validation
    query = ctx.get('Query', {})
    if 'validationToken' in query:
        logging.info(f"SharePoint validation: {query['validationToken']}")
        return
    
    try:
        notification = json.loads(ctx.get('Body', '{}'))
        
        for change in notification.get('value', []):
            change_type = change.get('changeType', '')
            site_url = change.get('siteUrl', '')
            resource_data = change.get('resourceData', {})
            
            logging.info(f"SharePoint Change: {change_type}")
            logging.info(f"Site: {site_url}")
            
            fields = resource_data.get('fields', {})
            if fields:
                title = fields.get('Title', 'Untitled')
                status = fields.get('Status', 'Unknown')
                assigned = fields.get('AssignedTo', 'Unassigned')
                
                logging.info(f"Item: {title}")
                logging.info(f"Status: {status}")
                logging.info(f"Assigned To: {assigned}")
                
                if change_type == 'created':
                    logging.info("New item - triggering workflow")
                    
    except json.JSONDecodeError as e:
        logging.error(f"Invalid SharePoint notification: {e}")


# =============================================================================
# Example 4: GitHub - OnPush trigger
# =============================================================================
@app.function_name(name="OnGitHubPush")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def on_github_push(context: str) -> None:
    """
    Handles GitHub webhook events.
    """
    logging.info("=== GitHub OnPush Trigger ===")
    
    ctx = json.loads(context)
    headers = ctx.get('Headers', {})
    
    event_type = headers.get('X-GitHub-Event') or headers.get('x-github-event', '')
    
    logging.info(f"Event: {event_type}")
    
    try:
        payload = json.loads(ctx.get('Body', '{}'))
        
        if event_type == 'push':
            ref = payload.get('ref', '')
            repo = payload.get('repository', {}).get('full_name', '')
            pusher = payload.get('pusher', {}).get('name', '')
            commits = payload.get('commits', [])
            
            logging.info(f"Push to {repo}")
            logging.info(f"Branch: {ref}")
            logging.info(f"By: {pusher}")
            logging.info(f"Commits: {len(commits)}")
            
            for commit in commits[:3]:
                msg = commit.get('message', '').split('\n')[0]
                logging.info(f"  - {msg}")
                
    except json.JSONDecodeError:
        logging.error("Failed to parse webhook payload")


# =============================================================================
# Example 5: Echo - For testing
# =============================================================================
@app.function_name(name="Echo")
@app.generic_trigger(arg_name="context", type="connectorTrigger")
def echo(context: str) -> None:
    """
    Simple echo function for testing.
    """
    logging.info("=== Echo Function ===")
    
    ctx = json.loads(context)
    logging.info(f"Method: {ctx.get('Method')}")
    logging.info(f"Body: {ctx.get('Body')}")
    logging.info(f"Headers: {json.dumps(ctx.get('Headers', {}), indent=2)}")
    logging.info(f"Query: {json.dumps(ctx.get('Query', {}), indent=2)}")
