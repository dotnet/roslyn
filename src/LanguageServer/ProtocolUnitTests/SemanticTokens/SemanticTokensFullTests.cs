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

#pragma warning disable format // We want to force explicit column spacing within the collection literals in this file, so we disable formatting.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens;

public sealed class SemanticTokensFullTests(ITestOutputHelper testOutputHelper) : AbstractSemanticTokensTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestGetSemanticTokensFull_FullDocAsync(bool mutatingLspWorkspace, bool isVS)
    {
        var markup =
            """
            {|caret:|}// Comment
            static class C { }

            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup, mutatingLspWorkspace, GetCapabilities(isVS));

        var results = await RunGetSemanticTokensFullAsync(testLspServer, testLspServer.GetLocations("caret").First());

        var expectedResults = new LSP.SemanticTokens();
        var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
        if (isVS)
        {
            expectedResults.Data =
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

        await VerifyBasicInvariantsAndNoMultiLineTokens(testLspServer, results.Data).ConfigureAwait(false);
        AssertEx.Equal(ConvertToReadableFormat(testLspServer.ClientCapabilities, expectedResults.Data), ConvertToReadableFormat(testLspServer.ClientCapabilities, results.Data));
    }
}
