"""
Azure Functions Connector Extension - Python Sample

Receives trigger callbacks from Connector Namespace managed connectors
and saves the payload to blob storage.
"""

import azure.functions as func
import azurefunctions.extensions.connectors.office365 as office365
import logging
from typing import List

app = func.FunctionApp()


@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="emails")
def on_new_email(emails: List[office365.ClientReceiveMessage]) -> None:
    """
    Receives Office 365 email trigger callbacks from Connector Namespace managed connectors
    and saves to blob storage.
    """
    logging.info("OnNewEmail trigger received. Payload: %s", emails)

    for email in emails:
        logging.info(f"Subject: {email.subject}")
        logging.info(f"From: {email.from_}")
