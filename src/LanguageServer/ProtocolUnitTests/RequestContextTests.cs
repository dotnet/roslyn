// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class RequestContextTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Fact]
    public async Task ClearSolutionContextClearsAllRequestContextCopies()
    {
        await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace: false);
        var context = await RequestContext.CreateAsync(
            mutatesSolutionState: false,
            requiresLSPSolution: true,
            textDocument: null,
            WellKnownLspServerKinds.AlwaysActiveVSLspServer,
            new ClientCapabilities(),
            [LanguageNames.CSharp],
            testLspServer.GetLspServices(),
            NoOpLspLogger.Instance,
            "test",
            CancellationToken.None);
        var copy = context;

        Assert.NotNull(await copy.GetRequiredSolutionAsync(CancellationToken.None));
        context.ClearSolutionContext();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await copy.GetRequiredWorkspaceAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await copy.GetRequiredSolutionAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await copy.GetTextDocumentAsync(CancellationToken.None));
        copy.TraceDebug("Context logging remains available after the solution is cleared.");
    }

    [Fact]
    public async Task WorkspaceAccessThrowsWhenSolutionContextWasNotRequested()
    {
        await using var testLspServer = await CreateTestLspServerAsync("class C { }", mutatingLspWorkspace: false);
        var contextWithoutSolution = await RequestContext.CreateAsync(
            mutatesSolutionState: false,
            requiresLSPSolution: false,
            textDocument: null,
            WellKnownLspServerKinds.AlwaysActiveVSLspServer,
            new ClientCapabilities(),
            [LanguageNames.CSharp],
            testLspServer.GetLspServices(),
            NoOpLspLogger.Instance,
            "test",
            CancellationToken.None);
        contextWithoutSolution.ClearSolutionContext();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await contextWithoutSolution.GetRequiredSolutionAsync(CancellationToken.None));
    }
}
