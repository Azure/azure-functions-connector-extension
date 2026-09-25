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
    [Fact]
    public async Task Providers_AreIndependentPerFunctionMetadata()
    {
        var credential = new TestTokenCredential();
        var connectionProvider = new TestConnectionOptionsProvider((_, _) =>
            new ConnectorConnectionOptions(credential));
        var endpoints = new List<ConnectorPollingEndpoints>();
        var depthFactory = new TestDepthClientFactory((value, _, _) =>
        {
            endpoints.Add(value);
            return new SequenceDepthClient(
                value.ReceiveUri.AbsolutePath.Contains(
                    "trigger-a",
                    StringComparison.Ordinal)
                    ? 9
                    : 21);
        });
        IServiceProvider services =
            BuildServices(connectionProvider, depthFactory);

        var first = new ConnectorScalerProvider(
            services,
            Metadata(
                "FunctionA",
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger-a",
                4,
                maxBatchSize: 1));
        var second = new ConnectorScalerProvider(
            services,
            Metadata(
                "FunctionB",
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger-b",
                10,
                maxBatchSize: 32));
        var firstScaler = first.GetTargetScaler();
        var secondScaler = second.GetTargetScaler();

        Assert.Equal("FunctionA", firstScaler.TargetScalerDescriptor.FunctionId);
        Assert.Equal("FunctionB", secondScaler.TargetScalerDescriptor.FunctionId);
        Assert.Equal(3, (await firstScaler.GetScaleResultAsync(new())).TargetWorkerCount);
        Assert.Equal(3, (await secondScaler.GetScaleResultAsync(new())).TargetWorkerCount);
        Assert.Equal(2, endpoints.Count);
        Assert.NotEqual(
            endpoints[0].ReceiveUri,
            endpoints[1].ReceiveUri);
    }

    [Fact]
    public void Provider_UsesAzureComponentFactoryInjectedByScaleController()
    {
        AzureComponentFactory? selectedFactory = null;
        var injectedFactory = new TestAzureComponentFactory(new TestTokenCredential("site-identity"));
        var connectionProvider = new TestConnectionOptionsProvider((_, factory) =>
        {
            selectedFactory = factory;
            return new ConnectorConnectionOptions(
                new TestTokenCredential());
        });
        var depthFactory = new TestDepthClientFactory(
            (_, _, _) => new SequenceDepthClient(0));
        TriggerMetadata metadata = Metadata(
            "Function",
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger",
            1);
        metadata.Properties[nameof(AzureComponentFactory)] = injectedFactory;

        _ = new ConnectorScalerProvider(
            BuildServices(connectionProvider, depthFactory),
            metadata);

        Assert.Same(injectedFactory, selectedFactory);
    }

    [Fact]
    public void Provider_UsesDedicatedScaleControllerCredentialForApiHub()
    {
        var defaultCredential = new TestTokenCredential("default");
        var apiHubCredential = new TestTokenCredential("apihub");
        TokenCredential? depthCredential = null;
        var connectionProvider = new TestConnectionOptionsProvider((_, _) =>
            new ConnectorConnectionOptions(defaultCredential));
        var depthFactory = new TestDepthClientFactory((_, credential, _) =>
        {
            depthCredential = credential;
            return new SequenceDepthClient(0);
        });
        TriggerMetadata metadata = Metadata(
            "Function",
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger",
            1);
        metadata.Properties[ConnectorScaleCredentialProperties.ApiHubTokenCredential] = apiHubCredential;

        _ = new ConnectorScalerProvider(
            BuildServices(connectionProvider, depthFactory),
            metadata);

        Assert.Same(apiHubCredential, depthCredential);
    }

    [Fact]
    public void Provider_ResolvesPollingEndpointFromAppSetting()
    {
        ConnectorPollingEndpoints? selectedEndpoints = null;
        var connectionProvider = new TestConnectionOptionsProvider(
            (_, _) => new ConnectorConnectionOptions(
                new TestTokenCredential()));
        var depthFactory = new TestDepthClientFactory(
            (endpoints, _, _) =>
            {
                selectedEndpoints = endpoints;
                return new SequenceDepthClient(0);
            });
        IServiceProvider services = BuildServices(
            connectionProvider,
            depthFactory,
            new TestNameResolver(
                name => name == "OnNewEmailEndpoint"
                    ? "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/on-new-email"
                    : null));

        _ = new ConnectorScalerProvider(
            services,
            Metadata("Function", "%OnNewEmailEndpoint%", 1));

        Assert.NotNull(selectedEndpoints);
        Assert.Equal(
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/on-new-email/approximateQueueDepth",
            selectedEndpoints.ApproximateQueueDepthUri.ToString());
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

        builder.AddConnectorScaleForTrigger(
            Metadata(
                "FunctionA",
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger-a",
                4,
                maxBatchSize: 1));
        builder.AddConnectorScaleForTrigger(
            Metadata(
                "FunctionB",
                "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger-b",
                4));
        TriggerMetadata webhook = Metadata(
            "Webhook",
            "https://app-12.region.logic.azure.com/api/connectorGateways/ns/triggerConfigs/trigger-webhook",
            4);
        webhook.Metadata["deliveryMode"] = "Webhook";
        builder.AddConnectorScaleForTrigger(webhook);

        Assert.Equal(2, builder.Services.Count(descriptor => descriptor.ServiceType == typeof(Microsoft.Azure.WebJobs.Host.Scale.ITargetScalerProvider)));
    }
    private static IServiceProvider BuildServices(
        IConnectorConnectionOptionsProvider connectionProvider,
        IConnectorQueueDepthClientFactory depthFactory,
        INameResolver? nameResolver = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(connectionProvider);
        services.AddSingleton(depthFactory);
        services.AddSingleton(Options.Create(new ConnectorOptions { DefaultConcurrency = 16 }));
        if (nameResolver is not null)
        {
            services.AddSingleton(nameResolver);
        }

        return services.BuildServiceProvider();
    }

    private static TriggerMetadata Metadata(
        string functionName,
        string pollingEndpoint,
        int concurrency,
        int maxBatchSize = 1)
    {
        var metadata = new JObject
        {
            ["functionName"] = functionName,
            ["deliveryMode"] = "Poll",
            ["connection"] = "ConnectorNamespace",
            ["pollingEndpoint"] = pollingEndpoint,
            ["maxBatchSize"] = maxBatchSize,
            ["concurrency"] = concurrency,
        };
        return new TriggerMetadata(metadata);
    }

    private sealed class TestWebJobsBuilder : IWebJobsBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
    }
}
