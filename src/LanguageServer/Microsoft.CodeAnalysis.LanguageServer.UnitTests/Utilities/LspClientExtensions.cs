// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal static class LspClientExtensions
{
    extension(ILspClient lspClient)
    {
        public Task Initialized()
        => lspClient.ExecuteRequestAsync<InitializedParams, object>(Methods.InitializedName, new InitializedParams(), CancellationToken.None);

        public async Task<InitializeResult?> Initialize(ClientCapabilities clientCapabilities)
        {
            return await lspClient.ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, new InitializeParams { Capabilities = clientCapabilities }, CancellationToken.None);
        }

        public Task OpenProjectsAsync(Uri[] projects)
            => lspClient.ExecuteNotificationAsync<OpenProjectHandler.NotificationParams>(OpenProjectHandler.OpenProjectName, new() { Projects = projects });

        public async Task<VSInternalCodeAction[]> RunGetCodeActionsAsync(
            CodeActionParams codeActionParams)
        {
            var result = await lspClient.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
            Assert.NotNull(result);
            return [.. result.Cast<VSInternalCodeAction>()];
        }

        public async Task<VSInternalCodeAction> RunGetCodeActionResolveAsync(
            VSInternalCodeAction unresolvedCodeAction)
        {
            var result = (VSInternalCodeAction?)await lspClient.ExecuteRequestAsync<CodeAction, CodeAction>(Methods.CodeActionResolveName, unresolvedCodeAction, CancellationToken.None);
            Assert.NotNull(result);
            return result;
        }
    }
}
