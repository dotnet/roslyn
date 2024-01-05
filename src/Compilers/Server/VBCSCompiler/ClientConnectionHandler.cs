// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// This class is responsible for processing a request from a client of the compiler server.
    /// </summary>
    internal sealed class ClientConnectionHandler
    {
        internal ICompilerServerHost CompilerServerHost { get; }
        internal ICompilerServerLogger Logger => CompilerServerHost.Logger;

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
                return await ProcessCoreAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error processing request for client");
                return CompletionData.RequestError;
            }

            async Task<CompletionData> ProcessCoreAsync()
            {
                using var clientConnection = await clientConnectionTask.ConfigureAwait(false);
                var request = await clientConnection.ReadBuildRequestAsync(cancellationToken).ConfigureAwait(false);
                Logger.Log($"Received request {request.RequestId} of type {request.GetType()}");

                if (!string.Equals(request.CompilerHash, BuildProtocolConstants.GetCommitHash(), StringComparison.OrdinalIgnoreCase))
                {
                    return await WriteBuildResponseAsync(
                        clientConnection,
                        request.RequestId,
                        new IncorrectHashBuildResponse(),
                        CompletionData.RequestError,
                        cancellationToken).ConfigureAwait(false);
                }

                if (request.Arguments.Count == 1 && request.Arguments[0].ArgumentId == BuildProtocolConstants.ArgumentId.Shutdown)
                {
                    return await WriteBuildResponseAsync(
                        clientConnection,
                        request.RequestId,
                        new ShutdownBuildResponse(Process.GetCurrentProcess().Id),
                        new CompletionData(CompletionReason.RequestCompleted, shutdownRequested: true),
                        cancellationToken).ConfigureAwait(false);
                }

                if (!allowCompilationRequests)
                {
                    return await WriteBuildResponseAsync(
                        clientConnection,
                        request.RequestId,
                        new RejectedBuildResponse("Compilation not allowed at this time"),
                        CompletionData.RequestCompleted,
                        cancellationToken).ConfigureAwait(false);
                }

                if (!Environment.Is64BitProcess && !MemoryHelper.IsMemoryAvailable(Logger))
                {
                    return await WriteBuildResponseAsync(
                        clientConnection,
                        request.RequestId,
                        new RejectedBuildResponse("Not enough resources to accept connection"),
                        CompletionData.RequestError,
                        cancellationToken).ConfigureAwait(false);
                }

                return await ProcessCompilationRequestAsync(clientConnection, request, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<CompletionData> WriteBuildResponseAsync(IClientConnection clientConnection, Guid requestId, BuildResponse response, CompletionData completionData, CancellationToken cancellationToken)
        {
            var message = response switch
            {
                RejectedBuildResponse r => $"Writing {r.Type} response '{r.Reason}' for {requestId}",
                _ => $"Writing {response.Type} response for {requestId}"
            };
            Logger.Log(message);
            await clientConnection.WriteBuildResponseAsync(response, cancellationToken).ConfigureAwait(false);
            return completionData;
        }

        private async Task<CompletionData> ProcessCompilationRequestAsync(IClientConnection clientConnection, BuildRequest request, CancellationToken cancellationToken)
        {
            // Need to wait for the compilation and client disconnection in parallel. If the client
            // suddenly disconnects we need to cancel the compilation that is occurring. It could be the 
            // client hit Ctrl-C due to a run away analyzer.
            var buildCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var compilationTask = ProcessCompilationRequestCoreAsync(CompilerServerHost, request, buildCancellationTokenSource.Token);
            await Task.WhenAny(compilationTask, clientConnection.DisconnectTask).ConfigureAwait(false);

            try
            {
                if (compilationTask.IsCompleted)
                {
                    BuildResponse response;
                    CompletionData completionData;
                    try
                    {
                        response = await compilationTask.ConfigureAwait(false);
                        completionData = response switch
                        {
                            // Once there is an analyzer inconsistency the assembly load space is polluted. The 
                            // request is an error.
                            AnalyzerInconsistencyBuildResponse _ => CompletionData.RequestError,
                            _ => new CompletionData(CompletionReason.RequestCompleted, newKeepAlive: CheckForNewKeepAlive(request))
                        };
                    }
                    catch (Exception ex)
                    {
                        // The compilation task should never throw. If it does we need to assume that the compiler is
                        // in a bad state and need to issue a RequestError
                        Logger.LogException(ex, $"Exception running compilation for {request.RequestId}");
                        response = new RejectedBuildResponse($"Exception during compilation: {ex.Message}");
                        completionData = CompletionData.RequestError;
                    }

                    return await WriteBuildResponseAsync(
                        clientConnection,
                        request.RequestId,
                        response,
                        completionData,
                        cancellationToken).ConfigureAwait(false);
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

            static Task<BuildResponse> ProcessCompilationRequestCoreAsync(ICompilerServerHost compilerServerHost, BuildRequest buildRequest, CancellationToken cancellationToken)
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
