// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// BTODO: separate file
    /// The interface used by <see cref="ServerDispatcher"/> to dispatch requests.
    /// </summary>
    internal interface IRequestHandler
    {
        BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken);
    }

    /// <summary>
    /// This class handles the named pipe creation, listening, thread creation,
    /// and so forth. When a request comes in, it is dispatched on a new thread
    /// to the <see cref="IRequestHandler"/> interface. The request handler does the actual
    /// compilation. This class itself has no dependencies on the compiler.
    /// </summary>
    /// <remarks>
    /// One instance of this is created per process.
    /// </remarks>
    public partial class ServerDispatcher
    {
        /// <summary>
        /// Time to delay after the last connection before initiating a garbage collection
        /// in the server. 
        /// </summary>
        private static readonly TimeSpan s_GCTimeout = TimeSpan.FromSeconds(30);

        private readonly ICompilerServerHost _compilerServerHost;
        private readonly IRequestHandler _handler;
        private readonly IDiagnosticListener _diagnosticListener;

        public ServerDispatcher(ICompilerServerHost compilerServerHost, string responseFileDirectory, IDiagnosticListener diagnosticListener) : 
            this(compilerServerHost, new CompilerRequestHandler(compilerServerHost, responseFileDirectory), diagnosticListener)
        {

        }

        /// <summary>
        /// BTODO: clean up the comments here
        /// Create a new server that listens on the given base pipe name.
        /// When a request comes in, it is dispatched on a separate thread
        /// via the IRequestHandler interface passed in.
        /// </summary>
        internal ServerDispatcher(ICompilerServerHost compilerServerHost, IRequestHandler handler, IDiagnosticListener diagnosticListener)
        {
            _compilerServerHost = compilerServerHost;
            _handler = handler;
            _diagnosticListener = diagnosticListener;
        }

        /// <summary>
        /// This function will accept and process new connections until an event causes
        /// the server to enter a passive shut down mode.  For example if analyzers change
        /// or the keep alive timeout is hit.  At which point this function will cease 
        /// accepting new connections and wait for existing connections to complete before
        /// returning.
        /// </summary>
        public void ListenAndDispatchConnections( /* BTODO: delete pipeName */ string pipeName, TimeSpan? keepAlive, CancellationToken cancellationToken = default(CancellationToken))
        {
            var isKeepAliveDefault = true;
            var connectionList = new List<Task<ConnectionData>>();
            Task gcTask = null;
            Task timeoutTask = null;
            Task<IClientConnection> listenTask = null;
            CancellationTokenSource listenCancellationTokenSource = null;

            do
            {
                // While this loop is running there should be an active named pipe listening for a 
                // connection.
                if (listenTask == null)
                {
                    Debug.Assert(listenCancellationTokenSource == null);
                    Debug.Assert(timeoutTask == null);
                    listenCancellationTokenSource = new CancellationTokenSource();
                    listenTask = _compilerServerHost.CreateListenTask(listenCancellationTokenSource.Token);
                }

                // If there are no active clients running then the server needs to be in a timeout mode.
                if (connectionList.Count == 0 && timeoutTask == null && keepAlive.HasValue)
                {
                    Debug.Assert(listenTask != null);
                    timeoutTask = Task.Delay(keepAlive.Value);
                }

                WaitForAnyCompletion(connectionList, new[] { listenTask, timeoutTask, gcTask }, cancellationToken);

                // If there is a connection event that has highest priority. 
                if (listenTask.IsCompleted && !cancellationToken.IsCancellationRequested)
                {
                    var connectionTask = CreateHandleConnectionTask(listenTask, _handler, cancellationToken);
                    connectionList.Add(connectionTask);
                    listenTask = null;
                    listenCancellationTokenSource = null;
                    timeoutTask = null;
                    gcTask = null;
                    continue;
                }

                if ((timeoutTask != null && timeoutTask.IsCompleted) || cancellationToken.IsCancellationRequested)
                {
                    listenCancellationTokenSource.Cancel();
                    break;
                }

                if (gcTask != null && gcTask.IsCompleted)
                {
                    gcTask = null;
                    GC.Collect();
                    continue;
                }

                // Only other option is a connection event.  Go ahead and clear out the dead connections
                if (!CheckConnectionTask(connectionList, ref keepAlive, ref isKeepAliveDefault))
                {
                    // If there is a client disconnection detected then the server needs to begin
                    // the shutdown process.  We have to assume that the client disconnected via
                    // Ctrl+C and wants the server process to terminate.  It's possible a compilation
                    // is running out of control and the client wants their machine back.  
                    listenCancellationTokenSource.Cancel();
                    break;
                }

                if (connectionList.Count == 0 && gcTask == null)
                {
                    gcTask = Task.Delay(s_GCTimeout);
                }
            } while (true);

            try
            {
                Task.WaitAll(connectionList.ToArray());
            }
            catch
            {
                // Server is shutting down, don't care why the above failed and Exceptions
                // are expected here.  For example AggregateException via, OperationCancelledException
                // is an expected case. 
            }
        }

        /// <summary>
        /// The server farms out work to Task values and this method needs to wait until at least one of them
        /// has completed.
        /// </summary>
        private void WaitForAnyCompletion(IEnumerable<Task<ConnectionData>> e, Task[] other, CancellationToken cancellationToken)
        {
            var all = new List<Task>();
            all.AddRange(e);
            all.AddRange(other.Where(x => x != null));

            try
            {
                Task.WaitAny(all.ToArray(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Thrown when the provided cancellationToken is cancelled.  This is handled in the caller, 
                // here it just serves to break out of the WaitAny call.
            }
        }

        /// <summary>
        /// Checks the completed connection objects.
        /// </summary>
        /// <returns>True if everything completed normally and false if there were any client disconnections.</returns>
        private bool CheckConnectionTask(List<Task<ConnectionData>> connectionList, ref TimeSpan? keepAlive, ref bool isKeepAliveDefault)
        {
            var allFine = true;
            var processedCount = 0;
            var i = 0;
            while (i < connectionList.Count)
            {
                var current = connectionList[i];
                if (!current.IsCompleted)
                {
                    i++;
                    continue;
                }

                connectionList.RemoveAt(i);
                processedCount++;

                var connectionData = current.Result;
                ChangeKeepAlive(connectionData.KeepAlive, ref keepAlive, ref isKeepAliveDefault);
                if (connectionData.CompletionReason == CompletionReason.ClientDisconnect)
                {
                    allFine = false;
                }
            }

            if (processedCount > 0)
            {
                _diagnosticListener.ConnectionProcessed(processedCount);
            }

            return allFine;
        }

        private void ChangeKeepAlive(TimeSpan? value, ref TimeSpan? keepAlive, ref bool isKeepAliveDefault)
        {
            if (value.HasValue)
            {
                if (isKeepAliveDefault || !keepAlive.HasValue || value.Value > keepAlive.Value)
                {
                    keepAlive = value;
                    isKeepAliveDefault = false;
                    _diagnosticListener.UpdateKeepAlive(value.Value);
                }
            }
        }

        /// <summary>
        /// Creates a Task representing the processing of the new connection.  This will return a task that
        /// will never fail.  It will always produce a <see cref="ConnectionData"/> value.  Connection errors
        /// will end up being represented as <see cref="CompletionReason.ClientDisconnect"/>
        /// </summary>
        internal static async Task<ConnectionData> CreateHandleConnectionTask(Task<IClientConnection> connectionTask, IRequestHandler handler, CancellationToken cancellationToken)
        {
            Connection connection;
            try
            {
                var clientConnection = await connectionTask.ConfigureAwait(true);
                connection = new Connection(clientConnection, handler);
            }
            catch (Exception ex)
            {
                // Unable to establish a connection with the client.  The client is responsible for
                // handling this case.  Nothing else for us to do here.
                CompilerServerLogger.LogException(ex, "Error creating client named pipe");
                return new ConnectionData(CompletionReason.CompilationNotStarted);
            }

            return await connection.ServeConnection(cancellationToken).ConfigureAwait(false);
        }
    }
}
