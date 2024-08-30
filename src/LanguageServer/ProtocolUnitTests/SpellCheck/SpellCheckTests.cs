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
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SpellCheck
{
    public sealed class SpellCheckTests(ITestOutputHelper testOutputHelper)
        : AbstractLanguageServerProtocolTests(testOutputHelper)
    {

        #region Document

        [Theory, CombinatorialData]
        public async Task TestNoDocumentResultsForClosedFiles(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            Assert.Empty(results);
        }

        [Theory, CombinatorialData]
        public async Task TestDocumentResultsForOpenFiles(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testDocument.AnnotatedSpans),
            });
        }

        [Theory, CombinatorialData]
        public async Task TestLotsOfResults(bool mutatingLspWorkspace)
        {
            // Produce an 'interesting' large string, with varying length identifiers, and varying distances between the spans. 
            var random = new Random(Seed: 0);
            var markup = string.Join(Environment.NewLine, Enumerable.Range(0, 5500).Select(v =>
$$"""
class {|Identifier:A{{v}}|}
{
}
{{string.Join(Environment.NewLine, Enumerable.Repeat("", random.Next() % 5))}}
"""));
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            // Calling GetTextBuffer will effectively open the file.
            var testDocument = testLspServer.TestWorkspace.Documents.Single();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI());

            var sourceText = await document.GetTextAsync();
            Assert.True(results.Length == 6);

            var allRanges = GetRanges(testDocument.AnnotatedSpans);
            for (var i = 0; i < results.Length; i++)
            {
                AssertJsonEquals(results[i], new VSInternalSpellCheckableRangeReport
                {
                    ResultId = "DocumentSpellCheckHandler:0",
                    Ranges = allRanges.Skip(3 * i * 1000).Take(3 * 1000).ToArray(),
                });
            }
        }

        [Theory, CombinatorialData]
        public async Task TestDocumentResultsForRemovedDocument(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
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
                Ranges = GetRanges(workspace.Documents.Single().AnnotatedSpans),
            });

            // Now remove the doc.
            workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
            await CloseDocumentAsync(testLspServer, document);

            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), results.Single().ResultId).ConfigureAwait(false);

            Assert.Null(results.Single().Ranges);
            Assert.Null(results.Single().ResultId);
        }

        [Theory, CombinatorialData]
        public async Task TestNoChangeIfDocumentResultsCalledTwice(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });

            var resultId = results.Single().ResultId;
            results = await RunGetDocumentSpellCheckSpansAsync(
                testLspServer, document.GetURI(), previousResultId: resultId);

            Assert.Null(results.Single().Ranges);
            Assert.Equal(resultId, results.Single().ResultId);
        }

        [Theory, CombinatorialData]
        public async Task TestDocumentResultChangedAfterEntityAdded(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}

