// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ServerDisconnectTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task ServerExitsCleanlyOnStreamReadError()
    {
        var server = await CreateLanguageServerAsync();

        server.SimulateStreamReadError();

        // Server should exit cleanly without throwing.
        await server.ServerExitTask;
    }

    [Fact]
    public async Task ServerExitsCleanlyWhenClientDisconnects()
    {
        var server = await CreateLanguageServerAsync();

        server.SimulateClientDisconnectError();

        // Server should exit cleanly without throwing.
        await server.ServerExitTask;
    }
}
