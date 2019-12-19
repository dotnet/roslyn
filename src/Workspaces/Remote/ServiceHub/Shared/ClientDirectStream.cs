// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Direct stream between server and client to pass around big chunk of data
    /// </summary>
    internal static class ClientDirectStream
    {
        // 4KB buffer size
        private const int BufferSize = 4 * 1024;
        private const int ConnectWithoutTimeout = 1;
        private const int MaxRetryAttemptsForFileNotFoundException = 3;
        private const int ErrorSemTimeoutHResult = unchecked((int)0x80070079);

        private static readonly TimeSpan s_connectRetryInterval = TimeSpan.FromMilliseconds(20);

        public static async Task WriteDataAsync<TData>(string streamName, TData data, Func<ObjectWriter, TData, CancellationToken, Task> dataWriter, CancellationToken cancellationToken)
        {
            try
            {
                var pipe = new NamedPipeClientStream(serverName: ".", pipeName: streamName, PipeDirection.Out);

                bool success = false;
                try
                {
                    await ConnectPipeAsync(pipe, cancellationToken).ConfigureAwait(false);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        pipe.Dispose();
                    }
                }

                // Transfer ownership of the pipe to BufferedStream, it will dispose it:
                using var stream = new BufferedStream(pipe, BufferSize);

                using (var objectWriter = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                {
                    await dataWriter(objectWriter, data, cancellationToken).ConfigureAwait(false);
                }

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // The stream has closed before we had chance to check cancellation.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static async Task ConnectPipeAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    // Try connecting without wait.
                    // Connecting with anything else will consume CPU causing a spin wait.
                    pipe.Connect(ConnectWithoutTimeout);
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // Prefer to throw OperationCanceledException if the caller requested cancellation.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
                catch (IOException ex) when (ex.HResult == ErrorSemTimeoutHResult)
                {
                    // Ignore and retry.
                }
                catch (TimeoutException)
                {
                    // Ignore and retry.
                }
                catch (FileNotFoundException) when (retryCount < MaxRetryAttemptsForFileNotFoundException)
                {
                    // Ignore and retry
                    retryCount++;
                }

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await Task.Delay(s_connectRetryInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // To be consistent as to what type of exception is thrown when cancellation is requested,
                    // always throw OperationCanceledException.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }
        }
    }
}
