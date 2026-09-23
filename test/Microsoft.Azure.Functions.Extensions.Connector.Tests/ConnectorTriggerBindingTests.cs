// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Azure.Core;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorTriggerBindingTests
{
    private readonly ConnectorExtensionConfigProvider _configProvider;
    private readonly ConnectorOptions _options = new();
    private readonly IConnectorConnectionOptionsProvider
        _connectionOptionsProvider;
    private readonly IConnectorPollingListenerFactory
        _pollingListenerFactory = new StubConnectorPollingListenerFactory();

    public ConnectorTriggerBindingTests()
    {
        _configProvider = new ConnectorExtensionConfigProvider(
            new ConnectorHttpRequestProcessor(
                NullLogger<ConnectorHttpRequestProcessor>.Instance),
            NullLoggerFactory.Instance,
            Options.Create(_options),
            new StubConnectorConnectionOptionsProvider(),
            _pollingListenerFactory);
        _connectionOptionsProvider =
            new StubConnectorConnectionOptionsProvider(
                new ConnectorConnectionOptions(
                    new ResourceIdentifier(
                        "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/ns"),
                    Mock.Of<TokenCredential>()));
    }

    [Fact]
    public void Constructor_ValidatesDependencies()
    {
        ParameterInfo parameter = GetParameter();
        var attribute = new ConnectorTriggerAttribute();

        Assert.Throws<ArgumentNullException>(() => CreateBinding(null!, attribute));
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                null!,
                attribute,
                _options,
                _connectionOptionsProvider,
                _pollingListenerFactory));
        Assert.Throws<ArgumentNullException>(() => CreateBinding(parameter, null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                attribute,
                null!,
                _connectionOptionsProvider,
                _pollingListenerFactory));
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                attribute,
                _options,
                null!,
                _pollingListenerFactory));
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorTriggerBinding(
                parameter,
                _configProvider,
                attribute,
                _options,
                _connectionOptionsProvider,
                null!));
    }

    [Fact]
    public async Task BindAsync_RejectsUnsupportedTriggerValue()
    {
        ConnectorTriggerBinding binding = CreateBinding(
            GetParameter(),
            new ConnectorTriggerAttribute());
        var context = new Mock<ValueBindingContext>(
            null!,
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            binding.BindAsync(new object(), context.Object));
    }

    [Fact]
    public void BindingContractAndDescriptor_AreStable()
    {
        ConnectorTriggerBinding binding = CreateBinding(
            GetParameter(),
            new ConnectorTriggerAttribute());

        Assert.Equal(typeof(ConnectorTriggerInput), binding.TriggerValueType);
        Assert.Empty(binding.BindingDataContract);
        Assert.Equal("body", binding.ToParameterDescriptor().Name);
    }

    [Fact]
    public async Task CreateListenerAsync_RejectsUnsupportedDeliveryMode()
    {
        ConnectorTriggerBinding binding = CreateBinding(
            GetParameter(),
            new ConnectorTriggerAttribute
            {
                DeliveryMode = (ConnectorTriggerDeliveryMode)42,
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            binding.CreateListenerAsync(CreateListenerContext()));
    }

    [Fact]
    public async Task CreateListenerAsync_RejectsNullContext()
    {
        ConnectorTriggerBinding binding = CreateBinding(
            GetParameter(),
            new ConnectorTriggerAttribute());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            binding.CreateListenerAsync(null!));
    }

    private ConnectorTriggerBinding CreateBinding(
        ParameterInfo parameter,
        ConnectorTriggerAttribute attribute) =>
        new(
            parameter,
            _configProvider,
            attribute,
            _options,
            _connectionOptionsProvider,
            _pollingListenerFactory);

    private static ParameterInfo GetParameter() =>
        typeof(TestFunctions)
            .GetMethod(nameof(TestFunctions.Function))!
            .GetParameters()[0];

    private static ListenerFactoryContext CreateListenerContext() =>
        new(
            new TestFunctionDescriptor { ShortName = "TestFunction" },
            Mock.Of<ITriggeredFunctionExecutor>(),
            CancellationToken.None);

    private static class TestFunctions
    {
        public static void Function([ConnectorTrigger] string body)
        {
        }
    }

    private sealed class TestFunctionDescriptor : FunctionDescriptor
    {
    }
}
