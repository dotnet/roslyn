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
        private const int DefaultServerKeepAlive = 100; // Minimal timeout

        /// <summary>
        /// Main entry point for the process. Initialize the server dispatcher
        /// and wait for connections.
        /// </summary>
        public static int Main(string[] args)
        {
            CompilerServerLogger.Initialize("SRV");
            CompilerServerLogger.Log("Process started");

            int keepaliveMs;
            // First try to get the die timeout from an environment variable,
            // then try to get the die timeout from the app.config file.
            // Set to default if any failures
            try
            {
                string keepaliveStr;
                if ((keepaliveStr = ConfigurationManager.AppSettings["keepalive"]) != null
                    && int.TryParse(keepaliveStr, out keepaliveMs)
                    && keepaliveMs > 0)
                {
                    // The die timeout settings are stored in seconds, not
                    // milliseconds
                    keepaliveMs *= 1000;
                }
                else
                {
                    keepaliveMs = DefaultServerKeepAlive;
                }
                CompilerServerLogger.Log("Die timeout is: " + keepaliveMs + "milliseconds.");
            }
            catch (ConfigurationErrorsException e)
            {
                keepaliveMs = DefaultServerKeepAlive;
                CompilerServerLogger.LogException(e, "Could not read AppSettings");
            }

            FatalError.Handler = FailFast.OnFatalException;

            var dispatcher = new ServerDispatcher(BuildProtocolConstants.PipeName,
                                                  new CompilerRequestHandler(),
                                                  keepaliveMs);

            dispatcher.ListenAndDispatchConnections();
            return 0;
        }

        // Size of the buffers to use
        private const int PipeBufferSize = 0x10000;  // 64K

        private readonly string basePipeName;
        private readonly IRequestHandler handler;

        private readonly KeepAliveTimer keepAliveTimer;

        /// <summary>
        /// The set of server state shared (read and write) amongst threads.  All access
        /// to this data must be done inside of a lock (this.sharedState) block.
        /// </summary>
        private readonly SharedState sharedState = new SharedState();

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
        /// This function will accept and process new connections until an event causes
        /// the server to enter a passive shut down mode.  For example if analyzers change
        /// or the idle timeout is hit.  At which point this function will cease 
        /// accepting new connections and wait for existing connections to complete before
        /// returning.
        /// </summary>
        public void ListenAndDispatchConnections()
        {
            Debug.Assert(SynchronizationContext.Current == null);

            // We loop here continuously, dispatching client connections as 
            // they come in, until TimeoutFired causes an exception to be
            // thrown. Each time through the loop we either have accepted a
            // client connection, or timed out. After each connection, 
            // we need to create a new instance of the pipe to listen on.
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
                    break;
                }

                bool startTimer;
                lock (this.sharedState)
                {
                    if (!this.sharedState.ShouldAcceptNewConnections)
                    {
                        break;
                    }

                    this.sharedState.WaitingPipe = pipeStream;
                    this.sharedState.ClearCompletedConnections();

                    // Whenever the server is doing no work (has no active connections) then the timeout
                    // should be in effect.  Connection completion does a similar check to restart the 
                    // timer.  
                    startTimer = this.sharedState.ActiveConnections.Count == 0;
                }

                if (startTimer)
                {
                    StartTimeoutTimer();
                }

                bool hadException = false;
                try
                {
                    // Wait for a connection or the timeout.  If a timeout occurs then the pipe will be
                    // closed by the timeout handler and an exception will occur here.
                    CompilerServerLogger.Log("Waiting for new connection");
                    pipeStream.WaitForConnection();
                    CompilerServerLogger.Log("Pipe connection detected.");
                }
                catch (Exception e)
                {
                    CompilerServerLogger.Log("Listening error: {0}", e.Message);
                    hadException = true;
                }

                lock (this.sharedState)
                {
                    this.sharedState.WaitingPipe = null;
                    if (hadException || !this.sharedState.ShouldAcceptNewConnections)
                    {
                        break;
                    }
                }

                // Cancel the timeouts
                this.keepAliveTimer.CancelIfActive();

                // Dispatch the new connection on the thread pool.
                var newConnection = DispatchConnection(pipeStream);

                lock (this.sharedState)
                {
                    this.sharedState.ActiveConnections.Add(newConnection);
                }

                if (this.keepAliveTimer.StopAfterFirstConnection)
                {
                    break;
                }
            }

            // At this point no new connections will be accepted.  The server process needs 
            // to wait until all of the existing requests are completed before exiting though.
            Task[] array;
            lock (this.sharedState)
            {
                array = this.sharedState.ActiveConnections.ToArray();
            }

            Task.WhenAll(array).Wait();
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
                }
                else
                {
                    CompilerServerLogger.Log("Memory tight - rejecting connection.");
                    // As long as we haven't written a response, the client has not 
                    // committed to this server instance and can look elsewhere.
                    pipeStream.Close();
                }
                ConnectionCompleted();
            }
            catch (Exception e) if (FatalError.Report(e))
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
        /// Called when a given Connection is completed.  
        /// </summary>
        private void ConnectionCompleted()
        {
            bool isQuiet = false;
            lock (this.sharedState)
            {
                this.sharedState.ClearCompletedConnections();
                isQuiet = this.sharedState.ActiveConnections.Count == 0;
                CompilerServerLogger.Log("Removed connection, {0} remaining.", this.sharedState.ActiveConnections.Count);
            }

            if (isQuiet)
            {
                StartTimeoutTimer();

                // Start GC timer
                const int GC_TIMEOUT = 30 * 1000; // 30 seconds
                Task.Delay(GC_TIMEOUT).ContinueWith(t =>
                {
                    lock (this.sharedState)
                    {
                        this.sharedState.ClearCompletedConnections();
                        if (this.sharedState.ActiveConnections.Count == 0)
                        {
                            GC.Collect();
                        }
                    }
                });
            }
        }

        private void StartTimeoutTimer()
        {
            if (this.keepAliveTimer.IsKeepAliveFinite)
            {
                this.keepAliveTimer.StartTimer()
                    ?.ContinueWith(ServerDieTimeoutFired);
            }
        }

        /// <summary>
        /// Called when the server should begin shutting down.  This will not actively shut down the
        /// server but instead put it in a state where it no longer accepts connections and hangs around
        /// long enough to process all remaining connections.
        /// </summary>
        private void InitiateShutdown()
        {
            lock (this.sharedState)
            {
                this.sharedState.ShouldAcceptNewConnections = false;
                if (this.sharedState.WaitingPipe != null)
                {
                    try
                    {
                        this.sharedState.WaitingPipe.Close();
                    }
                    catch
                    {
                        // Okay if an exception occurs here.  Just want to ensure the server is 
                        // interrupted if it is actively listening for a connection.
                    }
                }
            }

            this.keepAliveTimer.CancelIfActive();
        }

        /// <summary>
        /// Called from the <see cref="AnalyzerWatcher"/> class when an analyzer file
        /// changes on disk.
        /// </summary>
        private void AnalyzerFileChanged()
        {
            InitiateShutdown();
        }

        /// <summary>
        /// The timeout was fired -- check if we need to cancel the pipe.
        /// </summary>
        private void ServerDieTimeoutFired(Task timeoutTask)
        {
            if (timeoutTask.IsCanceled)
            {
                this.keepAliveTimer.Clear();
            }
            else
            {
                // The timeout occured, begin shutting down the server 
                InitiateShutdown();
                CompilerServerLogger.Log("Waiting for pipe connection timed out after {0} ms.", this.keepAliveTimer.KeepAliveTime);
            }
        }

        /// <summary>
        /// All of the data ServerDispatcher shares between threads.  This type does no internal
        /// synchronization.  It is the responsibility of the consumer to synchronize access to 
        /// the members.
        /// </summary>
        private sealed class SharedState
        {
            /// <summary>
            /// The list of active connection objects.  At any given time some
            /// of the items in this list may have completed but not yet been
            /// removed.
            /// </summary>
            public readonly List<Task> ActiveConnections = new List<Task>();

            /// <summary>
            /// When non-null this is the NamedPipeServerStream which the server is 
            /// actively listening for a connection on.
            /// </summary>
            public NamedPipeServerStream WaitingPipe;

            /// <summary>
            /// Should the server accept any new connections.
            /// </summary>
            public bool ShouldAcceptNewConnections = true;

            public void ClearCompletedConnections()
            {
                this.ActiveConnections.RemoveAll(x => x.IsCompleted);
            }
        }
    }
}
