// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorContextTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var context = new ConnectorContext();

        // Assert
        Assert.Equal(string.Empty, context.Body);
        Assert.NotNull(context.Headers);
        Assert.NotNull(context.Query);
        Assert.Equal("POST", context.Method);
        Assert.Equal(string.Empty, context.Url);
        Assert.Null(context.ContentType);
        Assert.NotEqual(default, context.Timestamp);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var context = new ConnectorContext
        {
            Body = "{\"test\": \"value\"}",
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Query = new Dictionary<string, string> { { "functionName", "Echo" } },
            Method = "PUT",
            Url = "http://localhost:7071/test",
            ContentType = "application/json",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal("{\"test\": \"value\"}", context.Body);
        Assert.Contains("Content-Type", context.Headers.Keys);
        Assert.Contains("functionName", context.Query.Keys);
        Assert.Equal("PUT", context.Method);
        Assert.Equal("http://localhost:7071/test", context.Url);
        Assert.Equal("application/json", context.ContentType);
    }

    [Fact]
    public void Headers_AreCaseInsensitive()
    {
        // Arrange
        var context = new ConnectorContext();
        context.Headers["Content-Type"] = "application/json";

        // Act & Assert
        Assert.True(context.Headers.ContainsKey("content-type"));
        Assert.True(context.Headers.ContainsKey("CONTENT-TYPE"));
    }
}
