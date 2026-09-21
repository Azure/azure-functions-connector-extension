// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorLinkedOutputClientTests
{
    private const string SignedUri =
        "https://content.test/path/output?signature=secret-value";

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        var httpClientFactory = new TestHttpClientFactory(
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException()));
        var logger = NullLogger<ConnectorLinkedOutputClient>.Instance;

        Assert.Equal(
            "httpClientFactory",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorLinkedOutputClient(null!, logger)).ParamName);
        Assert.Equal(
            "logger",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorLinkedOutputClient(
                    httpClientFactory,
                    null!)).ParamName);
        Assert.Equal(
            "retryDelayAsync",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorLinkedOutputClient(
                    httpClientFactory,
                    logger,
                    null!)).ParamName);
    }

    [Fact]
    public async Task DownloadAsync_RejectsNullOutputsLink()
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException()));

        Assert.Equal(
            "outputsLink",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.DownloadAsync(
                    null!,
                    1024,
                    CancellationToken.None))).ParamName);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsCompleteOutputsWithoutAuthorization()
    {
        string? authorization = null;
        var handler = new SequenceHttpMessageHandler(request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            return JsonResponse("""{"headers":{},"body":{"value":1}}""");
        });
        ConnectorLinkedOutputClient client = CreateClient(handler);

        BinaryData result = await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(SignedUri)),
            1024,
            CancellationToken.None);

        Assert.Equal(
            """{"headers":{},"body":{"value":1}}""",
            result.ToString());
        Assert.Null(authorization);
    }

    [Fact]
    public async Task DownloadAsync_AllowsExactActualByteLimit()
    {
        const string json = """{"value":1}""";
        int byteCount = Encoding.UTF8.GetByteCount(json);
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ => JsonResponse(json)));

        BinaryData result = await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(SignedUri)),
            byteCount,
            CancellationToken.None);

        Assert.Equal(byteCount, result.ToMemory().Length);
    }

    [Fact]
    public async Task DownloadAsync_RejectsActualBytesOverLimitAndRedactsUri()
    {
        const string json = """{"value":1}""";
        int byteCount = Encoding.UTF8.GetByteCount(json);
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
            {
                HttpResponseMessage response = JsonResponse(json);
                response.Content.Headers.ContentLength = null;
                return response;
            }));

        ConnectorLinkedOutputException exception =
            await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
                client.DownloadAsync(
                    new ConnectorOutputsLink(new Uri(SignedUri)),
                    byteCount - 1,
                    CancellationToken.None));

        Assert.DoesNotContain("secret-value", exception.ToString());
        Assert.Contains("https://content.test/[REDACTED]", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_AllowsAbsoluteActualByteLimit()
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
                StreamingJsonResponse(
                    ConnectorPollingProtocolLimits
                        .MaximumOutputsPayloadSizeInBytes)));

        BinaryData result = await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(SignedUri)),
            ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes,
            CancellationToken.None);

        Assert.Equal(
            ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes,
            result.ToMemory().Length);
    }

    [Fact]
    public async Task DownloadAsync_RejectsActualByteBeyondAbsoluteLimit()
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
                StreamingJsonResponse(
                    ConnectorPollingProtocolLimits
                        .MaximumOutputsPayloadSizeInBytes + 1)));

        await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                ConnectorPollingProtocolLimits
                    .MaximumOutputsPayloadSizeInBytes,
                CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(104857601)]
    public async Task DownloadAsync_RejectsInvalidConfiguredLimit(int limit)
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException("Request was not expected.")));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                limit,
                CancellationToken.None));
    }

    [Theory]
    [InlineData("application/json", null)]
    [InlineData("text/json", "utf-8")]
    [InlineData("application/json", "utf-16")]
    public async Task DownloadAsync_RequiresJsonUtf8ContentType(
        string mediaType,
        string? charSet)
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
            {
                HttpResponseMessage response =
                    JsonResponse("""{"value":1}""");
                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue(mediaType)
                    {
                        CharSet = charSet,
                    };
                return response;
            }));

        await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                1024,
                CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_RejectsContentEncoding()
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ =>
            {
                HttpResponseMessage response =
                    JsonResponse("""{"value":1}""");
                response.Content.Headers.ContentEncoding.Add("gzip");
                return response;
            }));

        await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                1024,
                CancellationToken.None));
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("{")]
    public async Task DownloadAsync_RequiresCompleteJsonObject(string body)
    {
        ConnectorLinkedOutputClient client = CreateClient(
            new SequenceHttpMessageHandler(_ => JsonResponse(body)));

        await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                1024,
                CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_RetriesTransientGet()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => JsonResponse(
                "{}",
                HttpStatusCode.ServiceUnavailable),
            _ => JsonResponse("""{"value":1}"""));
        ConnectorLinkedOutputClient client = CreateClient(handler);

        BinaryData result = await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(SignedUri)),
            1024,
            CancellationToken.None);

        Assert.Equal("""{"value":1}""", result.ToString());
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task DownloadAsync_RetriesTransportTimeout()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("timeout")),
            (_, _) => Task.FromResult(
                JsonResponse("""{"value":1}""")));
        ConnectorLinkedOutputClient client = CreateClient(handler);

        BinaryData result = await client.DownloadAsync(
            new ConnectorOutputsLink(new Uri(SignedUri)),
            1024,
            CancellationToken.None);

        Assert.Equal("""{"value":1}""", result.ToString());
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task DownloadAsync_DoesNotRetryCallerCancellation()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);
                throw new InvalidOperationException(
                    "Cancellation was not observed.");
            });
        ConnectorLinkedOutputClient client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.DownloadAsync(
                new ConnectorOutputsLink(new Uri(SignedUri)),
                1024,
                cancellation.Token));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task DownloadAsync_RedactsTransportFailureUri()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => throw new HttpRequestException(
                $"Request failed for {SignedUri}"),
            _ => throw new HttpRequestException(
                $"Request failed for {SignedUri}"),
            _ => throw new HttpRequestException(
                $"Request failed for {SignedUri}"));
        ConnectorLinkedOutputClient client = CreateClient(handler);

        ConnectorLinkedOutputException exception =
            await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
                client.DownloadAsync(
                    new ConnectorOutputsLink(new Uri(SignedUri)),
                    1024,
                    CancellationToken.None));

        Assert.Equal(3, handler.CallCount);
        Assert.DoesNotContain("secret-value", exception.ToString());
        Assert.DoesNotContain("/path/output", exception.ToString());
    }

    [Fact]
    public async Task DownloadAsync_DoesNotFollowOrRetryRedirect()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers =
                {
                    Location = new Uri(
                        "https://other.test/path?signature=other-secret"),
                },
            });
        ConnectorLinkedOutputClient client = CreateClient(handler);

        ConnectorLinkedOutputException exception =
            await Assert.ThrowsAsync<ConnectorLinkedOutputException>(() =>
                client.DownloadAsync(
                    new ConnectorOutputsLink(new Uri(SignedUri)),
                    1024,
                    CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
        Assert.DoesNotContain("other-secret", exception.ToString());
        Assert.DoesNotContain("secret-value", exception.ToString());
    }

    private static ConnectorLinkedOutputClient CreateClient(
        HttpMessageHandler handler) =>
        new(
            new TestHttpClientFactory(handler),
            NullLogger<ConnectorLinkedOutputClient>.Instance,
            (_, _) => Task.CompletedTask);

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"),
        };

    private static HttpResponseMessage StreamingJsonResponse(long byteCount) =>
        new(HttpStatusCode.OK)
        {
            Content = new GeneratedJsonContent(byteCount),
        };

    private sealed class GeneratedJsonContent : HttpContent
    {
        private readonly long _byteCount;

        internal GeneratedJsonContent(long byteCount)
        {
            _byteCount = byteCount;
            Headers.ContentType = new MediaTypeHeaderValue(
                ConnectorPollingHttpConstants.JsonMediaType)
            {
                CharSet = ConnectorPollingHttpConstants.Utf8CharacterSet,
            };
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            new GeneratedJsonStream(_byteCount).CopyToAsync(stream);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(
                new GeneratedJsonStream(_byteCount));

        protected override Task<Stream> CreateContentReadStreamAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(
                new GeneratedJsonStream(_byteCount));
    }

    private sealed class GeneratedJsonStream(long length) : Stream
    {
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadCore(buffer.AsSpan(offset, count));

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(ReadCore(buffer.Span));
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(
            byte[] buffer,
            int offset,
            int count) =>
            throw new NotSupportedException();

        private int ReadCore(Span<byte> buffer)
        {
            if (_position >= length)
            {
                return 0;
            }

            int count = (int)Math.Min(buffer.Length, length - _position);
            Span<byte> destination = buffer[..count];
            destination.Fill((byte)' ');
            if (_position == 0)
            {
                destination[0] = (byte)'{';
            }

            long finalIndex = length - 1;
            if (finalIndex >= _position &&
                finalIndex < _position + count)
            {
                destination[(int)(finalIndex - _position)] = (byte)'}';
            }

            _position += count;
            return count;
        }
    }
}
