// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
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

            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var results = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "1"
            };

            await VerifyNoMultiLineTokens(testLspServer, results.Data!).ConfigureAwait(false);
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

            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var caretLocation = locations["caret"].First();

            // 1. Range handler
            var range = new LSP.Range { Start = new Position(1, 0), End = new Position(2, 0) };
            var rangeResults = await RunGetSemanticTokensRangeAsync(testLspServer, caretLocation, range);
            var expectedRangeResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "1"
            };

            await VerifyNoMultiLineTokens(testLspServer, rangeResults.Data!).ConfigureAwait(false);
            Assert.Equal(expectedRangeResults.Data, rangeResults.Data);
            Assert.Equal(expectedRangeResults.ResultId, rangeResults.ResultId);
            Assert.True(rangeResults is RoslynSemanticTokens);

            // 2. Whole document handler
            var wholeDocResults = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            var expectedWholeDocResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "2"
            };

            await VerifyNoMultiLineTokens(testLspServer, wholeDocResults.Data!).ConfigureAwait(false);
            Assert.Equal(expectedWholeDocResults.Data, wholeDocResults.Data);
            Assert.Equal(expectedWholeDocResults.ResultId, wholeDocResults.ResultId);
            Assert.True(wholeDocResults is RoslynSemanticTokens);

            // 3. Edits handler - insert newline at beginning of file
            var newMarkup = @"
// Comment
static class C { }
";

            await UpdateDocumentTextAsync(newMarkup, testLspServer.TestWorkspace);
            var editResults = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "2");

            var expectedEdit = new LSP.SemanticTokensEdit { Start = 0, DeleteCount = 1, Data = new int[] { 1 } };

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensDelta)editResults).Edits.First());
            Assert.Equal("3", ((LSP.SemanticTokensDelta)editResults).ResultId);
            Assert.True((LSP.SemanticTokensDelta)editResults is RoslynSemanticTokensDelta);

            // 4. Edits handler - no changes (ResultId should remain same)
            var editResultsNoChange = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "3");
            Assert.Equal("3", ((LSP.SemanticTokensDelta)editResultsNoChange).ResultId);
            Assert.True((LSP.SemanticTokensDelta)editResultsNoChange is RoslynSemanticTokensDelta);

            // 5. Re-request whole document handler (may happen if LSP runs into an error)
            var wholeDocResults2 = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            var expectedWholeDocResults2 = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     10,   SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "4"
            };

            await VerifyNoMultiLineTokens(testLspServer, wholeDocResults2.Data!).ConfigureAwait(false);
            Assert.Equal(expectedWholeDocResults2.Data, wholeDocResults2.Data);
            Assert.Equal(expectedWholeDocResults2.ResultId, wholeDocResults2.ResultId);
            Assert.True(wholeDocResults2 is RoslynSemanticTokens);
        }

        [Fact]
        public async Task TestGetSemanticTokensMultiLineCommentAsync()
        {
            var markup =
@"{|caret:|}class C { /* one
two
three */ }
";

            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var results = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],   0, // 'C'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     6,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '/* one'
                       1,     0,     3,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // 'two'
                       1,     0,     8,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // 'three */'
                       0,     9,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
                ResultId = "1"
            };

            await VerifyNoMultiLineTokens(testLspServer, results.Data!).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results.Data);
            Assert.Equal(expectedResults.ResultId, results.ResultId);
        }

        [Fact]
        public async Task TestGetSemanticTokensStringLiteralAsync()
        {
            var markup =
@"{|caret:|}class C
{
    void M()
    {
        var x = @""one
two """"
three"";
    }
}
";

            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var results = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     4,     4,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'void'
                       0,     5,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     4,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     8,     3,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     5,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"one'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'two'
                       0,     4,     2,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.StringEscapeCharacter], 0, // '""'
                       1,     0,     6,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'three"'
                       0,     6,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                       1,     0,     1,    SemanticTokensCache.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                },
                ResultId = "1"
            };

            await VerifyNoMultiLineTokens(testLspServer, results.Data!).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results.Data);
            Assert.Equal(expectedResults.ResultId, results.ResultId);
        }
    }
}
