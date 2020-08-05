// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.Security.AccessControl;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        private readonly struct ListenResult
        {
            internal NamedPipeClientConnection? NamedPipeClientConnection { get; }
            internal Exception? Exception { get; }

            internal ListenResult(NamedPipeClientConnection? connection = null, Exception? exception = null)
            {
                Debug.Assert(connection is null || exception is null);
                NamedPipeClientConnection = connection;
                Exception = exception;
            }
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private ImmutableArray<Task> _listenTasks;
        private AsyncQueue<ListenResult>? _queue;

        public string PipeName { get; }
        public bool IsListening { get; private set; }

        internal NamedPipeClientConnectionHost(string pipeName)
        {
            PipeName = pipeName;
        }

        public void BeginListening()
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is null);
            Debug.Assert(_listenTasks.IsDefault);
            Debug.Assert(_queue is null);

            IsListening = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _queue = new AsyncQueue<ListenResult>();

            // The choice of 4 here is a bit arbitrary. The compiler server needs to scale to the number of clients that 
            // msbuild is going to attempt to connect here and be able to establish each connection in one second. In the 
            // majority of cases even one is enough to accomplish this. Four though gives us enough wiggle room to handle
            // severe load scenarios.
            // 
            // Should you ever want to change this number in the future make sure to test the new values on sufficiently
            // large builds such as dotnet/roslyn or dotnet/runtime
            var listenCount = Math.Min(4, Environment.ProcessorCount);
            var listenTasks = new List<Task>(capacity: listenCount);
            int clientLoggingIdentifier = 0;
            for (int i = 0; i < listenCount; i++)
            {
                var task = Task.Run(() => ListenCoreAsync(PipeName, _queue, GetNextClientLoggingIdentifier, _cancellationTokenSource.Token));
                listenTasks.Add(task);
            }
            _listenTasks = ImmutableArray.CreateRange(listenTasks);

            string GetNextClientLoggingIdentifier()
            {
                var count = Interlocked.Increment(ref clientLoggingIdentifier);
                return $"Client{count}";
            }
        }

        public void EndListening()
        {
            if (!IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is object);
            Debug.Assert(_queue is object);
            Debug.Assert(!_listenTasks.IsDefault);

            _cancellationTokenSource.Cancel();
            try
            {
                Task.WaitAll(_listenTasks.ToArray());
            }
            catch (Exception ex)
            {
                CompilerServerLogger.LogException(ex, "Listen tasks threw exception during EndListen");
            }

            _queue.Complete();
            _queue.WhenCompletedTask.Wait();

            // Anything left in the AsyncQueue after completion will not be handled by the client
            // and must be cleaned up by the host.
            while (_queue.TryDequeue(out var connectionResult))
            {
                connectionResult.NamedPipeClientConnection?.Dispose();
            }

            _queue = null;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _listenTasks = default;
            IsListening = false;
        }

        public async Task<IClientConnection> GetNextClientConnectionAsync()
        {
            if (!IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is object);
            Debug.Assert(_queue is object);

            var listenResult = await _queue.DequeueAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            if (listenResult.Exception is object)
            {
                throw new Exception("Error occurred listening for connections", listenResult.Exception);
            }

            if (listenResult.NamedPipeClientConnection is null)
            {
                // The AsyncQueue<> implementation will resolve all out-standing waiters as default 
                // when Complete is called. Treat that as cancellation from the perspective of our
                // callers
                throw new OperationCanceledException();
            }

            return listenResult.NamedPipeClientConnection;
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private static async Task ListenCoreAsync(
            string pipeName,
            AsyncQueue<ListenResult> queue,
            Func<string> getClientLoggingIdentifier,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NamedPipeServerStream? pipeStream = null;

                try
                {
                    // Create the pipe and begin waiting for a connection. This 
                    // doesn't block, but could fail in certain circumstances, such
                    // as Windows refusing to create the pipe for some reason 
                    // (out of handles?), or the pipe was disconnected before we 
                    // starting listening
                    CompilerServerLogger.Log($"Constructing pipe and waiting for connections '{pipeName}'");
                    pipeStream = NamedPipeUtil.CreateServer(pipeName);

                    // The WaitForConnectionAsync API does not fully respect the provided CancellationToken
                    // on all platforms:
                    //
                    //  https://github.com/dotnet/runtime/issues/40289
                    //
                    // To mitigate this we need to setup a cancellation Task and dispose the NamedPipeServerStream
                    // if it ever completes. Once all of the NamedPipeServerStream for the given pipe name are
                    // disposed they will all exit the WaitForConnectionAsync method
                    var connectTask = pipeStream.WaitForConnectionAsync(cancellationToken);
                    if (!PlatformInformation.IsWindows)
                    {
                        var cancelTask = Task.Delay(TimeSpan.FromMilliseconds(-1), cancellationToken);
                        var completedTask = await Task.WhenAny(new[] { connectTask, cancelTask }).ConfigureAwait(false);
                        if (completedTask == cancelTask)
                        {
                            throw new OperationCanceledException();
                        }
                    }

                    await connectTask.ConfigureAwait(false);
                    CompilerServerLogger.Log("Pipe connection established.");
                    var connection = new NamedPipeClientConnection(pipeStream, getClientLoggingIdentifier());
                    queue.Enqueue(new ListenResult(connection: connection));
                }
                catch (OperationCanceledException)
                {
                    // Expected when the host is shutting down.
                    CompilerServerLogger.Log($"Pipe connection cancelled");
                    pipeStream?.Dispose();
                }
                catch (Exception ex)
                {
                    CompilerServerLogger.LogException(ex, $"Pipe connection error");
                    queue.Enqueue(new ListenResult(exception: ex));
                    pipeStream?.Dispose();
                }
            }
        }
    }
}
