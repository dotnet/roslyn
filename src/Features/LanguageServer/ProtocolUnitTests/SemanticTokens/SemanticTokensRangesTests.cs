// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

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

            var range = new LSP.Range { Start = new Position(0, 0), End = new Position(2, 0) };
            var ranges = new LSP.Range[1] { range };
            var results = await RunGetSemanticTokensAsync(testLspServer, testLspServer.GetLocations("caret").First(), ranges: ranges);

            var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
            var expectedResults = new LSP.SemanticTokens();
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

        [Fact]
        public void StitchSemanticTokenResponsesTogether_OnEmptyInput_ReturnsEmptyResponseData()
        {
            // Arrange
            var responseData = Array.Empty<int[]>();

            // Act
            var result = SemanticTokensRangesHandler.StitchSemanticTokenResponsesTogether(responseData);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void StitchSemanticTokenResponsesTogether_ReturnsCombinedResponseData()
        {
            // Arrange
            var responseData = new int[][] {
             new int[] { 0, 0, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         0, 5, 0, 0, 0,
                         0, 3, 0, 0, 0,
                         2, 2, 0, 0, 0,
                         0, 3, 0, 0, 0 },
             new int[] { 10, 0, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         0, 5, 0, 0, 0,
                         0, 3, 0, 0, 0,
                         2, 2, 0, 0, 0,
                         0, 3, 0, 0, 0 },
             new int[] { 14, 7, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         1, 0, 0, 0, 0,
                         0, 5, 0, 0, 0,
                         0, 3, 0, 0, 0,
                         2, 2, 0, 0, 0,
                         0, 3, 0, 0, 0 },
         };

            var expectedResponseData = new int[] {
            0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0,
            6, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0,
            0, 2, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0
        };

            // Act
            var result = SemanticTokensRangesHandler.StitchSemanticTokenResponsesTogether(responseData);

            // Assert
            Assert.Equal(expectedResponseData, result);
        }
    }
}
