// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector.Tests;

public class ConnectorPollingRetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_BoundsAttemptsAndBackoff()
    {
        var delays = new List<TimeSpan>();
        var policy = new ConnectorPollingRetryPolicy((delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });
        int attempts = 0;
        var failure = new HttpRequestException("transport failure");

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            policy.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw failure;
                },
                TimeSpan.FromSeconds(30),
                CancellationToken.None));

        Assert.Same(failure, exception);
        Assert.Equal(3, attempts);
        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200) },
            delays);
    }

    [Fact]
    public async Task ExecuteAsync_SharesDeadlineTokenAcrossAttempts()
    {
        var tokens = new List<CancellationToken>();
        var policy = new ConnectorPollingRetryPolicy((_, token) =>
        {
            tokens.Add(token);
            return Task.CompletedTask;
        });
        int attempts = 0;

        int result = await policy.ExecuteAsync(
            token =>
            {
                tokens.Add(token);
                if (++attempts < 3)
                {
                    throw new HttpRequestException("transport failure");
                }

                return Task.FromResult(17);
            },
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(17, result);
        Assert.Equal(5, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(tokens[0], token));
    }

    [Fact]
    public async Task ExecuteAsync_RetriesInterruptedReads()
    {
        var policy = new ConnectorPollingRetryPolicy((_, _) => Task.CompletedTask);
        int attempts = 0;

        int result = await policy.ExecuteAsync(
            _ => ++attempts == 1
                ? Task.FromException<int>(new IOException("interrupted response"))
                : Task.FromResult(12),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(12, result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryNonTransientFailure()
    {
        var policy = new ConnectorPollingRetryPolicy((_, _) =>
            throw new InvalidOperationException("Delay was not expected."));
        int attempts = 0;
        var failure = new InvalidOperationException("configuration failure");

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                policy.ExecuteAsync<int>(
                    _ =>
                    {
                        attempts++;
                        throw failure;
                    },
                    TimeSpan.FromSeconds(30),
                    CancellationToken.None));

        Assert.Same(failure, exception);
        Assert.Equal(1, attempts);
    }
}
