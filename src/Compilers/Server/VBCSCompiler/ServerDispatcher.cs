// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.CSharp;
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
            /// Server is in the process of shutting down. New connections will not be accepted.
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
        private readonly ICompilerServerLogger _logger;
        private readonly IClientConnectionHost _clientConnectionHost;
        private readonly IDiagnosticListener _diagnosticListener;
        private State _state;
        private Task? _timeoutTask;
        private Task? _gcTask;
        private Task<IClientConnection>? _listenTask;
        private readonly List<Task<CompletionData>> _connectionList = new List<Task<CompletionData>>();
        private TimeSpan? _keepAlive;
        private bool _keepAliveIsDefault;

        internal ServerDispatcher(ICompilerServerHost compilerServerHost, IClientConnectionHost clientConnectionHost, IDiagnosticListener? diagnosticListener = null)
        {
            _compilerServerHost = compilerServerHost;
            _logger = compilerServerHost.Logger;
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
        public void ListenAndDispatchConnections(TimeSpan? keepAlive, CancellationToken cancellationToken = default)
        {
            _state = State.Running;
            _keepAlive = keepAlive;
            _keepAliveIsDefault = true;

            try
            {
                _clientConnectionHost.BeginListening();
                ListenAndDispatchConnectionsCore(cancellationToken);
            }
            finally
            {
                _state = State.Completed;
                _gcTask = null;
                _timeoutTask = null;

                if (_clientConnectionHost.IsListening)
                {
                    _clientConnectionHost.EndListening();
                }

                if (_listenTask is not null)
                {
                    // This type is responsible for cleaning up resources associated with _listenTask. Once EndListening
                    // is complete this task is guaranteed to be either completed or have a task scheduled to complete
                    // it. If it ran to completion we need to dispose of the value.
                    if (!_listenTask.IsCompleted)
                    {
                        // Wait for the task to complete
                        _listenTask.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                            .Wait(CancellationToken.None);
                    }

                    if (_listenTask.Status == TaskStatus.RanToCompletion)
                    {
                        try
                        {
                            _listenTask.Result.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex, $"Error disposing of {nameof(_listenTask)}");
                        }
                    }
                }
            }
            _logger.Log($"End ListenAndDispatchConnections");
        }

        public void ListenAndDispatchConnectionsCore(CancellationToken cancellationToken)
        {
            do
            {
                MaybeCreateListenTask();
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
                ChangeToShuttingDown("Server cancellation");
                Debug.Assert(_gcTask is null);
                Debug.Assert(_timeoutTask is null);
            }

            if (_listenTask?.IsCompleted == true)
            {
                _diagnosticListener.ConnectionReceived();
                var connectionTask = ProcessClientConnectionAsync(
                    _compilerServerHost,
                    _listenTask,
                    allowCompilationRequests: _state == State.Running,
                    cancellationToken);
                _connectionList.Add(connectionTask);

                // Timeout and GC are only done when there are no active connections.  Now that we have a new
                // connection cancel out these tasks.
                _timeoutTask = null;
                _gcTask = null;
                _listenTask = null;
            }

            if (_timeoutTask?.IsCompleted == true)
            {
                _diagnosticListener.KeepAliveReached();
                ChangeToShuttingDown("Keep alive hit");
            }

            if (_gcTask?.IsCompleted == true)
            {
                RunGC();
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
            AddNonNull(_timeoutTask);
            AddNonNull(_listenTask);
            AddNonNull(_gcTask);

            try
            {
                Task.WaitAny(all.ToArray(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Thrown when the provided cancellationToken is cancelled.  This is handled in the caller,
                // here it just serves to break out of the WaitAny call.
            }

            void AddNonNull(Task? task)
            {
                if (task is object)
                {
                    all.Add(task);
                }
            }
        }

        private void ChangeToShuttingDown(string reason)
        {
            if (_state == State.ShuttingDown)
            {
                return;
            }

            _logger.Log($"Shutting down server: {reason}");
            Debug.Assert(_state == State.Running);
            Debug.Assert(_clientConnectionHost.IsListening);

            _state = State.ShuttingDown;
            _timeoutTask = null;
            _gcTask = null;
        }

        private void RunGC()
        {
            _gcTask = null;
            GC.GetTotalMemory(forceFullCollection: true);
        }

        private void MaybeCreateListenTask()
        {
            if (_listenTask is null)
            {
                _listenTask = _clientConnectionHost.GetNextClientConnectionAsync();
            }
        }

        private void MaybeCreateTimeoutTask()
        {
            // If there are no active clients running then the server needs to be in a timeout mode.
            if (_state == State.Running && _connectionList.Count == 0 && _timeoutTask is null && _keepAlive.HasValue)
            {
                Debug.Assert(_listenTask != null);
                _timeoutTask = Task.Delay(_keepAlive.Value);
            }
        }

        private void MaybeCreateGCTask()
        {
            if (_state == State.Running && _connectionList.Count == 0 && _gcTask is null)
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

                // These task should never fail. Unexpected errors will be caught and translated into
                // a RequestError message
                Debug.Assert(current.Status == TaskStatus.RanToCompletion);
                var completionData = current.Result;
                switch (completionData.Reason)
                {
                    case CompletionReason.RequestCompleted:
                        _logger.Log("Client request completed");

                        if (completionData.NewKeepAlive is { } keepAlive)
                        {
                            _logger.Log($"Client changed keep alive to {keepAlive}");
                            ChangeKeepAlive(keepAlive);
                        }

                        if (completionData.ShutdownRequest)
                        {
                            _logger.Log("Client requested shutdown");
                            shutdown = true;
                        }

                        break;
                    case CompletionReason.RequestError:
                        _logger.LogError("Client request failed");
                        shutdown = true;
                        break;
                    default:
                        _logger.LogError("Unexpected enum value");
                        shutdown = true;
                        break;
                }

                _diagnosticListener.ConnectionCompleted(completionData);
            }

            if (shutdown)
            {
                ChangeToShuttingDown("Error handling client connection");
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
