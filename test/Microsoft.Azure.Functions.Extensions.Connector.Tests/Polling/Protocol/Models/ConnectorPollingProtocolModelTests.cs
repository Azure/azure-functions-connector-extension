// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingProtocolModelTests
{
    [Fact]
    public void PollingEndpoints_RedactsQueriesFromToString()
    {
        var endpoints = new ConnectorPollingEndpoints(
            new Uri("https://example.test/receive?sig=receive-secret"),
            new Uri("https://example.test/acknowledge?sig=ack-secret"),
            new Uri("https://example.test/hasMessages?sig=has-secret"),
            new Uri("https://example.test/depth?sig=depth-secret"));

        string value = endpoints.ToString();

        Assert.Contains("https://example.test/[REDACTED]?[REDACTED]", value);
        Assert.DoesNotContain("/receive", value);
        Assert.DoesNotContain("/acknowledge", value);
        Assert.DoesNotContain("/hasMessages", value);
        Assert.DoesNotContain("/depth", value);
        Assert.DoesNotContain("receive-secret", value);
        Assert.DoesNotContain("ack-secret", value);
        Assert.DoesNotContain("has-secret", value);
        Assert.DoesNotContain("depth-secret", value);
    }

    [Theory]
    [InlineData("http://example.test/path")]
    [InlineData("relative/path")]
    [InlineData("https://user@example.test/path")]
    [InlineData("https://example.test/path#fragment")]
    public void PollingEndpoints_RejectsUnsafeUris(string uri)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new ConnectorPollingEndpoints(
                new Uri(uri, UriKind.RelativeOrAbsolute),
                new Uri("https://example.test/acknowledge"),
                new Uri("https://example.test/hasMessages"),
                new Uri("https://example.test/depth")));

        Assert.DoesNotContain(uri, exception.Message);
    }

    [Fact]
    public void OutputsLink_RedactsSignedUriFromToString()
    {
        var outputsLink = new ConnectorOutputsLink(
            new Uri("https://example.test/content/item?sig=signed-secret"));

        string value = outputsLink.ToString();

        Assert.Contains("https://example.test/[REDACTED]?[REDACTED]", value);
        Assert.DoesNotContain("/content/item", value);
        Assert.DoesNotContain("signed-secret", value);
    }

    [Fact]
    public void ProtocolLimits_UsesConfirmedMaximumOutputsPayloadSize()
    {
        Assert.Equal(
            104_857_600,
            ConnectorPollingProtocolLimits.MaximumOutputsPayloadSizeInBytes);
    }

    [Fact]
    public void MessageAndLock_ToStringRedactsLockTokenAndPayload()
    {
        const string lockToken = "sensitive-lock-token";
        const string payloadSecret = "sensitive-payload";
        var message = ConnectorPollMessage.FromInlineOutputs(
            "message-1",
            lockToken,
            BinaryData.FromString($"{{\"secret\":\"{payloadSecret}\"}}"));

        string messageValue = message.ToString();
        string lockValue = message.MessageLock.ToString();

        Assert.DoesNotContain(lockToken, messageValue);
        Assert.DoesNotContain(payloadSecret, messageValue);
        Assert.DoesNotContain(lockToken, lockValue);
        Assert.DoesNotContain("message-1", messageValue);
        Assert.DoesNotContain("message-1", lockValue);
        Assert.Contains("[REDACTED]", messageValue);
        Assert.Contains("[REDACTED]", lockValue);
    }

    [Fact]
    public void PollMessage_CopiesInlineOutputs()
    {
        byte[] source = "{}"u8.ToArray();
        var message = ConnectorPollMessage.FromInlineOutputs(
            "message-1",
            "lock-1",
            new BinaryData(source));

        source[0] = (byte)'[';

        Assert.Equal("{}", message.Outputs!.ToString());
    }

    [Fact]
    public void AcknowledgeStatus_PreservesUnknownValues()
    {
        ConnectorAcknowledgeStatus status =
            ConnectorAcknowledgeStatus.FromWireValue("Deferred");

        Assert.False(status.IsKnown);
        Assert.False(status.IsAcknowledged);
        Assert.Equal("Deferred", status.ToString());
    }

    [Fact]
    public void AcknowledgeStatus_MatchesKnownValuesCaseInsensitively()
    {
        ConnectorAcknowledgeStatus status =
            ConnectorAcknowledgeStatus.FromWireValue("acknowledged");

        Assert.True(status.IsKnown);
        Assert.True(status.IsAcknowledged);
        Assert.Equal(ConnectorAcknowledgeStatus.Acknowledged, status);
    }

    [Fact]
    public void AcknowledgeItemResult_ToStringDoesNotExposeUnknownStatus()
    {
        const string untrustedStatus = "FutureStatus\r\nInjected-Log: value";
        var result = new ConnectorAcknowledgeItemResult(
            "message-1",
            ConnectorAcknowledgeStatus.FromWireValue(untrustedStatus));

        string value = result.ToString();

        Assert.Contains("[UNKNOWN]", value);
        Assert.DoesNotContain("message-1", value);
        Assert.DoesNotContain(untrustedStatus, value);
        Assert.DoesNotContain("Injected-Log", value);
    }

    [Fact]
    public void ResultModels_CopyInputCollections()
    {
        var messages = new List<ConnectorPollMessage>
        {
            ConnectorPollMessage.FromInlineOutputs(
                "message-1",
                "lock-1",
                BinaryData.FromString("{}")),
        };
        var acknowledgementItems = new List<ConnectorAcknowledgeItemResult>
        {
            new(
                "message-1",
                ConnectorAcknowledgeStatus.Acknowledged),
        };

        var receiveResult = new ConnectorReceiveResult(
            messages,
            moreMessagesAvailable: false);
        var acknowledgeResult = new ConnectorAcknowledgeResult(
            acknowledgementItems);
        messages.Clear();
        acknowledgementItems.Clear();

        Assert.Single(receiveResult.Messages);
        Assert.Single(acknowledgeResult.Results);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ConnectorPollMessage>)receiveResult.Messages).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ConnectorAcknowledgeItemResult>)acknowledgeResult.Results).Clear());
    }

    [Fact]
    public void ResultModels_RejectNullItems()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConnectorReceiveResult(
                [null!],
                moreMessagesAvailable: false));
        Assert.Throws<ArgumentException>(() =>
            new ConnectorAcknowledgeResult([null!]));
    }
}
