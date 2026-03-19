// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
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
        private Task[]? _listenTasks;
        private AsyncQueue<ListenResult>? _queue;

        public string PipeName { get; }
        public ICompilerServerLogger Logger { get; }
        public bool IsListening { get; private set; }

        internal NamedPipeClientConnectionHost(string pipeName, ICompilerServerLogger logger)
        {
            PipeName = pipeName;
            Logger = logger;
        }

        public void BeginListening()
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(_cancellationTokenSource is null);
            Debug.Assert(_listenTasks is null);
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
            _listenTasks = new Task[listenCount];
            for (int i = 0; i < listenCount; i++)
            {
                var task = Task.Run(() => ListenCoreAsync(PipeName, Logger, _queue, _cancellationTokenSource.Token));
                _listenTasks[i] = task;
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
            Debug.Assert(_listenTasks is object);

            try
            {
                // Even though the Tasks created to run the compilation servers can never throw, 
                // the CancellationToken from this source ends up getting passed throughout the 
                // named pipe infrastructure. Parts of that infrastructure hook into 
                // CancellationToken.Register and those will throw during a Cancel operation. 
                //
                // Most notably of these is IOCancellationHelper.Cancel. This has a race where it
                // will try to cancel IO on a disposed SafeHandle. That causes an ObjectDisposedException
                // to propagate out from the Cancel method here.
                //
                // There is no good way to guard against this hence we just have to accept it as a
                // possible outcome.
                _cancellationTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Cancelling server listens threw an exception");
            }

            try
            {
                Task.WaitAll(_listenTasks);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Listen tasks threw exception during {nameof(EndListening)}");
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
            _listenTasks = null;
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
            ICompilerServerLogger logger,
            AsyncQueue<ListenResult> queue,
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
                    logger.Log($"Constructing pipe and waiting for connections '{pipeName}'");
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
                    logger.Log("Pipe connection established.");
                    var connection = new NamedPipeClientConnection(pipeStream, logger);
                    queue.Enqueue(new ListenResult(connection: connection));
                }
                catch (OperationCanceledException)
                {
                    // Expected when the host is shutting down.
                    logger.Log($"Pipe connection cancelled");
                    pipeStream?.Dispose();
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, $"Pipe connection error");
                    queue.Enqueue(new ListenResult(exception: ex));
                    pipeStream?.Dispose();
                }
            }
        }
    }
}
