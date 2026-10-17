// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorHttpRequestProcessorSizeTests
{
    private const int FormerLimit = 10 * 1024 * 1024;

    [Theory]
    [InlineData(FormerLimit + 1, false)]
    [InlineData(FormerLimit + 1, true)]
    public async Task ProcessAsync_AcceptsAsciiAtFormerSizeBoundary(
        int byteCount,
        bool unknownLength)
    {
        string body = "{\"data\":\"" + new string('a', byteCount - 11) + "\"}";
        Assert.Equal(byteCount, Encoding.UTF8.GetByteCount(body));

        await AssertDeliveryAsync(body, unknownLength);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_AcceptsNonAsciiWithOrWithoutContentLength(
        bool unknownLength)
    {
        string body = "{\"data\":\"" + new string('\u20ac', FormerLimit / 3) + "\"}";
        Assert.True(Encoding.UTF8.GetByteCount(body) > FormerLimit);
        Assert.True(body.Length < FormerLimit);

        await AssertDeliveryAsync(body, unknownLength);
    }

    [Theory]
    [InlineData(8 * 1024 * 1024)]
    [InlineData(10 * 1024 * 1024)]
    public async Task ProcessAsync_AcceptsBase64AttachmentsBeyondFormerLimit(
        int attachmentBytes)
    {
        string body = "{\"body\":{\"value\":[{\"attachments\":[{\"contentBytes\":\""
            + Convert.ToBase64String(new byte[attachmentBytes])
            + "\"}]}]}}";
        int expectedBase64Length = 4 * ((attachmentBytes + 2) / 3);
        Assert.Equal(expectedBase64Length + 58, Encoding.UTF8.GetByteCount(body));

        await AssertDeliveryAsync(body, unknownLength: false);
    }

    private static async Task AssertDeliveryAsync(
        string body,
        bool unknownLength)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/")
        {
            Content = unknownLength
                ? new UnknownLengthContent(bytes)
                : new ByteArrayContent(bytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        Assert.Equal(unknownLength ? (long?)null : bytes.Length, request.Content.Headers.ContentLength);
        if (unknownLength)
        {
            request.Headers.TransferEncodingChunked = true;
        }

        var processor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        int invocations = 0;
        string? receivedBody = null;
        string? receivedFunctionName = null;
        using HttpResponseMessage response = await processor.ProcessAsync(
            request,
            "LargeOutputProbe",
            (received, function, _) =>
            {
                invocations++;
                receivedBody = received;
                receivedFunctionName = function;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, invocations);
        Assert.Equal(body, receivedBody);
        Assert.Equal("LargeOutputProbe", receivedFunctionName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_PreservesKestrelBodyLimit(bool unknownLength)
    {
        const int hostLimit = 1024 * 1024;
        var processor = new ConnectorHttpRequestProcessor(
            NullLogger<ConnectorHttpRequestProcessor>.Instance);
        int invocations = 0;
        int? receivedBytes = null;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Func<string, string, CancellationToken, Task<HttpResponseMessage>> callback = (body, _, _) =>
        {
            invocations++;
            receivedBytes = Encoding.UTF8.GetByteCount(body);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        };
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = hostLimit;
        });
        await using var host = builder.Build();
        host.Run(async context =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/")
            {
                Content = new StreamContent(context.Request.Body)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content.Headers.ContentLength = context.Request.ContentLength;
            using var response = await processor.ProcessAsync(
                request,
                "HostLimitProbe",
                callback,
                context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
        });

        try
        {
            await host.StartAsync(timeout.Token);
            using var client = new HttpClient();
            // Kestrel counts chunk framing too; leave headroom for the chunked success case.
            int acceptedBytes = unknownLength ? hostLimit / 2 : hostLimit;
            int[] sizes = unknownLength
                ? [acceptedBytes, hostLimit, hostLimit + 1]
                : [acceptedBytes, hostLimit + 1];
            foreach (int byteCount in sizes)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(
                    "{\"data\":\"" + new string('a', byteCount - 11) + "\"}");
                using var request = new HttpRequestMessage(HttpMethod.Post, Assert.Single(host.Urls))
                {
                    Content = unknownLength
                        ? new UnknownLengthContent(bytes)
                        : new ByteArrayContent(bytes)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Headers.TransferEncodingChunked = unknownLength;

                using var response = await client.SendAsync(request, timeout.Token);

                Assert.Equal(byteCount == acceptedBytes
                    ? HttpStatusCode.Accepted
                    : HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
                Assert.Equal(1, invocations);
                Assert.Equal(acceptedBytes, receivedBytes);
            }
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }

    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
