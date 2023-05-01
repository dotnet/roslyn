// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public abstract class AbstractSemanticTokensTests : AbstractLanguageServerProtocolTests
    {
        protected AbstractSemanticTokensTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private protected static IReadOnlyDictionary<string, int> GetTokenTypeToIndex(TestLspServer server)
            => SemanticTokensSchema.GetSchema(server.ClientCapabilities).TokenTypeToIndex;

        private protected static async Task<LSP.SemanticTokens> RunGetSemanticTokensRangeAsync(TestLspServer testLspServer, LSP.Location caret, LSP.Range range)
        {
            var result = await testLspServer.ExecuteRequestAsync<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>(LSP.Methods.TextDocumentSemanticTokensRangeName,
                CreateSemanticTokensRangeParams(caret, range), CancellationToken.None);
            Contract.ThrowIfNull(result);
            return result;
        }

        private static LSP.SemanticTokensRangeParams CreateSemanticTokensRangeParams(LSP.Location caret, LSP.Range range)
            => new LSP.SemanticTokensRangeParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                Range = range
            };

        protected static async Task UpdateDocumentTextAsync(string updatedText, Workspace workspace)
        {
            var docId = ((TestWorkspace)workspace).Documents.First().Id;
            await ((TestWorkspace)workspace).ChangeDocumentAsync(docId, SourceText.From(updatedText));
        }

        // VS doesn't currently support multi-line tokens, so we want to verify that we aren't
        // returning any in the tokens array.
        private protected static async Task VerifyBasicInvariantsAndNoMultiLineTokens(TestLspServer testLspServer, int[] tokens)
        {
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var text = await document.GetTextAsync().ConfigureAwait(false);

            var currentLine = 0;
            var currentChar = 0;

            Assert.True(tokens.Length % 5 == 0);

            for (var i = 0; i < tokens.Length; i += 5)
            {
                // i: line # (relative to previous line)
                // i + 1: character # (relative to start of previous token in the line or 0)
                // i + 2: token length

                // Gets the current absolute line index
                Assert.True(tokens[i] >= 0, "The line offset should never be negative.");
                currentLine += tokens[i];

                // Gets the character # relative to the start of the line
                Assert.True(tokens[i + 1] >= 0, "The character offset should never be negative.");

                if (tokens[i] != 0)
                {
                    currentChar = tokens[i + 1];
                }
                else
                {
                    currentChar += tokens[i + 1];
                    Assert.True(currentChar >= 0, "The first token on the line can't be a negative position, but applying an offset took us there.");
                }

                // Gets the length of the token
                var tokenLength = tokens[i + 2];
                Assert.True(tokenLength >= 0, "The token cannot have a negative length.");

                var lineLength = text.Lines[currentLine].SpanIncludingLineBreak.Length;

                // If this assertion fails, we didn't break up a multi-line token properly.
                var tokenTypeToIndex = GetTokenTypeToIndex(testLspServer);
                var kind = tokenTypeToIndex.Where(kvp => kvp.Value == tokens[i + 3]).Single().Key;

                Assert.True(currentChar + tokenLength <= lineLength,
                    $"Multi-line token of type {kind} found on line {currentLine} at character index {currentChar}. " +
                    $"The token ends at index {currentChar + tokenLength}, which exceeds the line length of {lineLength}.");
            }
        }

        /// <summary>
        /// This converts the raw array form back to a slightly more readable form for the purposes of understanding the diff should a test
        /// fail. This groups rows by five (so that way the diff can't desynced from the start of a new token), and also replaces the token index
        /// back with the string again.
        /// </summary>
        protected static ImmutableArray<string> ConvertToReadableFormat(
            ClientCapabilities capabilities, int[] data)
        {
            var convertedStringsBuilder = ImmutableArray.CreateBuilder<string>(data.Length / 5);
            var tokenTypeToIndex = SemanticTokensSchema.GetSchema(capabilities).TokenTypeToIndex;

            for (var i = 0; i < data.Length; i += 5)
            {
                var kind = tokenTypeToIndex.Single(kvp => kvp.Value == data[i + 3]).Key;

                convertedStringsBuilder.Add($"{data[i]}, {data[i + 1]}, {data[i + 2]}, {kind}, {data[i + 4]}");
            }

            return convertedStringsBuilder.MoveToImmutable();
        }
    }
}
