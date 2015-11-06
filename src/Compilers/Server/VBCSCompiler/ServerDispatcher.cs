// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
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
    internal partial class ServerDispatcher
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
            CompilerServerLogger.Initialize("SRV");
            CompilerServerLogger.Log("Process started");

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

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="pipeName">Name of the pipe on which the instance will listen for requests.</param>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTask(string pipeName, CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            NamedPipeServerStream pipeStream = ConstructPipe(pipeName);

            // Unfortunately the version of .Net we are using doesn't support the WaitForConnectionAsync
            // method.  When it is available it should absolutely be used here.  In the meantime we
            // have to deal with the idea that this WaitForConnection call will block a thread
            // for a significant period of time.  It is unadvisable to do this to a thread pool thread 
            // hence we will use an explicit thread here.
            var listenSource = new TaskCompletionSource<NamedPipeServerStream>();
            var listenTask = listenSource.Task;
            var listenThread = new Thread(() =>
            {
                try
                {
                    CompilerServerLogger.Log("Waiting for new connection");
                    pipeStream.WaitForConnection();
                    CompilerServerLogger.Log("Pipe connection detected.");

                    if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
                    {
                        CompilerServerLogger.Log("Memory available - accepting connection");
                        listenSource.SetResult(pipeStream);
                        return;
                    }

                    try
                    {
                        pipeStream.Close();
                    }
                    catch
                    {
                        // Okay for Close failure here.  
                    }

                    listenSource.SetException(new Exception("Insufficient resources to process new connection."));
                }
                catch (Exception ex)
                {
                    listenSource.SetException(ex);
                }
            });
            listenThread.Start();

            // Create a tasks that waits indefinitely (-1) and completes only when cancelled.
            var waitCancellationTokenSource = new CancellationTokenSource();
            var waitTask = Task.Delay(
                Timeout.Infinite,
                CancellationTokenSource.CreateLinkedTokenSource(waitCancellationTokenSource.Token, cancellationToken).Token);
            await Task.WhenAny(listenTask, waitTask).ConfigureAwait(false);
            if (listenTask.IsCompleted)
            {
                waitCancellationTokenSource.Cancel();
                return await listenTask.ConfigureAwait(false);
            }

            // The listen operation was cancelled.  Close the pipe stream throw a cancellation exception to
            // simulate the cancel operation.
            waitCancellationTokenSource.Cancel();
            try
            {
                pipeStream.Close();
            }
            catch
            {
                // Okay for Close failure here.
            }

            throw new OperationCanceledException();
        }

        /// <summary>
        /// Creates a Task representing the processing of the new connection.  This will return a task that
        /// will never fail.  It will always produce a <see cref="ConnectionData"/> value.  Connection errors
        /// will end up being represented as <see cref="CompletionReason.ClientDisconnect"/>
        /// </summary>
        internal static async Task<ConnectionData> CreateHandleConnectionTask(Task<NamedPipeServerStream> pipeStreamTask, IRequestHandler handler, CancellationToken cancellationToken)
        {
            Connection connection;
            try
            {
                var pipeStream = await pipeStreamTask.ConfigureAwait(false);
                var clientConnection = new NamedPipeClientConnection(pipeStream);
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

        /// <summary>
        /// Create an instance of the pipe. This might be the first instance, or a subsequent instance.
        /// There always needs to be an instance of the pipe created to listen for a new client connection.
        /// </summary>
        /// <returns>The pipe instance or throws an exception.</returns>
        private NamedPipeServerStream ConstructPipe(string pipeName)
        {
            CompilerServerLogger.Log("Constructing pipe '{0}'.", pipeName);

            SecurityIdentifier identifier = WindowsIdentity.GetCurrent().Owner;
            PipeSecurity security = new PipeSecurity();

            // Restrict access to just this account.  
            PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
            security.AddAccessRule(rule);
            security.SetOwner(identifier);

            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                PipeBufferSize, // Default input buffer
                PipeBufferSize, // Default output buffer
                security,
                HandleInheritability.None);

            CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", pipeName);

            return pipeStream;
        }
    }
}
