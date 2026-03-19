// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector;

/// <summary>
/// Value provider that returns the trigger value to the function parameter.
/// For out-of-process workers (Python, Node.js, etc.), serializes to JSON string.
/// </summary>
internal sealed class ConnectorValueProvider : IValueProvider
{
    private readonly object _value;
    private readonly string _serializedValue;

    public ConnectorValueProvider(object value, Type type)
    {
        _value = value;
        Type = type;

        // For out-of-process workers, we need to serialize to JSON
        // The tuple contains (ConnectorContext, JToken) - serialize the context
        if (value is ValueTuple<ConnectorContext, JToken> tuple)
        {
            _serializedValue = JsonConvert.SerializeObject(tuple.Item1);
        }
        else
        {
            _serializedValue = value?.ToString() ?? string.Empty;
        }
    }

    public Type Type { get; }

    public Task<object> GetValueAsync()
    {
        // Return serialized JSON string for out-of-process workers
        return Task.FromResult<object>(_serializedValue);
    }

    public string ToInvokeString()
    {
        return _serializedValue;
    }
}
