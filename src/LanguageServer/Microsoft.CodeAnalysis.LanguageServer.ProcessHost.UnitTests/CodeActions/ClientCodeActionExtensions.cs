// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests.CodeActions;

public static class ClientCodeActionExtensions
{
    extension(AbstractLanguageServerClientTests.TestLspClient target)
    {
        internal async Task<CodeAction[]> RunGetCodeActionsAsync(CodeActionParams codeActionParams)
        {
            var result = await target.ExecuteRequestAsync<CodeActionParams, CodeAction[]>(Methods.TextDocumentCodeActionName, codeActionParams, CancellationToken.None);
            Assert.NotNull(result);
            return [.. result.Cast<CodeAction>()];
        }

        internal async Task<CodeAction> RunGetCodeActionResolveAsync(CodeAction unresolvedCodeAction)
        {
            var result = (CodeAction?)await target.ExecuteRequestAsync<CodeAction, CodeAction>(Methods.CodeActionResolveName, unresolvedCodeAction, CancellationToken.None);
            Assert.NotNull(result);
            return result;
        }
    }
}