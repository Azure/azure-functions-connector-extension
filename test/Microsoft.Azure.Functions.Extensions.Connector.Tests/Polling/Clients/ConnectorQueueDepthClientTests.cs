// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
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

    [Fact]
    public async Task GetApproximateQueueDepthAsync_ThrowsForHttpError()
    {
        ConnectorPollingEndpoints endpoints =
            Endpoints("https://runtime.test/depth");
        var handler = new SequenceHttpMessageHandler(_ => JsonResponse(HttpStatusCode.InternalServerError, "{}"));
        var client = CreateClient(endpoints, new TestTokenCredential(), handler);

        await Assert.ThrowsAsync<ConnectorQueueDepthException>(() => client.GetApproximateQueueDepthAsync());

        Assert.Equal(1, handler.CallCount);
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
    }

    private static ConnectorQueueDepthClient CreateClient(
        ConnectorPollingEndpoints endpoints,
        TestTokenCredential credential,
        HttpMessageHandler handler,
        TimeSpan? timeout = null) =>
        new(
            endpoints,
            credential,
            new TestHttpClientFactory(handler, timeout),
            "Function",
            NullLogger<ConnectorQueueDepthClient>.Instance);

    private static ConnectorPollingEndpoints Endpoints(string depthUri) => new(
        new Uri("https://runtime.test/receive"),
        new Uri("https://runtime.test/ack"),
        new Uri(depthUri));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class TrackingHttpContent(string json) : HttpContent
    {
        private readonly byte[] _content = Encoding.UTF8.GetBytes(json);

        internal bool WasBuffered { get; private set; }

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
            Task.FromResult<Stream>(
                new MemoryStream(_content, writable: false));
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
