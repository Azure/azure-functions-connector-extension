// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorFunctionRegistration
{
    public ConnectorFunctionRegistration(string functionName, ITriggeredFunctionExecutor executor, string connectorNamespace, string triggerName)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggerName);
        FunctionName = functionName;
        Executor = executor;
        ConnectorNamespace = connectorNamespace;
        TriggerName = triggerName;
    }

    public string FunctionName { get; }
    public ITriggeredFunctionExecutor Executor { get; }
    public string ConnectorNamespace { get; }
    public string TriggerName { get; }
}
