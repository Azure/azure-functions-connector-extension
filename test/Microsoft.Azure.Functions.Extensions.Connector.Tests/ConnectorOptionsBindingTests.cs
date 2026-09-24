// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    public void AddConnector_RegistersAzureHttpAndPollingFactories()
    {
        using IHost host = BuildHost(new Dictionary<string, string?>());

        Assert.NotNull(host.Services.GetRequiredService<AzureComponentFactory>());
        Assert.NotNull(host.Services.GetRequiredService<IHttpClientFactory>());
        Assert.NotNull(
            host.Services.GetRequiredService<IConnectorConnectionOptionsProvider>());
        Assert.NotNull(
            host.Services.GetRequiredService<IConnectorScaleConnectionOptionsProvider>());
        Assert.NotNull(host.Services.GetRequiredService<IConnectorPollingEndpointResolverFactory>());
        Assert.NotNull(host.Services.GetRequiredService<IConnectorQueueDepthClientFactory>());
        Assert.NotNull(host.Services.GetRequiredService<IConnectorPollDeliveryClientFactory>());
        Assert.NotNull(host.Services.GetRequiredService<IConnectorLinkedOutputClient>());
        Assert.Same(
            host.Services.GetRequiredService<ConnectorLinkedOutputInvocationLimiter>(),
            host.Services.GetRequiredService<ConnectorLinkedOutputInvocationLimiter>());
        Assert.NotNull(host.Services.GetRequiredService<IConnectorPollingListenerFactory>());
    }

    [Fact]
    public void AddConnector_ConfiguresSecureLinkedOutputHandler()
    {
        using IHost host = BuildHost(new Dictionary<string, string?>());
        IHttpMessageHandlerFactory factory =
            host.Services.GetRequiredService<IHttpMessageHandlerFactory>();

        HttpMessageHandler handler =
            factory.CreateHandler(ConnectorLinkedOutputClient.HttpClientName);
        while (handler is DelegatingHandler delegatingHandler)
        {
            handler = Assert.IsAssignableFrom<HttpMessageHandler>(
                delegatingHandler.InnerHandler);
        }

        HttpClientHandler primaryHandler =
            Assert.IsType<HttpClientHandler>(handler);
        Assert.False(primaryHandler.AllowAutoRedirect);
        Assert.Equal(
            System.Net.DecompressionMethods.None,
            primaryHandler.AutomaticDecompression);
    }

    [Fact]
    public async Task AddConnector_DoesNotLogSignedLinkedOutputUri()
    {
        const string signedUri =
            "https://content.test/private/output?signature=secret-value";
        var loggerProvider = new CapturingLoggerProvider();
        var handler = new SequenceHttpMessageHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"body":{"value":1}}""",
                    System.Text.Encoding.UTF8,
                    "application/json"),
            });
        using IHost host = BuildHost(
            new Dictionary<string, string?>(),
            configureServices: services =>
                services.AddHttpClient(
                    ConnectorLinkedOutputClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => handler),
            configureLogging: logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddProvider(loggerProvider);
            });
        IConnectorLinkedOutputClient client =
            host.Services.GetRequiredService<IConnectorLinkedOutputClient>();

        await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(signedUri)),
            1024,
            CancellationToken.None);

        Assert.DoesNotContain(
            loggerProvider.Messages,
            message =>
                message.Contains("secret-value", StringComparison.Ordinal) ||
                message.Contains(
                    "/private/output",
                    StringComparison.Ordinal));
    }

    private static IHost BuildHost(
        IDictionary<string, string?> settings,
        Action<ConnectorOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        IHostBuilder hostBuilder = new HostBuilder()
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
            });
        if (configureServices is not null)
        {
            hostBuilder.ConfigureServices(configureServices);
        }

        if (configureLogging is not null)
        {
            hostBuilder.ConfigureLogging(configureLogging);
        }

        return hostBuilder.Build();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string>
            _messages = new();

        internal IReadOnlyCollection<string> Messages => _messages.ToArray();

        public ILogger CreateLogger(string categoryName) =>
            new CapturingLogger(_messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(
            System.Collections.Concurrent.ConcurrentQueue<string> messages) :
            ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull =>
                null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Enqueue(formatter(state, exception));
                if (exception is not null)
                {
                    messages.Enqueue(exception.ToString());
                }
            }
        }
    }
}
