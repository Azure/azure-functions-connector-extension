// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingProviderTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorTriggerBindingProvider _provider;

    public ConnectorTriggerBindingProviderTests()
    {
        // Create real instances since ConnectorExtensionConfigProvider is sealed
        var loggerFactory = NullLoggerFactory.Instance;
        var httpRequestProcessor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        _configProvider = new ConnectorExtensionConfigProvider(httpRequestProcessor, loggerFactory);
        _provider = new ConnectorTriggerBindingProvider(_configProvider);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenConfigProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBindingProvider(null!));
    }

    [Fact]
    public async Task TryCreateAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _provider.TryCreateAsync(null!));
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsNull_WhenParameterHasNoAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithoutAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsBinding_WhenParameterHasAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ConnectorTriggerBinding>(result);
    }

    [Fact]
    public async Task TryCreateAsync_ReturnsNull_ForDifferentTriggerAttribute()
    {
        // Arrange
        var parameter = typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.FunctionWithDifferentAttribute))!
            .GetParameters()[0];

        var context = new TriggerBindingProviderContext(parameter, CancellationToken.None);

        // Act
        var result = await _provider.TryCreateAsync(context);

        // Assert
        Assert.Null(result);
    }

    // Test class with different function signatures
    private static class TestFunctions
    {
        public static void FunctionWithAttribute([ConnectorTrigger] string body)
        {
        }

        public static void FunctionWithoutAttribute(string body)
        {
        }

        public static void FunctionWithDifferentAttribute([TestAttribute] string data)
        {
        }
    }

    // Dummy attribute for testing
    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class TestAttribute : Attribute
    {
    }
}
