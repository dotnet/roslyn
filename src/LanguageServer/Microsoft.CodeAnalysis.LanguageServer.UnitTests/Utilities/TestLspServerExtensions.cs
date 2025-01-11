// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Roslyn.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.LanguageServer.UnitTests.AbstractLanguageServerHostTests;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal static class TestLspServerExtensions
{
    public static async Task Initialized(this TestLspServer testLspServer)
    {
        await testLspServer.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);
    }

    public static async Task<InitializeResult?> Initialize(this TestLspServer testLspServer, ClientCapabilities clientCapabilities)
    {
        return await testLspServer.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = clientCapabilities }, CancellationToken.None);
    }

    public static async Task OpenProjectsAsync(this TestLspServer testLspServer, Uri[] projects)
    {
        await testLspServer.ExecuteNotificationAsync<OpenProjectHandler.NotificationParams>(OpenProjectHandler.OpenProjectName, new() { Projects = projects });
    }

    public static async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(
        this TestLspServer testLspServer,
        CodeActionParams codeActionParams)
    {
        var result = await testLspServer.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
        Assert.NotNull(result);
        return [.. result.Cast<VSInternalCodeAction>()];
    }

    public static async Task<VSInternalCodeAction> RunGetCodeActionResolveAsync(
        this TestLspServer testLspServer,
        VSInternalCodeAction unresolvedCodeAction)
    {
        var result = (VSInternalCodeAction?)await testLspServer.ExecuteRequestAsync<CodeAction, CodeAction>(Methods.CodeActionResolveName, unresolvedCodeAction, CancellationToken.None);
        Assert.NotNull(result);
        return result;
    }
}
