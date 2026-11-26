package main

import (
	"context"
	"testing"

	"github.com/azure/azure-functions-golang-worker/sdk"
)

func TestHandlePayload(t *testing.T) {
	for _, payload := range []string{
		`{"body":{"arbitrary":true}}`,
		`[{"arbitrary":true}]`,
		`"{\"body\":{\"arbitrary\":true}}"`,
		`"[{\"arbitrary\":true}]"`,
	} {
		if err := handlePayload(context.Background(), payload); err != nil {
			t.Errorf("handlePayload(%q) returned an error: %v", payload, err)
		}
	}
	for _, payload := range []string{"", "not JSON", "null", "42", `""`, `"""x"""`} {
		if err := handlePayload(context.Background(), payload); err == nil {
			t.Errorf("handlePayload(%q) succeeded; want an error", payload)
		}
	}
}

func TestGenericWebhookRegistration(t *testing.T) {
	app := newApp()
	var registered bool
	app.GetRegisteredFunctions().Range(func(_, value any) bool {
		function := value.(*sdk.RegisteredFunction)
		registered = true
		if function.FuncName != "OnGenericWebhook" || len(function.RawBindings) != 1 {
			t.Errorf("unexpected function registration: %#v", function)
			return true
		}
		binding := function.RawBindings[0]
		if binding.Type != "connectorTrigger" || binding.Name != "payload" || binding.Direction != "in" {
			t.Errorf("unexpected trigger binding: %#v", binding)
		}
		generic := binding.GenericBinding
		if generic == nil || generic.DataType != "string" ||
			generic.Properties["deliveryMode"] != nil ||
			generic.Properties["connection"] != nil ||
			generic.Properties["triggerConfigName"] != nil {
			t.Errorf("unexpected generic binding settings: %#v", generic)
		}
		return true
	})
	if !registered {
		t.Fatal("function was not registered")
	}
}
