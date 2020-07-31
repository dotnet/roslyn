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

        internal string PipeName { get; }
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

            IsListening = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _queue = new AsyncQueue<ListenResult>();

            int clientLoggingIdentifier = 0;
            var listenCount = Math.Min(4, Environment.ProcessorCount);
            var listenTasks = new List<Task>(capacity: listenCount);
            for (int i = 0; i< listenCount; i++)
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
            while (_queue.TryDequeue(out var connectionResult))
            {
                connectionResult.NamedPipeClientConnection?.Dispose();
            }

            _queue = null;
            _cancellationTokenSource = null;
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

            Debug.Assert(listenResult.NamedPipeClientConnection is object);
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
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Create the pipe and begin waiting for a connection. This 
                    // doesn't block, but could fail in certain circumstances, such
                    // as Windows refusing to create the pipe for some reason 
                    // (out of handles?), or the pipe was disconnected before we 
                    // starting listening.
                    CompilerServerLogger.Log("Constructing pipe '{0}'.", pipeName);
                    var pipeStream = NamedPipeUtil.CreateServer(pipeName);
                    CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", pipeName);

                    CompilerServerLogger.Log("Waiting for new connection");
                    await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    CompilerServerLogger.Log("Pipe connection detected.");

                    var connection = new NamedPipeClientConnection(pipeStream, getClientLoggingIdentifier());
                    queue.Enqueue(new ListenResult(connection: connection));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the host is shutting down.
            }
            catch (Exception ex)
            {
                queue.Enqueue(new ListenResult(exception: ex));
            }
        }
    }
}
