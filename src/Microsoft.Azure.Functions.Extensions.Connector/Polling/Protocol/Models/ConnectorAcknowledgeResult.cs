// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorAcknowledgeResult
{
    internal ConnectorAcknowledgeResult(
        IReadOnlyList<ConnectorAcknowledgeItemResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Any(static result => result is null))
        {
            throw new ArgumentException(
                "Connector acknowledgement results must not contain null items.",
                nameof(results));
        }

        Results = Array.AsReadOnly(results.ToArray());
    }

    internal IReadOnlyList<ConnectorAcknowledgeItemResult> Results { get; }

    public override string ToString() =>
        $"{nameof(ConnectorAcknowledgeResult)} {{ " +
        $"ResultCount = {Results.Count} }}";
}
