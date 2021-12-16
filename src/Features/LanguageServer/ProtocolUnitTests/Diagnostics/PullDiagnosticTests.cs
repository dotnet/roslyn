// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var results = await RunGetDocumentPullDiagnosticsAsync(workspace, queue, server, document);

            Assert.Empty(results);
        }

        [Fact]
        public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, workspace.CurrentSolution.Projects.Single().Documents.Single());

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, document);

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

            await OpenDocumentAsync(queue, server, document);

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
            await CloseDocumentAsync(queue, server, document);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, document);

            Assert.Equal("CS1513", results.Single().Diagnostics.Single().Code);

            var resultId = results.Single().ResultId;
            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, workspace.CurrentSolution.Projects.Single().Documents.Single(), previousResultId: resultId);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, document);

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);

            await InsertTextAsync(queue, server, document, buffer.CurrentSnapshot.Length, "}");

            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, workspace.CurrentSolution.Projects.Single().Documents.Single());

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);
            var results = await RunGetDocumentPullDiagnosticsAsync(workspace, queue, server, document);

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            buffer.Insert(0, " ");
            await InsertTextAsync(queue, server, document, position: 0, text: " ");

            results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, workspace.CurrentSolution.Projects.Single().Documents.Single(),
                previousResultId: results[0].ResultId);

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics.Single().Range.Start);
        }

        private static async Task InsertTextAsync(
            RequestExecutionQueue queue,
            LanguageServerProtocol server,
            Document document,
            int position,
            string text)
        {
            var sourceText = await document.GetTextAsync();
            var lineInfo = sourceText.Lines.GetLinePositionSpan(new TextSpan(position, 0));

            await server.ExecuteRequestAsync<DidChangeTextDocumentParams, object>(
                queue,
                Methods.TextDocumentDidChangeName,
                new DidChangeTextDocumentParams
                {
                    TextDocument = ProtocolConversions.DocumentToVersionedTextDocumentIdentifier(document),
                    ContentChanges = new TextDocumentContentChangeEvent[]
                    {
                        new TextDocumentContentChangeEvent
                        {
                            Range = new LSP.Range
                            {
                                Start = ProtocolConversions.LinePositionToPosition(lineInfo.Start),
                                End  =ProtocolConversions.LinePositionToPosition(lineInfo.End),
                            },
                            Text = text,
                        },
                    },
                },
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);
        }

        private static async Task OpenDocumentAsync(RequestExecutionQueue queue, LanguageServerProtocol server, Document document)
        {
            await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(
                queue,
                Methods.TextDocumentDidOpenName,
                new DidOpenTextDocumentParams
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = document.GetURI(),
                        Text = document.GetTextSynchronously(CancellationToken.None).ToString(),
                    }
                },
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);
        }

        private static async Task CloseDocumentAsync(RequestExecutionQueue queue, LanguageServerProtocol server, Document document)
        {
            await server.ExecuteRequestAsync<DidCloseTextDocumentParams, object>(
                queue,
                Methods.TextDocumentDidCloseName,
                new DidCloseTextDocumentParams
                {
                    TextDocument = new TextDocumentIdentifier
                    {
                        Uri = document.GetURI(),
                    }
                },
                new LSP.ClientCapabilities(),
                clientName: null,
                CancellationToken.None);
        }

        [Fact]
        public async Task TestStreamingDocumentDiagnostics()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            await OpenDocumentAsync(queue, server, document);

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, queue, server, workspace.CurrentSolution.Projects.Single().Documents.Single(), progress: progress);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            workspace.OnDocumentRemoved(workspace.Documents.First().Id);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, queue, server, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, queue, server, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Empty(results[1].Diagnostics);

            var buffer = workspace.Documents.First().GetTextBuffer();
            buffer.Insert(buffer.CurrentSnapshot.Length, "}");

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(
                workspace, queue, server, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            Assert.Empty(results[1].Diagnostics);

            var buffer = workspace.Documents.First().GetTextBuffer();
            buffer.Insert(0, " ");

            var document = workspace.CurrentSolution.Projects.Single().Documents.First();
            var text = await document.GetTextAsync();

            // Hacky, but we need to close the document manually since editing the text-buffer will open it in the
            // test-workspace.
            workspace.OnDocumentClosed(
                document.Id, TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));

            var results2 = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

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

            var queue = CreateRequestQueue(workspace.CurrentSolution);
            var server = GetLanguageServer(workspace.CurrentSolution);

            var results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server);

            Assert.Equal(2, results.Length);
            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
            Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics.Single().Range.Start);

            var progress = BufferedProgress.Create<DiagnosticReport>(null);
            results = await RunGetWorkspacePullDiagnosticsAsync(workspace, queue, server, progress: progress);

            Assert.Null(results);
            Assert.Equal("CS1513", progress.GetValues()![0].Diagnostics![0].Code);
        }

        #endregion

        private static async Task<DiagnosticReport[]> RunGetDocumentPullDiagnosticsAsync(
            TestWorkspace workspace,
            RequestExecutionQueue queue,
            LanguageServerProtocol server,
            Document document,
            string? previousResultId = null,
            IProgress<DiagnosticReport[]>? progress = null)
        {
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
            RequestExecutionQueue queue,
            LanguageServerProtocol server,
            DiagnosticParams[]? previousResults = null,
            IProgress<WorkspaceDiagnosticReport[]>? progress = null)
        {
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
                    .WithChangedOption(InternalDiagnosticsOptions.NormalDiagnosticMode, pullDiagnostics ? DiagnosticMode.Pull : DiagnosticMode.Push)));

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));
        }
    }
}
