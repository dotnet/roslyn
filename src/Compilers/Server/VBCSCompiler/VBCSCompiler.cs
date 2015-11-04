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
    internal partial class VBCSCompiler
    {
        /// <summary>
        /// Default time the server will stay alive after the last request disconnects.
        /// </summary>
        private static readonly TimeSpan s_defaultServerKeepAlive = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Time to delay after the last connection before initiating a garbage collection
        /// in the server. 
        /// </summary>
        private static readonly TimeSpan s_GCTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Main entry point for the process. Initialize the server dispatcher
        /// and wait for connections.
        /// </summary>
        public static int Main(string[] args)
        {
            TimeSpan? keepAliveTimeout = null;

            // VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
            // location of the response files.
            var compilerExeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Pipename should be passed as the first and only argument to the server process
            // and it must have the form "-pipename:name". Otherwise, exit with a non-zero
            // exit code
            const string pipeArgPrefix = "-pipename:";
            if (args.Length != 1 ||
                args[0].Length <= pipeArgPrefix.Length ||
                !args[0].StartsWith(pipeArgPrefix))
            {
                return CommonCompiler.Failed;
            }

            var pipeName = args[0].Substring(pipeArgPrefix.Length);

            // Grab the server mutex to prevent multiple servers from starting with the same
            // pipename and consuming excess resources. If someone else holds the mutex
            // exit immediately with a non-zero exit code
            var serverMutexName = $"{pipeName}.server";
            bool holdsMutex;
            using (var serverMutex = new Mutex(initiallyOwned: true,
                                               name: serverMutexName,
                                               createdNew: out holdsMutex))
            {
                if (!holdsMutex)
                {
                    return CommonCompiler.Failed;
                }

                try
                {
                    return Run(keepAliveTimeout, compilerExeDirectory, pipeName);
                }
                finally
                {
                    serverMutex.ReleaseMutex();
                }
            }
        }

        private static int Run(TimeSpan? keepAliveTimeout, string compilerExeDirectory, string pipeName)
        {
            try
            {
                int keepAliveValue;
                string keepAliveStr = ConfigurationManager.AppSettings["keepalive"];
                if (int.TryParse(keepAliveStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out keepAliveValue) &&
                    keepAliveValue >= 0)
                {
                    if (keepAliveValue == 0)
                    {
                        // This is a one time server entry.
                        keepAliveTimeout = null;
                    }
                    else
                    {
                        keepAliveTimeout = TimeSpan.FromSeconds(keepAliveValue);
                    }
                }
                else
                {
                    keepAliveTimeout = s_defaultServerKeepAlive;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                keepAliveTimeout = s_defaultServerKeepAlive;
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
            }

            CompilerServerLogger.Log("Keep alive timeout is: {0} milliseconds.", keepAliveTimeout?.TotalMilliseconds ?? 0);
            FatalError.Handler = FailFast.OnFatalException;

            var dispatcher = new ServerDispatcher(new CompilerRequestHandler(compilerExeDirectory), new EmptyDiagnosticListener());

            dispatcher.ListenAndDispatchConnections(
                pipeName,
                keepAliveTimeout);
            return CommonCompiler.Succeeded;
        }

        // Size of the buffers to use
        private const int PipeBufferSize = 0x10000;  // 64K

        private readonly IRequestHandler _handler;
        private readonly IDiagnosticListener _diagnosticListener;

        /// <summary>
        /// Create a new server that listens on the given base pipe name.
        /// When a request comes in, it is dispatched on a separate thread
        /// via the IRequestHandler interface passed in.
        /// </summary>
        public ServerDispatcher(IRequestHandler handler, IDiagnosticListener diagnosticListener)
        {
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
        /// <remarks>
        /// The server as run for customer builds should always enable watching analyzer 
        /// files.  This option only exist to disable the feature when running in our unit
        /// test framework.  The code hooks <see cref="AppDomain.AssemblyResolve"/> in a way
        /// that prevents xUnit from running correctly and hence must be disabled. 
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.GC.Collect",
            Justification = "We intentionally call GC.Collect when anticipate long period on inactivity.")]
        public void ListenAndDispatchConnections(string pipeName, TimeSpan? keepAlive, CancellationToken cancellationToken = default(CancellationToken))
        {
            var isKeepAliveDefault = true;
            var connectionList = new List<Task<ConnectionData>>();
            Task gcTask = null;
            Task timeoutTask = null;
            Task<NamedPipeServerStream> listenTask = null;
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
                    listenTask = CreateListenTask(pipeName, listenCancellationTokenSource.Token);
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

    }
}
