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

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testDocument.AnnotatedSpans),
            });
        }

        [Fact]
        public async Task TestDocumentResultsForRemovedDocument()
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var workspace = testLspServer.TestWorkspace;

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            // Get the diagnostics for the solution containing the doc.
            var solution = document.Project.Solution;

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI()).ConfigureAwait(false);

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, workspace.Documents.Single().AnnotatedSpans),
            });

            // Now remove the doc.
            workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
            await CloseDocumentAsync(testLspServer, document);

            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), results.Single().ResultId).ConfigureAwait(false);

            Assert.Null(results.Single().Ranges);
            Assert.Null(results.Single().ResultId);
        }

        [Fact]
        public async Task TestNoChangeIfDocumentResultsCalledTwice()
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });

            var resultId = results.Single().ResultId;
            results = await RunGetDocumentSpellCheckSpansAsync(
                testLspServer, document.GetURI(), previousResultId: resultId);

            Assert.Null(results.Single().Ranges);
            Assert.Equal(resultId, results.Single().ResultId);
        }

        [Fact]
        public async Task TestDocumentResultChangedAfterEntityAdded()
        {
            var markup =
@"class {|Identifier:A|}
{
}

";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });

            await InsertTextAsync(testLspServer, document, buffer.CurrentSnapshot.Length, "// comment");

            document = testLspServer.GetManager().TryGetHostLspSolution()!.Projects.Single().Documents.Single();
            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), results.Single().ResultId);

            MarkupTestFile.GetSpans(
@"class {|Identifier:A|}
{
}

{|Comment:// comment|}", out _, out IDictionary<string, ImmutableArray<TextSpan>> annotatedSpans);

            sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:1",
                Ranges = GetRanges(sourceText, annotatedSpans),
            });
        }

        [Fact]
        public async Task TestDocumentResultIdChangesAfterEdit()
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);
            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });

            await InsertTextAsync(testLspServer, document, sourceText.Length, text: " ");

            results = await RunGetDocumentSpellCheckSpansAsync(
                testLspServer, document.GetURI(),
                previousResultId: results[0].ResultId);

            sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:1",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        [Fact]
        public async Task TestDocumentResultsAreNotMapped()
        {
            var markup =
@"#line 4 ""test.txt""
class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        [Fact]
        public async Task TestStreamingDocumentDiagnostics()
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), useProgress: true);

            var sourceText = await document.GetTextAsync();
            Assert.Single(results);
            AssertJsonEquals(results.Single(), new VSInternalSpellCheckableRangeReport
            {
                ResultId = "DocumentSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        #endregion

        #region Workspace Diagnostics

        [Fact]
        public async Task TestWorkspaceResultsForClosedFiles()
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 });

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);
        }

        [Fact]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesInProjectsWithIncorrectLanguage()
        {
            var csharpMarkup =
@"class A {";
            var typeScriptMarkup = "???";

            var workspaceXml =
@$"<Workspace>
            <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
                <Document FilePath=""C:\C.cs"">{csharpMarkup}</Document>
            </Project>
            <Project Language=""TypeScript"" CommonReferences=""true"" AssemblyName=""TypeScriptProj"">
                <Document FilePath=""C:\T.ts"">{typeScriptMarkup}</Document>
            </Project>
        </Workspace>";

            using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.True(results.All(r => r.TextDocument!.Uri.LocalPath == "C:\\C.cs"));
        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsForSourceGeneratedFiles()
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        //            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
        //                markups: Array.Empty<string>(),
        //                sourceGeneratedMarkups: new[] { markup1, markup2 },
        //                BackgroundAnalysisScope.FullSolution,
        //                useVSDiagnostics);

        //            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

        //            // Project.GetSourceGeneratedDocumentsAsync may not return documents in a deterministic order, so we sort
        //            // the results here to ensure subsequent assertions are not dependent on the order of items provided by the
        //            // project.
        //            results = results.Sort((x, y) => x.Uri.ToString().CompareTo(y.Uri.ToString()));

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);
        //        }

        [Fact]
        public async Task TestWorkspaceResultsForRemovedDocument()
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 });

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            testLspServer.TestWorkspace.OnDocumentRemoved(testLspServer.TestWorkspace.Documents.First().Id);

            var results2 = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, previousResults: CreateParamsFromPreviousReports(results));

            // First doc should show up as removed.
            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Ranges);
            Assert.Null(results2[0].ResultId);

            // Second doc should be unchanged
            Assert.Empty(results[1].Ranges);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestNoChangeIfWorkspaceResultsCalledTwice()
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 });

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            var results2 = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, previousResults: CreateParamsFromPreviousReports(results));

            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Ranges);
            Assert.Null(results2[1].Ranges);

            Assert.Equal(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceResultUpdatedAfterEdit()
        {
            var markup1 =
@"class {|Identifier:A|}
{
}

";
            var markup2 = "";
            using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 });

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "// comment");

            var results2 = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, previousResults: CreateParamsFromPreviousReports(results));

            Assert.Equal(2, results2.Length);
            document = testLspServer.GetManager().TryGetHostLspSolution()!.Projects.Single().Documents.First();
            sourceText = await document.GetTextAsync();

            MarkupTestFile.GetSpans(
@"class {|Identifier:A|}
{
}

{|Comment:// comment|}", out _, out IDictionary<string, ImmutableArray<TextSpan>> annotatedSpans);

            AssertJsonEquals(results2[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:2",
                Ranges = GetRanges(sourceText, annotatedSpans),
            });
            Assert.Null(results2[1].Ranges);

            Assert.NotEqual(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestStreamingWorkspaceResults()
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 });

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(sourceText, testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, CreateParamsFromPreviousReports(results), useProgress: true);

            Assert.Equal(2, results.Length);
            Assert.Null(results[0].Ranges);
            Assert.Null(results[1].Ranges);
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

        private static Task CloseDocumentAsync(TestLspServer testLspServer, Document document)
            => testLspServer.CloseDocumentAsync(document.GetURI());

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

        private static async Task<VSInternalWorkspaceSpellCheckableReport[]> RunGetWorkspaceSpellCheckSpansAsync(
            TestLspServer testLspServer,
            ImmutableArray<(string resultId, Uri uri)>? previousResults = null,
            bool useProgress = false)
        {
            BufferedProgress<VSInternalWorkspaceSpellCheckableReport>? progress = useProgress ? BufferedProgress.Create<VSInternalWorkspaceSpellCheckableReport>(null) : null;
            var spans = await testLspServer.ExecuteRequestAsync<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]>(
                VSInternalMethods.WorkspaceSpellCheckableRangesName,
                CreateWorkspaceParams(previousResults, progress),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(spans);
                spans = progress!.Value.GetValues();
            }

            AssertEx.NotNull(spans);
            return spans;
        }

        private static async Task InsertTextAsync(
            TestLspServer testLspServer,
            Document document,
            int position,
            string text)
        {
            var sourceText = await document.GetTextAsync();
            var lineInfo = sourceText.Lines.GetLinePositionSpan(new TextSpan(position, 0));

            await testLspServer.InsertTextAsync(document.GetURI(), (lineInfo.Start.Line, lineInfo.Start.Character, text));
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

        private static VSInternalWorkspaceSpellCheckableParams CreateWorkspaceParams(
            ImmutableArray<(string resultId, Uri uri)>? previousResults = null,
            IProgress<VSInternalWorkspaceSpellCheckableReport[]>? progress = null)
        {
            return new VSInternalWorkspaceSpellCheckableParams
            {
                PreviousResults = previousResults?.Select(r => new VSInternalStreamingParams { PreviousResultId = r.resultId, TextDocument = new TextDocumentIdentifier { Uri = r.uri } }).ToArray(),
                PartialResultToken = progress,
            };
        }

        private static ImmutableArray<(string resultId, Uri uri)> CreateParamsFromPreviousReports(VSInternalWorkspaceSpellCheckableReport[] results)
        {
            return results.Select(r => (r.ResultId!, r.TextDocument.Uri)).ToImmutableArray();
        }
    }
}
