// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal readonly struct ConnectorAcknowledgeStatus :
    IEquatable<ConnectorAcknowledgeStatus>
{
    private readonly string? _value;

    private ConnectorAcknowledgeStatus(string value)
    {
        _value = value;
    }

    internal static ConnectorAcknowledgeStatus Acknowledged { get; } =
        new("Acknowledged");

    internal static ConnectorAcknowledgeStatus NotFound { get; } =
        new("NotFound");

    internal static ConnectorAcknowledgeStatus Failed { get; } =
        new("Failed");

    internal bool IsAcknowledged => Equals(Acknowledged);

    internal bool IsKnown =>
        Equals(Acknowledged) ||
        Equals(NotFound) ||
        Equals(Failed);

    internal static ConnectorAcknowledgeStatus FromWireValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Connector acknowledgement status must not be empty.",
                nameof(value));
        }

        return new ConnectorAcknowledgeStatus(value);
    }

    public bool Equals(ConnectorAcknowledgeStatus other) =>
        string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) =>
        obj is ConnectorAcknowledgeStatus other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(_value ?? string.Empty);

    public override string ToString() => _value ?? string.Empty;

    public static bool operator ==(
        ConnectorAcknowledgeStatus left,
        ConnectorAcknowledgeStatus right) =>
        left.Equals(right);

    public static bool operator !=(
        ConnectorAcknowledgeStatus left,
        ConnectorAcknowledgeStatus right) =>
        !left.Equals(right);
}
