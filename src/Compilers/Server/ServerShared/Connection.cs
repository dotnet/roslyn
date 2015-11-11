// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
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
        Completed,

        /// <summary>
        /// The compilation process was initiated and the client disconnected before 
        /// the results could be provided to them.  
        /// </summary>
        ClientDisconnect,

        /// <summary>
        /// There was an unhandled exception processing the result.
        /// </summary>
        ClientException,
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

        /// <summary>
        /// Close the connection.  Can be called multiple times.
        /// </summary>
        public abstract void Close();

        public async Task<ConnectionData> HandleConnection(CancellationToken cancellationToken)
        {
            try
            {
                BuildRequest request;
                try
                {
                    Log("Begin reading request.");
                    request = await BuildRequest.ReadAsync(_stream, cancellationToken).ConfigureAwait(false);
                    Log("End reading request.");
                }
                catch (Exception e)
                {
                    LogException(e, "Error reading build request.");
                    return new ConnectionData(CompletionReason.CompilationNotStarted);
                }

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
                        reason = CompletionReason.Completed;
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
            finally
            {
                Close();
            }
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

        protected virtual Task<BuildResponse> ServeBuildRequest(BuildRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Do the compilation
                    Log("Begin compilation");
                    BuildResponse response = ServeBuildRequestCore(request, cancellationToken);
                    Log("End compilation");
                    return response;
                }
                catch (Exception e) when (FatalError.Report(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            });
        }

        private BuildResponse ServeBuildRequestCore(BuildRequest buildRequest, CancellationToken cancellationToken)
        {
            var request = BuildProtocolUtil.GetRunRequest(buildRequest);
            CommonCompiler compiler;
            if (!_compilerServerHost.TryCreateCompiler(request, out compiler))
            {
                // We can't do anything with a request we don't know about. 
                Log($"Got request with id '{request.Language}'");
                return new CompletedBuildResponse(-1, false, "", "");
            }

            Log($"CurrentDirectory = '{request.CurrentDirectory}'");
            Log($"LIB = '{request.LibDirectory}'");
            for (int i = 0; i < request.Arguments.Length; ++i)
            {
                Log($"Argument[{i}] = '{request.Arguments[i]}'");
            }

            bool utf8output = compiler.Arguments.Utf8Output;
            if (!_compilerServerHost.CheckAnalyzers(request.CurrentDirectory, compiler.Arguments.AnalyzerReferences))
            {
                return new AnalyzerInconsistencyBuildResponse();
            }

            Log($"****Running {request.Language} compiler...");
            TextWriter output = new StringWriter(CultureInfo.InvariantCulture);
            int returnCode = compiler.Run(output, cancellationToken);
            Log($"****{request.Language} Compilation complete.\r\n****Return code: {returnCode}\r\n****Output:\r\n{output.ToString()}\r\n");
            return new CompletedBuildResponse(returnCode, utf8output, output.ToString(), "");
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
