// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal sealed class ConnectorLinkedOutputInvocationLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    internal async Task<IDisposable> AcquireAsync(
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Lease(_semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
