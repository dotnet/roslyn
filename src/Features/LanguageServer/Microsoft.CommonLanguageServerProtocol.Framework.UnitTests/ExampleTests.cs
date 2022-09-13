// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

public partial class ExampleTests
{
    [Fact]
    public async Task InitializeServer_SerializesCorrectly()
    {
        var logger = GetLogger();
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);

        var result = await server.InitializeServerAsync();
        Assert.True(result.Capabilities.SemanticTokensOptions!.Range!.Value.First);
    }

    [Fact]
    public async Task ShutdownServer_Succeeds()
    {
        var logger = GetLogger();
        var server = TestExampleLanguageServer.CreateLanguageServer(logger);

        _ = await server.InitializeServerAsync();

        await server.ShutdownServerAsync();

        var result = await server.WaitForShutdown();
        Assert.True(0 == result, "Server failed to shut down properly");
    }

    private static ILspLogger GetLogger()
    {
        return NoOpLspLogger.Instance;
    }
}
