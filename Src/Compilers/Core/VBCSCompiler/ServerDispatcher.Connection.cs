// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    partial class ServerDispatcher
    {
        /// <summary>
        /// The reason that a connection completed.
        /// </summary>
        private enum CompletionReason { IOFailure = 1, Success, Cancelled, SecurityViolation, TimeOut, InternalError };

        /// <summary>
        /// Represents a single connection from a client process. Handles the named pipe
        /// from when the client connects to it, until the request is finished or abandoned.
        /// A new thread is created to actually service the connection and do the operation.
        /// </summary>
        /// <remarks>
        /// Why do we directly create threads instead of using TPL Tasks? Two reasons:
        /// 1) Compilation tasks are long-running and compute bound. Using a thread pool
        ///    won't provide much advantage and may provide a disadvantage -- we don't want
        ///    a short compilation to get "stuck" waiting for a long compilation to finish.
        ///    We could use TaskCreationOptions.LongRunning, but that causes TPL to create a new
        ///    thread anyway.
        /// 2) Although it's not currently implemented, using a thread allows us to control
        ///    stack size, and possibly to abort the thread.
        ///    
        /// So in this specific case, we don't get any advantage from TPL, and we might want the
        /// flexibility of directly controlling the thread.
        /// </remarks>
        private class Connection
        {
            private const string LogFormat = "Connection {0}: {1}"; // {0} = this.identifer, {1} = normal log message
            internal readonly int LoggingIdentifier; // For logging only - don't depend on this.

            private readonly NamedPipeServerStream pipeStream;
            private readonly IRequestHandler handler;

            private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            private bool isFinished = false;

            /// <summary>
            /// Create a new connection, and listen for request on a new thread.
            /// </summary>
            public Connection(NamedPipeServerStream pipeStream, IRequestHandler handler)
            {
                this.LoggingIdentifier = pipeStream.SafePipeHandle.DangerousGetHandle().ToInt32();
                this.pipeStream = pipeStream;
                this.handler = handler;
            }

            public async Task ServeConnection()
            {
                BuildRequest request;
                try
                {
                    Log("Begin reading request");
                    request = await BuildRequest.ReadAsync(pipeStream, cancellationTokenSource.Token);
                    Log("End reading request");
                }
                catch (IOException e)
                {
                    LogException(e, "Reading request from named pipe.");
                    FinishConnection(CompletionReason.IOFailure);
                    return;
                }
                catch (OperationCanceledException e)
                {
                    LogException(e, "Reading request from named pipe.");
                    FinishConnection(CompletionReason.Cancelled);
                    return;
                }

                if (!ClientAndOurIdentitiesMatch(pipeStream))
                {
                    Log("Client identity doesn't match.");
                    FinishConnection(CompletionReason.SecurityViolation);
                    return;
                }

                // Start a monitor that cancels if the pipe closes on us
                var _ = MonitorPipeForDisconnection().ConfigureAwait(false);

                // Do the compilation
                Log("Begin compilation");
                BuildResponse response = await Task.Run(() =>
                {
                    try
                    {
                        return handler.HandleRequest(request, cancellationTokenSource.Token);
                    }
                    catch (Exception e) if (CompilerFatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                });

                Log("End compilation");

                try
                {
                    Log("Begin writing response");
                    await response.WriteAsync(pipeStream, cancellationTokenSource.Token);
                    Log("End writing response");
                }
                catch (IOException e)
                {
                    LogException(e, "Writing response to named pipe.");
                    FinishConnection(CompletionReason.IOFailure);
                    return;
                }
                catch (OperationCanceledException e)
                {
                    LogException(e, "Writing response to named pipe.");
                    FinishConnection(CompletionReason.Cancelled);
                    return;
                }

                Log("Completed writing response to named pipe.");
                FinishConnection(CompletionReason.Success);
            }

            // The IsConnected property on named pipes does not detect when the client has disconnected
            // if we don't attempt any new I/O after the client disconnects. We start an async I/O here
            // which serves to check the pipe for disconnection. 
            private async Task MonitorPipeForDisconnection()
            {
                var buffer = new byte[0];

                while (!this.isFinished && pipeStream.IsConnected)
                {
                    Log("Before poking pipe.");
                    try
                    {
                        await pipeStream.ReadAsync(buffer, 0, 0).ConfigureAwait(continueOnCapturedContext: false);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Another thread may have closed the stream already.  Not a problem.
                        Log("Pipe has already been closed.");
                        return;
                    }
                    Log("After poking pipe.");
                    // Wait a tenth of a second before trying again
                    await Task.Delay(100);
                }

                Log("Pipe disconnected; cancelling.");
                Cancel();
            }

            /// <summary>
            /// Cancel whatever is being done. This abandons any compilation that is in process.
            /// We do this only if the client connect is cancelled. For example, this occurs if the 
            /// client presses Ctrl+C on a command line compile or MSBuild decides to cancel a build.
            /// We want to cancel the compilation process quickly.
            /// </summary>
            private void Cancel()
            {
                CompilerServerLogger.Log("Cancellation requested.");

                if (this.isFinished)
                {
                    Log("Connection already finished.");
                }
                else
                {
                    // CONSIDER: If we want to be more aggressive, we could wait a small timeout for the 
                    // cancel to finish, and if the thread hasn't terminated, then call thread.Abort().
                    // This would give up CPU resources more quickly in case, say, a compilation is taking a very
                    // long time and isn't checking the cancellation token often enough (say it enters an
                    // infinite loop because of a bug).
                    //
                    // I don't think this is strictly needed, though, because once we mark the connection as
                    // finished (by called FinishConnection), the compilation process is free to go away, 
                    // so the process will be terminated anyway once the process timeout expires (around 30 seconds),
                    // no matter if the thread dies or not. Since ThreadAbortExceptions make me a little nervous,
                    // I've chosen not to go this route for now.
                    //
                    // If we decide to extend the process timeout significantly, and the compiler isn't 
                    // responsive to cancellation requests, then using thread.Abort() might be necessary, because
                    // people will look askance at the compiler burning lots of CPU after the compilation is cancelled.

                    Log("Setting cancellation token to cancelled state.");
                    cancellationTokenSource.Cancel();
                    FinishConnection(CompletionReason.Cancelled);
                }
            }

            /// <summary>
            /// Return the current user name. Also, return if the user is elevated permissions.
            /// </summary>
            private string GetIdentity(bool impersonating, out bool elevatedToAdmin)
            {
                WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent(impersonating);
                WindowsPrincipal currentPrincipal = new WindowsPrincipal(currentIdentity);
                elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
                return currentIdentity.Name;
            }

            /// <summary>
            /// Does the client of "pipeStream" have the same identity and elevation as we do?
            /// </summary>
            private bool ClientAndOurIdentitiesMatch(NamedPipeServerStream pipeStream)
            {
                string ourUserName, clientUserName = null;
                bool ourElevation, clientElevation = false;

                // Get the identities of ourselves and our client.
                ourUserName = GetIdentity(false, out ourElevation);
                pipeStream.RunAsClient(delegate() { clientUserName = GetIdentity(true, out clientElevation); });

                Log("Server identity = '{0}', server elevation='{1}'.", ourUserName, ourElevation);
                Log("Client identity = '{0}', client elevation='{1}'.", clientUserName, clientElevation);
                return (ourUserName == clientUserName && ourElevation == clientElevation);
            }

            /// <summary>
            /// Report that this connection is finished, and the reason.
            /// </summary>
            internal void FinishConnection(CompletionReason completionReason)
            {
                Log("Connection finishing with reason {0}.", completionReason);

                // Close the pipe, we're done with it.
                pipeStream.Close();
                this.isFinished = true;
            }

            private void Log(string message)
            {
                CompilerServerLogger.Log(LogFormat, LoggingIdentifier, message);
            }

            private void Log(string format, params object[] args)
            {
                Log(string.Format(format, args));
            }

            private void LogException(Exception e, string description)
            {
                CompilerServerLogger.LogException(e, string.Format(LogFormat, LoggingIdentifier, description));
            }
        }
    }
}
