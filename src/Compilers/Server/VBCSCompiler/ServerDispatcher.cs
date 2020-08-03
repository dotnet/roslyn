// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CompilerServer
{
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

        private readonly ICompilerServerHost _compilerServerHost;
        private readonly IClientConnectionHost _clientConnectionHost;
        private readonly IDiagnosticListener _diagnosticListener;
        private State _state;
        private Task _timeoutTask;
        private Task _gcTask;
        private Task<IClientConnection> _listenTask;
        private CancellationTokenSource _listenCancellationTokenSource;
        private List<Task<CompletionData>> _connectionList = new List<Task<CompletionData>>();
        private TimeSpan? _keepAlive;
        private bool _keepAliveIsDefault;

        internal ServerDispatcher(ICompilerServerHost compilerServerHost, IClientConnectionHost clientConnectionHost, IDiagnosticListener diagnosticListener = null)
        {
            _compilerServerHost = compilerServerHost;
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

                if (_listenTask != null)
                {
                    CloseListenTask();
                }
            }
        }

        public void ListenAndDispatchConnectionsCore(CancellationToken cancellationToken)
        {
            CreateListenTask();
            do
            {
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
                HandleCancellation();
                return;
            }

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

            HandleCompletedConnections();
        }

        private void HandleCancellation()
        {
            Debug.Assert(_listenTask != null);

            // If cancellation has been requested then the server needs to be in the process
            // of shutting down.
            _state = State.ShuttingDown;

            CloseListenTask();

            try
            {
                Task.WaitAll(_connectionList.ToArray());
            }
            catch
            {
                // It's expected that some will throw exceptions, in particular OperationCanceledException.  It's
                // okay for them to throw so long as they complete.
            }

            HandleCompletedConnections();
            Debug.Assert(_connectionList.Count == 0);
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
            Debug.Assert(_listenTask == null);
            Debug.Assert(_timeoutTask == null);
            _listenCancellationTokenSource = new CancellationTokenSource();
            _listenTask = _clientConnectionHost.ListenAsync(_listenCancellationTokenSource.Token);
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
            var connectionTask = ProcessClientConnectionAsync(_compilerServerHost, _listenTask, allowCompilationRequests, cancellationToken);
            _connectionList.Add(connectionTask);

            // Timeout and GC are only done when there are no active connections.  Now that we have a new
            // connection cancel out these tasks.
            _timeoutTask = null;
            _gcTask = null;

            // Begin listening again for new connections.
            _listenTask = null;
            CreateListenTask();
        }

        private void HandleCompletedTimeoutTask()
        {
            CompilerServerLogger.Log("Timeout triggered. Shutting down server.");
            _diagnosticListener.KeepAliveReached();
            _listenCancellationTokenSource.Cancel();
            _timeoutTask = null;
            _state = State.ShuttingDown;
        }

        private void HandleCompletedGCTask()
        {
            _gcTask = null;
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
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
        /// Checks the completed connection objects and updates the server state based on their 
        /// results.
        /// </summary>
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

                var completionData = current.Result;
                switch (completionData.Reason)
                {
                    case CompletionReason.RequestCompleted:
                        CompilerServerLogger.Log("Client request completed");

                        if (completionData.NewKeepAlive is { } keepAlive)
                        {
                            CompilerServerLogger.Log($"Client changed keep alive to {keepAlive}");
                            ChangeKeepAlive(keepAlive);
                        }

                        if (completionData.ShutdownRequest)
                        {
                            CompilerServerLogger.Log("Client requested shutdown");
                            shutdown = true;
                        }

                        // These are all normal shutdown states.  Nothing to do here.
                        break;
                    case CompletionReason.RequestError:
                        CompilerServerLogger.LogError("Client request failed");
                        shutdown = true;
                        break;
                    default:
                        CompilerServerLogger.LogError("Unexpected enum value");
                        shutdown = true;
                        break;
                }

                _diagnosticListener.ConnectionCompleted(completionData);
            }

            if (shutdown)
            {
                CompilerServerLogger.Log($"Shutting down server");
                _state = State.ShuttingDown;
            }
        }

        private void ChangeKeepAlive(TimeSpan keepAlive)
        {
            if (_keepAliveIsDefault || !_keepAlive.HasValue || keepAlive > _keepAlive.Value)
            {
                _keepAlive = keepAlive;
                _keepAliveIsDefault = false;
                _diagnosticListener.UpdateKeepAlive(keepAlive);
            }
        }

        internal static async Task<CompletionData> ProcessClientConnectionAsync(
            ICompilerServerHost compilerServerHost,
            Task<IClientConnection> clientStreamTask,
            bool allowCompilationRequests,
            CancellationToken cancellationToken)
        {
            var clientHandler = new ClientConnectionHandler(compilerServerHost);
            return await clientHandler.ProcessAsync(clientStreamTask, allowCompilationRequests, cancellationToken).ConfigureAwait(false);
        }
    }
}
