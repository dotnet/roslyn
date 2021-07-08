﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestNoDocumentDiagnosticsForOpenFilesWithFSAOffIfInPushMode()
        {
            var markup =
@"class A {";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects, pullDiagnostics: false);

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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects, DiagnosticMode.Default);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();
            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            await OpenDocumentAsync(testLspServer, document);

            // Ensure we get no diagnostics when feature flag is off.
            var testExperimentationService = (TestExperimentationService)testLspServer.TestWorkspace.Services.GetRequiredService<IExperimentationService>();
            testExperimentationService.SetExperimentOption(WellKnownExperimentNames.LspPullDiagnosticsFeatureFlag, false);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Assert.Empty(results.Single().Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesIfDefaultAndFeatureFlagOn()
        {
            var markup =
@"class A {";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects, DiagnosticMode.Default);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();
            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            await OpenDocumentAsync(testLspServer, document);

            var testExperimentationService = (TestExperimentationService)testLspServer.TestWorkspace.Services.GetRequiredService<IExperimentationService>();
            testExperimentationService.SetExperimentOption(WellKnownExperimentNames.LspPullDiagnosticsFeatureFlag, true);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());
            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForRemovedDocument()
        {
            var markup =
@"class A {";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);
            var workspace = testLspServer.TestWorkspace;

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            // Get the diagnostics for the solution containing the doc.
            var solution = document.Project.Solution;

            await OpenDocumentAsync(testLspServer, document);

            await WaitForDiagnosticsAsync(workspace);
            var results = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                MSLSPMethods.DocumentPullDiagnosticName,
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
            results = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                MSLSPMethods.DocumentPullDiagnosticName,
                new DocumentDiagnosticsParams { PreviousResultId = results.Single().ResultId, TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document) },
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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);

            await InsertTextAsync(testLspServer, document, buffer.CurrentSnapshot.Length, "}");

            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Empty(results[0].Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsRemainAfterErrorIsNotFixed()
        {
            var markup =
@"class A {";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);
            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI());

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            buffer.Insert(0, " ");
            await InsertTextAsync(testLspServer, document, position: 0, text: " ");

            results = await RunGetDocumentPullDiagnosticsAsync(
                testLspServer, document.GetURI(),
                previousResultId: results[0].ResultId);

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics.Single().Range.Start);
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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

            await OpenDocumentAsync(testLspServer, document);

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
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

            using var testLspServer = CreateTestWorkspaceFromXml(workspaceXml, BackgroundAnalysisScope.OpenFilesAndProjects);

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

        #endregion

        #region Workspace Diagnostics

        [Fact]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOff()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, pullDiagnostics: false);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Equal(2, results.Length);
            Assert.Empty(results[0].Diagnostics);
            Assert.Empty(results[1].Diagnostics);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsForRemovedDocument()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            testLspServer.TestWorkspace.OnDocumentRemoved(testLspServer.TestWorkspace.Documents.First().Id);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

            // First doc should show up as removed.
            Assert.Equal(2, results2.Length);
            Assert.Null(results2[0].Diagnostics);
            Assert.Null(results2[0].ResultId);

            // Second doc should show up as unchanged.
            Assert.Null(results2[1].Diagnostics);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        private static DiagnosticParams[] CreateDiagnosticParamsFromPreviousReports(WorkspaceDiagnosticReport[] results)
        {
            return results.Select(r => new DiagnosticParams { TextDocument = r.TextDocument, PreviousResultId = r.ResultId }).ToArray();
        }

        [Fact]
        public async Task TestNoChangeIfWorkspaceDiagnosticsCalledTwice()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var buffer = testLspServer.TestWorkspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

            Assert.Equal(2, results2.Length);
            Assert.Empty(results2[0].Diagnostics);
            Assert.Null(results2[1].Diagnostics);

            Assert.NotEqual(results[0].ResultId, results2[0].ResultId);
            Assert.Equal(results[1].ResultId, results2[1].ResultId);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsRemainAfterErrorIsNotFixed()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

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
            using var testLspServer = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
            results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()![0].Diagnostics![0].Code);
        }

        #endregion

        private static async Task<DiagnosticReport[]> RunGetDocumentPullDiagnosticsAsync(
            TestLspServer testLspServer,
            Uri uri,
            string? previousResultId = null,
            IProgress<DiagnosticReport[]>? progress = null)
        {
            await WaitForDiagnosticsAsync(testLspServer.TestWorkspace);

            var result = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                MSLSPMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(uri, previousResultId, progress),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            return result;
        }

        private static async Task<WorkspaceDiagnosticReport[]> RunGetWorkspacePullDiagnosticsAsync(
            TestLspServer testLspServer,
            DiagnosticParams[]? previousResults = null,
            IProgress<WorkspaceDiagnosticReport[]>? progress = null)
        {
            await WaitForDiagnosticsAsync(testLspServer.TestWorkspace);

            var result = await testLspServer.ExecuteRequestAsync<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport[]>(
                MSLSPMethods.WorkspacePullDiagnosticName,
                CreateWorkspaceDiagnosticParams(previousResults, progress),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            return result;
        }

        private static async Task WaitForDiagnosticsAsync(TestWorkspace workspace)
        {
            var listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            await listenerProvider.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.SolutionCrawler).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
        }

        private static DocumentDiagnosticsParams CreateDocumentDiagnosticParams(
            Uri uri,
            string? previousResultId = null,
            IProgress<DiagnosticReport[]>? progress = null)
        {
            return new DocumentDiagnosticsParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                PreviousResultId = previousResultId,
                PartialResultToken = progress,
            };
        }

        private static WorkspaceDocumentDiagnosticsParams CreateWorkspaceDiagnosticParams(
            DiagnosticParams[]? previousResults = null,
            IProgress<WorkspaceDiagnosticReport[]>? progress = null)
        {
            return new WorkspaceDocumentDiagnosticsParams
            {
                PreviousResults = previousResults,
                PartialResultToken = progress,
            };
        }

        private TestLspServer CreateTestWorkspaceWithDiagnostics(string markup, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
            => CreateTestWorkspaceWithDiagnostics(markup, scope, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);

        private TestLspServer CreateTestWorkspaceWithDiagnostics(string markup, BackgroundAnalysisScope scope, DiagnosticMode mode)
        {
            var testLspServer = CreateTestLspServer(markup, out _);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, mode);
            return testLspServer;
        }

        private TestLspServer CreateTestWorkspaceFromXml(string xmlMarkup, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var testLspServer = CreateXmlTestLspServer(xmlMarkup, out _);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);
            return testLspServer;
        }

        private TestLspServer CreateTestWorkspaceWithDiagnostics(string[] markups, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var testLspServer = CreateTestLspServer(markups, out _);
            InitializeDiagnostics(scope, testLspServer.TestWorkspace, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push);
            return testLspServer;
        }

        private static void InitializeDiagnostics(BackgroundAnalysisScope scope, TestWorkspace workspace, DiagnosticMode diagnosticMode)
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.Options
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope)
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope)
                    .WithChangedOption(InternalDiagnosticsOptions.NormalDiagnosticMode, diagnosticMode)));

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));
        }
    }
}
