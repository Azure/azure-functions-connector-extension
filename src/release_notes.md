## What's Changed

### Microsoft.Azure.Functions.Extensions.Connector

- First stable release of the Azure Functions Connector extension, supporting Webhook and Poll trigger delivery.
- Reduce avoidable UTF-16 allocations when serializing the worker-binding envelope.
- Configure Poll linked-output downloads with `MaxLinkedOutputSizeInBytes`, defaulting to 100 MiB. The download limit is not a supported payload-size guarantee; see [tested coverage and unqualified workloads](../docs/connector-trigger-poll-delivery-design.md#deferred-linked-output-release-work).
- See open issues [#39](https://github.com/Azure/azure-functions-connector-extension/issues/39) (linked-output qualification) and [#44](https://github.com/Azure/azure-functions-connector-extension/issues/44) (queue-depth request retries and failure handling) for known issues and limitations.

### Microsoft.Azure.Functions.Worker.Extensions.Connector

- First stable release of the Connector extension for .NET isolated worker apps, supporting Webhook and Poll trigger delivery.
