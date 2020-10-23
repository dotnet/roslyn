// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
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
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);
            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Empty(results.Single().Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);
        }

        [Fact]
        public async Task TestNoDocumentDiagnosticsForOpenFilesWithFSAOffIfInPushMode()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects, pullDiagnostics: false);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Empty(results.Single().Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForRemovedDocument()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Get the diagnostics for the solution containing the doc.
            var solution = document.Project.Solution;
            var queue = CreateRequestQueue(solution);
            var server = GetLanguageServer(solution);

            await WaitForDiagnosticsAsync(workspace);
            var results = await server.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                queue,
                MSLSPMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(document),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            // Now remove the doc.
            workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
            UpdateSolutionProvider(workspace, workspace.CurrentSolution);

            // And get diagnostic again, using the same doc-id as before.
            await WaitForDiagnosticsAsync(workspace);
            results = await server.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                queue,
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
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            var resultId = results.Single().ResultId;
            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single(),
                previousResultId: resultId);

            Assert.Null(results.Single().Diagnostics);
            Assert.Equal(resultId, results.Single().ResultId);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsRemovedAfterErrorIsFixed()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);

            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Empty(results[0].Diagnostics);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsRemainAfterErrorIsNotFixed()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            var buffer = workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            buffer.Insert(0, " ");

            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single(),
                previousResultId: results[0].ResultId);

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics.Single().Range.Start);
        }

        [Fact]
        public async Task TestStreamingDocumentDiagnostics()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single(), progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()!.Single().Diagnostics.Single().Code);
        }

        #endregion

        #region Workspace Diagnostics

        [Fact]
        public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOff()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.OpenFilesAndProjects);
            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestWorkspaceDiagnosticsForClosedFilesWithFSAOn()
        {
            var markup1 =
@"class A {";
            var markup2 = "";
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);
            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution, pullDiagnostics: false);
            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            workspace.OnDocumentRemoved(workspace.Documents.First().Id);
            UpdateSolutionProvider(workspace, workspace.CurrentSolution);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var buffer = workspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            Assert.Empty(results[1].Diagnostics);

            var buffer = workspace.Documents.First().GetTextBuffer();
            buffer.Insert(0, " ");

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(workspace);

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(
                 new[] { markup1, markup2 }, BackgroundAnalysisScope.FullSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
            results = await RunGetWorkspacePullDiagnosticsAsync(workspace, progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()![0].Diagnostics![0].Code);
        }

        #endregion

        private static async Task<DiagnosticReport[]> RunGetDocumentPullDiagnosticsAsync(
            TestWorkspace workspace, Document document,
            string? previousResultId = null,
            IProgress<DiagnosticReport[]>? progress = null)
        {
            var solution = document.Project.Solution;
            var queue = CreateRequestQueue(solution);
            var server = GetLanguageServer(solution);

            await WaitForDiagnosticsAsync(workspace);

            var result = await server.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                queue,
                MSLSPMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(document, previousResultId, progress),
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);

            return result;
        }

        private static async Task<WorkspaceDiagnosticReport[]> RunGetWorkspacePullDiagnosticsAsync(
            TestWorkspace workspace,
            DiagnosticParams[]? previousResults = null,
            IProgress<WorkspaceDiagnosticReport[]>? progress = null)
        {
            var solution = workspace.CurrentSolution;
            var queue = CreateRequestQueue(solution);
            var server = GetLanguageServer(solution);

            await WaitForDiagnosticsAsync(workspace);

            var result = await server.ExecuteRequestAsync<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport[]>(
                queue,
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
            Document document,
            string? previousResultId = null,
            IProgress<DiagnosticReport[]>? progress = null)
        {
            return new DocumentDiagnosticsParams
            {
                TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document),
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

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string markup, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var workspace = CreateTestWorkspace(markup, out _);
            InitializeDiagnostics(scope, workspace, pullDiagnostics);
            return workspace;
        }

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string[] markups, BackgroundAnalysisScope scope, bool pullDiagnostics = true)
        {
            var workspace = CreateTestWorkspace(markups, out _);
            InitializeDiagnostics(scope, workspace, pullDiagnostics);
            return workspace;
        }

        private static void InitializeDiagnostics(BackgroundAnalysisScope scope, TestWorkspace workspace, bool pullDiagnostics)
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.Options
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope)
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope)
                    .WithChangedOption(InternalDiagnosticsOptions.LspPullDiagnostics, pullDiagnostics)));

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));
        }
    }
}
