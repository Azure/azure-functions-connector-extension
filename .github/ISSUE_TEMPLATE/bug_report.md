---
name: Bug Report
description: Report a bug with the Azure Functions Connector Extension
title: "[Bug]: "
labels: ["bug"]
---

## Describe the bug

A clear and concise description of what the bug is.

## To reproduce

Steps to reproduce the behavior:

1.
2.
3.

## Expected behavior

A clear and concise description of what you expected to happen.

## Actual behavior

What actually happened instead.

## Environment

- **Extension version**: [e.g., 0.4.0-alpha]
- **Extension Bundle Id and Version**: [e.g., Microsoft.Azure.Functions.ExtensionBundle.Preview 4.42.0]
- **Functions runtime version**: [e.g., 4.1049.100]
- **Language/SDK**: [e.g., .NET 10, Python 3.13, Node.js 22]
- **Connector SDK version** (if applicable): [e.g., Azure.Connectors.Sdk 0.14.0-preview.1]
- **Delivery mode**: [Webhook or Poll]
- **Hosting environment and plan**: [local Core Tools, Consumption, Flex Consumption, Premium, Dedicated]
- **OS**: [e.g., Windows 11, Ubuntu 22.04]

## Trigger configuration

- **Connector and operation**: [e.g., Office 365 / OnNewEmailV3]
- **Function binding**: [paste the redacted attribute, decorator, or generic binding metadata]
- **Trigger Config state**: [Enabled or Disabled]
- **Invocation cardinality**: [one or many]
- **MaxBatchSize / Concurrency** (Poll only): [effective values]
- **Credential mode** (Poll only): [developer credential, system-assigned managed identity, or user-assigned managed identity]
- **Is `<Connection>__pollingEndpoint` configured?** (Poll only): [yes or no; do not paste its value]
- **Is `TriggerConfigName` configured and resolving successfully?** (Poll only): [yes or no; do not paste sensitive identifiers]

## Regression

- **Did this work previously?** [yes or no]
- **Last known working extension version** (if applicable):

## Logs

```
Paste relevant logs from the Functions host output here.
```

Remove or redact access tokens, system keys, complete Poll endpoint URLs, signed `outputsLink` URLs, lock tokens, payload contents, email addresses, and other customer data.

## Connector Namespace Run Logs

- [ ] I have checked the Connector Namespace run logs for errors

Please share redacted logs relevant to your Function from the Connector Trigger Config Runs that could be relevant for extension diagnosis:

```
Paste redacted Connector Trigger Config run logs here (if applicable).
```

## Additional context

Add any other context about the problem here (e.g., connector type, trigger operation, payload size).
