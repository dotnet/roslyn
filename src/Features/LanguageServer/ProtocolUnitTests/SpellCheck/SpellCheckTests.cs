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

            Assert.Single(results);

            var sourceText = await document.GetTextAsync();
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

            Assert.Single(results);

            var sourceText = await document.GetTextAsync();
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

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsRemovedAfterErrorIsFixed(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"class A {";
        // using var testLspServer = await CreateTestLspServerAsync(markup);

        //            // Calling GetTextBuffer will effectively open the file.
        //            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);

        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);

        //            await InsertTextAsync(testLspServer, document, buffer.CurrentSnapshot.Length, "}");

        //            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), results.Single().ResultId);
        //            Assert.Empty(results[0].Diagnostics);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsRemainAfterErrorIsNotFixed(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"class A {";
        // using var testLspServer = await CreateTestLspServerAsync(markup);

        //            // Calling GetTextBuffer will effectively open the file.
        //            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);
        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

        //            buffer.Insert(0, " ");
        //            await InsertTextAsync(testLspServer, document, position: 0, text: " ");

        //            results = await RunGetDocumentSpellCheckSpansAsync(
        //                testLspServer, document.GetURI(),
        //                useVSDiagnostics,
        //                previousResultId: results[0].ResultId);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics.Single().Range.Start);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsAreNotMapped(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"#line 1 ""test.txt""
        //class A {";
        // using var testLspServer = await CreateTestLspServerAsync(markup);

        //            // Calling GetTextBuffer will effectively open the file.
        //            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);

        //            var results = await RunGetDocumentSpellCheckSpansAsync(
        //                testLspServer, document.GetURI(), useVSDiagnostics);

        //            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        //            Assert.Equal(1, results.Single().Diagnostics.Single().Range.Start.Line);
        //        }

        //        private static async Task InsertTextAsync(
        //            TestLspServer testLspServer,
        //            Document document,
        //            int position,
        //            string text)
        //        {
        //            var sourceText = await document.GetTextAsync();
        //            var lineInfo = sourceText.Lines.GetLinePositionSpan(new TextSpan(position, 0));

        //            await testLspServer.InsertTextAsync(document.GetURI(), (lineInfo.Start.Line, lineInfo.Start.Character, text));
        //        }

        //        private static Task OpenDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.OpenDocumentAsync(document.GetURI());

        //        private static Task CloseDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.CloseDocumentAsync(document.GetURI());

        //        [Fact]
        //        public async Task TestStreamingDocumentDiagnostics(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"class A {";
        // using var testLspServer = await CreateTestLspServerAsync(markup);

        //            // Calling GetTextBuffer will effectively open the file.
        //            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);

        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, document.GetURI(), useProgress: true);

        //            Assert.Equal("CS1513", results!.Single().Diagnostics.Single().Code);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsForOpenFilesUsesActiveContext(bool useVSDiagnostics)
        //        {
        //            var documentText =
        //@"#if ONE
        //class A {
        //#endif
        //class B {";
        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"" PreprocessorSymbols=""ONE"">
        //        <Document FilePath=""C:\C.cs"">{documentText}</Document>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1"">{documentText}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(markup);

        //            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Open either of the documents via LSP, we're tracking the URI and text.
        //            await OpenDocumentAsync(testLspServer, csproj1Document);

        //            // This opens all documents in the workspace and ensures buffers are created.
        //            testLspServer.TestWorkspace.GetTestDocument(csproj1Document.Id).GetTextBuffer();

        //            // Set CSProj2 as the active context and get diagnostics.
        //            testLspServer.TestWorkspace.SetDocumentContext(csproj2Document.Id);
        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj2Document.GetURI(), useVSDiagnostics);
        //            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        //            if (useVSDiagnostics)
        //            {
        //                // Only VSDiagnostics will have the project.
        //                var vsDiagnostic = (LSP.VSDiagnostic)results.Single().Diagnostics.Single();
        //                Assert.Equal("CSProj2", vsDiagnostic.Projects.Single().ProjectName);
        //            }

        //            // Set CSProj1 as the active context and get diagnostics.
        //            testLspServer.TestWorkspace.SetDocumentContext(csproj1Document.Id);
        //            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        //            Assert.Equal(2, results.Single().Diagnostics!.Length);
        //            Assert.All(results.Single().Diagnostics, d => Assert.Equal("CS1513", d.Code));

        //            if (useVSDiagnostics)
        //            {
        //                Assert.All(results.Single().Diagnostics, d => Assert.Equal("CSProj1", ((VSDiagnostic)d).Projects.Single().ProjectName));
        //            }
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsWithChangeInReferencedProject(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //        <ProjectReference>CSProj2</ProjectReference>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(markup);
        //            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
        //            await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

        //            // Verify we a diagnostic in A.cs since B does not exist.
        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        //            Assert.Single(results);
        //            Assert.Equal("CS0246", results.Single().Diagnostics.Single().Code);

        //            // Insert B into B.cs and verify that the error in A.cs is now gone.
        //            var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
        //            await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
        //            var originalResultId = results.Single().ResultId;
        //            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj1Document.GetURI(), originalResultId);
        //            Assert.Single(results);
        //            Assert.Empty(results.Single().Diagnostics);
        //            Assert.NotEqual(originalResultId, results.Single().ResultId);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsWithChangeInNotReferencedProject(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(markup);
        //            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
        //            await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

        //            // Verify we get a diagnostic in A since the class B does not exist.
        //            var results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        //            Assert.Single(results);
        //            Assert.Equal("CS0246", results.Single().Diagnostics.Single().Code);

        //            // Add B to CSProj2 and verify that we get an unchanged result (still has diagnostic) for A.cs
        //            // since CSProj1 does not reference CSProj2
        //            var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
        //            await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
        //            var originalResultId = results.Single().ResultId;
        //            results = await RunGetDocumentSpellCheckSpansAsync(testLspServer, csproj1Document.GetURI(), originalResultId);
        //            Assert.Single(results);
        //            Assert.Null(results.Single().Diagnostics);
        //            Assert.Equal(originalResultId, results.Single().ResultId);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsFromRazorServer(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"class A {";

        //            // Turn off pull diagnostics by default, but send a request to the razor LSP server which is always pull.
        //            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFiles, DiagnosticMode.Push, serverKind: WellKnownLspServerKinds.RazorLspServer);

        //            // Calling GetTextBuffer will effectively open the file.
        //            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);

        //            var results = await RunGetDocumentSpellCheckSpansAsync(
        //                testLspServer, document.GetURI(), useVSDiagnostics);

        //            // Assert that we have diagnostics even though the option is set to push.
        //            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        //            Assert.NotNull(results.Single().Diagnostics.Single().CodeDescription!.Href);
        //        }

        //        [Fact]
        //        public async Task TestDocumentDiagnosticsFromLiveShareServer(bool useVSDiagnostics)
        //        {
        //            var markup =
        //@"class A {";

        //            // Turn off pull diagnostics by default, but send a request to the razor LSP server which is always pull.
        //            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFiles, DiagnosticMode.Push, serverKind: WellKnownLspServerKinds.LiveShareLspServer);

        //            // Calling GetTextBuffer will effectively open the file.
        //            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        //            await OpenDocumentAsync(testLspServer, document);

        //            var results = await RunGetDocumentSpellCheckSpansAsync(
        //                testLspServer, document.GetURI(), useVSDiagnostics);

        //            // Assert that we have diagnostics even though the option is set to push.
        //            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        //            Assert.NotNull(results.Single().Diagnostics.Single().CodeDescription!.Href);
        //        }

        //        #endregion

        //        #region Workspace Diagnostics

        //        [Fact]
        //        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOff(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                new[] { markup1, markup2 }, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Empty(results);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsForClosedFilesWithFSAOn(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);
        //        }

        //        [Fact]
        //        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOnAndInPushMode(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, pullDiagnostics: false);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Empty(results[0].Diagnostics);
        //            Assert.Empty(results[1].Diagnostics);
        //        }

        //        [Fact]
        //        public async Task TestNoWorkspaceDiagnosticsForClosedFilesInProjectsWithIncorrectLanguage(bool useVSDiagnostics)
        //        {
        //            var csharpMarkup =
        //@"class A {";
        //            var typeScriptMarkup = "???";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\C.cs"">{csharpMarkup}</Document>
        //    </Project>
        //    <Project Language=""TypeScript"" CommonReferences=""true"" AssemblyName=""TypeScriptProj"">
        //        <Document FilePath=""C:\T.ts"">{typeScriptMarkup}</Document>
        //    </Project>
        //</Workspace>";

        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.True(results.All(r => r.TextDocument!.Uri.LocalPath == "C:\\C.cs"));
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsForSourceGeneratedFiles(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        //            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
        //                markups: Array.Empty<string>(),
        //                sourceGeneratedMarkups: new[] { markup1, markup2 },
        //                BackgroundAnalysisScope.FullSolution,
        //                useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            // Project.GetSourceGeneratedDocumentsAsync may not return documents in a deterministic order, so we sort
        //            // the results here to ensure subsequent assertions are not dependent on the order of items provided by the
        //            // project.
        //            results = results.Sort((x, y) => x.Uri.ToString().CompareTo(y.Uri.ToString()));

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsForRemovedDocument(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);

        //            testLspServer.TestWorkspace.OnDocumentRemoved(testLspServer.TestWorkspace.Documents.First().Id);

        //            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        //            // First doc should show up as removed.
        //            Assert.Equal(2, results2.Length);
        //            Assert.Null(results2[0].Diagnostics);
        //            Assert.Null(results2[0].ResultId);

        //            // Second doc should be changed as the project has changed.
        //            Assert.Empty(results[1].Diagnostics);
        //            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        //        }

        //        private static ImmutableArray<(string resultId, Uri uri)> CreateDiagnosticParamsFromPreviousReports(ImmutableArray<TestDiagnosticResult> results)
        //        {
        //            return results.Select(r => (r.ResultId, r.Uri)).ToImmutableArray();
        //        }

        //        [Fact]
        //        public async Task TestNoChangeIfWorkspaceDiagnosticsCalledTwice(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);

        //            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        //            Assert.Equal(2, results2.Length);
        //            Assert.Null(results2[0].Diagnostics);
        //            Assert.Null(results2[1].Diagnostics);

        //            Assert.Equal(results[0].ResultId, results2[0].ResultId);
        //            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsRemovedAfterErrorIsFixed(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Empty(results[1].Diagnostics);

        //            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
        //            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

        //            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        //            Assert.Equal(2, results2.Length);
        //            Assert.Empty(results2[0].Diagnostics);
        //            // Project has changed, so we re-computed diagnostics as changes in the first file
        //            // may have changed results in the second.
        //            Assert.Empty(results2[1].Diagnostics);

        //            Assert.NotEqual(results[0].ResultId, results2[0].ResultId);
        //            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsRemainAfterErrorIsNotFixed(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

        //            Assert.Empty(results[1].Diagnostics);

        //            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
        //            buffer.Insert(0, " ");

        //            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.First();
        //            var text = await document.GetTextAsync();

        //            // Hacky, but we need to close the document manually since editing the text-buffer will open it in the
        //            // test-workspace.
        //            testLspServer.TestWorkspace.OnDocumentClosed(
        //                document.Id, TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));

        //            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal("CS1513", results2[0].Diagnostics.Single().Code);
        //            Assert.Equal(new Position { Line = 0, Character = 10 }, results2[0].Diagnostics.Single().Range.Start);

        //            Assert.Empty(results2[1].Diagnostics);
        //            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestStreamingWorkspaceDiagnostics(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useProgress: true);

        //            Assert.Equal("CS1513", results[0].Diagnostics![0].Code);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsAreNotMapped(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"#line 1 ""test.txt""
        //class A {";
        //            var markup2 = "";
        // using var testLspServer = await CreateTestLspServerAsync(
        //                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            Assert.Equal(2, results.Length);
        //            Assert.Equal(new Uri("C:/test1.cs"), results[0].TextDocument!.Uri);
        //            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        //            Assert.Equal(1, results[0].Diagnostics.Single().Range.Start.Line);
        //            Assert.Empty(results[1].Diagnostics);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsWithChangeInReferencedProject(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //        <ProjectReference>CSProj2</ProjectReference>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Verify we a diagnostic in A.cs since B does not exist
        //            // and a diagnostic in B.cs since it is missing the class name.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
        //            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

        //            // Insert B into B.cs via the workspace.
        //            var caretLocation = testLspServer.GetLocations("caret").First().Range;
        //            var csproj2DocumentText = await csproj2Document.GetTextAsync();
        //            var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
        //            await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

        //            // Get updated workspace diagnostics for the change.
        //            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);

        //            // Verify diagnostics for A.cs are updated as the type B now exists.
        //            Assert.Empty(results[0].Diagnostics);
        //            Assert.NotEqual(previousResultIds[0].resultId, results[0].ResultId);

        //            // Verify diagnostics for B.cs are updated as the class definition is now correct.
        //            Assert.Empty(results[1].Diagnostics);
        //            Assert.NotEqual(previousResultIds[1].resultId, results[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsWithChangeInRecursiveReferencedProject(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    public class A
        //    {
        //    }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class B
        //    {
        //    }
        //}";
        //            var markup3 =
        //@"namespace M
        //{
        //    public class {|caret:|}
        //    {
        //    }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <ProjectReference>CSProj2</ProjectReference>
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <ProjectReference>CSProj3</ProjectReference>
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj3"">
        //        <Document FilePath=""C:\C.cs"">{markup3}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(
        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj3Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj3").Single().Documents.First();

        //            // Verify we have a diagnostic in C.cs initially.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(3, results.Length);
        //            Assert.Empty(results[0].Diagnostics);
        //            Assert.Empty(results[1].Diagnostics);
        //            Assert.Equal("CS1001", results[2].Diagnostics.Single().Code);

        //            // Insert C into C.cs via the workspace.
        //            var caretLocation = testLspServer.GetLocations("caret").First().Range;
        //            var csproj3DocumentText = await csproj3Document.GetTextAsync().ConfigureAwait(false);
        //            var newCsProj3Document = csproj3Document.WithText(csproj3DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj3DocumentText), "C")));
        //            await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj3Document.Id, newCsProj3Document.Project.Solution).ConfigureAwait(false);

        //            // Get updated workspace diagnostics for the change.
        //            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds).ConfigureAwait(false);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(3, results.Length);

        //            // Verify that new diagnostics are returned for all files (even though the diagnostics for the first two files are the same)
        //            // since we re-calculate when transitive project dependencies change.
        //            Assert.Empty(results[0].Diagnostics);
        //            Assert.NotEqual(previousResultIds[0].resultId, results[0].ResultId);

        //            Assert.Empty(results[1].Diagnostics);
        //            Assert.NotEqual(previousResultIds[1].resultId, results[1].ResultId);

        //            Assert.Empty(results[2].Diagnostics);
        //            Assert.NotEqual(previousResultIds[2].resultId, results[2].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsWithChangeInNotReferencedProject(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(
        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Verify we a diagnostic in A.cs since B does not exist
        //            // and a diagnostic in B.cs since it is missing the class name.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
        //            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

        //            // Insert B into B.cs via the workspace.
        //            var caretLocation = testLspServer.GetLocations("caret").First().Range;
        //            var csproj2DocumentText = await csproj2Document.GetTextAsync();
        //            var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
        //            await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

        //            // Get updated workspace diagnostics for the change.
        //            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResultIds);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);

        //            // Verify the diagnostic result for A.cs is unchanged as A.cs does not reference CSProj2.
        //            Assert.Null(results[0].Diagnostics);
        //            Assert.Equal(previousResultIds[0].resultId, results[0].ResultId);

        //            // Verify that the diagnostics result for B.cs reflects the change we made to it.
        //            Assert.Empty(results[1].Diagnostics);
        //            Assert.NotEqual(previousResultIds[1].resultId, results[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedAndChanged(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //        <ProjectReference>CSProj2</ProjectReference>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(
        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Verify we a diagnostic in A.cs since B does not exist
        //            // and a diagnostic in B.cs since it is missing the class name.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
        //            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

        //            // Change and reload the project via the workspace.
        //            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        //            projectInfo = projectInfo.WithCompilationOptions(projectInfo.CompilationOptions!.WithPlatform(Platform.X64));
        //            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        //            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        //            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        //            // Get updated workspace diagnostics for the change.
        //            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);

        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);

        //            // The diagnostics should have been recalculated for both projects as a referenced project changed.
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
        //            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedUnChanged(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";
        //            var markup2 =
        //@"namespace M
        //{
        //    public class {|caret:|} { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //        <ProjectReference>CSProj2</ProjectReference>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs"">{markup2}</Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(
        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Verify we a diagnostic in A.cs since B does not exist
        //            // and a diagnostic in B.cs since it is missing the class name.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
        //            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

        //            // Reload the project via the workspace.
        //            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        //            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        //            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        //            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        //            // Get updated workspace diagnostics for the change.
        //            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);

        //            // Verify that since no actual changes have been made we report unchanged diagnostics.
        //            AssertEx.NotNull(results);
        //            Assert.Equal(2, results.Length);

        //            // Diagnostics should be unchanged as the referenced project was only unloaded / reloaded, but did not actually change.
        //            Assert.Null(results[0].Diagnostics);
        //            Assert.Equal(previousResultIds[0].resultId, results[0].ResultId);
        //            Assert.Null(results[1].Diagnostics);
        //            Assert.Equal(previousResultIds[1].resultId, results[1].ResultId);
        //        }

        //        [Fact]
        //        public async Task TestWorkspaceDiagnosticsOrderOfReferencedProjectsReloadedDoesNotMatter(bool useVSDiagnostics)
        //        {
        //            var markup1 =
        //@"namespace M
        //{
        //    class A : B { }
        //}";

        //            var workspaceXml =
        //@$"<Workspace>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        //        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        //        <ProjectReference>CSProj2</ProjectReference>
        //        <ProjectReference>CSProj3</ProjectReference>
        //    </Project>
        //    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        //        <Document FilePath=""C:\B.cs""></Document>
        //    </Project>
        //<Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj3"">
        //        <Document FilePath=""C:\C.cs""></Document>
        //    </Project>
        //</Workspace>";

        // using var testLspServer = await CreateTestLspServerAsync(
        //            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        //            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        //            // Verify we a diagnostic in A.cs since B does not exist
        //            // and a diagnostic in B.cs since it is missing the class name.
        //            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        //            AssertEx.NotNull(results);
        //            Assert.Equal(3, results.Length);
        //            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);

        //            // Reload the project via the workspace.
        //            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        //            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        //            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        //            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        //            // Get updated workspace diagnostics for the change.
        //            var previousResults = CreateDiagnosticParamsFromPreviousReports(results);
        //            var previousResultIds = previousResults.Select(param => param.resultId).ToImmutableArray();
        //            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResults);

        //            // Verify that since no actual changes have been made we report unchanged diagnostics.
        //            AssertEx.NotNull(results);
        //            Assert.Equal(3, results.Length);

        //            // Diagnostics should be unchanged as a referenced project was unloaded and reloaded.  Order should not matter.
        //            Assert.Null(results[0].Diagnostics);
        //            Assert.All(results, result => Assert.Null(result.Diagnostics));
        //            Assert.All(results, result => Assert.True(previousResultIds.Contains(result.ResultId)));
        //        }

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
