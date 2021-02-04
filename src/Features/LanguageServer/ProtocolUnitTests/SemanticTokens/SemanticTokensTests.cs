// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensTests : AbstractSemanticTokensTests
    {
        [Fact]
        public async Task TestGetSemanticTokensAsync()
        {
            var markup =
@"{|caret:|}// Comment
static class C { }";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var results = await RunGetSemanticTokensAsync(workspace.CurrentSolution, locations["caret"].First());

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                            | Modifier
                       0,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],  0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Class],    (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "1"
            };

            Assert.Equal(expectedResults.Data, results.Data);
            Assert.Equal(expectedResults.ResultId, results.ResultId);
        }

        /// <summary>
        /// Tests all three handlers in succession and makes sure we receive the expected result at each stage.
        /// </summary>
        [Fact]
        public async Task TestAllHandlersAsync()
        {
            var markup =
@"{|caret:|}// Comment
static class C { }
";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var caretLocation = locations["caret"].First();

            // 1. Range handler
            var range = new LSP.Range { Start = new Position(1, 0), End = new Position(2, 0) };
            var rangeResults = await RunGetSemanticTokensRangeAsync(workspace.CurrentSolution, caretLocation, range);
            var expectedRangeResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                            | Modifier
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Class],    (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "1"
            };

            Assert.Equal(expectedRangeResults.Data, rangeResults.Data);
            Assert.Equal(expectedRangeResults.ResultId, rangeResults.ResultId);

            // 2. Whole document handler
            var wholeDocResults = await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            var expectedWholeDocResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                            | Modifier
                       0,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],  0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Class],    (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "2"
            };

            Assert.Equal(expectedWholeDocResults.Data, wholeDocResults.Data);
            Assert.Equal(expectedWholeDocResults.ResultId, wholeDocResults.ResultId);

            // 3. Edits handler - insert newline at beginning of file
            var newMarkup = @"
// Comment
static class C { }
";

            UpdateDocumentText(newMarkup, workspace);
            var editResults = await RunGetSemanticTokensEditsAsync(workspace.CurrentSolution, caretLocation, previousResultId: "2");

            var expectedEdit = SemanticTokensEditsHandler.GenerateEdit(0, 1, new int[] { 1 });

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensEdits)editResults).Edits.First());
            Assert.Equal("3", ((LSP.SemanticTokensEdits)editResults).ResultId);

            // 4. Re-request whole document handler (may happen if LSP runs into an error)
            var wholeDocResults2 = await RunGetSemanticTokensAsync(workspace.CurrentSolution, caretLocation);
            var expectedWholeDocResults2 = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                            | Modifier
                       1,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],  0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],  0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Class],    (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "4"
            };

            Assert.Equal(expectedWholeDocResults2.Data, wholeDocResults2.Data);
            Assert.Equal(expectedWholeDocResults2.ResultId, wholeDocResults2.ResultId);
        }
    }
}
