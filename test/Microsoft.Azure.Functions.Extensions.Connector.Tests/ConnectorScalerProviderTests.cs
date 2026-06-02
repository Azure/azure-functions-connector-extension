// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorScalerProviderTests
{
    private static IServiceProvider BuildServices(
        ConnectorOptions? options = null,
        INameResolver? nameResolver = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.Configure<ConnectorOptions>(o =>
        {
            o.DefaultConcurrency = options?.DefaultConcurrency ?? 16;
            o.DefaultBatchSize = options?.DefaultBatchSize ?? 1;
            o.MockPendingEvents = options?.MockPendingEvents ?? 50;
            o.ThrottleScaleDownInterval = options?.ThrottleScaleDownInterval ?? TimeSpan.FromMinutes(3);
        });
        if (nameResolver is not null)
        {
            services.AddSingleton(nameResolver);
        }
        return services.BuildServiceProvider();
    }

    private static TriggerMetadata MakeMetadata(string functionName, JObject metadata)
    {
        // TriggerMetadata expects functionName + functionGroup on the JObject.
        metadata["functionName"] = functionName;
        return new TriggerMetadata(metadata);
    }

    [Fact]
    public void Ctor_LiteralValues_PassThroughUnchanged()
    {
        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "my-ns",
            ["triggerName"] = "my-trigger"
        });

        var provider = new ConnectorScalerProvider(BuildServices(), metadata);

        Assert.NotNull(provider.GetTargetScaler());
    }

    [Fact]
    public void Ctor_PercentSyntax_IsResolvedViaINameResolver()
    {
        // Customer wrote [ConnectorTrigger("%MyNs%", "%MyTrig%")] and set app settings.
        var resolver = new TestNameResolver(new Dictionary<string, string>
        {
            ["MyNs"]   = "resolved-namespace",
            ["MyTrig"] = "resolved-trigger"
        });

        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "%MyNs%",
            ["triggerName"]        = "%MyTrig%"
        });

        var provider = new ConnectorScalerProvider(BuildServices(nameResolver: resolver), metadata);

        Assert.NotNull(provider.GetTargetScaler());
        Assert.Equal(1, resolver.Lookups["MyNs"]);
        Assert.Equal(1, resolver.Lookups["MyTrig"]);
    }

    [Fact]
    public void Ctor_PercentSyntax_NoResolverRegistered_PassesThroughLiteral()
    {
        // Defensive: if INameResolver isn't in DI (e.g., minimal test rig),
        // we shouldn't crash. The literal %...% string survives to the scaler.
        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "%MyNs%",
            ["triggerName"]        = "%MyTrig%"
        });

        var provider = new ConnectorScalerProvider(BuildServices(nameResolver: null), metadata);

        Assert.NotNull(provider.GetTargetScaler());
    }

    [Fact]
    public void Ctor_MixedLiteralAndPercent_OnlyPercentValueIsResolved()
    {
        var resolver = new TestNameResolver(new Dictionary<string, string>
        {
            ["MyTrig"] = "resolved-trigger"
        });

        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "literal-ns",
            ["triggerName"]        = "%MyTrig%"
        });

        var provider = new ConnectorScalerProvider(BuildServices(nameResolver: resolver), metadata);

        Assert.NotNull(provider.GetTargetScaler());
        Assert.False(resolver.Lookups.ContainsKey("literal-ns"), "Literal should not trigger a lookup");
        Assert.Equal(1, resolver.Lookups["MyTrig"]);
    }

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "ns",
            ["triggerName"]        = "trig"
        });

        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorScalerProvider(serviceProvider: null!, triggerMetadata: metadata));
    }

    [Fact]
    public void Ctor_NullTriggerMetadata_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectorScalerProvider(serviceProvider: BuildServices(), triggerMetadata: null!));
    }

    [Fact]
    public void GetTargetScaler_ReturnsSameInstanceAcrossCalls()
    {
        var metadata = MakeMetadata("OnNewEmail", new JObject
        {
            ["connectorNamespace"] = "ns",
            ["triggerName"]        = "trig"
        });

        var provider = new ConnectorScalerProvider(BuildServices(), metadata);

        var s1 = provider.GetTargetScaler();
        var s2 = provider.GetTargetScaler();

        Assert.Same(s1, s2);
    }

    private sealed class TestNameResolver : INameResolver
    {
        private readonly IReadOnlyDictionary<string, string> _values;
        public Dictionary<string, int> Lookups { get; } = new(StringComparer.Ordinal);

        public TestNameResolver(IReadOnlyDictionary<string, string> values) => _values = values;

        public string? Resolve(string name)
        {
            Lookups[name] = Lookups.TryGetValue(name, out int n) ? n + 1 : 1;
            return _values.TryGetValue(name, out var v) ? v : null;
        }
    }
}
