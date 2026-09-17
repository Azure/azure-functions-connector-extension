// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorScalerProviderTests
{
    private const string ResourceId = "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg/providers/Microsoft.Web/connectorGateways/gateway";

    [Fact]
    public async Task Providers_AreIndependentPerFunctionMetadata()
    {
        var credential = new TestTokenCredential();
        var connectionFactory = new TestConnectionFactory((name, _) =>
            new ConnectorPollingConnection(name, new ResourceIdentifier(ResourceId), credential));
        var resolvers = new List<IConnectorPollingEndpointResolver>();
        var resolverFactory = new TestResolverFactory((_, _) =>
        {
            var resolver = new StubEndpointResolver(Endpoints());
            resolvers.Add(resolver);
            return resolver;
        });
        var depthFactory = new TestDepthClientFactory((_, _, _, trigger) =>
            new SequenceDepthClient(trigger == "trigger-a" ? 9 : 21));
        IServiceProvider services = BuildServices(connectionFactory, resolverFactory, depthFactory);

        var first = new ConnectorScalerProvider(services, Metadata("FunctionA", "trigger-a", 4, batchSize: 1));
        var second = new ConnectorScalerProvider(services, Metadata("FunctionB", "trigger-b", 10, batchSize: 32));
        var firstScaler = first.GetTargetScaler();
        var secondScaler = second.GetTargetScaler();

        Assert.Equal("FunctionA", firstScaler.TargetScalerDescriptor.FunctionId);
        Assert.Equal("FunctionB", secondScaler.TargetScalerDescriptor.FunctionId);
        Assert.Equal(3, (await firstScaler.GetScaleResultAsync(new())).TargetWorkerCount);
        Assert.Equal(3, (await secondScaler.GetScaleResultAsync(new())).TargetWorkerCount);
        Assert.Equal(2, resolvers.Count);
        Assert.NotSame(resolvers[0], resolvers[1]);
    }

    [Fact]
    public void Provider_UsesAzureComponentFactoryInjectedByScaleController()
    {
        AzureComponentFactory? selectedFactory = null;
        var injectedFactory = new TestAzureComponentFactory(new TestTokenCredential("site-identity"));
        var connectionFactory = new TestConnectionFactory((name, factory) =>
        {
            selectedFactory = factory;
            return new ConnectorPollingConnection(name, new ResourceIdentifier(ResourceId), new TestTokenCredential());
        });
        var resolverFactory = new TestResolverFactory((_, _) => new StubEndpointResolver(Endpoints()));
        var depthFactory = new TestDepthClientFactory((_, _, _, _) => new SequenceDepthClient(0));
        TriggerMetadata metadata = Metadata("Function", "trigger", 1);
        metadata.Properties[nameof(AzureComponentFactory)] = injectedFactory;

        _ = new ConnectorScalerProvider(BuildServices(connectionFactory, resolverFactory, depthFactory), metadata);

        Assert.Same(injectedFactory, selectedFactory);
    }

    [Fact]
    public void ReflectiveRegistrationSignature_IsInternalStaticAndExact()
    {
        MethodInfo method = typeof(ConnectorWebJobsBuilderExtensions).GetMethod(
            "AddConnectorScaleForTrigger",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        Assert.Equal(typeof(IWebJobsBuilder), method.ReturnType);
        Assert.Equal(new[] { typeof(IWebJobsBuilder), typeof(TriggerMetadata) }, method.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void ReflectiveRegistration_AddsExactlyOneProviderPerPollFunctionAndSkipsWebhook()
    {
        var builder = new TestWebJobsBuilder();

        builder.AddConnectorScaleForTrigger(Metadata("FunctionA", "trigger-a", 4, batchSize: 1));
        builder.AddConnectorScaleForTrigger(Metadata("FunctionB", "trigger-b", 4));
        TriggerMetadata webhook = Metadata("Webhook", "trigger-webhook", 4);
        webhook.Metadata["deliveryMode"] = "Webhook";
        builder.AddConnectorScaleForTrigger(webhook);

        Assert.Equal(2, builder.Services.Count(descriptor => descriptor.ServiceType == typeof(Microsoft.Azure.WebJobs.Host.Scale.ITargetScalerProvider)));
    }
    private static IServiceProvider BuildServices(
        IConnectorPollingConnectionFactory connectionFactory,
        IConnectorPollingEndpointResolverFactory resolverFactory,
        IConnectorQueueDepthClientFactory depthFactory)
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(connectionFactory);
        services.AddSingleton(resolverFactory);
        services.AddSingleton(depthFactory);
        services.AddSingleton(Options.Create(new ConnectorOptions { DefaultConcurrency = 16 }));
        return services.BuildServiceProvider();
    }

    private static TriggerMetadata Metadata(string functionName, string triggerConfigName, int concurrency, int batchSize = 1)
    {
        var metadata = new JObject
        {
            ["functionName"] = functionName,
            ["deliveryMode"] = "Poll",
            ["connection"] = "ConnectorNamespace",
            ["triggerConfigName"] = triggerConfigName,
            ["batchSize"] = batchSize,
            ["concurrency"] = concurrency,
        };
        return new TriggerMetadata(metadata);
    }

    private static ConnectorPollingEndpoints Endpoints() => new(
        new Uri("https://runtime.test/receive"),
        new Uri("https://runtime.test/ack"),
        new Uri("https://runtime.test/has"),
        new Uri("https://runtime.test/depth"));
    private sealed class TestWebJobsBuilder : IWebJobsBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
    }
}
