// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorOptionsBindingTests
{
    [Fact]
    public void Defaults_AreApplied_WhenConfigurationIsAbsent()
    {
        using IHost host = BuildHost(new Dictionary<string, string?>());

        ConnectorOptions options =
            host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(1, options.DefaultMaxBatchSize);
        Assert.Equal(16, options.DefaultConcurrency);
    }

    [Fact]
    public void HostConfiguration_BindsConnectorDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultMaxBatchSize"] = "4",
            ["AzureWebJobs:extensions:connector:defaultConcurrency"] = "8",
        };

        using IHost host = BuildHost(settings);

        ConnectorOptions options =
            host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(4, options.DefaultMaxBatchSize);
        Assert.Equal(8, options.DefaultConcurrency);
    }

    [Fact]
    public void ProgrammaticConfiguration_OverridesHostConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureWebJobs:extensions:connector:defaultMaxBatchSize"] = "4",
            ["AzureWebJobs:extensions:connector:defaultConcurrency"] = "8",
        };

        using IHost host = BuildHost(
            settings,
            options =>
            {
                options.DefaultMaxBatchSize = 2;
                options.DefaultConcurrency = 6;
            });

        ConnectorOptions options =
            host.Services.GetRequiredService<IOptions<ConnectorOptions>>().Value;

        Assert.Equal(2, options.DefaultMaxBatchSize);
        Assert.Equal(6, options.DefaultConcurrency);
    }

    [Fact]
    public void OptionsFormatter_IncludesConnectorDefaults()
    {
        var options = new ConnectorOptions
        {
            DefaultMaxBatchSize = 4,
            DefaultConcurrency = 8,
        };

        string formatted = ((IOptionsFormatter)options).Format();

        Assert.Contains("\"DefaultMaxBatchSize\": 4", formatted);
        Assert.Contains("\"DefaultConcurrency\": 8", formatted);
    }

    [Fact]
    public void AddConnector_RegistersConnectionServices()
    {
        using IHost host = BuildHost(new Dictionary<string, string?>());

        Assert.NotNull(host.Services.GetRequiredService<AzureComponentFactory>());
        Assert.NotNull(
            host.Services.GetRequiredService<IConnectorConnectionOptionsProvider>());
    }

    private static IHost BuildHost(
        IDictionary<string, string?> settings,
        Action<ConnectorOptions>? configure = null)
    {
        return new HostBuilder()
            .ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(settings))
            .ConfigureWebJobs(builder =>
            {
                if (configure is null)
                {
                    builder.AddConnector();
                }
                else
                {
                    builder.AddConnector(configure);
                }
            })
            .Build();
    }
}
