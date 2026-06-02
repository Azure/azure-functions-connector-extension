// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;


public class ConnectorOptionsBindingTests
{
    private static IHost BuildHost(
        IDictionary<string, string?> settings,
        Action<ConnectorOptions>? configure = null)
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(settings))
            .ConfigureWebJobs(b =>
            {
                if (configure is null)
                {
                    b.AddConnector();
                }
                else
                {
                    b.AddConnector(configure);
                }
            })
            .Build();
    }

    [Fact]
    public void Defaults_AreApplied_WhenHostJsonHasNoConnectorSection()
    {
        using var host = BuildHost(new Dictionary<string, string?>());

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(16, opts.DefaultConcurrency);
        Assert.Equal(1, opts.DefaultBatchSize);
        Assert.Equal(TimeSpan.FromMinutes(3), opts.ThrottleScaleDownInterval);
        Assert.Equal(50, opts.MockPendingEvents);
    }

    [Fact]
    public void HostJson_DefaultConcurrency_IsBound()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultConcurrency"] = "42"
        };

        using var host = BuildHost(settings);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(42, opts.DefaultConcurrency);
    }

    [Fact]
    public void HostJson_DefaultBatchSize_IsBound()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultBatchSize"] = "25"
        };

        using var host = BuildHost(settings);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(25, opts.DefaultBatchSize);
    }

    [Fact]
    public void HostJson_ThrottleScaleDownInterval_IsBound()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:throttleScaleDownInterval"] = "00:05:00"
        };

        using var host = BuildHost(settings);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(5), opts.ThrottleScaleDownInterval);
    }

    [Fact]
    public void HostJson_MockPendingEvents_IsBound()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:mockPendingEvents"] = "320"
        };

        using var host = BuildHost(settings);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(320, opts.MockPendingEvents);
    }

    [Fact]
    public void HostJson_AllProperties_BoundTogether()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultConcurrency"] = "64",
            ["AzureWebJobs:extensions:connector:defaultBatchSize"] = "10",
            ["AzureWebJobs:extensions:connector:throttleScaleDownInterval"] = "00:00:30",
            ["AzureWebJobs:extensions:connector:mockPendingEvents"] = "1000"
        };

        using var host = BuildHost(settings);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(64, opts.DefaultConcurrency);
        Assert.Equal(10, opts.DefaultBatchSize);
        Assert.Equal(TimeSpan.FromSeconds(30), opts.ThrottleScaleDownInterval);
        Assert.Equal(1000, opts.MockPendingEvents);
    }

    [Fact]
    public void CodeBasedConfigure_OverridesHostJson()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultConcurrency"] = "42"
        };

        using var host = BuildHost(settings, opts => opts.DefaultConcurrency = 99);

        var opts = host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        // PostConfigure runs after host.json binding, so the action wins.
        Assert.Equal(99, opts.DefaultConcurrency);
    }

    [Fact]
    public void IOptionsFormatter_Format_ProducesNonEmptyJson()
    {
        var opts = new ConnectorOptions
        {
            DefaultConcurrency = 32,
            DefaultBatchSize = 8,
            ThrottleScaleDownInterval = TimeSpan.FromMinutes(5),
            MockPendingEvents = 250
        };

        string formatted = ((IOptionsFormatter)opts).Format();

        Assert.Contains("\"DefaultConcurrency\": 32", formatted);
        Assert.Contains("\"DefaultBatchSize\": 8", formatted);
        Assert.Contains("\"MockPendingEvents\": 250", formatted);
        // TimeSpan serialises as "HH:mm:ss"
        Assert.Contains("00:05:00", formatted);
    }
}

