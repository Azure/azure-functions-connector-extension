"""Azure Functions Connector Poll sample."""

import json
import logging

import azure.functions as func

app = func.FunctionApp()


@app.function_name(name="OnNewEmailPoll")
@app.generic_trigger(
    arg_name="payload",
    type="connectorTrigger",
    deliveryMode="Poll",
    connection="ConnectorNamespace",
    triggerConfigName="%ConnectorTriggerConfigName%",
    maxBatchSize=1,
    concurrency=4,
)
def on_new_email_poll(payload: str) -> None:
    """Log one Connector Namespace Poll event."""
    message = json.loads(payload) if isinstance(payload, str) else payload
    logging.info("Poll connector payload: %s", message)
