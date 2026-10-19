## What's Changed

### Microsoft.Azure.Functions.Extensions.Connector

- First stable release of the Azure Functions Connector extension, supporting Webhook and Poll trigger delivery.
- Reduce avoidable UTF-16 allocations when serializing the worker-binding envelope.
- Configure Poll linked-output downloads with `MaxLinkedOutputSizeInBytes`, defaulting to 100 MiB. The download limit is not a supported payload-size guarantee.
- Check the repository's open issues for known issues and limitations.

### Microsoft.Azure.Functions.Worker.Extensions.Connector

- First stable release of the Connector extension for .NET isolated worker apps, supporting Webhook and Poll trigger delivery.
