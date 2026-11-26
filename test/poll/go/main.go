// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"

	"github.com/azure/azure-functions-golang-worker/sdk"
	"github.com/azure/azure-functions-golang-worker/sdk/bindings"
	"github.com/azure/azure-functions-golang-worker/worker"
)

func main() {
	worker.Start(newApp())
}

func newApp() *sdk.App {
	app := sdk.FunctionApp()
	app.GenericTrigger("OnGenericPoll", handlePayload, pollTrigger())
	return app
}

func pollTrigger() *bindings.GenericTrigger {
	return &bindings.GenericTrigger{
		Type:     "connectorTrigger",
		Name:     "payload",
		DataType: "string",
		Properties: map[string]any{
			"deliveryMode":      "Poll",
			"connection":        "ConnectorNamespace",
			"triggerConfigName": "%GenericTriggerConfigName%",
		},
	}
}

func handlePayload(ctx context.Context, payload string) error {
	if err := validatePayload(payload); err != nil {
		return err
	}
	slog.InfoContext(ctx, "Received generic Poll connector payload.")
	return nil
}

func validatePayload(payload string) error {
	for range 3 {
		var decoded any
		if err := json.Unmarshal([]byte(payload), &decoded); err != nil {
			return fmt.Errorf("decode connector payload: %w", err)
		}
		switch value := decoded.(type) {
		case map[string]any, []any:
			return nil
		case string:
			payload = value
		default:
			return fmt.Errorf("expected a JSON connector payload object or array, got %T", decoded)
		}
	}
	return fmt.Errorf("connector payload has too many nested JSON string layers")
}
