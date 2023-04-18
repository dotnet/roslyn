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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results.Data).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results.Data));
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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results));
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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],   0, // 'C'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     6,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '/* one'
                       2,     0,     3,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'two'
                       1,     0,     8,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Comment],      0, // 'three */'
                       0,     9,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                },
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results));
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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     4,     4,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'void'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     8,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     5,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"one'
                       1,     0,     4,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'two '
                       0,     4,     2,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.StringEscapeCharacter], 0, // '""'
                       1,     0,     6,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // 'three"'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '}'
                },
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results));
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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'System'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'Text'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'RegularExpressions'
                       0,     18,    1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       2,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'Regex'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // '('
                       0,     1,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexText],             0, // 'abc'
                       0,     3,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.String],                 0, // '"'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                }
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results));
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

            var expectedResults = new LSP.SemanticTokens
            {
                Data = new int[]
                {
                    // Line | Char | Len | Token type                                                                         | Modifier
                       0,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'using'
                       0,     6,     6,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'System'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'Text'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     18,   SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Namespace],         0, // 'RegularExpressions'
                       0,     18,    1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       2,     0,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'class'
                       0,     6,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'C'
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     1,     4,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0,  // 'void'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Method],            0, // 'M'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       1,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '{'
                       1,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Keyword],               0, // 'var'
                       0,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Variable],             0, // 'x'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '='
                       0,     2,     3,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Keyword],                0, // 'new'
                       0,     4,     5,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Class],             0, // 'Regex'
                       0,     5,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // '('
                       0,     1,     2,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '@"'
                       0,     2,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // '('
                       0,     1,     3,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexText],             0, // 'abc'
                       0,     3,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexGrouping],         0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexQuantifier],       0, // '*'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // ' '
                       0,     1,     9,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.RegexComment],          0, // '#comment'
                       1,     0,     27,   SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.VerbatimStringLiteral], 0, // '"'
                       0,     27,    1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ','
                       0,     2,     12,   SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Enum],              0, // 'RegexOptions'
                       0,     12,    1,    SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.Operator],               0, // '.'
                       0,     1,     23,   SemanticTokensHelpers.TokenTypeToIndex[SemanticTokenTypes.EnumMember],        0, // 'IgnorePatternWhitespace'
                       0,     23,    1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ')'
                       0,     1,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // ';'
                       1,     4,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                       1,     0,     1,    SemanticTokensHelpers.TokenTypeToIndex[ClassificationTypeNames.Punctuation],           0, // }
                }
            };

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(expectedResults.Data), ConvertToReadableFormat(results));
        }

        [Theory, MemberData(nameof(ClassificationTypeNamesToMatch))]
        public void TestGetSemanticTokensRange_AssertCustomTokenTypes(string fieldName)
            => Assert.True(SemanticTokensHelpers.RoslynCustomTokenTypes.Contains(fieldName), $"Missing token type {fieldName}.");

        public static IEnumerable<object[]> ClassificationTypeNamesToMatch => ClassificationTypeNames.AllTypeNames.Where(
            type => !SemanticTokensHelpers.ClassificationTypeToSemanticTokenTypeMap.ContainsKey(type) &&
                !ClassificationTypeNames.AdditiveTypeNames.Contains(type)).Select(field => new object[] { field });

    }
}
