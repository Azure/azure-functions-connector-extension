"""
Azure Functions Connector Extension - Python Sample

Receives trigger callbacks from AI Gateway managed connectors
and saves the payload to blob storage.
"""

import azure.functions as func
import json
import logging

app = func.FunctionApp()


@app.function_name(name="OnNewEmail")
@app.generic_trigger(
    arg_name="payload",
    type="connectorTrigger",
    connectorType="office365",
    operation="OnNewEmailV3",
    connection="Office365Connection")
@app.blob_output(
    arg_name="output",
    path="connector-messages/{rand-guid}.json",
    connection="BlobStoreConnection")
def on_new_email(payload: str, output: func.Out[str]) -> None:
    """
    Receives Office 365 email trigger callbacks and saves to blob storage.
    """
    logging.info("OnNewEmail trigger received")

    data = json.loads(payload)
    emails = data.get("body", {}).get("value", [])

    for email in emails:
        logging.info(f"Subject: {email.get('subject')}")
        logging.info(f"From: {email.get('from')}")

    output.set(payload)
