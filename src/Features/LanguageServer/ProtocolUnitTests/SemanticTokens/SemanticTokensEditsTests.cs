// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.Test.Utilities;
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

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");

            var expectedEdit = new LSP.SemanticTokensEdit { Start = 5, DeleteCount = 1, Data = new int[] { 2 } };

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensDelta)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensDelta)results).ResultId);
        }

        /// <summary>
        /// Tests making a deletion from the end of the file.
        /// </summary>
        [Fact]
        public async Task TestGetSemanticTokensEdits_EndDeletionAsync()
        {
            var updatedText =
@"// Comment";

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");

            var expectedEdit = new LSP.SemanticTokensEdit { Start = 5, DeleteCount = 25, Data = System.Array.Empty<int>() };

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensDelta)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensDelta)results).ResultId);
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

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");

            var expectedEdit = new LSP.SemanticTokensEdit
            {
                Start = 30,
                DeleteCount = 0,
                Data = new int[] { 1, 0, 10, SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment], 0 }
            };

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensDelta)results).Edits.First());
            Assert.Equal("2", ((LSP.SemanticTokensDelta)results).ResultId);
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

            using var testLspServer = CreateTestLspServer(s_singleLineCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);

            // Edit text
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");

            // 1. Updates length of token (10 to 5) and updates token type (comment to keyword)
            // 2. Creates new token for '// Comment'
            var expectedEdit = new LSP.SemanticTokensEdit
            {
                Start = 2,
                DeleteCount = 0,
                Data = new int[]
                {
                    // 'class'
                    /* 0, 0, */ 5, SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Keyword], 0,

                    // '// Comment'
                    1, 0, /* 10,  SemanticTokensCache.TokenTypeToIndex[LSP.SemanticTokenTypes.Comment], 0 */
                }
            };

            Assert.Equal(expectedEdit, ((LSP.SemanticTokensDelta)results).Edits?[0]);
            Assert.Equal("2", ((LSP.SemanticTokensDelta)results).ResultId);
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

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "10");

            // Make sure we're returned SemanticTokens instead of SemanticTokensEdits.
            Assert.True(results.Value is LSP.SemanticTokens);

            // The returned result should now be in the cache and should be of the LSP.SemanticTokensDelta type.
            var cachedResults = await RunGetSemanticTokensEditsAsync(
                testLspServer, caretLocation, previousResultId: ((LSP.SemanticTokens)results).ResultId!);
            Assert.True(cachedResults.Value is LSP.SemanticTokensDelta);
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_InsertNewlineInMiddleOfFile()
        {
            var updatedText =
@"// Comment

static class C { }";

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensDelta)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_ReplacementEdit()
        {
            var updatedText =
@"// Comment
internal struct S { }";

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensDelta)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

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

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensDelta)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact, WorkItem(54671, "https://github.com/dotnet/roslyn/issues/54671")]
        public async Task TestConvertSemanticTokenEditsIntoSemanticTokens_FragmentedTokens()
        {
            var originalText =
@"fo {|caret:|}r (int i = 0;/*c*/; i++)
{

}";

            var updatedText =
@"for (int i = 0;/*c*/; i++)
{

}";

            using var testLspServer = CreateTestLspServer(originalText, out var locations);
            var caretLocation = locations["caret"].First();
            var originalTokens = await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            // Edits to tokens conversion
            var edits = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "1");
            var editsToTokens = ApplySemanticTokensEdits(originalTokens.Data, (LSP.SemanticTokensDelta)edits);

            // Raw tokens
            var rawTokens = await RunGetSemanticTokensAsync(testLspServer, locations["caret"].First());

            Assert.True(Enumerable.SequenceEqual(rawTokens.Data, editsToTokens));
        }

        [Fact]
        public async Task TestGetSemanticTokensEdits_EmptyFileAsync()
        {
            var updatedText = @"";

            using var testLspServer = CreateTestLspServer(s_standardCase, out var locations);
            var caretLocation = locations["caret"].First();
            await RunGetSemanticTokensAsync(testLspServer, caretLocation);
            await UpdateDocumentTextAsync(updatedText, testLspServer.TestWorkspace);

            var results = await RunGetSemanticTokensEditsAsync(testLspServer, caretLocation, previousResultId: "10");

            // Ensure we're returned SemanticTokens instead of SemanticTokensEdits.
            Assert.True(results.Value is LSP.SemanticTokens);

            Assert.Empty(results.First.Data);
        }

        private static int[] ApplySemanticTokensEdits(int[]? originalTokens, LSP.SemanticTokensDelta edits)
        {
            var data = originalTokens.ToList();
            if (edits.Edits != null)
            {
                foreach (var edit in edits.Edits.Reverse())
                {
                    data.RemoveRange(edit.Start, edit.DeleteCount);

                    if (edit.Data is not null)
                    {
                        data.InsertRange(edit.Start, edit.Data);
                    }
                }
            }

            return data.ToArray();
        }
    }
}
