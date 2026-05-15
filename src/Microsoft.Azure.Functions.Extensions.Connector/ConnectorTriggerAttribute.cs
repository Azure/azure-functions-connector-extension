// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Trigger attribute for Connector Namespace webhooks.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public sealed class ConnectorTriggerAttribute : Attribute
{
}
