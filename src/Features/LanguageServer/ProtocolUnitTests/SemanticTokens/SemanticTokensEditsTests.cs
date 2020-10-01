// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
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
        private static readonly string s_standardCase = @"{|caret:|}// Comment
static class C { }";

        /*
         * Markup for single line test case:
         *     // Comment
         */
        private static readonly string s_singleLineCase = @"{|caret:|}// Comment";

        [Fact]
        public async Task TestInsertingNewLineInMiddleOfFile()
        {
            var updatedText = @"// Comment

static class C { }";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");

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
            var updatedText =
@"// Comment";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");

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
            var updatedText =
@"// Comment
static class C { }
// Comment";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 30, deleteCount: 0, data: new int[] { 1, 0, 10, SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment], 0 });

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests to make sure we return a minimal number of edits.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEdits_ReturnMinimalEdits()
        {
            var updatedText =
@"class
// Comment";

            using var workspace = CreateTestWorkspace(s_singleLineCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);

            // Edit text
            UpdateDocumentText(updatedText, workspace);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");

            // 1. Replaces length of token (10 to 5) and replaces token type (comment to keyword)
            // 2. Creates new token for '// Comment'
            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(
                start: 0, deleteCount: 5,
                data: new int[]
                {
                    0, 0, 5, SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword], 0,
                    1, 0, 10, SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment], 0
                });

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)results).Edits?[0]);
            Assert.Equal("2", ((LSP.SemanticTokensEdits)results).ResultId);
        }

        /// <summary>
        /// Tests to make sure that if we don't have a matching semantic token set for the document in the cache,
        /// we return the full set of semantic tokens.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEditsNoCacheAsync()
        {
            var updatedText =
@"// Comment

static class C { }";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            var results = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "10");

            // Make sure we're returned SemanticTokens instead of SemanticTokensEdits.
            Assert.True(results.Value is LSP.SemanticTokens);
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_InsertNewlineInMiddleOfFile()
        {
            var updatedText =
@"// Comment

static class C { }";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_ReplacementEdit()
        {
            var updatedText =
@"// Comment
internal struct S { }";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_ManyEdits()
        {
            var updatedText =
@"
// Comment
class C
{
    static void M(int x)
    {
        var v = 1;
    }
}";

            using var workspace = CreateTestWorkspace(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            UpdateDocumentText(updatedText, workspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensEdits)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        private static int[] ApplySemanticTokensEdits(int[]? originalTokens, LSP.SemanticTokensEdits edits)
        {
            var data = originalTokens.ToList();
            if (edits.Edits != null)
            {
                foreach (var edit in edits.Edits)
                {
                    data.RemoveRange(edit.Start, edit.DeleteCount);
                    data.InsertRange(edit.Start, edit.Data);
                }
            }

            return data.ToArray();
        }
    }
}
