// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal struct ConnectionData
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
    internal abstract class ClientConnection : IClientConnection
    {
        private readonly ICompilerServerHost _compilerServerHost;
        private readonly string _loggingIdentifier;
        private readonly Stream _stream;

        public string LoggingIdentifier => _loggingIdentifier;

        public ClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, Stream stream)
        {
            _compilerServerHost = compilerServerHost;
            _loggingIdentifier = loggingIdentifier;
            _stream = stream;
        }

        /// <summary>
        /// Returns a Task that resolves if the client stream gets disconnected.  
        /// </summary>
        protected abstract Task CreateMonitorDisconnectTask(CancellationToken cancellationToken);

        protected virtual void ValidateBuildRequest(BuildRequest request)
        {

        }

        /// <summary>
        /// Close the connection.  Can be called multiple times.
        /// </summary>
        public abstract void Close();

        public async Task<ConnectionData> HandleConnection(bool allowCompilationRequests = true, CancellationToken cancellationToken = default(CancellationToken))
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

                if (IsShutdownRequest(request))
                {
                    return await HandleShutdownRequest(cancellationToken).ConfigureAwait(false);
                }
                else if (!allowCompilationRequests)
                {
                    return await HandleRejectedRequest(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await HandleCompilationRequest(request, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Close();
            }
        }

        private async Task<ConnectionData> HandleCompilationRequest(BuildRequest request, CancellationToken cancellationToken)
        {
            var keepAlive = CheckForNewKeepAlive(request);

            // Kick off both the compilation and a task to monitor the pipe for closing.  
            var buildCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var compilationTask = ServeBuildRequest(request, buildCts.Token);
            var monitorTask = CreateMonitorDisconnectTask(buildCts.Token);
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

        private async Task<ConnectionData> HandleRejectedRequest(CancellationToken cancellationToken)
        {
            var response = new RejectedBuildResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleShutdownRequest(CancellationToken cancellationToken)
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
            return request.Arguments.Length == 1 && request.Arguments[0].ArgumentId == BuildProtocolConstants.ArgumentId.Shutdown;
        }

        protected virtual Task<BuildResponse> ServeBuildRequest(BuildRequest buildRequest, CancellationToken cancellationToken)
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
