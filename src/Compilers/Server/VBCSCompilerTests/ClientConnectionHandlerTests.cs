// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using static Microsoft.CodeAnalysis.CommandLine.BuildResponse;
namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class ClientConnectionHandlerTests
    {
        [Fact]
        public async Task ThrowDuringBuild()
        {
            var compilerServerHost = new TestableCompilerServerHost(delegate { throw new Exception(); });
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyCSharpBuildRequest),
            };
            var completionData = await clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection));
            Assert.Equal(CompletionData.RequestError, completionData);
        }

        [Fact]
        public async Task ThrowReadingRequest()
        {
            var compilerServerHost = new TestableCompilerServerHost(delegate
            {
                Assert.True(false, "Should not reach compilation");
                throw new Exception("");
            });
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => throw new Exception(),
            };

            var completionData = await clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection));
            Assert.Equal(CompletionData.RequestError, completionData);
        }

        [Fact]
        public async Task ThrowWritingResponse()
        {
            var compilerServerHost = new TestableCompilerServerHost(delegate { return ProtocolUtil.EmptyBuildResponse; });
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var threwException = false;
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyCSharpBuildRequest),
                WriteBuildResponseFunc = (response, cancellationToken) =>
                {
                    threwException = true;
                    throw new Exception("");
                }
            };

            var completionData = await clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection));
            Assert.Equal(CompletionData.RequestError, completionData);
            Assert.True(threwException);
        }

        /// <summary>
        /// Make sure that when compilation requests are disallowed we don't actually process them
        /// </summary>
        [Fact]
        public async Task CompilationsDisallowed()
        {
            var hitCompilation = false;
            var compilerServerHost = new TestableCompilerServerHost(delegate
            {
                hitCompilation = true;
                Assert.True(false, "Should not reach compilation when compilations are disallowed");
                throw new Exception("");
            });

            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);

            BuildResponse? response = null;
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyCSharpBuildRequest),
                WriteBuildResponseFunc = (r, _) =>
                {
                    response = r;
                    return Task.CompletedTask;
                }
            };

            var completionData = await clientConnectionHandler.ProcessAsync(
                Task.FromResult<IClientConnection>(clientConnection),
                allowCompilationRequests: false);

            Assert.Equal(CompletionData.RequestCompleted, completionData);
            Assert.True(response is RejectedBuildResponse);
            Assert.False(hitCompilation);
        }

        /// <summary>
        /// If a client requests a shutdown nothing else about the request should be processed
        /// </summary>
        [Theory]
        [CombinatorialData]
        public async Task ShutdownRequest(bool allowCompilationRequests)
        {
            var hitCompilation = false;
            var compilerServerHost = new TestableCompilerServerHost(delegate
            {
                hitCompilation = true;
                throw new Exception("");
            });

            BuildResponse? response = null;
            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(BuildRequest.CreateShutdown()),
                WriteBuildResponseFunc = (r, _) =>
                {
                    response = r;
                    return Task.CompletedTask;
                }
            };

            var completionData = await clientConnectionHandler.ProcessAsync(
                Task.FromResult<IClientConnection>(clientConnection),
                allowCompilationRequests: allowCompilationRequests);

            Assert.False(hitCompilation);
            Assert.Equal(new CompletionData(CompletionReason.RequestCompleted, shutdownRequested: true), completionData);
            Assert.True(response is ShutdownBuildResponse);
        }

        [Fact]
        public async Task ClientDisconnectDuringBuild()
        {
            using var buildStartedMre = new ManualResetEvent(initialState: false);
            using var clientClosedMre = new ManualResetEvent(initialState: false);
            var compilerServerHost = new TestableCompilerServerHost((request, cancellationToken) =>
            {
                buildStartedMre.Set();
                clientClosedMre.WaitOne();
                Assert.True(cancellationToken.IsCancellationRequested);
                return ProtocolUtil.EmptyBuildResponse;
            });

            var disconnectTaskCompletionSource = new TaskCompletionSource<object?>();
            var isDisposed = false;
            var clientConnection = new TestableClientConnection()
            {
                ReadBuildRequestFunc = _ => Task.FromResult(ProtocolUtil.EmptyBasicBuildRequest),
                DisconnectTask = disconnectTaskCompletionSource.Task,
                DisposeFunc = () => { isDisposed = true; },
            };

            var clientConnectionHandler = new ClientConnectionHandler(compilerServerHost);
            var task = clientConnectionHandler.ProcessAsync(Task.FromResult<IClientConnection>(clientConnection));

            // Don't trigger the disconnect until we confirm that the client has issued a 
            // build request.
            buildStartedMre.WaitOne();
            disconnectTaskCompletionSource.TrySetResult(null);

            var completionData = await task;
            Assert.Equal(CompletionData.RequestError, completionData);
            Assert.True(isDisposed);

            clientClosedMre.Set();
        }
    }
}
