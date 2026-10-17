"""Tests for the Python Poll sample's SDK payload conversion."""

import json
import unittest
from types import SimpleNamespace
from typing import get_type_hints
from unittest.mock import patch

from azure.functions import connectors, meta
from azure.connectors import TriggerCallbackPayload
from azure.connectors.office365 import (
    GraphClientReceiveFileAttachment,
    GraphClientReceiveMessage,
    SensitivityLabelMetadata,
)

import function_app as sample


class PollPayloadTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.function = sample.app.get_functions()[0]
        cls.handler = staticmethod(cls.function.get_user_function())

    def test_parses_single_and_collection_bodies(self) -> None:
        email = {
            "id": "synthetic-id",
            "from": "sender@example.invalid",
            "hasAttachments": True,
            "receivedDateTime": "2026-10-07T00:00:00Z",
            "attachments": [
                {"id": "attachment-id", "contentType": "text/plain", "isInline": False}
            ],
            "sensitivityLabelInfo": [
                {"sensitivityLabelId": "label-id", "displayName": "Test label"}
            ],
        }
        for body, count in [(email, 1), ({"value": [email, email]}, 2)]:
            with self.subTest(count=count):
                event = TriggerCallbackPayload.from_dict(
                    {"body": body}, item_parser=sample._parse_email
                )
                self.assertEqual(len(event.body.value), count)
                for parsed in event.body.value:
                    self.assertIsInstance(parsed, GraphClientReceiveMessage)
                    self.assertEqual(parsed.from_, email["from"])
                    self.assertTrue(parsed.has_attachments)
                    self.assertEqual(parsed.received_date_time, email["receivedDateTime"])
                    self.assertIsInstance(
                        parsed.attachments[0], GraphClientReceiveFileAttachment
                    )
                    self.assertEqual(parsed.attachments[0].content_type, "text/plain")
                    self.assertFalse(parsed.attachments[0].is_inline)
                    self.assertIsInstance(
                        parsed.sensitivity_label_info[0], SensitivityLabelMetadata
                    )
                    self.assertEqual(
                        parsed.sensitivity_label_info[0].display_name, "Test label"
                    )

    def test_optional_collections_remain_none(self) -> None:
        for data in [
            {"id": "minimal"},
            {"id": "minimal", "attachments": None, "sensitivityLabelInfo": None},
        ]:
            with self.subTest(data=data):
                parsed = sample._parse_email(data)
                self.assertEqual(parsed.id, "minimal")
                self.assertIsNone(parsed.attachments)
                self.assertIsNone(parsed.sensitivity_label_info)

    def test_handler_counts_emails_across_batched_events(self) -> None:
        for bodies, count in [
            ([{"id": "synthetic-id"}], 1),
            ([{"value": [{"id": "one"}, {"id": "two"}]}], 2),
            ([{"id": "one"}, {"value": [{"id": "two"}, {"id": "three"}]}], 3),
            ([{"value": []}], 0),
            ([{"value": None}], 0),
        ]:
            with self.subTest(event_count=len(bodies), email_count=count):
                with patch.object(sample.logging, "info") as log:
                    self.handler([json.dumps({"body": body}) for body in bodies])
                    log.assert_called_once()
                    self.assertEqual(log.call_args.args[1:], (len(bodies), count))

    def test_connector_decorator_emits_poll_batch_contract(self) -> None:
        metadata = json.loads(self.function.get_function_json())
        binding = next(
            value for value in metadata["bindings"] if value["type"] == "connectorTrigger"
        )
        self.assertEqual(binding["name"], "payloads")
        self.assertEqual(binding["deliveryMode"], "Poll")
        self.assertEqual(binding["connection"], "ConnectorNamespace")
        self.assertEqual(
            binding["triggerConfigName"], "%OnNewEmailTriggerConfigName%"
        )
        self.assertEqual(binding["cardinality"], "MANY")
        self.assertEqual(binding["maxBatchSize"], 4)
        self.assertNotIn("maxConcurrentCalls", binding)
        self.assertEqual(binding["targetPendingEventThreshold"], 16)

    def test_sdk_decodes_collection_string_before_handler(self) -> None:
        payloads = [
            json.dumps({"body": {"id": "one"}}),
            json.dumps({"body": {"value": [{"id": "two"}, {"id": "three"}]}}),
        ]
        decoded = connectors.ConnectorTriggerConverter.decode(
            meta.Datum(type="collection_string", value=SimpleNamespace(string=payloads)),
            trigger_metadata={},
        )
        self.assertEqual(decoded, payloads)
        with patch.object(sample.logging, "info") as log:
            self.handler(decoded)
            self.assertEqual(log.call_args.args[1:], (2, 3))

    def test_sdk_accepts_batch_parameter_annotation(self) -> None:
        self.assertTrue(
            connectors.ConnectorTriggerConverter.check_input_type_annotation(
                get_type_hints(self.handler)["payloads"]
            )
        )

    def test_handler_rejects_invalid_event_without_logging_success(self) -> None:
        for invalid in ["{", "{}", "null", '{"body":null}']:
            with self.subTest(payload=invalid):
                with patch.object(sample.logging, "info") as log:
                    with self.assertRaises(ValueError):
                        self.handler([json.dumps({"body": {"id": "valid"}}), invalid])
                    log.assert_not_called()


if __name__ == "__main__":
    unittest.main()
