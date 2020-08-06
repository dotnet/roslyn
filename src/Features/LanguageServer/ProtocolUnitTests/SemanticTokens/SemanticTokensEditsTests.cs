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
        /*
         * Markup for basic test case:
         *     // Comment
         *     static class C { }  
         */
        private static int[] StandardCase(Dictionary<string, int> tokenTypesToIndex)
            => new int[] {
                // Line | Char | Len | Token type                                       | Modifier
                0,        0,     10,   tokenTypesToIndex[LSP.SemanticTokenTypes.Comment],  0, // '// Comment'
                1,        0,     6,    tokenTypesToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'static'
                0,        7,     5,    tokenTypesToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'class'
                0,        6,     1,    tokenTypesToIndex[LSP.SemanticTokenTypes.Class],    (int)TokenModifiers.Static, // 'C'
                0,        2,     1,    tokenTypesToIndex[LSP.SemanticTokenTypes.Operator], 0, // '{'
                0,        2,     1,    tokenTypesToIndex[LSP.SemanticTokenTypes.Operator], 0, // '}'
            };

        /*
         * Markup for single line test case:
         *     // Comment
         */
        private static int[] SingleLineCase(Dictionary<string, int> tokenTypesToIndex)
            => new int[] {
                // Line | Char | Len | Token type                                       | Modifier
                0,        0,     10,   tokenTypesToIndex[LSP.SemanticTokenTypes.Comment], 0, // '// Comment'
            };

        [Fact]
        public async Task TestInsertingNewLineInMiddleOfFile()
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
            var cache = GetSemanticTokensCache(workspace);
            var results = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), StandardCase(cache.TokenTypesToIndex), previousResultId: "1", cache);

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(start: 5, deleteCount: 1, data: new int[] { 2 });

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
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

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var cache = GetSemanticTokensCache(workspace);
            var results = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), StandardCase(cache.TokenTypesToIndex), previousResultId: "1", cache);

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(start: 5, deleteCount: 25, data: System.Array.Empty<int>());

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
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

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var cache = GetSemanticTokensCache(workspace);
            var results = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), StandardCase(cache.TokenTypesToIndex), previousResultId: "1", cache);

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 30, deleteCount: 0, data: new int[] { 1, 0, 10, cache.TokenTypesToIndex[LSP.SemanticTokenTypes.Comment], 0 });

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
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
            var cache = GetSemanticTokensCache(workspace);
            var results = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), SingleLineCase(cache.TokenTypesToIndex), previousResultId: "1", cache);

            // First edit: Replaces length of token (10 to 5) and replaces token type (comment to keyword)
            var expectedFirstEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 0, deleteCount: 5, data: new int[] { 0, 0, 5, cache.TokenTypesToIndex[LSP.SemanticTokenTypes.Keyword], 0 });

            // Second edit: Creates new token for '// Comment'
            var expectedSecondEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 5, deleteCount: 0, data: new int[] { 1, 0, 10, cache.TokenTypesToIndex[LSP.SemanticTokenTypes.Comment], 0 });

            Assert.Equal(expectedFirstEdit, ((LSP.SemanticTokensEdits)results).Edits[0]);
            Assert.Equal(expectedSecondEdit, ((LSP.SemanticTokensEdits)results).Edits[1]);
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
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
            var cache = GetSemanticTokensCache(workspace);
            var results = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), StandardCase(cache.TokenTypesToIndex), previousResultId: "10", cache);

            // Make sure we're returned SemanticTokens instead of SemanticTokensEdits.
            Assert.True(results.Value is LSP.SemanticTokens);
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_InsertNewlineInMiddleOfFile()
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
            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = StandardCase(cache.TokenTypesToIndex);

            // Edits to tokens conversion
            var edits = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), originalTokens, previousResultId: "1", cache);
            var editsToTokens = ApplySemanticTokensEdits(originalTokens, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_ReplacementEdit()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}// Comment
internal struct S { }";

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = StandardCase(cache.TokenTypesToIndex);

            // Edits to tokens conversion
            var edits = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), originalTokens, previousResultId: "1", cache);
            var editsToTokens = ApplySemanticTokensEdits(originalTokens, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_ManyEdits()
        {
            /*
             * Original markup:
             *     // Comment
             *     static class C { }  
             */
            var updatedMarkup =
@"{|caret:|}
// Comment
class C
{
    static void M(int x)
    {
        var v = 1;
    }
}";

            using var workspace = CreateTestWorkspace(updatedMarkup, out var locations);
            var cache = GetSemanticTokensCache(workspace);
            var originalTokens = StandardCase(cache.TokenTypesToIndex);

            // Edits to tokens conversion
            var edits = await GetSemanticTokensEdits(
                workspace, locations["caret"].First(), originalTokens, previousResultId: "1", cache);
            var editsToTokens = ApplySemanticTokensEdits(originalTokens, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        private static async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> GetSemanticTokensEdits(
            Workspace workspace,
            LSP.Location caretLocation,
            int[] originalData,
            string previousResultId,
            SemanticTokensCache cache)
        {
            var originalTokens = GetOriginalTokens(originalData, cache);
            await cache.UpdateCacheAsync(caretLocation.Uri, originalTokens, CancellationToken.None);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId);
            return results;
        }

        private static LSP.SemanticTokens GetOriginalTokens(int[] originalData, SemanticTokensCache cache)
        {
            var originalTokens = new LSP.SemanticTokens
            {
                Data = originalData,
                ResultId = cache.GetNextResultId()
            };

            return originalTokens;
        }

        private static int[] ApplySemanticTokensEdits(int[] originalTokens, LSP.SemanticTokensEdits edits)
        {
            var data = originalTokens.ToList();
            foreach (var edit in edits.Edits)
            {
                data.RemoveRange(edit.Start, edit.DeleteCount);
                data.InsertRange(edit.Start, edit.Data);
            }

            return data.ToArray();
        }
    }
}
