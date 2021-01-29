﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private protected static async Task<LSP.SemanticTokens> RunGetSemanticTokensAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.SemanticTokensParams, LSP.SemanticTokens>(LSP.SemanticTokensMethods.TextDocumentSemanticTokensName,
                CreateSemanticTokensParams(caret), new LSP.VSClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SemanticTokensParams CreateSemanticTokensParams(LSP.Location caret)
            => new LSP.SemanticTokensParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri }
            };

        private protected static async Task<LSP.SemanticTokens> RunGetSemanticTokensRangeAsync(TestLspServer testLspServer, LSP.Location caret, LSP.Range range)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>(LSP.SemanticTokensMethods.TextDocumentSemanticTokensRangeName,
                CreateSemanticTokensRangeParams(caret, range), new LSP.VSClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SemanticTokensRangeParams CreateSemanticTokensRangeParams(LSP.Location caret, LSP.Range range)
            => new LSP.SemanticTokensRangeParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                Range = range
            };

        private protected static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> RunGetSemanticTokensEditsAsync(TestLspServer testLspServer, LSP.Location caret, string previousResultId)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>(LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
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
