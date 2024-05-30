// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensRangesTests : AbstractSemanticTokensTests
    {
        public SemanticTokensRangesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGetSemanticTokensRanges_FullDocAsync(bool mutatingLspWorkspace, bool isVS)
        {
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            await using var testLspServer = await CreateTestLspServerAsync(
                markup, mutatingLspWorkspace, GetCapabilities(isVS));

            var ranges = new[] { new LSP.Range { Start = new Position(0, 0), End = new Position(2, 0) } };
            var results = await RunGetSemanticTokensRangesAsync(testLspServer, testLspServer.GetLocations("caret").First(), ranges);

            var expectedResults = new LSP.SemanticTokens();
            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            if (isVS)
            {
                expectedResults.Data =
#pragma warning disable format // Force explicit column spacing.
                [
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[ClassificationTypeNames.ClassName],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                ];
            }
            else
            {
                expectedResults.Data =
                [
                    // Line | Char | Len | Token type                                                               | Modifier
                       0,     0,     10,   tokenTypeToIndex[SemanticTokenTypes.Comment],      0, // '// Comment'
                       1,     0,     6,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'static'
                       0,     7,     5,    tokenTypeToIndex[SemanticTokenTypes.Keyword],      0, // 'class'
                       0,     6,     1,    tokenTypeToIndex[SemanticTokenTypes.Class],   (int)TokenModifiers.Static, // 'C'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '{'
                       0,     2,     1,    tokenTypeToIndex[ClassificationTypeNames.Punctuation], 0, // '}'
                ];
            }
#pragma warning restore format

            await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results.Data).ConfigureAwait(false);
            AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results.Data));
        }
    }
}
