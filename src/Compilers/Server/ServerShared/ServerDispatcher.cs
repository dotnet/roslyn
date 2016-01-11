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
        private enum State
        {
            /// <summary>
            /// Server running and accepting all requests
            /// </summary>
            Running,

            /// <summary>
            /// Server processing existing requests, responding to shutdown commands but is not accepting
            /// new build requests.
            /// </summary>
            ShuttingDown,

            /// <summary>
            /// Server is done.
            /// </summary>
            Completed,
        }

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
        private State _state;
        private Task _timeoutTask;
        private Task _gcTask;
        private Task<IClientConnection> _listenTask;
        private CancellationTokenSource _listenCancellationTokenSource;
        private List<Task<ConnectionData>> _connectionList = new List<Task<ConnectionData>>();
        private TimeSpan? _keepAlive;
        private bool _keepAliveIsDefault;

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
            _state = State.Running;
            _keepAlive = keepAlive;
            _keepAliveIsDefault = true;

            try
            {
                ListenAndDispatchConnectionsCore(cancellationToken);
            }
            finally
            {
                _state = State.Completed;
                _gcTask = null;
                _timeoutTask = null;
                CloseListenTask();
            }
        }

        public void ListenAndDispatchConnectionsCore(CancellationToken cancellationToken)
        {
            CreateListenTask();
            do
            {
                // There should always be a listen task when the core server loop is running.
                Debug.Assert(_listenTask != null);

                MaybeCreateTimeoutTask();
                MaybeCreateGCTask();
                WaitForAnyCompletion(cancellationToken);
                CheckCompletedTasks(cancellationToken);

            } while (_connectionList.Count > 0 || _state == State.Running);
        }

        private void CheckCompletedTasks(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // If cancellation has been requested then the server needs to be in the process
                // of shutting down.
                _state = State.ShuttingDown;
            }
            else
            {
                // The only action that should be processed if cancellation is requested is the 
                // finishing of the connections.  All of the other active tasks like listening for
                // new connections should only occur if we aren't being cancelled. 

                if (_listenTask.IsCompleted)
                {
                    HandleCompletedListenTask(cancellationToken);
                }

                if (_timeoutTask?.IsCompleted == true)
                {
                    HandleCompletedTimeoutTask();
                }

                if (_gcTask?.IsCompleted == true)
                {
                    HandleCompletedGCTask();
                }
            }

            HandleCompletedConnections();
        }

        /// <summary>
        /// The server farms out work to Task values and this method needs to wait until at least one of them
        /// has completed.
        /// </summary>
        private void WaitForAnyCompletion(CancellationToken cancellationToken)
        {
            var all = new List<Task>();
            all.AddRange(_connectionList);
            all.Add(_timeoutTask);
            all.Add(_listenTask);
            all.Add(_gcTask);

            try
            {
                var waitArray = all.Where(x => x != null).ToArray();
                Task.WaitAny(waitArray, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Thrown when the provided cancellationToken is cancelled.  This is handled in the caller, 
                // here it just serves to break out of the WaitAny call.
            }
        }

        private void CreateListenTask()
        {
            Debug.Assert(_timeoutTask == null);
            _listenCancellationTokenSource = new CancellationTokenSource();
            _listenTask = _clientConnectionHost.CreateListenTask(_listenCancellationTokenSource.Token);
            _diagnosticListener.ConnectionListening();
        }

        private void CloseListenTask()
        {
            Debug.Assert(_listenTask != null);
            _listenCancellationTokenSource.Cancel();
            _listenCancellationTokenSource = null;
            _listenTask = null;
        }

        private void HandleCompletedListenTask(CancellationToken cancellationToken)
        {
            _diagnosticListener.ConnectionReceived();
            var allowCompilationRequests = _state == State.Running;
            var connectionTask = HandleClientConnection(_listenTask, allowCompilationRequests, cancellationToken);
            _connectionList.Add(connectionTask);

            // Timeout and GC are only done when there are no active connections.  Now that we have a new 
            // connection cancel out these tasks.
            _timeoutTask = null;
            _gcTask = null;

            // Begin listening again for new connections.
            CreateListenTask();
        }

        private void HandleCompletedTimeoutTask()
        {
            _diagnosticListener.KeepAliveReached();
            _listenCancellationTokenSource.Cancel();
            _timeoutTask = null;
            _state = State.ShuttingDown;
        }

        private void HandleCompletedGCTask()
        {
            _gcTask = null;
            GC.Collect();
        }

        private void MaybeCreateTimeoutTask()
        {
            // If there are no active clients running then the server needs to be in a timeout mode.
            if (_connectionList.Count == 0 && _timeoutTask == null && _keepAlive.HasValue)
            {
                Debug.Assert(_listenTask != null);
                _timeoutTask = Task.Delay(_keepAlive.Value);
            }
        }

        private void MaybeCreateGCTask()
        {
            if (_connectionList.Count == 0 && _gcTask == null)
            {
                _gcTask = Task.Delay(GCTimeout);
            }
        }

        /// <summary>
        /// Checks the completed connection objects.
        /// </summary>
        /// <returns>False if the server needs to begin shutting down</returns>
        private void HandleCompletedConnections()
        {
            var shutdown = false;
            var processedCount = 0;
            var i = 0;
            while (i < _connectionList.Count)
            {
                var current = _connectionList[i];
                if (!current.IsCompleted)
                {
                    i++;
                    continue;
                }

                _connectionList.RemoveAt(i);
                processedCount++;

                var connectionData = current.Result;
                ChangeKeepAlive(connectionData.KeepAlive);

                switch (connectionData.CompletionReason)
                {
                    case CompletionReason.CompilationCompleted:
                    case CompletionReason.CompilationNotStarted:
                        // These are all normal shutdown states.  Nothing to do here.
                        break;
                    case CompletionReason.ClientDisconnect:
                        // Have to assume the worst here which is user pressing Ctrl+C at the command line and
                        // hence wanting all compilation to end.
                        _diagnosticListener.ConnectionRudelyEnded();
                        shutdown = true;
                        break;
                    case CompletionReason.ClientException:
                    case CompletionReason.ClientShutdownRequest:
                        _diagnosticListener.ConnectionRudelyEnded();
                        shutdown = true;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected enum value {connectionData.CompletionReason}");
                }
            }

            if (processedCount > 0)
            {
                _diagnosticListener.ConnectionCompleted(processedCount);
            }

            if (shutdown)
            {
                _state = State.ShuttingDown;
            }
        }

        private void ChangeKeepAlive(TimeSpan? value)
        {
            if (value.HasValue)
            {
                if (_keepAliveIsDefault || !_keepAlive.HasValue || value.Value > _keepAlive.Value)
                {
                    _keepAlive = value;
                    _keepAliveIsDefault = false;
                    _diagnosticListener.UpdateKeepAlive(value.Value);
                }
            }
        }

        /// <summary>
        /// Creates a Task representing the processing of the new connection.  This will return a task that
        /// will never fail.  It will always produce a <see cref="ConnectionData"/> value.  Connection errors
        /// will end up being represented as <see cref="CompletionReason.ClientDisconnect"/>
        /// </summary>
        internal static async Task<ConnectionData> HandleClientConnection(Task<IClientConnection> clientConnectionTask, bool allowCompilationRequests = true, CancellationToken cancellationToken = default(CancellationToken))
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
                return await clientConnection.HandleConnection(allowCompilationRequests, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CompilerServerLogger.LogException(ex, "Error handling connection");
                return new ConnectionData(CompletionReason.ClientException);
            }
        }
    }
}
