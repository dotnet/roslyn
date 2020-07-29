// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// This class is responsible for processing a request from a client of the compiler server.
    /// </summary>
    internal sealed class ClientConnectionHandler
    {
        internal ICompilerServerHost CompilerServerHost { get; }

        internal ClientConnectionHandler(ICompilerServerHost compilerServerHost)
        {
            CompilerServerHost = compilerServerHost;
        }

        /// <summary>
        /// Handles a client connection. The returned task here will never fail. Instead all exceptions will be wrapped
        /// in a <see cref="CompletionReason.RequestError"/>
        /// </summary>
        internal async Task<CompletionData> ProcessAsync(
            Task<IClientConnection> clientConnectionTask,
            bool allowCompilationRequests = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ProcessCore().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CompilerServerLogger.LogException(ex, $"Error processing request for client");
                return CompletionData.RequestError;
            }

            async Task<CompletionData> ProcessCore()
            {
                using var clientConnection = await clientConnectionTask.ConfigureAwait(false);
                var request = await clientConnection.ReadBuildRequestAsync(cancellationToken).ConfigureAwait(false);

                if (request.ProtocolVersion != BuildProtocolConstants.ProtocolVersion)
                {
                    var response = new MismatchedVersionBuildResponse();
                    await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
                    return CompletionData.RequestCompleted;
                }

                if (!string.Equals(request.CompilerHash, BuildProtocolConstants.GetCommitHash(), StringComparison.OrdinalIgnoreCase))
                {
                    var response = new IncorrectHashBuildResponse();
                    await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
                    return CompletionData.RequestCompleted;
                }

                if (request.Arguments.Count == 1 && request.Arguments[0].ArgumentId == BuildProtocolConstants.ArgumentId.Shutdown)
                {
                    var id = Process.GetCurrentProcess().Id;
                    var response = new ShutdownBuildResponse(id);
                    await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
                    return new CompletionData(CompletionReason.RequestCompleted, shutdownRequested: true);
                }

                if (!allowCompilationRequests)
                {
                    var response = new RejectedBuildResponse("Compilation not allowed at this time");
                    await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
                    return CompletionData.RequestCompleted;
                }

                return await ProcessCompilationRequestAsync(clientConnection, request, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<CompletionData> ProcessCompilationRequestAsync(IClientConnection clientConnection, BuildRequest request, CancellationToken cancellationToken)
        {
            // Need to wait for the compilation and client disconnection in parallel. If the client
            // suddenly disconnects we need to cancel the compilation that is occuring. It could be the 
            // client hit Ctrl-C due to a run away analyzer.
            var buildCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var compilationTask = ProcessCompilationRequestCore(CompilerServerHost, request, buildCancellationTokenSource.Token);
            await Task.WhenAny(compilationTask, clientConnection.DisconnectTask).ConfigureAwait(false);

            try
            {
                if (compilationTask.IsCompleted)
                {
                    BuildResponse response;
                    try
                    {
                        response = await compilationTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        CompilerServerLogger.LogException(ex, $"Exception running compilation for {clientConnection.LoggingIdentifier}");
                        response = new RejectedBuildResponse($"Exception during compilation: {ex.Message}");
                    }

                    await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
                    var newKeepAlive = CheckForNewKeepAlive(request);
                    var completionReason = response switch
                    {
                        AnalyzerInconsistencyBuildResponse _ => CompletionReason.RequestError,
                        RejectedBuildResponse _ => CompletionReason.RequestError,
                        _ => CompletionReason.RequestCompleted
                    };
                    return new CompletionData(completionReason, newKeepAlive);
                }
                else
                {
                    return CompletionData.RequestError;
                }
            }
            finally
            {
                buildCancellationTokenSource.Cancel();
            }

            static Task<BuildResponse> ProcessCompilationRequestCore(ICompilerServerHost compilerServerHost, BuildRequest buildRequest, CancellationToken cancellationToken)
            {
                Func<BuildResponse> func = () =>
                {
                    var request = BuildProtocolUtil.GetRunRequest(buildRequest);
                    var response = compilerServerHost.RunCompilation(request, cancellationToken);
                    return response;
                };

                var task = new Task<BuildResponse>(func, cancellationToken, TaskCreationOptions.LongRunning);
                task.Start();
                return task;
            }
        }

        /// <summary>
        /// Check the request arguments for a new keep alive time. If one is present,
        /// set the server timer to the new time.
        /// </summary>
        private static TimeSpan? CheckForNewKeepAlive(BuildRequest request)
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
    }
}
