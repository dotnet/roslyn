// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal interface IClientConnectionHost
    {
        Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken);
    }

    /// <summary>
    /// This class manages the connections, timeout and general scheduling of the client 
    /// requests.  
    /// </summary>
    internal sealed class ServerDispatcher
    {
        /// <summary>
        /// Default time the server will stay alive after the last request disconnects.
        /// </summary>
        internal static readonly TimeSpan DefaultServerKeepAlive = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Time to delay after the last connection before initiating a garbage collection
        /// in the server. 
        /// </summary>
        internal static readonly TimeSpan GCTimeout = TimeSpan.FromSeconds(30);

        private readonly IClientConnectionHost _clientConnectionHost;
        private readonly IDiagnosticListener _diagnosticListener;

        internal ServerDispatcher(IClientConnectionHost clientConnectionHost, IDiagnosticListener diagnosticListener = null)
        {
            _clientConnectionHost = clientConnectionHost;
            _diagnosticListener = diagnosticListener ?? new EmptyDiagnosticListener();
        }

        /// <summary>
        /// This function will accept and process new connections until an event causes
        /// the server to enter a passive shut down mode.  For example if analyzers change
        /// or the keep alive timeout is hit.  At which point this function will cease 
        /// accepting new connections and wait for existing connections to complete before
        /// returning.
        /// </summary>
        public void ListenAndDispatchConnections(TimeSpan? keepAlive, CancellationToken cancellationToken = default(CancellationToken))
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
                    listenTask = _clientConnectionHost.CreateListenTask(listenCancellationTokenSource.Token);
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
                    var connectionTask = HandleClientConnection(listenTask, cancellationToken);
                    connectionList.Add(connectionTask);
                    listenTask = null;
                    listenCancellationTokenSource = null;
                    timeoutTask = null;
                    gcTask = null;
                    continue;
                }

                if ((timeoutTask != null && timeoutTask.IsCompleted) || cancellationToken.IsCancellationRequested)
                {
                    _diagnosticListener.KeepAliveReached();
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
                    _diagnosticListener.DetectedBadConnection();
                    listenCancellationTokenSource.Cancel();
                    break;
                }

                if (connectionList.Count == 0 && gcTask == null)
                {
                    gcTask = Task.Delay(GCTimeout);
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
                if (connectionData.CompletionReason == CompletionReason.ClientDisconnect || connectionData.CompletionReason == CompletionReason.ClientException)
                {
                    allFine = false;
                }
            }

            if (processedCount > 0)
            {
                _diagnosticListener.ConnectionCompleted(processedCount);
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
        internal static async Task<ConnectionData> HandleClientConnection(Task<IClientConnection> clientConnectionTask, CancellationToken cancellationToken = default(CancellationToken))
        {
            IClientConnection clientConnection;
            try
            {
                clientConnection = await clientConnectionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Unable to establish a connection with the client.  The client is responsible for
                // handling this case.  Nothing else for us to do here.
                CompilerServerLogger.LogException(ex, "Error creating client named pipe");
                return new ConnectionData(CompletionReason.CompilationNotStarted);
            }

            try
            {
                return await clientConnection.HandleConnection(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CompilerServerLogger.LogException(ex, "Error handling connection");
                return new ConnectionData(CompletionReason.ClientException);
            }
        }
    }
}
