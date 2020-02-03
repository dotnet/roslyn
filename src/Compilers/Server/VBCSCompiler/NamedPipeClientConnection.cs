// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using System.IO.Pipes;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal readonly struct ConnectionData
    {
        public readonly CompletionReason CompletionReason;
        public readonly TimeSpan? KeepAlive;

        public ConnectionData(CompletionReason completionReason, TimeSpan? keepAlive = null)
        {
            CompletionReason = completionReason;
            KeepAlive = keepAlive;
        }
    }

    internal enum CompletionReason
    {
        /// <summary>
        /// There was an error creating the <see cref="BuildRequest"/> object and a compilation was never
        /// created.
        /// </summary>
        CompilationNotStarted,

        /// <summary>
        /// The compilation completed and results were provided to the client.
        /// </summary>
        CompilationCompleted,

        /// <summary>
        /// The compilation process was initiated and the client disconnected before
        /// the results could be provided to them.
        /// </summary>
        ClientDisconnect,

        /// <summary>
        /// There was an unhandled exception processing the result.
        /// </summary>
        ClientException,

        /// <summary>
        /// There was a request from the client to shutdown the server.
        /// </summary>
        ClientShutdownRequest,
    }

    /// <summary>
    /// Represents a single connection from a client process. Handles the named pipe
    /// from when the client connects to it, until the request is finished or abandoned.
    /// A new task is created to actually service the connection and do the operation.
    /// </summary>
    internal sealed class NamedPipeClientConnection : IClientConnection
    {
        private readonly ICompilerServerHost _compilerServerHost;
        private readonly string _loggingIdentifier;
        private readonly NamedPipeServerStream _stream;

        public string LoggingIdentifier => _loggingIdentifier;

        public NamedPipeClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, NamedPipeServerStream stream)
        {
            _compilerServerHost = compilerServerHost;
            _loggingIdentifier = loggingIdentifier;
            _stream = stream;
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        private Task MonitorDisconnectAsync(CancellationToken cancellationToken)
        {
            return BuildServerConnection.MonitorDisconnectAsync(_stream, LoggingIdentifier, cancellationToken);
        }

        private void ValidateBuildRequest(BuildRequest request)
        {
            // Now that we've read data from the stream we can validate the identity.
            if (!NamedPipeUtil.CheckClientElevationMatches(_stream))
            {
                throw new Exception("Client identity does not match server identity.");
            }
        }

        /// <summary>
        /// Close the connection.  Can be called multiple times.
        /// </summary>
        public void Close()
        {
            CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: Closing.");
            try
            {
                _stream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format($"Pipe {LoggingIdentifier}: Error closing pipe.");
                CompilerServerLogger.LogException(e, msg);
            }
        }

        public async Task<ConnectionData> HandleConnectionAsync(bool allowCompilationRequests = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                BuildRequest request;
                try
                {
                    Log("Begin reading request.");
                    request = await BuildRequest.ReadAsync(_stream, cancellationToken).ConfigureAwait(false);
                    ValidateBuildRequest(request);
                    Log("End reading request.");
                }
                catch (Exception e)
                {
                    LogException(e, "Error reading build request.");
                    return new ConnectionData(CompletionReason.CompilationNotStarted);
                }

                if (request.ProtocolVersion != BuildProtocolConstants.ProtocolVersion)
                {
                    return await HandleMismatchedVersionRequestAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (!string.Equals(request.CompilerHash, BuildProtocolConstants.GetCommitHash(), StringComparison.OrdinalIgnoreCase))
                {
                    return await HandleIncorrectHashRequestAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (IsShutdownRequest(request))
                {
                    return await HandleShutdownRequestAsync(cancellationToken).ConfigureAwait(false);
                }
                else if (!allowCompilationRequests)
                {
                    return await HandleRejectedRequestAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await HandleCompilationRequestAsync(request, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Close();
            }
        }

        private async Task<ConnectionData> HandleCompilationRequestAsync(BuildRequest request, CancellationToken cancellationToken)
        {
            var keepAlive = CheckForNewKeepAlive(request);

            // Kick off both the compilation and a task to monitor the pipe for closing.
            var buildCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var compilationTask = ServeBuildRequestAsync(request, buildCts.Token);
            var monitorTask = MonitorDisconnectAsync(buildCts.Token);
            await Task.WhenAny(compilationTask, monitorTask).ConfigureAwait(false);

            // Do an 'await' on the completed task, preference being compilation, to force
            // any exceptions to be realized in this method for logging.
            CompletionReason reason;
            if (compilationTask.IsCompleted)
            {
                var response = await compilationTask.ConfigureAwait(false);

                try
                {
                    Log("Begin writing response.");
                    await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
                    reason = CompletionReason.CompilationCompleted;
                    Log("End writing response.");
                }
                catch
                {
                    reason = CompletionReason.ClientDisconnect;
                }
            }
            else
            {
                await monitorTask.ConfigureAwait(false);
                reason = CompletionReason.ClientDisconnect;
            }

            // Begin the tear down of the Task which didn't complete.
            buildCts.Cancel();
            return new ConnectionData(reason, keepAlive);
        }

        private async Task<ConnectionData> HandleMismatchedVersionRequestAsync(CancellationToken cancellationToken)
        {
            var response = new MismatchedVersionBuildResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleIncorrectHashRequestAsync(CancellationToken cancellationToken)
        {
            var response = new IncorrectHashBuildResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleRejectedRequestAsync(CancellationToken cancellationToken)
        {
            var response = new RejectedBuildResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleShutdownRequestAsync(CancellationToken cancellationToken)
        {
            var id = Process.GetCurrentProcess().Id;
            var response = new ShutdownBuildResponse(id);
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.ClientShutdownRequest);
        }

        /// <summary>
        /// Check the request arguments for a new keep alive time. If one is present,
        /// set the server timer to the new time.
        /// </summary>
        private TimeSpan? CheckForNewKeepAlive(BuildRequest request)
        {
            TimeSpan? timeout = null;
            foreach (var arg in request.Arguments)
            {
                if (arg.ArgumentId == BuildProtocolConstants.ArgumentId.KeepAlive)
                {
                    int result;
                    // If the value is not a valid integer for any reason,
                    // ignore it and continue with the current timeout. The client
                    // is responsible for validating the argument.
                    if (int.TryParse(arg.Value, out result))
                    {
                        // Keep alive times are specified in seconds
                        timeout = TimeSpan.FromSeconds(result);
                    }
                }
            }

            return timeout;
        }

        private bool IsShutdownRequest(BuildRequest request)
        {
            return request.Arguments.Count == 1 && request.Arguments[0].ArgumentId == BuildProtocolConstants.ArgumentId.Shutdown;
        }

        private Task<BuildResponse> ServeBuildRequestAsync(BuildRequest buildRequest, CancellationToken cancellationToken)
        {
            Func<BuildResponse> func = () =>
            {
                // Do the compilation
                Log("Begin compilation");

                var request = BuildProtocolUtil.GetRunRequest(buildRequest);
                var response = _compilerServerHost.RunCompilation(request, cancellationToken);

                Log("End compilation");
                return response;
            };

            var task = new Task<BuildResponse>(func, cancellationToken, TaskCreationOptions.LongRunning);
            task.Start();
            return task;
        }

        private void Log(string message)
        {
            CompilerServerLogger.Log("Client {0}: {1}", _loggingIdentifier, message);
        }

        private void LogException(Exception e, string message)
        {
            CompilerServerLogger.LogException(e, string.Format("Client {0}: {1}", _loggingIdentifier, message));
        }
    }
}
