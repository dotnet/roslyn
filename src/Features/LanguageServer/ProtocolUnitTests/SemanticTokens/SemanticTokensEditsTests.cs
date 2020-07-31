// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensEditsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetSemanticTokensEditsBasicAsync()
        {
            var updatedMarkup =
@"{|caret:|}// Comment

static class C { }";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(5, 1, new int[] { 2 }) };
            var expectedEdits = GenerateEdits(expectedEdit, "1");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var caretLocation = locations["caret"].First();

            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = await GetOriginalTokens(caretLocation.Uri, cache);
            await cache.UpdateCacheAsync(caretLocation.Uri, originalTokens, CancellationToken.None);

            var results = await GetEditResults(workspace, caretLocation);

            Assert.Equal(expectedEdits.Edits.First(), ((SemanticTokensEdits)results).Edits.First());
            Assert.Equal(expectedEdits.ResultId, ((SemanticTokensEdits)results).ResultId);
        }

        [Fact]
        public async Task TestGetSemanticTokensEdits_EndDeletionAsync()
        {
            var updatedMarkup =
@"{|caret:|}// Comment";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(5, 25, System.Array.Empty<int>()) };
            var expectedEdits = GenerateEdits(expectedEdit, "1");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var caretLocation = locations["caret"].First();

            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = await GetOriginalTokens(caretLocation.Uri, cache);
            await cache.UpdateCacheAsync(caretLocation.Uri, originalTokens, CancellationToken.None);

            var results = await GetEditResults(workspace, caretLocation);

            Assert.Equal(expectedEdits.Edits.First(), ((SemanticTokensEdits)results).Edits.First());
            Assert.Equal(expectedEdits.ResultId, ((SemanticTokensEdits)results).ResultId);
        }

        [Fact]
        public async Task TestGetSemanticTokensEdits_EndInsertionAsync()
        {
            var updatedMarkup =
@"{|caret:|}// Comment
static class C { }
// Comment";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(30, 0, new int[] { 1, 0, 10, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Comment), 0 }) };
            var expectedEdits = GenerateEdits(expectedEdit, "1");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var caretLocation = locations["caret"].First();

            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = await GetOriginalTokens(caretLocation.Uri, cache);
            await cache.UpdateCacheAsync(caretLocation.Uri, originalTokens, CancellationToken.None);

            var results = await GetEditResults(workspace, caretLocation);

            Assert.Equal(expectedEdits.Edits.First(), ((SemanticTokensEdits)results).Edits.First());
            Assert.Equal(expectedEdits.ResultId, ((SemanticTokensEdits)results).ResultId);
        }

        /*
         * Original markup:
         *      // Comment
         *      static class C { }  
         */
        private static async Task<LSP.SemanticTokens> GetOriginalTokens(Uri uri, SemanticTokensCache cache)
        {
            var originalTokens = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                               | Modifier
                    0, 0, 10, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Comment), 0,       // '// Comment'
                    1, 0, 6, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Keyword), 0,       // 'static'
                    0, 7, 5, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Keyword), 0,       // 'class'
                    0, 6, 1, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Class), (int)TokenModifiers.Static, // 'C'
                    0, 2, 1, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Operator), 0,       // '{'
                    0, 2, 1, SemanticTokensHelpers.GetTokenTypeIndex(SemanticTokenTypes.Operator), 0,       // '}'
                },
                ResultId = await cache.GetNextResultIdAsync(uri, CancellationToken.None)
            };
            return originalTokens;
        }

        private static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> GetEditResults(
            Workspace workspace,
            LSP.Location caretLocation)
        {
            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation);
            return results;
        }

        private static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> RunGetSemanticTokensEditsAsync(
            Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).ExecuteRequestAsync<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>(
                LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                CreateSemanticTokensParams(caret), new LSP.VSClientCapabilities(), null, CancellationToken.None);

        private static LSP.SemanticTokensEditsParams CreateSemanticTokensParams(LSP.Location caret)
            => new LSP.SemanticTokensEditsParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                PreviousResultId = "0"
            };

        private static SemanticTokensEdits GenerateEdits(SemanticTokensEdit[] edits, string resultId)
            => new SemanticTokensEdits
            {
                Edits = edits,
                ResultId = resultId
            };

        private static SemanticTokensCache GetSemanticTokensCache(Workspace workspace)
        {
            var exportProvider = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<SemanticTokensCache>();
            return Assert.IsType<SemanticTokensCache>(exportProvider);
        }
    }
}
