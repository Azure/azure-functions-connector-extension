// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Connector;

/// <summary>
/// Trigger attribute for AI Gateway connector webhooks.
/// </summary>
[InputConverter(typeof(ConnectorTriggerConverter))]
public sealed class ConnectorTriggerAttribute : TriggerBindingAttribute
{

}
