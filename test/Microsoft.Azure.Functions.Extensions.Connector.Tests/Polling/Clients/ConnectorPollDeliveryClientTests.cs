// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollDeliveryClientTests
{
    [Fact]
    public void Factory_RejectsNullDependenciesAndCredential()
    {
        var httpClientFactory = new TestHttpClientFactory(
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException()));

        Assert.Equal(
            "httpClientFactory",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorPollDeliveryClientFactory(null!)).ParamName);

        var factory = new ConnectorPollDeliveryClientFactory(httpClientFactory);
        Assert.Equal(
            "credential",
            Assert.Throws<ArgumentNullException>(() =>
                factory.Create(null!)).ParamName);
    }

    [Fact]
    public void Client_RejectsNullDependencies()
    {
        var credential = new TestTokenCredential();
        var httpClientFactory = new TestHttpClientFactory(
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException()));
        Assert.Equal(
            "credential",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorPollDeliveryClient(
                    null!,
                    httpClientFactory)).ParamName);
        Assert.Equal(
            "httpClientFactory",
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectorPollDeliveryClient(
                    credential,
                    null!)).ParamName);
    }

    [Fact]
    public async Task Operations_RejectNullArguments()
    {
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException()));

        Assert.Equal(
            "endpoints",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.ReceiveAsync(
                    null!,
                    1,
                    CancellationToken.None))).ParamName);
        Assert.Equal(
            "endpoints",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.AcknowledgeAsync(
                    null!,
                    [new ConnectorMessageLock("message", "lock")],
                    CancellationToken.None))).ParamName);
        Assert.Equal(
            "messages",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.AcknowledgeAsync(
                    Endpoints(),
                    null!,
                    CancellationToken.None))).ParamName);
    }

    [Fact]
    public async Task ReceiveAsync_PreservesQueryAuthenticatesAndParsesResponse()
    {
        var credential = new TestTokenCredential("runtime-token");
        string? authorization = null;
        var handler = new SequenceHttpMessageHandler(request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            var response = JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "messages": [
                    {
                      "messageId": "message-1",
                      "lockToken": "lock-1",
                      "outputs": { "body": { "value": 1 } }
                    },
                    {
                      "messageId": "message-2",
                      "lockToken": "lock-2",
                      "outputsLink": {
                        "uri": "https://content.test/output?signature=secret"
                      }
                    }
                  ]
                }
                """);
            response.Headers.Add(
                ConnectorPollDeliveryClient.MoreMessagesAvailableHeader,
                "true");
            return response;
        });
        ConnectorPollDeliveryClient client = CreateClient(credential, handler);

        ConnectorReceiveResult result = await client.ReceiveAsync(
            Endpoints("https://runtime.test/receive?api-version=1"),
            7,
            CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.True(result.MoreMessagesAvailable);
        Assert.NotNull(result.Messages[0].Outputs);
        Assert.NotNull(result.Messages[1].OutputsLink);
        Assert.Equal(
            "https://runtime.test/receive?api-version=1&maxEvents=7",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer runtime-token", authorization);
        Assert.Equal(
            new[] { ConnectorPollDeliveryClient.ApiHubScope },
            Assert.Single(credential.RequestedScopes));
    }

    [Fact]
    public async Task ReceiveAsync_ReplacesExistingMaxEventsParameter()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """{"messages":[]}"""));
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        await client.ReceiveAsync(
            Endpoints(
                "https://runtime.test/receive?api-version=1&maxEvents=32"),
            4,
            CancellationToken.None);

        Assert.Equal(
            "https://runtime.test/receive?api-version=1&maxEvents=4",
            Assert.Single(handler.Requests).RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public async Task ReceiveAsync_RejectsInvalidMaxEvents(int maxEvents)
    {
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            new SequenceHttpMessageHandler(_ =>
                throw new InvalidOperationException("Request was not expected.")));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.ReceiveAsync(
                Endpoints(),
                maxEvents,
                CancellationToken.None));
    }

    [Fact]
    public async Task ReceiveAsync_DoesNotRetryAmbiguousFailure()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.InternalServerError, "{}"));
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        await Assert.ThrowsAsync<ConnectorPollDeliveryException>(() =>
            client.ReceiveAsync(
                Endpoints(),
                1,
                CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ReceiveAsync_DoesNotRetryTransportTimeout()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("timeout")));
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        ConnectorPollDeliveryException exception =
            await Assert.ThrowsAsync<ConnectorPollDeliveryException>(() =>
                client.ReceiveAsync(
                    Endpoints(),
                    1,
                    CancellationToken.None));

        Assert.Contains("timed out", exception.Message);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ReceiveAsync_RejectsMalformedMoreMessagesHeader()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
        {
            HttpResponseMessage response = JsonResponse(
                HttpStatusCode.OK,
                """{"messages":[]}""");
            response.Headers.Add(
                ConnectorPollDeliveryClient.MoreMessagesAvailableHeader,
                "sometimes");
            return response;
        });
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        await Assert.ThrowsAsync<ConnectorPollDeliveryException>(() =>
            client.ReceiveAsync(
                Endpoints(),
                1,
                CancellationToken.None));
    }

    [Fact]
    public async Task AcknowledgeAsync_SerializesLocksAndParsesMixedStatuses()
    {
        string? requestBody = null;
        string? authorization = null;
        var handler = new SequenceHttpMessageHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync()
                .GetAwaiter().GetResult();
            authorization = request.Headers.Authorization?.ToString();
            return JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "results": [
                    { "messageId": "message-1", "status": "Acknowledged" },
                    { "messageId": "message-2", "status": "NotFound" }
                  ]
                }
                """);
        });
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential("runtime-token"),
            handler);
        ConnectorMessageLock[] locks =
        [
            new("message-1", "lock-1"),
            new("message-2", "lock-2"),
        ];

        ConnectorAcknowledgeResult result = await client.AcknowledgeAsync(
            Endpoints(),
            locks,
            CancellationToken.None);

        Assert.Equal(
            ConnectorAcknowledgeStatus.Acknowledged,
            result.Results[0].Status);
        Assert.Equal(
            ConnectorAcknowledgeStatus.NotFound,
            result.Results[1].Status);
        Assert.Equal("Bearer runtime-token", authorization);
        Assert.Contains("\"lockToken\":\"lock-1\"", requestBody);
        Assert.Contains("\"lockToken\":\"lock-2\"", requestBody);
    }

    [Fact]
    public async Task AcknowledgeAsync_DoesNotRetryAmbiguousFailure()
    {
        var handler = new SequenceHttpMessageHandler(_ =>
            JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"));
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        await Assert.ThrowsAsync<ConnectorPollDeliveryException>(() =>
            client.AcknowledgeAsync(
                Endpoints(),
                [new ConnectorMessageLock("message", "lock")],
                CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task AcknowledgeAsync_DoesNotRetryTransportTimeout()
    {
        var handler = new AsyncSequenceHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("timeout")));
        ConnectorPollDeliveryClient client = CreateClient(
            new TestTokenCredential(),
            handler);

        ConnectorPollDeliveryException exception =
            await Assert.ThrowsAsync<ConnectorPollDeliveryException>(() =>
                client.AcknowledgeAsync(
                    Endpoints(),
                    [new ConnectorMessageLock("message", "lock")],
                    CancellationToken.None));

        Assert.Contains("timed out", exception.Message);
        Assert.Equal(1, handler.CallCount);
    }

    private static ConnectorPollDeliveryClient CreateClient(
        TestTokenCredential credential,
        HttpMessageHandler handler) =>
        new(
            credential,
            new TestHttpClientFactory(handler));

    private static ConnectorPollingEndpoints Endpoints(
        string receiveUri = "https://runtime.test/receive") =>
        new(
            new Uri(receiveUri),
            new Uri("https://runtime.test/ack"),
            new Uri("https://runtime.test/depth"));

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string json) =>
        new(statusCode)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"),
        };

}
