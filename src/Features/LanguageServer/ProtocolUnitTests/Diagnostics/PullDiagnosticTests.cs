// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    public class PullDiagnosticTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestNoDiagnosticsForClosedFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Empty(results[0].Diagnostics);
        }

        [Fact]
        public async Task TestDiagnosticsForOpenFilesWithFSAOff()
        {
            var markup =
@"class A {";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup, BackgroundAnalysisScope.OpenFilesAndProjects);

            // Calling GetTextBuffer will effectively open the file.
            workspace.Documents.Single().GetTextBuffer();

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Assert.Equal("CS1513", results[0].Diagnostics.Single().Code);
        }

        private static async Task<DiagnosticReport[]> RunGetDocumentPullDiagnosticsAsync(TestWorkspace workspace, Document document)
        {
            var solution = document.Project.Solution;
            var queue = CreateRequestQueue(solution);
            var server = GetLanguageServer(solution);

            await WaitForDiagnosticsAsync(workspace);

            var result = await server.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(
                queue,
                MSLSPMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(document),
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

        private static DocumentDiagnosticsParams CreateDocumentDiagnosticParams(Document document, string? previousResultId = null)
        {
            return new DocumentDiagnosticsParams
            {
                TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document),
                PreviousResultId = previousResultId,
            };
        }

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string markup, BackgroundAnalysisScope scope)
        {
            var workspace = CreateTestWorkspace(markup, out _);

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

            return workspace;
        }
    }
}
