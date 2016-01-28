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

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class ClientConnectionTests 
    {
        private sealed class TestableClientConnection : ClientConnection
        {
            internal Stream Stream;
            internal Func<CancellationToken, Task> CreateMonitorDisconnectTaskFunc;
            internal Func<BuildRequest, CancellationToken, Task<BuildResponse>> ServeBuildRequestFunc;
            internal Action<BuildRequest> ValidateBuildRequestFunc;

            internal TestableClientConnection(ICompilerServerHost compilerServerHost, Stream stream)
                :base(compilerServerHost, "identifier", stream)
            {
                Stream = stream;
                CreateMonitorDisconnectTaskFunc = ct => Task.Delay(-1, ct);
            }

            public override void Close()
            {

            }

            protected override void ValidateBuildRequest(BuildRequest request)
            {
                ValidateBuildRequestFunc?.Invoke(request);
            }

            protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
            {
                return CreateMonitorDisconnectTaskFunc(cancellationToken);
            }

            protected override Task<BuildResponse> ServeBuildRequest(BuildRequest request, CancellationToken cancellationToken)
            {
                if (ServeBuildRequestFunc != null)
                {
                    return ServeBuildRequestFunc(request, cancellationToken);
                }

                return base.ServeBuildRequest(request, cancellationToken);
            }
        }

        private sealed class TestableStream : Stream
        {
            internal readonly MemoryStream ReadStream = new MemoryStream();
            internal readonly MemoryStream WriteStream = new MemoryStream();

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length { get { throw new NotImplementedException(); } }
            public override long Position
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override void Flush()
            {

            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ReadStream.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return ReadStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteStream.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return WriteStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        private static readonly BuildRequest s_emptyCSharpBuildRequest = new BuildRequest(
            1,
            RequestLanguage.CSharpCompile,
            ImmutableArray<BuildRequest.Argument>.Empty);

        private static readonly BuildResponse s_emptyBuildResponse = new CompletedBuildResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty);

        private static TestableClientConnection CreateConnection(Stream stream, ICompilerServerHost compilerServerHost = null)
        {
            compilerServerHost = compilerServerHost ?? new Mock<ICompilerServerHost>().Object;
            return new TestableClientConnection(compilerServerHost, stream);
        }

        [Fact]
        public async Task ReadFailure()
        {
            var stream = new Mock<Stream>(MockBehavior.Strict);
            var connection = CreateConnection(stream.Object);
            var result = await connection.HandleConnection().ConfigureAwait(true);
            Assert.Equal(CompletionReason.CompilationNotStarted, result.CompletionReason);
        }

        /// <summary>
        /// A failure to write the results to the client is considered a client disconnection.  Any error
        /// from when the build starts to when the write completes should be handled this way. 
        /// </summary>
        [Fact]
        public async Task WriteError()
        {
            var realStream = new MemoryStream();
            await s_emptyCSharpBuildRequest.WriteAsync(realStream, CancellationToken.None).ConfigureAwait(true);
            realStream.Position = 0;

            var stream = new Mock<Stream>(MockBehavior.Strict);
            stream
                .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] array, int start, int length, CancellationToken ct) => Task.FromResult(realStream.Read(array, start, length)));

            var connection = CreateConnection(stream.Object);
            connection.ServeBuildRequestFunc = delegate { return Task.FromResult(s_emptyBuildResponse); };
            var connectionData = await connection.HandleConnection().ConfigureAwait(true);
            Assert.Equal(CompletionReason.ClientDisconnect, connectionData.CompletionReason);
            Assert.Null(connectionData.KeepAlive);
        }

        /// <summary>
        /// Ensure the Connection correctly handles the case where a client disconnects while in the 
        /// middle of a build event.
        /// </summary>
        [Fact]
        public async Task ClientDisconnectsDuringBuild()
        {
            var memoryStream = new MemoryStream();
            await s_emptyCSharpBuildRequest.WriteAsync(memoryStream, CancellationToken.None).ConfigureAwait(true);
            memoryStream.Position = 0;

            // Fake a long running build task here that we can validate later on. 
            var buildTaskSource = new TaskCompletionSource<BuildResponse>();
            var buildTaskCancellationToken = default(CancellationToken);
            var clientConnection = CreateConnection(memoryStream);
            clientConnection.ServeBuildRequestFunc = (req, ct) =>
            {
                buildTaskCancellationToken = ct;
                return buildTaskSource.Task;
            };

            var readyTaskSource = new TaskCompletionSource<bool>();
            var monitorTaskSource = new TaskCompletionSource<bool>();
            clientConnection.CreateMonitorDisconnectTaskFunc = (ct) =>
            {
                readyTaskSource.SetResult(true);
                return monitorTaskSource.Task;
            };

            var handleTask = clientConnection.HandleConnection();

            // Wait until the monitor task is actually created and running. 
            await readyTaskSource.Task.ConfigureAwait(false);

            // Now simulate a disconnect by the client.  
            monitorTaskSource.SetResult(true);

            var connectionData = await handleTask.ConfigureAwait(true);
            Assert.Equal(CompletionReason.ClientDisconnect, connectionData.CompletionReason);
            Assert.Null(connectionData.KeepAlive);
            Assert.True(buildTaskCancellationToken.IsCancellationRequested);
        }

        [Fact]
        public async Task ValidateBuildRequest()
        {
            var stream = new TestableStream();
            await s_emptyCSharpBuildRequest.WriteAsync(stream.ReadStream, CancellationToken.None).ConfigureAwait(true);
            stream.ReadStream.Position = 0;

            var validated = false;
            var connection = CreateConnection(stream);
            connection.ServeBuildRequestFunc = (req, token) => Task.FromResult(s_emptyBuildResponse);
            connection.ValidateBuildRequestFunc = _ => { validated = true; };
            await connection.HandleConnection().ConfigureAwait(true);

            Assert.True(validated);
        }

        [Fact]
        public async Task NoCompilationsRejectBuildRequest()
        {
            var stream = new TestableStream();
            await s_emptyCSharpBuildRequest.WriteAsync(stream.ReadStream, CancellationToken.None).ConfigureAwait(true);
            stream.ReadStream.Position = 0;

            var connection = CreateConnection(stream);
            var connectionData = await connection.HandleConnection(allowCompilationRequests: false).ConfigureAwait(false);
            Assert.Equal(CompletionReason.CompilationNotStarted, connectionData.CompletionReason);

            stream.WriteStream.Position = 0;
            var response = await BuildResponse.ReadAsync(stream.WriteStream).ConfigureAwait(false);
            Assert.Equal(BuildResponse.ResponseType.Rejected, response.Type);
        }

        [Fact]
        public async Task NoCompilationsProcessShutdown()
        {
            var stream = new TestableStream();
            await BuildRequest.CreateShutdown().WriteAsync(stream.ReadStream, CancellationToken.None).ConfigureAwait(true);
            stream.ReadStream.Position = 0;

            var connection = CreateConnection(stream);
            var connectionData = await connection.HandleConnection(allowCompilationRequests: false).ConfigureAwait(false);
            Assert.Equal(CompletionReason.ClientShutdownRequest, connectionData.CompletionReason);

            stream.WriteStream.Position = 0;
            var response = await BuildResponse.ReadAsync(stream.WriteStream).ConfigureAwait(false);
            Assert.Equal(BuildResponse.ResponseType.Shutdown, response.Type);
        }
    }
}
