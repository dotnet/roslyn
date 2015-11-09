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
        [Fact]
        public void ReadWriteCompleted()
        {
            Task.Run(async () =>
            {
                var response = new CompletedBuildResponse(42, utf8output: false, output: "a string", errorOutput: "b string");
                var memoryStream = new MemoryStream();
                await response.WriteAsync(memoryStream, default(CancellationToken)).ConfigureAwait(false);
                Assert.True(memoryStream.Position > 0);
                memoryStream.Position = 0;
                var read = (CompletedBuildResponse)(await BuildResponse.ReadAsync(memoryStream, default(CancellationToken)).ConfigureAwait(false));
                Assert.Equal(42, read.ReturnCode);
                Assert.False(read.Utf8Output);
                Assert.Equal("a string", read.Output);
                Assert.Equal("b string", read.ErrorOutput);
            }).Wait();
        }

        [Fact]
        public void ReadWriteRequest()
        {
            Task.Run(async () =>
            {
                var request = new BuildRequest(
                    BuildProtocolConstants.ProtocolVersion,
                    RequestLanguage.VisualBasicCompile,
                    ImmutableArray.Create(
                        new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CurrentDirectory, argumentIndex: 0, value: "directory"),
                        new BuildRequest.Argument(BuildProtocolConstants.ArgumentId.CommandLineArgument, argumentIndex: 1, value: "file")));
                var memoryStream = new MemoryStream();
                await request.WriteAsync(memoryStream, default(CancellationToken)).ConfigureAwait(false);
                Assert.True(memoryStream.Position > 0);
                memoryStream.Position = 0;
                var read = await BuildRequest.ReadAsync(memoryStream, default(CancellationToken)).ConfigureAwait(false);
                Assert.Equal(BuildProtocolConstants.ProtocolVersion, read.ProtocolVersion);
                Assert.Equal(RequestLanguage.VisualBasicCompile, read.Language);
                Assert.Equal(2, read.Arguments.Length);
                Assert.Equal(BuildProtocolConstants.ArgumentId.CurrentDirectory, read.Arguments[0].ArgumentId);
                Assert.Equal(0, read.Arguments[0].ArgumentIndex);
                Assert.Equal("directory", read.Arguments[0].Value);
                Assert.Equal(BuildProtocolConstants.ArgumentId.CommandLineArgument, read.Arguments[1].ArgumentId);
                Assert.Equal(1, read.Arguments[1].ArgumentIndex);
                Assert.Equal("file", read.Arguments[1].Value);
            }).Wait();
        }
    }
}
