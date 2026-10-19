// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorQueueDepthClientTests
{
    [Fact]
    public async Task GetApproximateQueueDepthAsync_UsesOpaqueEndpointRuntimeScopeAndParsesDepth()
    {
        ConnectorPollingEndpoints endpoints = Endpoints("https://opaque.runtime.test/custom/depth?server=value");
        var credential = new TestTokenCredential();
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"approximateQueueDepth\":37}"));
        var client = CreateClient(endpoints, credential, handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(37, depth);
        Assert.Equal(new[] { ConnectorQueueDepthClient.ApiHubScope }, Assert.Single(credential.RequestedScopes));
        Assert.Equal(endpoints.ApproximateQueueDepthUri, Assert.Single(handler.Requests).RequestUri);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_StreamsResponseBeforeReadingContent()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var content = new TrackingHttpContent(
            """{"approximateQueueDepth":1}""");
        var handler = new SequenceHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content,
            });
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(1, depth);
        Assert.False(content.WasBuffered);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_TimesOutWhileReadingContent()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new BlockingHttpContent(),
            });
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler,
            TimeSpan.FromMilliseconds(50));

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(
                () => client.GetApproximateQueueDepthAsync());

        Assert.Equal(
            "Connector approximate queue depth HTTP request timed out.",
            exception.Message);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_SupportsInt64Depth()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(
                HttpStatusCode.OK,
                "{\"approximateQueueDepth\":2147483648}"));
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(2147483648L, depth);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 1)]
    [InlineData(HttpStatusCode.Unauthorized, 1)]
    [InlineData(HttpStatusCode.Forbidden, 1)]
    [InlineData(HttpStatusCode.NotFound, 1)]
    [InlineData(HttpStatusCode.NotImplemented, 1)]
    [InlineData(HttpStatusCode.TooManyRequests, 3)]
    [InlineData(HttpStatusCode.InternalServerError, 3)]
    public async Task GetApproximateQueueDepthAsync_ThrowsAfterEligibleAttempts(
        HttpStatusCode statusCode, int expectedAttempts)
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(statusCode, """{"error":"sensitive-response-body"}"""));
        var client = CreateClient(endpoints, new TestTokenCredential(), handler);

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                client.GetApproximateQueueDepthAsync());

        Assert.Contains($"HTTP {(int)statusCode}", exception.Message);
        Assert.DoesNotContain("sensitive-response-body", exception.ToString(), StringComparison.Ordinal);
        Assert.Equal(expectedAttempts, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task GetApproximateQueueDepthAsync_RecoversFromTransientHttpErrors(
        HttpStatusCode statusCode)
    {
        var credential = new TestTokenCredential();
        var handler = new SequenceHttpMessageHandler(
            _ => JsonResponse(statusCode, "{}"),
            _ => JsonResponse(statusCode, "{}"),
            _ => JsonResponse(HttpStatusCode.OK, """{"approximateQueueDepth":0}"""));
        var client = CreateClient(Endpoints("https://runtime.test/depth"), credential, handler);

        long depth = await client.GetApproximateQueueDepthAsync();

        Assert.Equal(0, depth);
        Assert.Equal(3, handler.CallCount);
        Assert.Single(credential.RequestedScopes);
        Assert.NotSame(handler.Requests[0], handler.Requests[1]);
        Assert.NotSame(handler.Requests[1], handler.Requests[2]);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
        });
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_RecoversFromTransportError()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new HttpRequestException("sensitive request details")),
            (_, _) => Task.FromResult(
                JsonResponse(HttpStatusCode.OK, """{"approximateQueueDepth":17}""")));
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"), new TestTokenCredential(), handler);

        Assert.Equal(17, await client.GetApproximateQueueDepthAsync());
        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetApproximateQueueDepthAsync_RetriesInterruptedResponseReads(bool recovers)
    {
        const string sensitiveDetails = "response payload and endpoint secret";
        int attempts = 0;
        var handler = new SequenceHttpMessageHandler(_ =>
            ++attempts == 2 && recovers
                ? JsonResponse(HttpStatusCode.OK, """{"approximateQueueDepth":12}""")
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new TrackingHttpContent("{}", new IOException(sensitiveDetails)),
                });
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"), new TestTokenCredential(), handler);

        if (recovers)
        {
            Assert.Equal(12, await client.GetApproximateQueueDepthAsync());
            Assert.Equal(2, handler.CallCount);
        }
        else
        {
            ConnectorQueueDepthException exception =
                await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                    client.GetApproximateQueueDepthAsync());

            Assert.Null(exception.InnerException);
            Assert.DoesNotContain(sensitiveDetails, exception.ToString(), StringComparison.Ordinal);
            Assert.Equal(3, handler.CallCount);
        }
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DisposesFailedResponseBeforeBackoff()
    {
        var content = new TrackingHttpContent("{}");
        var handler = new SequenceHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = content },
            _ => JsonResponse(HttpStatusCode.OK, """{"approximateQueueDepth":1}"""));
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            retryDelayAsync: (_, _) =>
            {
                Assert.True(content.WasDisposed);
                return Task.CompletedTask;
            });

        Assert.Equal(1, await client.GetApproximateQueueDepthAsync());
        Assert.False(content.WasBuffered);
        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetApproximateQueueDepthAsync_HonorsRetryAfter(bool useDate)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var handler = new SequenceHttpMessageHandler(
            _ =>
            {
                var response = JsonResponse(HttpStatusCode.TooManyRequests, "{}");
                response.Headers.RetryAfter = useDate
                    ? new RetryConditionHeaderValue(now.AddSeconds(10))
                    : new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
                return response;
            },
            _ => JsonResponse(HttpStatusCode.OK, """{"approximateQueueDepth":1}"""));
        var delays = new List<TimeSpan>();
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            retryDelayAsync: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        Assert.Equal(1, await client.GetApproximateQueueDepthAsync());
        Assert.InRange(Assert.Single(delays), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DoesNotShortenRetryAfterToFitBudget()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
        {
            var response = JsonResponse(HttpStatusCode.ServiceUnavailable, "{}");
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
            return response;
        });
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            retryDelayAsync: (_, _) =>
                throw new InvalidOperationException("A retry delay was not expected."));

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                client.GetApproximateQueueDepthAsync());

        Assert.Contains("HTTP 503", exception.Message);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_CancelsDuringRetryDelay()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"));
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            retryDelayAsync: (_, token) =>
            {
                cancellation.Cancel();
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetApproximateQueueDepthAsync(cancellation.Token));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DeadlineIncludesRetryDelay()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"));
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            TimeSpan.FromSeconds(1),
            (_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token));

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                client.GetApproximateQueueDepthAsync());

        Assert.Equal("Connector approximate queue depth HTTP request timed out.", exception.Message);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DeadlineIsSharedAcrossAttemptsAndBody()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromResult(JsonResponse(HttpStatusCode.ServiceUnavailable, "{}")),
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new BlockingHttpContent(),
                }));
        CancellationToken? operationToken = null;
        var client = CreateClient(
            Endpoints("https://runtime.test/depth"),
            new TestTokenCredential(),
            handler,
            TimeSpan.FromSeconds(1),
            (_, token) =>
            {
                operationToken = token;
                return Task.CompletedTask;
            });

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(() =>
                client.GetApproximateQueueDepthAsync());

        Assert.Equal("Connector approximate queue depth HTTP request timed out.", exception.Message);
        Assert.Equal(2, handler.CallCount);
        Assert.True(operationToken?.IsCancellationRequested);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_DoesNotExposeHttpRequestDetails()
    {
        const string sensitiveRequestDetails =
            "https://runtime.test/depth?signature=secret-value";
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new HttpRequestException(sensitiveRequestDetails)));
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler);

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(
                () => client.GetApproximateQueueDepthAsync());

        Assert.Equal(
            "Connector approximate queue depth HTTP request failed.",
            exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(
            sensitiveRequestDetails,
            exception.ToString(),
            StringComparison.Ordinal);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_PropagatesCallerCancellation()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, cancellationToken) =>
                Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetApproximateQueueDepthAsync(cancellation.Token));

        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetApproximateQueueDepthAsync_RejectsOversizedResponse()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(_ =>
        {
            HttpResponseMessage response = JsonResponse(
                HttpStatusCode.OK,
                "{\"approximateQueueDepth\":1}");
            response.Content.Headers.ContentLength =
                ConnectorPollingProtocolLimits
                    .MaximumQueueDepthResponseSizeInBytes + 1L;
            return response;
        });
        var client = CreateClient(
            endpoints,
            new TestTokenCredential(),
            handler);

        ConnectorQueueDepthException exception =
            await Assert.ThrowsAsync<ConnectorQueueDepthException>(
                () => client.GetApproximateQueueDepthAsync());

        Assert.Contains("byte limit", exception.Message);
        Assert.Equal(1, handler.CallCount);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"approximateQueueDepth\":-1}")]
    [InlineData("{\"approximateQueueDepth\":\"4\"}")]
    public async Task GetApproximateQueueDepthAsync_RejectsInvalidDepth(string body)
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.OK, body));
        var client = CreateClient(endpoints, new TestTokenCredential(), handler);

        await Assert.ThrowsAsync<ConnectorQueueDepthException>(() => client.GetApproximateQueueDepthAsync());
        Assert.Equal(1, handler.CallCount);
    }

    private static ConnectorQueueDepthClient CreateClient(
        ConnectorPollingEndpoints endpoints,
        TestTokenCredential credential,
        HttpMessageHandler handler,
        TimeSpan? timeout = null,
        Func<TimeSpan, CancellationToken, Task>? retryDelayAsync = null) =>
        new(
            endpoints,
            credential,
            new TestHttpClientFactory(handler, timeout),
            "Function",
            NullLogger<ConnectorQueueDepthClient>.Instance,
            new ConnectorPollingRetryPolicy(
                retryDelayAsync ?? ((_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                })));

    private static ConnectorPollingEndpoints Endpoints(string depthUri) => new(
        new Uri("https://runtime.test/receive"),
        new Uri("https://runtime.test/ack"),
        new Uri(depthUri));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class TrackingHttpContent(
        string json, Exception? readFailure = null) : HttpContent
    {
        private readonly byte[] _content = Encoding.UTF8.GetBytes(json);

        internal bool WasBuffered { get; private set; }
        internal bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            WasBuffered = true;
            return stream.WriteAsync(_content).AsTask();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            readFailure is null
                ? Task.FromResult<Stream>(new MemoryStream(_content, writable: false))
                : Task.FromException<Stream>(readFailure);
    }

    private sealed class BlockingHttpContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            throw new NotSupportedException();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new BlockingStream());
    }

    private sealed class BlockingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            new(WaitForCancellationAsync(cancellationToken));

        private static async Task<int> WaitForCancellationAsync(
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();
        public override void SetLength(long value) =>
            throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
