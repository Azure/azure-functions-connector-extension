// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingProtocolTests
{
    [Fact]
    public void DeserializeReceive_ParsesInlineOutputs()
    {
        BinaryData content = BinaryData.FromString(
            """
            {
              "messages": [
                {
                  "messageId": "message-1",
                  "lockToken": "lock-1",
                  "outputs": {
                    "headers": { "content-type": "application/json" },
                    "body": { "id": "123" }
                  }
                }
              ]
            }
            """);

        ConnectorReceiveResult result =
            ConnectorPollingProtocol.DeserializeReceive(
                content,
                moreMessagesAvailable: true);

        ConnectorPollMessage message = Assert.Single(result.Messages);
        Assert.Equal("message-1", message.MessageId);
        Assert.Equal("lock-1", message.LockToken);
        Assert.NotNull(message.Outputs);
        Assert.Null(message.OutputsLink);
        Assert.True(result.MoreMessagesAvailable);

        using JsonDocument outputs = JsonDocument.Parse(message.Outputs);
        Assert.Equal(
            "123",
            outputs.RootElement.GetProperty("body").GetProperty("id").GetString());
    }

    [Fact]
    public void DeserializeReceive_ParsesLinkedOutputs()
    {
        const string signedUri =
            "https://example.test/content/item?sig=sensitive-signature";
        BinaryData content = BinaryData.FromString(
            $$"""
            {
              "messages": [
                {
                  "messageId": "message-1",
                  "lockToken": "lock-1",
                  "outputsLink": {
                    "uri": "{{signedUri}}"
                  }
                }
              ]
            }
            """);

        ConnectorReceiveResult result =
            ConnectorPollingProtocol.DeserializeReceive(
                content,
                moreMessagesAvailable: false);

        ConnectorPollMessage message = Assert.Single(result.Messages);
        Assert.Null(message.Outputs);
        Assert.Equal(signedUri, message.OutputsLink!.Uri.AbsoluteUri);
        Assert.DoesNotContain(
            "sensitive-signature",
            message.OutputsLink.ToString());
    }

    [Fact]
    public void DeserializeReceive_ParsesEmptyMessages()
    {
        ConnectorReceiveResult result =
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString("""{"messages":[]}"""),
                moreMessagesAvailable: false);

        Assert.Empty(result.Messages);
        Assert.False(result.MoreMessagesAvailable);
    }

    [Fact]
    public void DeserializeReceive_AcceptsExplicitNullForUnusedOutputSource()
    {
        ConnectorReceiveResult result =
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString(
                    """
                    {
                      "messages": [
                        {
                          "messageId": "message-1",
                          "lockToken": "lock-1",
                          "outputs": {},
                          "outputsLink": null
                        }
                      ]
                    }
                    """),
                moreMessagesAvailable: false);

        ConnectorPollMessage message = Assert.Single(result.Messages);
        Assert.NotNull(message.Outputs);
        Assert.Null(message.OutputsLink);
    }

    [Theory]
    [InlineData("""{"messages":[{"messageId":"id","lockToken":"lock"}]}""")]
    [InlineData("""{"messages":[{"messageId":"id","lockToken":"lock","outputs":{},"outputsLink":{"uri":"https://example.test/content"}}]}""")]
    [InlineData("""{"messages":[{"messageId":"id","lockToken":"lock","outputs":null,"outputsLink":null}]}""")]
    public void DeserializeReceive_RejectsInvalidOutputSourceCombinations(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString(json),
                moreMessagesAvailable: false));

        Assert.Contains("exactly one", exception.Message);
    }

    [Fact]
    public void DeserializeReceive_RejectsNullMessageItem()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString("""{"messages":[null]}"""),
                moreMessagesAvailable: false));

        Assert.Contains("index 0", exception.Message);
        Assert.Contains("JSON object", exception.Message);
    }

    [Theory]
    [InlineData("""{"messages":[{"messageId":"","lockToken":"secret-lock","outputs":{}}]}""", "messageId")]
    [InlineData("""{"messages":[{"messageId":"id","lockToken":"","outputs":{}}]}""", "lockToken")]
    [InlineData("""{"messages":[{"messageId":"id","lockToken":"secret-lock","outputs":[]}]}""", "outputs")]
    public void DeserializeReceive_RejectsInvalidMessageFields(
        string json,
        string expectedProperty)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString(json),
                moreMessagesAvailable: false));

        Assert.Contains(expectedProperty, exception.Message);
        Assert.DoesNotContain("secret-lock", exception.ToString());
    }

    [Theory]
    [InlineData("http://example.test/content?sig=sensitive")]
    [InlineData("relative/content?sig=sensitive")]
    [InlineData("https://user@example.test/content?sig=sensitive")]
    [InlineData("https://example.test/content?sig=sensitive#fragment")]
    public void DeserializeReceive_RejectsUnsafeLinkedOutputUriWithoutDisclosure(
        string uri)
    {
        string json =
            $$"""
            {
              "messages": [
                {
                  "messageId": "id",
                  "lockToken": "secret-lock",
                  "outputsLink": {
                    "uri": "{{uri}}"
                  }
                }
              ]
            }
            """;

        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString(json),
                moreMessagesAvailable: false));

        Assert.Contains("outputsLink.uri", exception.Message);
        Assert.DoesNotContain("sensitive", exception.ToString());
        Assert.DoesNotContain("secret-lock", exception.ToString());
    }

    [Fact]
    public void DeserializeReceive_RejectsMoreThanMaximumBatchSize()
    {
        string messages = string.Join(
            ",",
            Enumerable.Range(
                0,
                ConnectorPollingProtocolLimits.MaximumBatchSize + 1)
            .Select(index =>
                $"{{\"messageId\":\"message-{index}\",\"lockToken\":\"lock-{index}\",\"outputs\":{{}}}}"));
        string json = $$"""{"messages":[{{messages}}]}""";

        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                BinaryData.FromString(json),
                moreMessagesAvailable: false));

        Assert.Contains("must not contain more than", exception.Message);
    }

    [Fact]
    public void DeserializeReceive_InvalidJsonDoesNotExposeContent()
    {
        const string secret = "sensitive-lock-token";
        BinaryData content = BinaryData.FromString(
            $$"""{"messages":[{"lockToken":"{{secret}}" """);

        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeReceive(
                content,
                moreMessagesAvailable: false));

        Assert.DoesNotContain(secret, exception.ToString());
    }

    [Fact]
    public void SerializeAcknowledgeRequest_UsesCanonicalWireShape()
    {
        var locks = new[]
        {
            new ConnectorMessageLock("message-1", "lock-1"),
            new ConnectorMessageLock("message-2", "lock-2"),
        };

        BinaryData content =
            ConnectorPollingProtocol.SerializeAcknowledgeRequest(locks);

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement messages = document.RootElement.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal(
            "message-1",
            messages[0].GetProperty("messageId").GetString());
        Assert.Equal(
            "lock-1",
            messages[0].GetProperty("lockToken").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void SerializeAcknowledgeRequest_RejectsInvalidBatchSize(int count)
    {
        ConnectorMessageLock[] locks = Enumerable
            .Range(0, count)
            .Select(index => new ConnectorMessageLock(
                $"message-{index}",
                $"lock-{index}"))
            .ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConnectorPollingProtocol.SerializeAcknowledgeRequest(locks));
    }

    [Fact]
    public void DeserializeAcknowledge_ParsesMixedAndUnknownStatuses()
    {
        var locks = new[]
        {
            new ConnectorMessageLock("message-1", "lock-1"),
            new ConnectorMessageLock("message-2", "lock-2"),
            new ConnectorMessageLock("message-3", "lock-3"),
            new ConnectorMessageLock("message-4", "lock-4"),
        };
        BinaryData content = BinaryData.FromString(
            """
            {
              "results": [
                { "messageId": "message-1", "status": "Acknowledged" },
                { "messageId": "message-2", "status": "NotFound" },
                { "messageId": "message-3", "status": "Failed" },
                { "messageId": "message-4", "status": "Deferred" }
              ]
            }
            """);

        ConnectorAcknowledgeResult result =
            ConnectorPollingProtocol.DeserializeAcknowledge(content, locks);

        Assert.Equal(4, result.Results.Count);
        Assert.True(result.Results[0].IsAcknowledged);
        Assert.Equal(
            ConnectorAcknowledgeStatus.NotFound,
            result.Results[1].Status);
        Assert.Equal(
            ConnectorAcknowledgeStatus.Failed,
            result.Results[2].Status);
        Assert.Equal("Deferred", result.Results[3].Status.ToString());
        Assert.False(result.Results[3].Status.IsKnown);
    }

    [Theory]
    [InlineData(
        """{"results":[]}""",
        "result count")]
    [InlineData(
        """{"results":[{"messageId":"different","status":"Acknowledged"}]}""",
        "does not match")]
    public void DeserializeAcknowledge_RejectsBrokenCorrelation(
        string json,
        string expectedMessage)
    {
        var locks = new[]
        {
            new ConnectorMessageLock("message-1", "secret-lock"),
        };

        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeAcknowledge(
                BinaryData.FromString(json),
                locks));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.DoesNotContain("secret-lock", exception.ToString());
    }

    [Fact]
    public void DeserializeAcknowledge_RejectsInvalidSubmittedLocksBeforeParsing()
    {
        ConnectorMessageLock[] locks = [null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            ConnectorPollingProtocol.DeserializeAcknowledge(
                BinaryData.FromString("""{"results":[]}"""),
                locks));

        Assert.Contains("index 0", exception.Message);
    }

    [Fact]
    public void DeserializeAcknowledge_RejectsNullResultItem()
    {
        var locks = new[]
        {
            new ConnectorMessageLock("message-1", "secret-lock"),
        };

        JsonException exception = Assert.Throws<JsonException>(() =>
            ConnectorPollingProtocol.DeserializeAcknowledge(
                BinaryData.FromString("""{"results":[null]}"""),
                locks));

        Assert.Contains("index 0", exception.Message);
        Assert.Contains("JSON object", exception.Message);
        Assert.DoesNotContain("secret-lock", exception.ToString());
    }

}
