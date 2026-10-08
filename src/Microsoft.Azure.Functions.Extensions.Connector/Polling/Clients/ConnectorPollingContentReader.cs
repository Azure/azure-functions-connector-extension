// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers;

namespace Microsoft.Azure.Functions.Extensions.Connector;

internal static class ConnectorPollingContentReader
{
    private const int BufferSize = 81920;
    private const int InitialCapacity = 4096;

    internal static async Task<BinaryData> ReadAsync(
        HttpContent content,
        int maximumSizeInBytes,
        Func<string, Exception> createException,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumSizeInBytes);
        ArgumentNullException.ThrowIfNull(createException);

        if (content.Headers.ContentLength is long contentLength &&
            contentLength > maximumSizeInBytes)
        {
            throw createException(
                $"response exceeded the {maximumSizeInBytes}-byte limit");
        }

        await using Stream input = await content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream(
            Math.Min(
                maximumSizeInBytes,
                content.Headers.ContentLength is long declaredLength
                    ? checked((int)declaredLength)
                    : InitialCapacity));
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            int totalBytesRead = 0;
            while (true)
            {
                int bytesRead = await input.ReadAsync(
                    buffer.AsMemory(0, BufferSize),
                    cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                if (bytesRead > maximumSizeInBytes - totalBytesRead)
                {
                    throw createException(
                        $"response exceeded the {maximumSizeInBytes}-byte limit");
                }

                await output.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
            }

            return new BinaryData(
                output.GetBuffer().AsMemory(
                    0,
                    checked((int)output.Length)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
