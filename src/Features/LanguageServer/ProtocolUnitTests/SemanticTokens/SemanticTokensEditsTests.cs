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
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensEditsTests : AbstractSemanticTokensTests
    {
        /*
         * Markup for basic test case:
         *     // Comment
         *     static class C { }  
         */
        private static readonly int[] s_basicDataCase =
            {
                    // Line | Char | Len | Token type                                                             | Modifier
                    0,        0,     10,   SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Comment), 0, // '// Comment'
                    1,        0,     6,    SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Keyword), 0, // 'static'
                    0,        7,     5,    SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Keyword), 0, // 'class'
                    0,        6,     1,    SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Class), (int)TokenModifiers.Static, // 'C'
                    0,        2,     1,    SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Operator), 0, // '{'
                    0,        2,     1,    SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Operator), 0, // '}'
            };

        /*
         * Markup for single line test case:
         *     // Comment
         */
        private static readonly int[] s_singleLineDataCase =
            {
                    // Line | Char | Len | Token type                                                             | Modifier
                    0,        0,     10,   SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Comment), 0, // '// Comment'
            };

        /// <summary>
        /// Tests inserting a newline in the middle of the file.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEditsBasicAsync()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}// Comment

static class C { }";

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(start: 5, deleteCount: 1, data: new int[] { 2 });

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetActualEdits(workspace, locations["caret"].First(), s_basicDataCase, previousResultId: "0");

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("1", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests making a deletion from the end of the file.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEdits_EndDeletionAsync()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}// Comment";

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(start: 5, deleteCount: 25, data: System.Array.Empty<int>());

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetActualEdits(workspace, locations["caret"].First(), s_basicDataCase, previousResultId: "0");

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("1", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests making an insertion at the end of the file.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEdits_EndInsertionAsync()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}// Comment
static class C { }
// Comment";

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 30, deleteCount: 0, data: new int[] { 1, 0, 10, SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Comment), 0 });

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetActualEdits(workspace, locations["caret"].First(), s_basicDataCase, previousResultId: "0");

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("1", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests to make sure we return a minimal number of edits.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEdits_ReturnMinimalEdits()
        {
            /*
             * Original markup:
             *     // Comment  
             */
            var updatedMarkup =
@"{|caret:|}class
// Comment";

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetActualEdits(workspace, locations["caret"].First(), s_singleLineDataCase, previousResultId: "0");

            // First edit: Replaces length of token (10 to 5) and replaces token type (comment to keyword)
            var expectedFirstEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 2, deleteCount: 2, data: new int[] { 5, SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Keyword) });

            // Second edit: Creates new token for '// Comment'
            var expectedSecondEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 5, deleteCount: 0, data: new int[] { 1, 0, 10, SemanticTokensHelpers.GetTokenTypeIndex(LSP.SemanticTokenTypes.Comment), 0 });

            Assert.Equal(expectedFirstEdit, ((LSP.SemanticTokensEdits)results).Edits[0]);
            Assert.Equal(expectedSecondEdit, ((LSP.SemanticTokensEdits)results).Edits[1]);
            Assert.Equal("1", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests to make sure that if we don't have a matching semantic token set for the document in the cache,
        /// we return the full set of semantic tokens.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEditsNoCacheAsync()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}// Comment

static class C { }";

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var results = await GetActualEdits(workspace, locations["caret"].First(), s_basicDataCase, previousResultId: "10");

            // Make sure we're returned SemanticTokens instead of SemanticTokensEdits.
            Assert.True(results.Value is LSP.SemanticTokens);
        }

        private static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> GetActualEdits(
            Workspace workspace,
            LSP.Location caretLocation,
            int[] originalData,
            string previousResultId)
        {
            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = await GetOriginalTokens(caretLocation.Uri, originalData, cache);
            await cache.UpdateCacheAsync(caretLocation.Uri, originalTokens, CancellationToken.None);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId);
            return results;
        }
        private static async Task<LSP.SemanticTokens> GetOriginalTokens(Uri uri, int[] originalData, SemanticTokensCache cache)
        {
            var originalTokens = new LSP.SemanticTokens
            {
                Data = originalData,
                ResultId = await cache.GetNextResultIdAsync(uri, CancellationToken.None)
            };
            return originalTokens;
        }

        private static SemanticTokensCache GetSemanticTokensCache(Workspace workspace)
        {
            var exportProvider = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<SemanticTokensCache>();
            return Assert.IsType<SemanticTokensCache>(exportProvider);
        }
    }
}
