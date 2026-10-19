"""Azure Functions Connector Poll sample."""

import json
import logging
from dataclasses import fields
from typing import Any, List

import azure.functions as func
from azure.connectors import TriggerCallbackPayload
from azure.connectors.office365 import (
    GraphClientReceiveFileAttachment,
    GraphClientReceiveMessage,
    SensitivityLabelMetadata,
)

app = func.FunctionApp()


@app.function_name(name="OnNewEmailPoll")
@app.connector_trigger(
    arg_name="payloads",
    delivery_mode=func.DeliveryMode.POLL,
    connection="ConnectorNamespace",
    trigger_config_name="%OnNewEmailTriggerConfigName%",
    cardinality=func.Cardinality.MANY,
    max_batch_size=4,
    target_pending_event_threshold=16,
)
def on_new_email_poll(payloads: List[str]) -> None:
    """Parse each Poll event in the invocation into typed Office 365 messages."""
    email_count = 0
    for payload in payloads:
        event = TriggerCallbackPayload.from_dict(
            json.loads(payload), item_parser=_parse_email
        )
        if event is None or event.body is None:
            raise ValueError("Office 365 Poll payload must contain a body.")

        emails: list[GraphClientReceiveMessage] = event.body.value or []
        email_count += len(emails)
    logging.info(
        "Received a Poll invocation with %s event(s) containing %s email(s).",
        len(payloads),
        email_count,
    )


def _model_values(model_type: type, data: dict[str, Any]) -> dict[str, Any]:
    """Map SDK dataclass fields using their JSON wire names."""
    return {
        model_field.name: data[wire_name]
        for model_field in fields(model_type)
        if (wire_name := model_field.metadata.get("wire_name", model_field.name)) in data
    }


def _parse_labels(data: dict[str, Any]) -> list[SensitivityLabelMetadata] | None:
    labels = data.get("sensitivityLabelInfo")
    return (
        [
            SensitivityLabelMetadata(**_model_values(SensitivityLabelMetadata, label))
            for label in labels
        ]
        if labels is not None
        else None
    )


def _parse_email(data: dict[str, Any]) -> GraphClientReceiveMessage:
    values = _model_values(GraphClientReceiveMessage, data)
    attachments = data.get("attachments")
    values["attachments"] = (
        [
            GraphClientReceiveFileAttachment(
                **_model_values(GraphClientReceiveFileAttachment, attachment)
            )
            for attachment in attachments
        ]
        if attachments is not None
        else None
    )
    values["sensitivity_label_info"] = _parse_labels(data)
    return GraphClientReceiveMessage(**values)
