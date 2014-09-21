// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    extern alias Microsoft_CodeAnalysis_Desktop;
    using FailFast = Microsoft_CodeAnalysis_Desktop::Microsoft.CodeAnalysis.FailFast;

    /// <summary>
    /// The interface used by ServerDispatcher to dispatch requests.
    /// </summary>
    interface IRequestHandler
    {
        BuildResponse HandleRequest(BuildRequest req, CancellationToken cancellationToken);
    }

    /// <summary>
    /// This class handles the named pipe creation, listening, thread creation,
    /// and so forth. When a request comes in, it is dispatched on a new thread
    /// to the IRequestHandler interface. The request handler does the actual
    /// compilation. This class itself has no dependencies on the compiler.
    /// </summary>
    /// <remarks>
    /// One instance of this is created per process.
    /// </remarks>
    partial class ServerDispatcher
    {
        /// Number of milliseconds that the server will stay alive 
        /// after the last request disconnects.
        private const int DefaultServerDieTimeout = 100; // Minimal timeout

        /// <summary>
        /// Main entry point for the process. Initialize the server dispatcher
        /// and wait for connections.
        /// </summary>
        public static int Main(string[] args)
        {
            CompilerServerLogger.Initialize("SRV");
            CompilerServerLogger.Log("Process started");

            int dieTimeoutMs;
            // First try to get the die timeout from an environment variable,
            // then try to get the die timeout from the app.config file.
            // Set to default if any failures
            try
            {
                string dieTimeoutStr;
                if (((dieTimeoutStr = Environment.GetEnvironmentVariable("RoslynDieTimeout")) != null ||
                     (dieTimeoutStr = ConfigurationManager.AppSettings["dieTimeout"]) != null)
                    && int.TryParse(dieTimeoutStr, out dieTimeoutMs)
                    && dieTimeoutMs > 0)
                {
                    // The die timeout settings are stored in seconds, not
                    // milliseconds
                    dieTimeoutMs *= 1000;
                }
                else
                {
                    dieTimeoutMs = DefaultServerDieTimeout;
                }
                CompilerServerLogger.Log("Die timeout is: " + dieTimeoutMs + "milliseconds.");
            }
            catch (ConfigurationErrorsException e)
            {
                dieTimeoutMs = DefaultServerDieTimeout;
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
            }

            CompilerFatalError.Handler = FailFast.OnFatalException;

            var dispatcher = new ServerDispatcher(BuildProtocolConstants.PipeName,
                                                  new CompilerRequestHandler(),
                                                  dieTimeoutMs);

            dispatcher.ListenAndDispatchConnections();
            return 0;
        }

        // Size of the buffers to use
        private const int PipeBufferSize = 0x10000;  // 64K

        private readonly string basePipeName;
        private readonly IRequestHandler handler;

        // Semaphore for number of active connections.
        // All writes should be Interlocked.
        private int activeConnectionCount = 0;

        private readonly KeepAliveTimer keepAliveTimer;

        // The current pipe stream which is waiting for connections
        private NamedPipeServerStream waitingPipeStream = null;

        // The list of active connections. To avoid the need to synchronize
        // access, items are only added or removed when a new connection
        // comes along. At any given time, some of the items in this list may
        // have completed but not yet removed.
        private readonly List<Task> activeConnections = new List<Task>();

        /// <summary>
        /// Create a new server that listens on the given base pipe name.
        /// When a request comes in, it is dispatched on a separate thread
        /// via the IRequestHandler interface passed in.
        /// </summary>
        /// <param name="basePipeName">Base name for named pipe</param>
        /// <param name="handler">Handler that handles requests</param>
        /// <param name="serverDieTimeout">
        /// The timeout in milliseconds before the server automatically dies.
        /// </param>
        public ServerDispatcher(string basePipeName,
                                IRequestHandler handler,
                                int serverDieTimeout)
        {
            this.basePipeName = basePipeName;
            this.handler = handler;

            this.keepAliveTimer = new KeepAliveTimer(serverDieTimeout);

            var _ = new AnalyzerWatcher(this);
        }

        /// <summary>
        /// This function never returns. It loops and dispatches requests 
        /// until the process it terminated. Each incoming request is
        /// dispatched to a new thread which runs.
        /// </summary>
        public void ListenAndDispatchConnections()
        {
            Debug.Assert(SynchronizationContext.Current == null);
            // We loop here continuously, dispatching client connections as 
            // they come in, until TimeoutFired causes an exception to be
            // thrown. Each time through the loop we either have accepted a
            // client connection, or timed out. After each connection, 
            // we need to create a new instance of the pipe to listen on.

            bool firstConnection = true;

            while (true)
            {
                // Create the pipe and begin waiting for a connection. This 
                // doesn't block, but could fail in certain circumstances, such
                // as Windows refusing to create the pipe for some reason 
                // (out of handles?), or the pipe was disconnected before we 
                // starting listening.
                NamedPipeServerStream pipeStream = ConstructPipe();
                if (pipeStream == null)
                {
                    return;
                }

                this.waitingPipeStream = pipeStream;

                // If this is the first connection then we want to start a timeout
                // Otherwise, we should start the timeout when the last connection
                // finishes processing.
                if (firstConnection)
                {
                    // Since no timeout could have been started before now, 
                    // this will definitely start a timeout
                    StartTimeoutTimerIfNecessary();
                    firstConnection = false;
                }

                CompilerServerLogger.Log("Waiting for new connection");

                // Wait for a connection or the timeout
                // If a timeout occurs then the pipe will be closed and we will throw an exception
                // to the calling function.
                try
                {
                    pipeStream.WaitForConnection();
                }
                catch (ObjectDisposedException)
                {
                    CompilerServerLogger.Log("Listening pipe closed; exiting.");
                    break;
                }
                catch (IOException)
                {
                    CompilerServerLogger.Log("The pipe was closed or the client has been disconnected");
                    break;
                }

                // We have a connection
                CompilerServerLogger.Log("Pipe connection detected.");

                // Cancel the timeouts
                this.keepAliveTimer.CancelIfActive();

                // Dispatch the new connection on the thread pool
                var newConnection = DispatchConnection(pipeStream);
                // Connection object now owns the connected pipe. 

                // Cleanup any connections that have completed, and then add
                // the new one.
                activeConnections.RemoveAll(t => t.IsCompleted);
                activeConnections.Add(newConnection);

                if (this.keepAliveTimer.StopAfterFirstConnection)
                {
                    break;
                }

                // Next time around the loop, create a new instance of the pipe
                // to listen for another connection.
            }

            Task.WhenAll(activeConnections).Wait();
        }

        /// <summary>
        /// Checks to see if memory is available, and if it is creates a new
        /// Connection object, awaits the completion of the connection, then
        /// runs <see cref="ConnectionCompleted"/> for cleanup.
        /// </summary>
        private async Task DispatchConnection(NamedPipeServerStream pipeStream)
        {
            try
            {
                // There is always a race between timeout and connections because
                // there is no way to cancel listening on the pipe without
                // closing the pipe. We immediately increment the connection
                // semaphore while processing connections in order to narrow
                // the race window as much as possible.
                Interlocked.Increment(ref this.activeConnectionCount);

                if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
                {
                    CompilerServerLogger.Log("Memory available - accepting connection");

                    Connection connection = new Connection(pipeStream, handler, this.keepAliveTimer);

                    try
                    {
                        await connection.ServeConnection().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException e)
                    {
                        // If the client closes the pipe while we're reading or writing
                        // we'll get an object disposed exception on the pipe
                        // Log the failure and continue
                        CompilerServerLogger.Log(
                            "Client pipe closed: received exception " + e.Message);
                    }

                    // The connection should be finished
                    ConnectionCompleted(connection);
                }
                else
                {
                    CompilerServerLogger.Log("Memory tight - rejecting connection.");
                    // As long as we haven't written a response, the client has not 
                    // committed to this server instance and can look elsewhere.
                    pipeStream.Close();

                    // We didn't create a connection -- decrement the semaphore
                    Interlocked.Decrement(ref this.activeConnectionCount);

                    // Start a terminate server timer if there are no active
                    // connections
                    StartTimeoutTimerIfNecessary();
                }
            }
            catch (Exception e) if (CompilerFatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Create an instance of the pipe. This might be the first instance, or a subsequent instance.
        /// There always needs to be an instance of the pipe created to listen for a new client connection.
        /// </summary>
        /// <returns>The pipe instance, or NULL if the pipe couldn't be created..</returns>
        private NamedPipeServerStream ConstructPipe()
        {
            // Add the process ID onto the pipe name so each process gets a unique pipe name.
            // The client must user this algorithm too to connect.
            string pipeName = basePipeName + Process.GetCurrentProcess().Id.ToString();

            try
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
            catch (Exception e)
            {
                // Windows may not create the pipe for a number of reasons.
                CompilerServerLogger.LogException(e, string.Format("Construction of pipe '{0}' failed", pipeName));
                return null;
            }
        }

        /// <summary>
        /// Called from the Connection class when the connection is complete.
        /// </summary>
        private void ConnectionCompleted(Connection connection)
        {
            Interlocked.Decrement(ref this.activeConnectionCount);

            StartTimeoutTimerIfNecessary();
            CompilerServerLogger.Log("Removed connection {0}, {1} remaining.", connection.LoggingIdentifier, this.activeConnectionCount);
        }

        /// <summary>
        /// Called from the <see cref="AnalyzerWatcher"/> class when an analyzer file
        /// changes on disk.
        /// </summary>
        private void AnalyzerFileChanged()
        {
            // An analyzer file has changed on disk. Close the waiting stream, which
            // will both prevent further connections and signal that we should shut
            // down.
            this.waitingPipeStream.Close();
        }

        /// <summary>
        /// The timeout was fired -- check if we need to cancel the pipe.
        /// </summary>
        private void ServerDieTimeoutFired(Task timeoutTask)
        {
            // If the timeout wasn't cancelled and we have no connections
            // we should shut down
            if (!timeoutTask.IsCanceled && this.activeConnectionCount == 0)
            {
                // N.B. There is no way to cancel waiting for a connection other than closing the
                // pipe, so there is a race between closing the pipe and getting another 
                // connection. We should close the pipe as soon as possible and do any necessary 
                // cleanup afterwards
                this.waitingPipeStream.Close();
                CompilerServerLogger.Log("Waiting for pipe connection timed out after {0} ms.",
                    this.keepAliveTimer.KeepAliveTime);
            }
            else
            {
                this.keepAliveTimer.Clear();
            }
        }

        /// <summary>
        /// If no timer is active in this instance, start a new one.
        /// </summary>
        public void StartTimeoutTimerIfNecessary()
        {
            if (this.keepAliveTimer.IsKeepAliveFinite &&
                this.activeConnectionCount == 0)
            {
                var timerTask = this.keepAliveTimer.StartTimer();
                if (timerTask != null)
                {
                    timerTask.ContinueWith(ServerDieTimeoutFired);
                }
            }
        }
    }
}
