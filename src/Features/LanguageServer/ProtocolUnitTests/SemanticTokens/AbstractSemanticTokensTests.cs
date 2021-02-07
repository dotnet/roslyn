// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public abstract class AbstractSemanticTokensTests : AbstractLanguageServerProtocolTests
    {
        protected static async Task<LSP.SemanticTokens> RunGetSemanticTokensAsync(
            Solution solution, LSP.Location caret)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.SemanticTokensParams, LSP.SemanticTokens>(queue,
                           LSP.SemanticTokensMethods.TextDocumentSemanticTokensName,
                           CreateSemanticTokensParams(caret), new LSP.VSClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SemanticTokensParams CreateSemanticTokensParams(LSP.Location caret)
            => new LSP.SemanticTokensParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri }
            };

        protected static async Task<LSP.SemanticTokens> RunGetSemanticTokensRangeAsync(
            Solution solution, LSP.Location caret, LSP.Range range)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>(queue,
                           LSP.SemanticTokensMethods.TextDocumentSemanticTokensRangeName,
                           CreateSemanticTokensRangeParams(caret, range), new LSP.VSClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SemanticTokensRangeParams CreateSemanticTokensRangeParams(LSP.Location caret, LSP.Range range)
            => new LSP.SemanticTokensRangeParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                Range = range
            };

        protected static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> RunGetSemanticTokensEditsAsync(
            Solution solution, LSP.Location caret, string previousResultId)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>(queue,
                           LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                           CreateSemanticTokensParams(caret, previousResultId), new LSP.VSClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SemanticTokensEditsParams CreateSemanticTokensParams(LSP.Location caret, string previousResultId)
            => new LSP.SemanticTokensEditsParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                PreviousResultId = previousResultId
            };

        protected static void UpdateDocumentText(string updatedText, Workspace workspace)
        {
            var docId = ((TestWorkspace)workspace).Documents.First().Id;
            ((TestWorkspace)workspace).ChangeDocument(docId, SourceText.From(updatedText));
        }
    }
}
