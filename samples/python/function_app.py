"""
Azure Functions Connector Extension - Python Sample

Receives trigger callbacks from Connector Namespace managed connectors
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
    connectorNamespace="my-connector-namespace",
    triggerName="email-newemail-trigger")
@app.blob_output(
    arg_name="output",
    path="connector-messages/{rand-guid}.json",
    connection="AzureWebJobsStorage")
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
