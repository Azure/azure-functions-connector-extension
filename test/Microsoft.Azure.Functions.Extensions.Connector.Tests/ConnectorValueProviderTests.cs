// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorValueProviderTests
{
    [Fact]
    public async Task GetValueAsync_ReturnsSerialized_ConnectorContext()
    {
        // Arrange
        var context = new ConnectorContext
        {
            Body = "{\"message\": \"test\"}",
            Method = "POST"
        };
        
        var jsonBody = JToken.Parse("{\"message\": \"test\"}");
        var tuple = (context, jsonBody);
        var valueProvider = new ConnectorValueProvider(tuple, typeof((ConnectorContext, JToken)));

        // Act
        var result = await valueProvider.GetValueAsync();

        // Assert
        Assert.IsType<string>(result);
        var resultString = (string)result;
        Assert.Contains("POST", resultString);
    }

    [Fact]
    public void ToInvokeString_ReturnsSerializedValue()
    {
        // Arrange
        var context = new ConnectorContext
        {
            Body = "body",
            Method = "POST"
        };
        
        var jsonBody = JToken.Parse("{}");
        var tuple = (context, jsonBody);
        var valueProvider = new ConnectorValueProvider(tuple, typeof((ConnectorContext, JToken)));

        // Act
        var result = valueProvider.ToInvokeString();

        // Assert
        Assert.Contains("id-123", result);
    }

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        // Arrange
        var context = new ConnectorContext();
        var jsonBody = JToken.Parse("{}");
        var tuple = (context, jsonBody);
        var expectedType = typeof((ConnectorContext, JToken));
        var valueProvider = new ConnectorValueProvider(tuple, expectedType);

        // Act & Assert
        Assert.Equal(expectedType, valueProvider.Type);
    }
}
