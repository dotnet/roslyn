// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensRangeTests : AbstractSemanticTokensTests
    {
        public SemanticTokensRangeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRange_FullDocAsync(bool mutatingLspWorkspace, bool isVS)
        {
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(2, 0) };
            var results = await RunGetSemanticTokensRangeAsync(testLspServer, testLspServer.GetLocations("caret").First(), range);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results.Data).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results.Data));
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRange_PartialDocAsync(bool mutatingLspWorkspace, bool isVS)
        {
            // Razor docs should be returning semantic + syntactic reuslts.
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(1, 0), End = new Position(2, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                testLspServer.ClientCapabilities, document, range, options, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results));
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRange_MultiLineComment_IncludeSyntacticClassificationsAsync(bool mutatingLspWorkspace, bool isVS)
        {
            // Testing as a Razor doc so we get both syntactic + semantic results; otherwise the results would be empty.
            var markup =
@"{|caret:|}class C { /* one

two
three */ }
";
            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(4, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                testLspServer.ClientCapabilities, document, range, options, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],   0, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     6,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '/* one'
                       2,     0,     3,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'two'
                       1,     0,     8,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'three */'
                       0,     9,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],   0, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     6,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '/* one'
                       2,     0,     3,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'two'
                       1,     0,     8,    tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'three */'
                       0,     9,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results));
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRange_StringLiteral_IncludeSyntacticClassificationsAsync(bool mutatingLspWorkspace, bool isVS)
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

            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(9, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                testLspServer.ClientCapabilities, document, range, options, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     4,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'void'
                       0,     5,     1,    tokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     8,     3,    tokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     5,    tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"one'
                       1,     0,     4,    tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'two '
                       0,     4,     2,    tokenTypeToIndex[ClassificationTypeNames.StringEscapeCharacter], 0, // '""'
                       1,     0,     6,    tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'three"'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     4,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'void'
                       0,     5,     1,    tokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ')'
                       1,     4,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     8,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     5,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // '@"one'
                       1,     0,     4,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // 'two '
                       0,     4,     2,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringEscapeCharacter], 0, // '""'
                       1,     0,     6,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // 'three"'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results));
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRange_Regex_IncludeSyntacticClassificationsAsync(bool mutatingLspWorkspace, bool isVS)
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

            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(9, 0) };
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                testLspServer.ClientCapabilities, document, range, options, CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'System'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'Text'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'RegularExpressions'
                       0,     18,    1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       2,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    tokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     2,     3,    tokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    tokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'Regex'
                       0,     5,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // '('
                       0,     1,     3,    tokenTypeToIndex[ClassificationTypeNames.RegexText],             0, // 'abc'
                       0,     3,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    tokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'System'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'Text'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'RegularExpressions'
                       0,     18,    1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ';'
                       2,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    tokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ')'
                       1,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    tokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'Regex'
                       0,     5,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexGrouping],         0, // '('
                       0,     1,     3,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexText],             0, // 'abc'
                       0,     3,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    tokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ')'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // }
                       1,     0,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // }
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results));
        }

        [Theory, CombinatorialData]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1710519")]
        public async Task TestGetSemanticTokensRange_RegexWithComment_IncludeSyntacticClassificationsAsync(bool mutatingLspWorkspace, bool isVS)
        {
            var markup =
@"{|caret:|}using System.Text.RegularExpressions;

class C
{
	void M()
	{
		var x = new Regex(@""(abc)* #comment
                          "", RegexOptions.IgnorePatternWhitespace);
    }
}
";

            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var options = ClassificationOptions.Default;
            var results = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                testLspServer.ClientCapabilities, document, range: null, options: options, cancellationToken: CancellationToken.None);

            var expectedResults = new LSP.SemanticTokens();

            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'System'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'Text'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   tokenTypeToIndex[ClassificationTypeNames.NamespaceName],         0, // 'RegularExpressions'
                       0,     18,    1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       2,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    tokenTypeToIndex[ClassificationTypeNames.MethodName],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     2,     3,    tokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.LocalName],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    tokenTypeToIndex[ClassificationTypeNames.ClassName],             0, // 'Regex'
                       0,     5,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     2,    tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // '('
                       0,     1,     3,    tokenTypeToIndex[ClassificationTypeNames.RegexText],             0, // 'abc'
                       0,     3,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // ' '
                       0,     1,     9,    tokenTypeToIndex[ClassificationTypeNames.RegexComment],          0, // '#comment'
                       1,     0,     27,   tokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '"'
                       0,     27,    1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ','
                       0,     2,     12,   tokenTypeToIndex[ClassificationTypeNames.EnumName],              0, // 'RegexOptions'
                       0,     12,    1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     23,   tokenTypeToIndex[ClassificationTypeNames.EnumMemberName],        0, // 'IgnorePatternWhitespace'
                       0,     23,    1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       0,     1,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                       1,     0,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                };
            }
            else
            {
                expectedResults.Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'System'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'Text'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   tokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'RegularExpressions'
                       0,     18,    1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ';'
                       2,     0,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     1,     4,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    tokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '('
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ')'
                       1,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '{'
                       1,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],               0, // 'var'
                       0,     4,     1,    tokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    tokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    tokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'Regex'
                       0,     5,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // '('
                       0,     1,     2,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // '@"'
                       0,     2,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexGrouping],         0, // '('
                       0,     1,     3,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexText],             0, // 'abc'
                       0,     3,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // ' '
                       0,     1,     9,    tokenTypeToIndex[CustomLspSemanticTokenNames.RegexComment],          0, // '#comment'
                       1,     0,     27,   tokenTypeToIndex[CustomLspSemanticTokenNames.StringVerbatim], 0, // '"'
                       0,     27,    1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ','
                       0,     2,     12,   tokenTypeToIndex[SemanticTokenTypes.Enum],              0, // 'RegexOptions'
                       0,     12,    1,    tokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     23,   tokenTypeToIndex[SemanticTokenTypes.EnumMember],        0, // 'IgnorePatternWhitespace'
                       0,     23,    1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ')'
                       0,     1,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // ';'
                       1,     4,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // }
                       1,     0,     1,    tokenTypeToIndex[CustomLspSemanticTokenNames.Punctuation],           0, // }
                };
            }

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results));
        }

        [Theory, CombinatorialData]
        public void TestGetSemanticTokensRange_AssertCustomTokenTypes(bool isVS)
        {
            var capabilities = GetCapabilities(isVS);
            var schema = SemanticTokensSchema.GetSchema(capabilities.HasVisualStudioLspCapability());

            var expectedNames = ClassificationTypeNames.AllTypeNames.Where(s => !ClassificationTypeNames.AdditiveTypeNames.Contains(s));
            foreach (var expectedClassificationName in expectedNames)
            {
                // Assert that the classification type name exists and is mapped to a semantic token name.
                Assert.True(schema.TokenTypeMap.ContainsKey(expectedClassificationName), $"Missing token type for {expectedClassificationName}.");

                var tokenName = schema.TokenTypeMap[expectedClassificationName];
                Assert.True(schema.AllTokenTypes.Contains(tokenName));
            }
        }
    }
}
