// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using System.Runtime.InteropServices;
using System.IO.Pipes;
using System.Data.SqlClient;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class ClientConnectionTests
    {
        private static readonly BuildRequest s_emptyCSharpBuildRequest = new BuildRequest(
            BuildProtocolConstants.ProtocolVersion,
            RequestLanguage.CSharpCompile,
            BuildProtocolConstants.GetCommitHash(),
            ImmutableArray<BuildRequest.Argument>.Empty);

        private static readonly BuildResponse s_emptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty);

        private static async Task<(NamedPipeClientStream Client, NamedPipeServerStream Server)> CreateNamedPipePair()
        {
            var pipeName = Guid.NewGuid().ToString("N").Substring(0, 10);
            var serverStream = NamedPipeUtil.CreateServer(pipeName);
            var clientStream = NamedPipeUtil.CreateClient(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            var listenTask = serverStream.WaitForConnectionAsync();
            await clientStream.ConnectAsync().ConfigureAwait(false);
            await listenTask.ConfigureAwait(false);
            return (clientStream, serverStream);
        }

        [Fact]
        public async Task ReadFailure()
        {
            var host = new TestableCompilerServerHost(runCompilation: delegate
            {
                return s_emptyBuildResponse;
            });

            var (clientStream, serverStream) = await CreateNamedPipePair().ConfigureAwait(false);
            try
            {
                var connection = new NamedPipeClientConnection(host, "identifier", serverStream);
                clientStream.Close();
                var connectionData = await connection.HandleConnection().ConfigureAwait(false);
                Assert.Equal(CompletionReason.CompilationNotStarted, connectionData.CompletionReason);
                Assert.Null(connectionData.KeepAlive);
            }
            finally
            {
                clientStream.Close();
                serverStream.Close();
            }
        }

        /// <summary>
        /// A failure to write the results to the client is considered a client disconnection.  Any error
        /// from when the build starts to when the write completes should be handled this way. 
        /// </summary>
        [Fact]
        public async Task WriteError()
        {
            using var compileMre = new ManualResetEvent(initialState: false);
            using var closedStreamMre = new ManualResetEvent(initialState: false);
            var host = new TestableCompilerServerHost(runCompilation: delegate
            {
                compileMre.Set();
                closedStreamMre.WaitOne();
                return s_emptyBuildResponse;
            });

            var (clientStream, serverStream) = await CreateNamedPipePair().ConfigureAwait(false);
            try
            {
                var connection = new NamedPipeClientConnection(host, "identifier", serverStream);

                await s_emptyCSharpBuildRequest.WriteAsync(clientStream).ConfigureAwait(false);
                var connectionTask = connection.HandleConnection();
                await compileMre.WaitOneAsync().ConfigureAwait(false);
                clientStream.Close();
                closedStreamMre.Set();

                var connectionData = await connectionTask.ConfigureAwait(false);
                Assert.Equal(CompletionReason.ClientDisconnect, connectionData.CompletionReason);
                Assert.Null(connectionData.KeepAlive);
            }
            finally
            {
                clientStream.Close();
                serverStream.Close();
            }
        }

        [Fact]
        public async Task NoCompilationsRejectBuildRequest()
        {
            var host = new TestableCompilerServerHost(runCompilation: delegate
            {
                // We should never get here.
                Assert.False(true);
                throw null;
            });

            var (clientStream, serverStream) = await CreateNamedPipePair().ConfigureAwait(false);
            try
            {
                var connection = new NamedPipeClientConnection(host, "identifier", serverStream);
                await s_emptyCSharpBuildRequest.WriteAsync(clientStream).ConfigureAwait(false);
                var connectionData = await connection.HandleConnection(allowCompilationRequests: false).ConfigureAwait(false);
                Assert.Equal(CompletionReason.CompilationNotStarted, connectionData.CompletionReason);
                Assert.Null(connectionData.KeepAlive);

                var response = await BuildResponse.ReadAsync(clientStream);
                Assert.Equal(BuildResponse.ResponseType.Rejected, response.Type);
            }
            finally
            {
                clientStream.Close();
                serverStream.Close();
            }
        }

        [Fact]
        public async Task NoCompilationsProcessShutdown()
        {
            var host = new TestableCompilerServerHost(runCompilation: delegate
            {
                // We should never get here.
                Assert.False(true);
                throw null;
            });

            var (clientStream, serverStream) = await CreateNamedPipePair().ConfigureAwait(false);
            try
            {
                var connection = new NamedPipeClientConnection(host, "identifier", serverStream);
                await BuildRequest.CreateShutdown().WriteAsync(clientStream).ConfigureAwait(false);
                var connectionData = await connection.HandleConnection(allowCompilationRequests: false).ConfigureAwait(false);
                Assert.Equal(CompletionReason.ClientShutdownRequest, connectionData.CompletionReason);
                Assert.Null(connectionData.KeepAlive);

                var response = await BuildResponse.ReadAsync(clientStream);
                Assert.Equal(BuildResponse.ResponseType.Shutdown, response.Type);
            }
            finally
            {
                clientStream.Close();
                serverStream.Close();
            }
        }
    }
}
