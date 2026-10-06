"""Azure Functions Connector Poll sample."""

import logging

import azure.functions as func

app = func.FunctionApp()


@app.function_name(name="OnNewEmailPoll")
@app.generic_trigger(
    arg_name="payload",
    type="connectorTrigger",
    deliveryMode="Poll",
    connection="ConnectorNamespace",
    triggerConfigName="%OnNewEmailTriggerConfigName%",
    cardinality=func.Cardinality.ONE,
    maxBatchSize=1,
    maxConcurrentCalls=1,
    targetPendingEventThreshold=16,
)
def on_new_email_poll(payload) -> None:
    """Log a Connector Namespace Poll event."""
    logging.info("Received a Poll connector payload.")