";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });

            await InsertTextAsync(testLspServer, document, buffer.CurrentSnapshot.Length, "// comment");

            var (_, lspSolution) = await testLspServer.GetManager().GetLspSolutionInfoAsync(CancellationToken.None).ConfigureAwait(false);
            document = lspSolution!.Projects.Single().Documents.Single();
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
                Ranges = GetRanges(annotatedSpans),
            });
        }

        [Theory, CombinatorialData]
        public async Task TestDocumentResultIdChangesAfterEdit(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        [Theory, CombinatorialData]
        public async Task TestDocumentResultsAreNotMapped(bool mutatingLspWorkspace)
        {
            var markup =
@"#line 4 ""test.txt""
class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        [Theory, CombinatorialData]
        public async Task TestStreamingDocumentDiagnostics(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|Identifier:A|}
{
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.Single().AnnotatedSpans),
            });
        }

        #endregion

        #region Workspace Diagnostics

        [Theory, CombinatorialData]
        public async Task TestWorkspaceResultsForClosedFiles(bool mutatingLspWorkspace)
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            await using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 }, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);
        }

        [Theory, CombinatorialData]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesInProjectsWithIncorrectLanguage(bool mutatingLspWorkspace)
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

            await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.True(results.All(r => r.TextDocument!.Uri.LocalPath == "C:\\C.cs"));
        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsForSourceGeneratedFiles()
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        //            await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
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

        [Theory, CombinatorialData]
        public async Task TestWorkspaceResultsForRemovedDocument(bool mutatingLspWorkspace)
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            await using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 }, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
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

        [Theory, CombinatorialData]
        public async Task TestNoChangeIfWorkspaceResultsCalledTwice(bool mutatingLspWorkspace)
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            await using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 }, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            var results2 = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, previousResults: CreateParamsFromPreviousReports(results));

            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Ranges);
            Assert.Null(results2[1].Ranges);

            Assert.Equal(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Theory, CombinatorialData]
        public async Task TestWorkspaceResultUpdatedAfterEdit(bool mutatingLspWorkspace)
        {
            var markup1 =
@"class {|Identifier:A|}
{
}

";
            var markup2 = "";
            await using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 }, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "// comment");

            var results2 = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, previousResults: CreateParamsFromPreviousReports(results));

            Assert.Equal(2, results2.Length);
            var (_, lspSolution) = await testLspServer.GetManager().GetLspSolutionInfoAsync(CancellationToken.None).ConfigureAwait(false);
            document = lspSolution!.Projects.Single().Documents.First();
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
                Ranges = GetRanges(annotatedSpans),
            });
            Assert.Null(results2[1].Ranges);

            Assert.NotEqual(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Theory, CombinatorialData]
        public async Task TestStreamingWorkspaceResults(bool mutatingLspWorkspace)
        {
            var markup1 =
@"class {|Identifier:A|}
{
}";
            var markup2 = "";
            await using var testLspServer = await CreateTestLspServerAsync(new[] { markup1, markup2 }, mutatingLspWorkspace);

            var results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer);

            Assert.Equal(2, results.Length);

            var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.First();
            var sourceText = await document.GetTextAsync();
            AssertJsonEquals(results[0], new VSInternalWorkspaceSpellCheckableReport
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
                ResultId = "WorkspaceSpellCheckHandler:0",
                Ranges = GetRanges(testLspServer.TestWorkspace.Documents.First().AnnotatedSpans),
            });
            Assert.Empty(results[1].Ranges);

            results = await RunGetWorkspaceSpellCheckSpansAsync(testLspServer, CreateParamsFromPreviousReports(results), useProgress: true);

            Assert.Equal(2, results.Length);
            Assert.Null(results[0].Ranges);
            Assert.Null(results[1].Ranges);
        }

        #endregion

        private static int[] GetRanges(IDictionary<string, ImmutableArray<TextSpan>> annotatedSpans)
        {
            var allSpans = annotatedSpans
                .SelectMany(kvp => kvp.Value.Select(textSpan => (kind: kvp.Key, textSpan))
                .OrderBy(t => t.textSpan.Start))
                .ToImmutableArray();

            var ranges = new int[allSpans.Length * 3];
            var index = 0;
            var lastSpanEnd = 0;

            foreach (var (kind, span) in allSpans)
            {
                ranges[index++] = (int)Convert(kind);
                ranges[index++] = span.Start - lastSpanEnd;
                ranges[index++] = span.Length;

                lastSpanEnd = span.End;
            }

            return ranges;
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
            BufferedProgress<VSInternalSpellCheckableRangeReport[]>? progress = useProgress
                ? BufferedProgress.Create<VSInternalSpellCheckableRangeReport[]>(null) : null;
            var spans = await testLspServer.ExecuteRequestAsync<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport[]>(
                VSInternalMethods.TextDocumentSpellCheckableRangesName,
                CreateDocumentParams(uri, previousResultId, progress),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(spans);
                spans = progress!.Value.GetFlattenedValues();
            }

            AssertEx.NotNull(spans);
            return spans;
        }

        private static async Task<VSInternalWorkspaceSpellCheckableReport[]> RunGetWorkspaceSpellCheckSpansAsync(
            TestLspServer testLspServer,
            ImmutableArray<(string resultId, Uri uri)>? previousResults = null,
            bool useProgress = false)
        {
            BufferedProgress<VSInternalWorkspaceSpellCheckableReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalWorkspaceSpellCheckableReport[]>(null) : null;
            var spans = await testLspServer.ExecuteRequestAsync<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]>(
                VSInternalMethods.WorkspaceSpellCheckableRangesName,
                CreateWorkspaceParams(previousResults, progress),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(spans);
                spans = progress!.Value.GetFlattenedValues();
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
