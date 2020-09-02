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
    public class SemanticTokensRangeTests : AbstractSemanticTokensTests
    {
        [Fact]
        public async Task TestGetSemanticTokensRangeAsync()
        {
            var markup =
@"{|caret:|}// Comment
static class C { }
";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var range = new LSP.Range { Start = new Position(1, 0), End = new Position(2, 0) };
            var results = await RunGetSemanticTokensRangeAsync(workspace.CurrentSolution, locations["caret"].First(), range);

            var expectedResults = new LSP.SemanticTokens
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

            Assert.Equal(expectedResults.Data, results.Data);
            Assert.Equal(expectedResults.ResultId, results.ResultId);
        }
    }
}
