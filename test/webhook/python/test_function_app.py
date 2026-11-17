"""Offline validation of the Webhook sample's released connector bindings."""

import json
import unittest
from typing import List
from unittest.mock import Mock, patch

from azure.connectors.office365 import GraphClientReceiveMessage
from azurefunctions.extensions.base import Datum
from azurefunctions.extensions.connectors.connectorConverter import ConnectorConverter

import function_app as sample


class WebhookPayloadTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.function = sample.app.get_functions()[0]
        cls.handler = staticmethod(cls.function.get_user_function())

    def test_decodes_records_and_callback_envelopes_with_http_metadata(self) -> None:
        email = {
            "subject": "Synthetic email",
            "from": "sender@example.invalid",
            "importance": "high",
            "hasAttachments": False,
        }
        for payload in [
            email,
            {"body": email},
            {"body": {"value": [email]}, "headers": {"x-test": "value"}, "statusCode": 200},
        ]:
            with self.subTest(payload=payload):
                decoded = ConnectorConverter.decode(
                    Datum(value=json.dumps(payload), type="json"),
                    trigger_metadata={},
                    pytype=List[sample.office365.GraphClientReceiveMessage],
                )
                self.assertEqual(len(decoded), 1)
                self.assertIsInstance(decoded[0], GraphClientReceiveMessage)
                self.assertEqual(decoded[0].subject, email["subject"])
                self.assertEqual(decoded[0].from_, email["from"])
                self.assertEqual(decoded[0].importance, "high")
                self.assertFalse(decoded[0].has_attachments)

                output = Mock()
                with patch.object(sample.logging, "info"):
                    self.handler(decoded, output)
                saved = json.loads(output.set.call_args.args[0])
                self.assertEqual(saved[0]["subject"], email["subject"])

    def test_rejects_malformed_input(self) -> None:
        for payload in ["{", "null", "[]"]:
            with self.subTest(payload=payload):
                with self.assertRaises(ValueError):
                    ConnectorConverter.decode(
                        Datum(value=payload, type="json"),
                        trigger_metadata={},
                        pytype=List[sample.office365.GraphClientReceiveMessage],
                    )


if __name__ == "__main__":
    unittest.main()
