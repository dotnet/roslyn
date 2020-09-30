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
        public async Task TestDocumentDiagnosticsForRemovedDocument()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var results = await RunGetDocumentPullDiagnosticsAsync(workspace, document);

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
            UpdateSolutionProvider(workspace, workspace.CurrentSolution);

            await WaitForDiagnosticsAsync(workspace);

            var solution = workspace.CurrentSolution;
            var queue = CreateRequestQueue(solution);
            var server = GetLanguageServer(solution);

            await WaitForDiagnosticsAsync(workspace);

            results = await server.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                queue,
                MSLSPMethods.DocumentPullDiagnosticName,
                new DocumentDiagnosticsParams { TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document) },
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

            var progress = BufferedProgress.Create<DiagnosticReport[]>(null);
            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single(), progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()!.Single().Single().Diagnostics.Single().Code);
        }

        #endregion

        #region Workspace Diagnostics

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

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string markup, BackgroundAnalysisScope scope)
        {
            var workspace = CreateTestWorkspace(markup, out _);
            InitializeDiagnostics(scope, workspace);
            return workspace;
        }

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string[] markups, BackgroundAnalysisScope scope)
        {
            var workspace = CreateTestWorkspace(markups, out _);
            InitializeDiagnostics(scope, workspace);
            return workspace;
        }

        private static void InitializeDiagnostics(BackgroundAnalysisScope scope, TestWorkspace workspace)
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.Options
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope)
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope)));

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));
        }
    }
}
