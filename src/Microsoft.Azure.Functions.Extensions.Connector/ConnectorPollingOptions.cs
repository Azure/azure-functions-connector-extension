// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Immutable configuration for a Connector Namespace Poll trigger.
/// </summary>
internal sealed record ConnectorPollingOptions(
    string Connection,
    string TriggerConfigName,
    int MaxBatchSize,
    int Concurrency)
{
    internal const int MaximumBatchSize = 32;

    internal static ConnectorPollingOptions Create(
        ConnectorTriggerAttribute attribute,
        ConnectorOptions defaults)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(defaults);

        if (string.IsNullOrWhiteSpace(attribute.Connection))
        {
            throw new InvalidOperationException(
                "Connector trigger Connection is required for Poll delivery.");
        }

        if (string.IsNullOrWhiteSpace(attribute.TriggerConfigName))
        {
            throw new InvalidOperationException(
                "Connector trigger TriggerConfigName is required for Poll delivery.");
        }

        int maxBatchSize = ResolveMaxBatchSize(attribute.MaxBatchSize, defaults.DefaultMaxBatchSize);
        int concurrency = ResolveConcurrency(attribute.Concurrency, defaults.DefaultConcurrency);

        return new ConnectorPollingOptions(
            attribute.Connection,
            attribute.TriggerConfigName,
            maxBatchSize,
            concurrency);
    }

    private static int ResolveMaxBatchSize(int configuredValue, int defaultValue)
    {
        if (configuredValue < 0 || configuredValue > MaximumBatchSize)
        {
            throw new InvalidOperationException(
                $"Connector trigger MaxBatchSize must be between 0 and {MaximumBatchSize}.");
        }

        int value = configuredValue == 0 ? defaultValue : configuredValue;
        if (value < 1 || value > MaximumBatchSize)
        {
            throw new InvalidOperationException(
                $"Connector DefaultMaxBatchSize must be between 1 and {MaximumBatchSize}.");
        }

        return value;
    }

    private static int ResolveConcurrency(int configuredValue, int defaultValue)
    {
        if (configuredValue < 0)
        {
            throw new InvalidOperationException(
                "Connector trigger Concurrency must be zero or greater.");
        }

        int value = configuredValue == 0 ? defaultValue : configuredValue;
        if (value < 1)
        {
            throw new InvalidOperationException(
                "Connector DefaultConcurrency must be greater than zero.");
        }

        return value;
    }
}
