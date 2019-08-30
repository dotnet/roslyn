// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class BuildProtocolTest : TestBase
    {
        private void VerifyShutdownRequest(BuildRequest request)
        {
            Assert.Equal(1, request.Arguments.Count);

            var argument = request.Arguments[0];
            Assert.Equal(BuildProtocolConstants.ArgumentId.Shutdown, argument.ArgumentId);
            Assert.Equal(0, argument.ArgumentIndex);
            Assert.Equal("", argument.Value);
        }

        [Fact]
        public async Task ReadWriteCompleted()
        {
            var response = new CompletedBuildResponse(42, utf8output: false, output: "a string");
            var memoryStream = new MemoryStream();
            await response.WriteAsync(memoryStream, default(CancellationToken));
            Assert.True(memoryStream.Position > 0);
            memoryStream.Position = 0;
            var read = (CompletedBuildResponse)(await BuildResponse.ReadAsync(memoryStream, default(CancellationToken)));
            Assert.Equal(42, read.ReturnCode);
            Assert.False(read.Utf8Output);
            Assert.Equal("a string", read.Output);
            Assert.Equal("", read.ErrorOutput);
        }

        [Fact]
        public async Task ReadWriteRequest()
        {
            var request = new BuildRequest(
                BuildProtocolConstants.ProtocolVersion,
                RequestLanguage.VisualBasicCompile,
                "HashValue",
                ImmutableArray.Create(
                    new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CurrentDirectory, argumentIndex: 0, value: "directory"),
                    new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CommandLineArgument, argumentIndex: 1, value: "file")));
            var memoryStream = new MemoryStream();
            await request.WriteAsync(memoryStream, default(CancellationToken));
            Assert.True(memoryStream.Position > 0);
            memoryStream.Position = 0;
            var read = await BuildRequest.ReadAsync(memoryStream, default(CancellationToken));
            Assert.Equal(BuildProtocolConstants.ProtocolVersion, read.ProtocolVersion);
            Assert.Equal(RequestLanguage.VisualBasicCompile, read.Language);
            Assert.Equal("HashValue", read.CompilerHash);
            Assert.Equal(2, read.Arguments.Count);
            Assert.Equal(BuildProtocolConstants.ArgumentId.CurrentDirectory, read.Arguments[0].ArgumentId);
            Assert.Equal(0, read.Arguments[0].ArgumentIndex);
            Assert.Equal("directory", read.Arguments[0].Value);
            Assert.Equal(BuildProtocolConstants.ArgumentId.CommandLineArgument, read.Arguments[1].ArgumentId);
            Assert.Equal(1, read.Arguments[1].ArgumentIndex);
            Assert.Equal("file", read.Arguments[1].Value);
        }

        [Fact]
        public void ShutdownMessage()
        {
            var request = BuildRequest.CreateShutdown();
            VerifyShutdownRequest(request);
            Assert.Equal(1, request.Arguments.Count);

            var argument = request.Arguments[0];
            Assert.Equal(BuildProtocolConstants.ArgumentId.Shutdown, argument.ArgumentId);
            Assert.Equal(0, argument.ArgumentIndex);
            Assert.Equal("", argument.Value);
        }

        [Fact]
        public async Task ShutdownRequestWriteRead()
        {
            var memoryStream = new MemoryStream();
            var request = BuildRequest.CreateShutdown();
            await request.WriteAsync(memoryStream, CancellationToken.None);
            memoryStream.Position = 0;
            var read = await BuildRequest.ReadAsync(memoryStream, CancellationToken.None);
            VerifyShutdownRequest(read);
        }

        [Fact]
        public async Task ShutdownResponseWriteRead()
        {
            var response = new ShutdownBuildResponse(42);
            Assert.Equal(BuildResponse.ResponseType.Shutdown, response.Type);

            var memoryStream = new MemoryStream();
            await response.WriteAsync(memoryStream, CancellationToken.None);
            memoryStream.Position = 0;

            var read = await BuildResponse.ReadAsync(memoryStream, CancellationToken.None);
            Assert.Equal(BuildResponse.ResponseType.Shutdown, read.Type);
            var typed = (ShutdownBuildResponse)read;
            Assert.Equal(42, typed.ServerProcessId);
        }
    }
}
