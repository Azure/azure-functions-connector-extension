// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Functions.Extensions.Connector;

// Only for replay-safe operations; never apply to Receive or Acknowledge.
internal sealed class ConnectorPollingRetryPolicy
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public ConnectorPollingRetryPolicy()
        : this(Task.Delay)
    {
    }

    internal ConnectorPollingRetryPolicy(
        Func<TimeSpan, CancellationToken, Task> delayAsync) =>
        _delayAsync = delayAsync ??
            throw new ArgumentNullException(nameof(delayAsync));

    internal async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        using var timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        CancellationToken operationToken = timeoutSource.Token;
        var elapsed = Stopwatch.StartNew();

        for (int attempt = 1; ; attempt++)
        {
            operationToken.ThrowIfCancellationRequested();
            try
            {
                T result = await operation(operationToken).ConfigureAwait(false);
                operationToken.ThrowIfCancellationRequested();
                return result;
            }
            catch (Exception exception) when (
                attempt < ConnectorPollingHttpConstants.MaximumSafeOperationAttempts &&
                !operationToken.IsCancellationRequested &&
                IsTransient(exception))
            {
                TimeSpan delay =
                    ConnectorPollingHttpConstants.BaseRetryDelay * attempt;
                if (exception is ConnectorPollingHttpException httpException)
                {
                    TimeSpan? serverDelay =
                        httpException.RetryAfter?.Delta ??
                        httpException.RetryAfter?.Date - DateTimeOffset.UtcNow;
                    if (serverDelay > delay)
                    {
                        delay = serverDelay.Value;
                    }
                }

                // Do not shorten Retry-After or start a fresh timeout per attempt.
                if (delay >= timeout - elapsed.Elapsed)
                {
                    throw;
                }

                await _delayAsync(delay, operationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception exception) =>
        exception is IOException ||
        exception is HttpRequestException requestException &&
        requestException.StatusCode is null or
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
}

internal sealed class ConnectorPollingHttpException(
    HttpStatusCode statusCode,
    RetryConditionHeaderValue? retryAfter)
    : HttpRequestException(
        "Connector Poll HTTP request failed.",
        inner: null,
        statusCode: statusCode)
{
    internal RetryConditionHeaderValue? RetryAfter { get; } = retryAfter;
}
