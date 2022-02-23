// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SpellCheck
{
    public class SpellCheckTests : AbstractLanguageServerProtocolTests
    {
        #region Document

        [Fact]
        public async Task TestNoDocumentResultsForClosedFiles()
        {
            var markup =
@"class A
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestDocumentResultsForOpenFiles()
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            var testDocument = testLspServer.TestWorkspace.Documents.Single();
            testDocument.GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            Assert.Single(results);

            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testDocument.AnnotatedSpans),
            });
        }

        #endregion

        private static VSInternalSpellCheckableRange[] GetRanges(SourceText sourceText, IDictionary<string, ImmutableArray<TextSpan>> annotatedSpans)
        {
            var allSpans = annotatedSpans.SelectMany(kvp => kvp.Value.Select(textSpan => (kind: kvp.Key, textSpan)).OrderBy(t => t.textSpan.Start));
            var ranges = allSpans.Select(t => new VSInternalSpellCheckableRange
            {
                Kind = Convert(t.kind),
                Start = ProtocolConversions.LinePositionToPosition(sourceText.Lines.GetLinePosition(t.textSpan.Start)),
                End = ProtocolConversions.LinePositionToPosition(sourceText.Lines.GetLinePosition(t.textSpan.End)),
            });

            return ranges.ToArray();
        }

        private static VSInternalSpellCheckableRangeKind Convert(string kind)
            => kind switch
            {
                "String" => VSInternalSpellCheckableRangeKind.String,
                "Comment" => VSInternalSpellCheckableRangeKind.Comment,
                "Identifier" => VSInternalSpellCheckableRangeKind.Identifier,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };

        private static Task OpenDocumentAsync(TestLspServer testLspServer, Document document)
            => testLspServer.OpenDocumentAsync(document.GetURI());

        private static async Task<VSInternalSpellCheckableRangeReport[]> RunGetDocumentSpellCheckSpansAsync(
            TestLspServer testLspServer,
            Uri uri,
            string? previousResultId = null,
            bool useProgress = false)
        {
            BufferedProgress<VSInternalSpellCheckableRangeReport>? progress = useProgress
                ? BufferedProgress.Create<VSInternalSpellCheckableRangeReport>(null) : null;
            var spans = await testLspServer.ExecuteRequestAsync<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport[]>(
                VSInternalMethods.TextDocumentSpellCheckableRangesName,
                CreateDocumentParams(uri, previousResultId, progress),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(spans);
                spans = progress!.Value.GetValues();
            }

            AssertEx.NotNull(spans);
            return spans;
        }

        private static VSInternalDocumentSpellCheckableParams CreateDocumentParams(
            Uri uri,
            string? previousResultId = null,
            IProgress<VSInternalSpellCheckableRangeReport[]>? progress = null)
        {
            return new VSInternalDocumentSpellCheckableParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                PreviousResultId = previousResultId,
                PartialResultToken = progress,
            };
        }
    }
}
