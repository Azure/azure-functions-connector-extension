"""
Azure Functions Connector Extension - Python Sample

Receives trigger callbacks from Connector Namespace managed connectors.
Persists the raw payload to blob storage for auditing/replay.
"""

import azure.functions as func
import azurefunctions.extensions.connectors.office365 as office365
import json
import logging
from typing import List

app = func.FunctionApp()


@app.function_name(name="OnNewEmail")
@app.connector_trigger(arg_name="emails")
@app.blob_output(
    arg_name="outputblob",
    path="connector-messages/{rand-guid}.json",
    connection="BlobStoreConnection",
)
def on_new_email(emails: List[office365.ClientReceiveMessage], outputblob: func.Out[str]) -> None:
    """Triggered when a new email arrives in Office 365."""
    logging.info("OnNewEmail trigger received.")

    for email in emails:
        logging.info(f"Subject: '{email.subject}'.")
        logging.info(f"From: '{email.from_}'.")
        logging.info(f"Importance: '{email.importance}'.")
        logging.info(f"Has attachments: '{email.has_attachments}'.")

    logging.info(f"Batch contains '{len(emails)}' email(s).")

    # Persist the raw payload to blob storage for auditing/replay
    outputblob.set(json.dumps([vars(e) for e in emails], default=str))
