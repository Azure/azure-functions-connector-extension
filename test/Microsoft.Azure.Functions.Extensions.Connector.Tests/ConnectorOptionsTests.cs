// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var options = new ConnectorOptions();

        Assert.Equal(1, options.DefaultMaxBatchSize);
        Assert.Equal(16, options.DefaultConcurrency);
    }
}
