## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions.Connector 0.2.0-alpha

- **Breaking:** `ConnectorTriggerAttribute` now requires `ConnectorNamespace` and `TriggerName` constructor parameters
- Added header validation for `x-ms-trigger-name` and `x-ms-gateway-resource-name` on each callback request
- Added `Content-Type` validation (must be `application/json`)
- Renamed "AI Gateway" references to "Connector Namespace" throughout
- Improved request logging with trigger name and gateway resource details

### Microsoft.Azure.Functions.Worker.Extensions.Connector 0.2.0-alpha

- **Breaking:** `ConnectorTriggerAttribute` now requires `ConnectorNamespace` and `TriggerName` constructor parameters
- Added `ConnectorNamespace` and `TriggerName` properties for header validation support
- Renamed "AI Gateway" references to "Connector Namespace" throughout
