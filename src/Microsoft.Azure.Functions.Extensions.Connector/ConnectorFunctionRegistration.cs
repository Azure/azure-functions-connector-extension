// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Executors;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorFunctionRegistration
{
    public ConnectorFunctionRegistration(string functionName, ITriggeredFunctionExecutor executor)
    {
        FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public string FunctionName { get; }
    public ITriggeredFunctionExecutor Executor { get; }
    public string? ConnectorType { get; init; }
    public string? Operation { get; init; }
    public string? Connection { get; init; }
}
