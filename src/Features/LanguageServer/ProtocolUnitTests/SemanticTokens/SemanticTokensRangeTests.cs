// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensRangeTests : AbstractSemanticTokensTests
    {
        [Fact]
        public async Task TestGetSemanticTokensRange_FullDocAsync()
        {
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(2, 0) };
            var results = await RunGetSemanticTokensRangeAsync(testLspServer, testLspServer.GetLocations("caret").First(), range);

            // Everything is colorized syntactically, so we shouldn't be returning any semantic results.
            var expectedResults = new LSP.SemanticTokens
            {
                Data = Array.Empty<int>()
            };

            Assert.Equal(expectedResults.Data, results.Data);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_FullDoc_RazorAsync()
        {
            // Razor docs should be returning semantic + syntactic reuslts.
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(2, 0) };
            var options = ClassificationOptions.Default;

            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensHelpers.TokenTypeToIndex, range, options, includeSyntacticClassifications: true, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_PartialDoc_RazorAsync()
        {
            // Razor docs should be returning semantic + syntactic reuslts.
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(1, 0), End = new Position(2, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensHelpers.TokenTypeToIndex, range, options, includeSyntacticClassifications: true, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_MultiLineComment_RazorAsync()
        {
            // Testing as a Razor doc so we get both syntactic + semantic results; otherwise the results would be empty.
            var markup =
@"{|caret:|}class C { /* one

two
three */ }
";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(4, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensHelpers.TokenTypeToIndex, range, options, includeSyntacticClassifications: true, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],   0, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     6,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // '/* one'
                       2,     0,     3,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // 'two'
                       1,     0,     8,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment],      0, // 'three */'
                       0,     9,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_StringLiteralAsync()
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

            using var testLspServer = await CreateTestLspServerAsync(markup);
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(9, 0) };
            var results = await RunGetSemanticTokensRangeAsync(testLspServer, testLspServer.GetLocations("caret").First(), range);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       4,     8,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       1,     4,     2,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.StringEscapeCharacter], 0, // '""'
                },
            };

            await VerifyNoMultiLineTokens(testLspServer, results.Data!).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results.Data);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_StringLiteral_RazorAsync()
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

            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(9, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensHelpers.TokenTypeToIndex, range, options, includeSyntacticClassifications: true, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     4,     4,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'void'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     8,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     5,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"one'
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'two'
                       0,     4,     2,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.StringEscapeCharacter], 0, // '""'
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'three"'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                },
            };

            await VerifyNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results);
        }

        [Fact]
        public async Task TestGetSemanticTokensRange_Regex_RazorAsync()
        {
            var markup =
@"{|caret:|}using System.Text.RegularExpressions;

class C
{
	void M()
	{
		var x = new Regex(""(abc)*"");
    }
}
";

            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(9, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensHelpers.TokenTypeToIndex, range, options, includeSyntacticClassifications: true, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'System'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'Text'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'RegularExpressions'
                       0,     18,    1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       2,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'Regex'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     8,    SemanticTokensHelpers.TokenTypeToIndex[LSP.SemanticTokenTypes.String],                 0, // '"(abc)*"'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // '('
                       0,     1,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexText],             0, // 'abc'
                       0,     3,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexQuantifier],       0, // '*'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                }
            };

            await VerifyNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            Assert.Equal(expectedResults.Data, results);
        }
    }
}
