// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensEditsTests : AbstractSemanticTokensTests
    {
        [Fact]
        public async Task TestGetSemanticTokensEditsBasicAsync()
        {
            var originalTokens = GetOriginalTokens();

            var updatedMarkup =
@"{|caret:|}// Comment

static class C { }";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(5, 1, new int[] { 2 }) };
            var expectedEdits = GenerateEdits(expectedEdit, "2");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetEditResults(originalTokens, workspace, locations);

            Assert.Equal(expectedEdits.Edits.First(), results.SemanticTokensEdits!.Edits.First());
            Assert.Equal(expectedEdits.ResultId, results.SemanticTokensEdits!.ResultId);
        }

        [Fact]
        public async Task TestGetSemanticTokensEdits_EndDeletionAsync()
        {
            var originalTokens = GetOriginalTokens();

            var updatedMarkup =
@"{|caret:|}// Comment";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(5, 25, System.Array.Empty<int>()) };
            var expectedEdits = GenerateEdits(expectedEdit, "2");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetEditResults(originalTokens, workspace, locations);

            Assert.Equal(expectedEdits.Edits.First(), results.SemanticTokensEdits!.Edits.First());
            Assert.Equal(expectedEdits.ResultId, results.SemanticTokensEdits!.ResultId);
        }

        [Fact]
        public async Task TestGetSemanticTokensEdits_EndInsertionAsync()
        {
            var originalTokens = GetOriginalTokens();

            var updatedMarkup =
@"{|caret:|}// Comment
static class C { }
// Comment";

            var expectedEdit = new SemanticTokensEdit[] { SemanticTokensEditsHandler.GenerateEdit(30, 0, new int[] { 1, 0, 10, GetTypeIndex(SemanticTokenTypes.Comment), 0 }) };
            var expectedEdits = GenerateEdits(expectedEdit, "2");

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetEditResults(originalTokens, workspace, locations);

            Assert.Equal(expectedEdits.Edits.First(), results.SemanticTokensEdits!.Edits.First());
            Assert.Equal(expectedEdits.ResultId, results.SemanticTokensEdits!.ResultId);
        }

        private static LSP.SemanticTokens GetOriginalTokens()
        {
            var originalTokens = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                               | Modifier
                       0,     0,     10,   GetTypeIndex(SemanticTokenTypes.Comment),  0,       // '// Comment'
                       1,     0,     6,    GetTypeIndex(SemanticTokenTypes.Keyword),  0,       // 'static'
                       0,     7,     5,    GetTypeIndex(SemanticTokenTypes.Keyword),  0,       // 'class'
                       0,     6,     1,    GetTypeIndex(SemanticTokenTypes.Class),    GetModifierBits(SemanticTokenModifiers.Static, 0), // 'C'
                       0,     2,     1,    GetTypeIndex(SemanticTokenTypes.Operator), 0,       // '{'
                       0,     2,     1,    GetTypeIndex(SemanticTokenTypes.Operator), 0,       // '}'
                },
                ResultId = "1"
            };
            return originalTokens;
        }

        private static async Task<SemanticTokensEditsResult> GetEditResults(
            LSP.SemanticTokens originalTokens,
            Workspace workspace,
            Dictionary<string, IList<LSP.Location>> locations)
        {
            var caretLocation = locations["caret"].First();

            var cache = new SemanticTokensCache();
            cache.UpdateCache(new LSP.TextDocumentIdentifier { Uri = caretLocation.Uri }, originalTokens);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, cache);
            return results;
        }

        private static async Task<SemanticTokensEditsResult> RunGetSemanticTokensEditsAsync(
            Solution solution,
            LSP.Location caret,
            SemanticTokensCache cache)
            => await GetLanguageServer(solution).ExecuteSemanticTokensRequestAsync<LSP.SemanticTokensEditsParams, SemanticTokensEditsResult>(
                LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                CreateSemanticTokensParams(caret), cache, new LSP.VSClientCapabilities(), null, CancellationToken.None);

        private static LSP.SemanticTokensEditsParams CreateSemanticTokensParams(LSP.Location caret)
            => new LSP.SemanticTokensEditsParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                PreviousResultId = "1"
            };

        private static SemanticTokensEdits GenerateEdits(SemanticTokensEdit[] edits, string resultId)
            => new SemanticTokensEdits
            {
                Edits = edits,
                ResultId = resultId
            };
    }
}
