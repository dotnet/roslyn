// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    public class PullDiagnosticTests : AbstractLanguageServerProtocolTests
    {
        #region Document Diagnostics

        [Fact]
        public async Task TestNoDocumentDiagnosticsForClosedFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
            Assert.NotNull(results.Single().Diagnostics.Single().CodeDescription!.Href);
        }

        [Fact]
        public async Task TestNoDocumentDiagnosticsForOpenFilesWithFSAOffIfInPushMode()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects, pullDiagnostics: false);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Empty(results.Single().Diagnostics);
        }

        [Fact]
        public async Task TestNoDocumentDiagnosticsForOpenFilesIfDefaultAndFeatureFlagOff()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects, DiagnosticMode.Default);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();
            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            await OpenDocumentAsync(testLspServer, document);

            // Ensure we get no diagnostics when feature flag is off.
            testLspServer.TestWorkspace.SetOptions(testLspServer.TestWorkspace.Options.WithChangedOption(DiagnosticOptions.LspPullDiagnosticsFeatureFlag, false));

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Assert.Empty(results.Single().Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesIfDefaultAndFeatureFlagOn()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects, DiagnosticMode.Default);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();
            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            await OpenDocumentAsync(testLspServer, document);

            testLspServer.TestWorkspace.SetOptions(testLspServer.TestWorkspace.Options.WithChangedOption(DiagnosticOptions.LspPullDiagnosticsFeatureFlag, true));

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForRemovedDocument()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);
            var workspace = testLspServer.TestWorkspace;

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            // Get the diagnostics for the solution containing the doc.
            var solution = document.Project.Solution;

            await OpenDocumentAsync(testLspServer, document);

            await WaitForDiagnosticsAsync(workspace);
            var results = await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(
                VSInternalMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(document.GetURI()),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            // Now remove the doc.
            workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
            await CloseDocumentAsync(testLspServer, document);

            // And get diagnostic again, using the same doc-id as before.
            await WaitForDiagnosticsAsync(workspace);
            results = await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(
                VSInternalMethods.DocumentPullDiagnosticName,
                new VSInternalDocumentDiagnosticsParams { PreviousResultId = results.Single().ResultId, TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document) },
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            Assert.Null(results.Single().Diagnostics);
            Assert.Null(results.Single().ResultId);
        }

        [Fact]
        public async Task TestNoChangeIfDocumentDiagnosticsCalledTwice()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            var resultId = results.Single().ResultId;
            results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI(), previousResultId: resultId);

            Assert.Null(results.Single().Diagnostics);
            Assert.Equal(resultId, results.Single().ResultId);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsRemovedAfterErrorIsFixed()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Contract.ThrowIfNull(results);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);

            await InsertTextAsync(testLspServer, document, buffer.CurrentSnapshot.Length, "}");

            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Contract.ThrowIfNull(results);
            Assert.Empty(results[0].Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsRemainAfterErrorIsNotFixed()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Contract.ThrowIfNull(results);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            buffer.Insert(0, " ");
            await InsertTextAsync(testLspServer, document, position: 0, text: " ");

            results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI(),
                previousResultId: results[0].ResultId);
            Contract.ThrowIfNull(results);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics.Single().Range.Start);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsAreNotMapped()
        {
            var markup =
@"#line 1 ""test.txt""
class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
            Assert.Equal(1, results.Single().Diagnostics.Single().Range.Start.Line);
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

        private static Task OpenDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.OpenDocumentAsync(document.GetURI());

        private static Task CloseDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.CloseDocumentAsync(document.GetURI());

        [Fact]
        public async Task TestStreamingDocumentDiagnostics()
        {
            var markup =
@"class A {";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var progress = BufferedProgress.Create<VSInternalDiagnosticReport>(null);
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()!.Single().Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesUsesActiveContext()
        {
            var documentText =
@"#if ONE
class A {
#endif
class B {";
            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""C:\C.cs"">{documentText}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""CSProj1"">{documentText}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.OpenFilesAndProjects);

            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Open either of the documents via LSP, we're tracking the URI and text.
            await OpenDocumentAsync(testLspServer, csproj1Document);

            // This opens all documents in the workspace and ensures buffers are created.
            testLspServer.TestWorkspace.GetTestDocument(csproj1Document.Id).GetTextBuffer();

            // Set CSProj2 as the active context and get diagnostics.
            testLspServer.TestWorkspace.SetDocumentContext(csproj2Document.Id);
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj2Document.GetURI());
            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
            var vsDiagnostic = (LSP.VSDiagnostic)results.Single().Diagnostics.Single();
            Assert.Equal("CSProj2", vsDiagnostic.Projects.Single().ProjectName);

            // Set CSProj1 as the active context and get diagnostics.
            testLspServer.TestWorkspace.SetDocumentContext(csproj1Document.Id);
            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI());
            Assert.Equal(2, results.Single().Diagnostics!.Length);
            Assert.All(results.Single().Diagnostics, d => Assert.Equal("CS1513", d.Code));
            Assert.All(results.Single().Diagnostics, d => Assert.Equal("CSProj1", ((VSDiagnostic)d).Projects.Single().ProjectName));
        }

        [Fact]
        public async Task TestDocumentDiagnosticsWithChangeInReferencedProject()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        <ProjectReference>CSProj2</ProjectReference>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
            await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

            // Verify we a diagnostic in A.cs since B does not exist.
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI());
            Assert.Single(results);
            Assert.Equal("CS0246", results.Single().Diagnostics.Single().Code);

            // Insert B into B.cs and verify that the error in A.cs is now gone.
            var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
            await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
            var originalResultId = results.Single().ResultId;
            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), originalResultId);
            Assert.Single(results);
            Assert.Empty(results.Single().Diagnostics);
            Assert.NotEqual(originalResultId, results.Single().ResultId);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsWithChangeInNotReferencedProject()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
            await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

            // Verify we get a diagnostic in A since the class B does not exist.
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI());
            Assert.Single(results);
            Assert.Equal("CS0246", results.Single().Diagnostics.Single().Code);

            // Add B to CSProj2 and verify that we get an unchanged result (still has diagnostic) for A.cs
            // since CSProj1 does not reference CSProj2
            var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
            await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
            var originalResultId = results.Single().ResultId;
            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), originalResultId);
            Assert.Single(results);
            Assert.Null(results.Single().Diagnostics);
            Assert.Equal(originalResultId, results.Single().ResultId);
        }

        #endregion

        #region Workspace Diagnostics

        [Fact]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOff()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.OpenFilesAndProjects);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsForClosedFilesWithFSAOn()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);
        }

        [Fact]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOnAndInPushMode()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, pullDiagnostics: false);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Empty(results[0].Diagnostics);
            Assert.Empty(results[1].Diagnostics);
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

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.True(results.All(r => r.TextDocument!.Uri.OriginalString == "C:\\C.cs"));
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsForRemovedDocument()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            testLspServer.TestWorkspace.OnDocumentRemoved(testLspServer.TestWorkspace.Documents.First().Id);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
            Contract.ThrowIfNull(results2);

            // First doc should show up as removed.
            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Diagnostics);
            Assert.Null(results2[0].ResultId);

            // Second doc should be changed as the project has changed.
            Assert.Empty(results[1].Diagnostics);
            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        }

        private static VSInternalDiagnosticParams[] CreateDiagnosticParamsFromPreviousReports(VSInternalWorkspaceDiagnosticReport[] results)
        {
            return results.Select(r => new VSInternalDiagnosticParams { TextDocument = r.TextDocument, PreviousResultId = r.ResultId }).ToArray();
        }

        [Fact]
        public async Task TestNoChangeIfWorkspaceDiagnosticsCalledTwice()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
            Contract.ThrowIfNull(results2);

            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Diagnostics);
            Assert.Null(results2[1].Diagnostics);

            Assert.Equal(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsRemovedAfterErrorIsFixed()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
            Contract.ThrowIfNull(results2);

            Assert.Equal(2, results2.Length);
            Assert.Empty(results2[0].Diagnostics);
            // Project has changed, so we re-computed diagnostics as changes in the first file
            // may have changed results in the second.
            Assert.Empty(results2[1].Diagnostics);

            Assert.NotEqual(results[0].ResultId, results2[0].ResultId);
            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsRemainAfterErrorIsNotFixed()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            Assert.Empty(results[1].Diagnostics);

            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
            buffer.Insert(0, " ");

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.First();
            var text = await document.GetTextAsync();

            // Hacky, but we need to close the document manually since editing the text-buffer will open it in the
            // test-workspace.
            testLspServer.TestWorkspace.OnDocumentClosed(
                document.Id, TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results2);

            Assert.Equal("CS1513", results2[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 10 }, results2[0].Diagnostics.Single().Range.Start);

            Assert.Empty(results2[1].Diagnostics);
            Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestStreamingWorkspaceDiagnostics()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            var progress = BufferedProgress.Create<VSInternalDiagnosticReport>(null);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()![0].Diagnostics![0].Code);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsAreNotMapped()
        {
            var markup1 =
@"#line 1 ""test.txt""
class A {";
            var markup2 = "";
            using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            Contract.ThrowIfNull(results);
            Assert.Equal(2, results.Length);
            Assert.Equal(new Uri("C:/test1.cs"), results[0].TextDocument!.Uri);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(1, results[0].Diagnostics.Single().Range.Start.Line);
            Assert.Empty(results[1].Diagnostics);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsWithChangeInReferencedProject()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        <ProjectReference>CSProj2</ProjectReference>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Verify we a diagnostic in A.cs since B does not exist
            // and a diagnostic in B.cs since it is missing the class name.
            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

            // Insert B into B.cs via the workspace.
            var caretLocation = testLspServer.GetLocations("caret").First().Range;
            var csproj2DocumentText = await csproj2Document.GetTextAsync();
            var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
            await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

            // Get updated workspace diagnostics for the change.
            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);

            // Verify diagnostics for A.cs are updated as the type B now exists.
            Assert.Empty(results[0].Diagnostics);
            Assert.NotEqual(previousResultIds[0].PreviousResultId, results[0].ResultId);

            // Verify diagnostics for B.cs are updated as the class definition is now correct.
            Assert.Empty(results[1].Diagnostics);
            Assert.NotEqual(previousResultIds[1].PreviousResultId, results[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsWithChangeInNotReferencedProject()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Verify we a diagnostic in A.cs since B does not exist
            // and a diagnostic in B.cs since it is missing the class name.
            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

            // Insert B into B.cs via the workspace.
            var caretLocation = testLspServer.GetLocations("caret").First().Range;
            var csproj2DocumentText = await csproj2Document.GetTextAsync();
            var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
            await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

            // Get updated workspace diagnostics for the change.
            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResultIds);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);

            // Verify the diagnostic result for A.cs is unchanged as A.cs does not reference CSProj2.
            Assert.Null(results[0].Diagnostics);
            Assert.Equal(previousResultIds[0].PreviousResultId, results[0].ResultId);

            // Verify that the diagnostics result for B.cs reflects the change we made to it.
            Assert.Empty(results[1].Diagnostics);
            Assert.NotEqual(previousResultIds[1].PreviousResultId, results[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedAndChanged()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        <ProjectReference>CSProj2</ProjectReference>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Verify we a diagnostic in A.cs since B does not exist
            // and a diagnostic in B.cs since it is missing the class name.
            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

            // Change and reload the project via the workspace.
            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
            projectInfo = projectInfo.WithCompilationOptions(projectInfo.CompilationOptions!.WithPlatform(Platform.X64));
            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

            // Get updated workspace diagnostics for the change.
            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);

            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);

            // The diagnostics should have been recalculated for both projects as a referenced project changed.
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedUnChanged()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";
            var markup2 =
@"namespace M
{
    public class {|caret:|} { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        <ProjectReference>CSProj2</ProjectReference>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs"">{markup2}</Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Verify we a diagnostic in A.cs since B does not exist
            // and a diagnostic in B.cs since it is missing the class name.
            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);
            Assert.Equal("CS1001", results[1].Diagnostics.Single().Code);

            // Reload the project via the workspace.
            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

            // Get updated workspace diagnostics for the change.
            var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResultIds);

            // Verify that since no actual changes have been made we report unchanged diagnostics.
            AssertEx.NotNull(results);
            Assert.Equal(2, results.Length);

            // Diagnostics should be unchanged as the referenced project was only unloaded / reloaded, but did not actually change.
            Assert.Null(results[0].Diagnostics);
            Assert.Equal(previousResultIds[0].PreviousResultId, results[0].ResultId);
            Assert.Null(results[1].Diagnostics);
            Assert.Equal(previousResultIds[1].PreviousResultId, results[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsOrderOfReferencedProjectsReloadedDoesNotMatter()
        {
            var markup1 =
@"namespace M
{
    class A : B { }
}";

            var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\A.cs"">{markup1}</Document>
        <ProjectReference>CSProj2</ProjectReference>
        <ProjectReference>CSProj3</ProjectReference>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"">
        <Document FilePath=""C:\B.cs""></Document>
    </Project>
<Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj3"">
        <Document FilePath=""C:\C.cs""></Document>
    </Project>
</Workspace>";

            using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, BackgroundAnalysisScope.FullSolution).ConfigureAwait(false);
            var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

            // Verify we a diagnostic in A.cs since B does not exist
            // and a diagnostic in B.cs since it is missing the class name.
            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);
            AssertEx.NotNull(results);
            Assert.Equal(3, results.Length);
            Assert.Equal("CS0246", results[0].Diagnostics.Single().Code);

            // Reload the project via the workspace.
            var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
            testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
            var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

            // Get updated workspace diagnostics for the change.
            var previousResults = CreateDiagnosticParamsFromPreviousReports(results);
            var previousResultIds = previousResults.Select(param => param.PreviousResultId).ToImmutableArray();
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: previousResults);

            // Verify that since no actual changes have been made we report unchanged diagnostics.
            AssertEx.NotNull(results);
            Assert.Equal(3, results.Length);

            // Diagnostics should be unchanged as a referenced project was unloaded and reloaded.  Order should not matter.
            Assert.Null(results[0].Diagnostics);
            Assert.All(results, result => Assert.Null(result.Diagnostics));
            Assert.All(results, result => Assert.True(previousResultIds.Contains(result.ResultId)));
        }

        #endregion

        private static async Task<VSInternalDiagnosticReport[]?> RunGetDocumentPullDiagnosticsAsync(
            TestLspServer testLspServer,
            Uri uri,
            string? previousResultId = null,
            IProgress<VSInternalDiagnosticReport[]>? progress = null)
        {
            await WaitForDiagnosticsAsync(testLspServer.TestWorkspace);

            return await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(
                VSInternalMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(uri, previousResultId, progress),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);
        }

        private static async Task<VSInternalWorkspaceDiagnosticReport[]?> RunGetWorkspacePullDiagnosticsAsync(
            TestLspServer testLspServer,
            VSInternalDiagnosticParams[]? previousResults = null,
            IProgress<VSInternalWorkspaceDiagnosticReport[]>? progress = null)
        {
            await WaitForDiagnosticsAsync(testLspServer.TestWorkspace);

            return await testLspServer.ExecuteRequestAsync<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]>(
                VSInternalMethods.WorkspacePullDiagnosticName,
                CreateWorkspaceDiagnosticParams(previousResults, progress),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);
        }

        private static async Task WaitForDiagnosticsAsync(TestWorkspace workspace)
        {
            var listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            await listenerProvider.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.SolutionCrawler).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
        }

        private static VSInternalDocumentDiagnosticsParams CreateDocumentDiagnosticParams(
            Uri uri,
            string? previousResultId = null,
            IProgress<VSInternalDiagnosticReport[]>? progress = null)
        {
            return new VSInternalDocumentDiagnosticsParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                PreviousResultId = previousResultId,
                PartialResultToken = progress,
            };
        }

        private static VSInternalWorkspaceDiagnosticsParams CreateWorkspaceDiagnosticParams(
            VSInternalDiagnosticParams[]? previousResults = null,
            IProgress<VSInternalWorkspaceDiagnosticReport[]>? progress = null)
        {
            return new VSInternalWorkspaceDiagnosticsParams
            {
                PreviousResults = previousResults,
                PartialResultToken = progress,
            };
        }

        private Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string markup, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
            => CreateTestWorkspaceWithDiagnosticsAsync(markup, scope, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);

        private async Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string markup, BackgroundAnalysisScope scope, DiagnosticMode mode)
        {
            var testLspServer = await CreateTestLspServerAsync(markup);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, mode);
            return testLspServer;
        }

        private async Task<TestLspServer> CreateTestWorkspaceFromXmlAsync(string xmlMarkup, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var testLspServer = await CreateXmlTestLspServerAsync(xmlMarkup);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);
            return testLspServer;
        }

        private async Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string[] markups, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var testLspServer = await CreateTestLspServerAsync(markups);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);
            return testLspServer;
        }

        private static void InitializeDiagnostics(BackgroundAnalysisScope scope, TestWorkspace workspace, DiagnosticMode diagnosticMode)
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.Options
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope)
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope)
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript, scope)
                    .WithChangedOption(InternalDiagnosticsOptions.NormalDiagnosticMode, diagnosticMode)));

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap().Add(
                InternalLanguageNames.TypeScript, ImmutableArray.Create<DiagnosticAnalyzer>(new MockTypescriptDiagnosticAnalyzer())));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));
        }

        protected override TestComposition Composition => base.Composition.AddParts(typeof(MockTypescriptDiagnosticAnalyzer));

        [DiagnosticAnalyzer(InternalLanguageNames.TypeScript), PartNotDiscoverable]
        private class MockTypescriptDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            "TS01", "TS error", "TS error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.FromResult(ImmutableArray.Create(
                    Diagnostic.Create(Descriptor, Location.Create(document.FilePath!, default, default))));
            }
        }
    }
}
