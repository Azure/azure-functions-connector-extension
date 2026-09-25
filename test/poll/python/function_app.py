"""Azure Functions Connector Poll sample."""

import json
import logging

import azure.functions as func

app = func.FunctionApp()


@app.function_name(name="OnNewEmailPoll")
@app.generic_trigger(
    arg_name="payloads",
    type="connectorTrigger",
    deliveryMode="Poll",
    connection="ConnectorNamespace",
    pollingEndpoint="%OnNewEmailEndpoint%",
    cardinality=func.Cardinality.MANY,
    maxBatchSize=4,
    concurrency=4,
)
def on_new_email_poll(payloads: list[str]) -> None:
    """Log a batch of Connector Namespace Poll events."""
    for payload in payloads:
        message = json.loads(payload) if isinstance(payload, str) else payload
        logging.info("Poll connector payload: %s", message)
