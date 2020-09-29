// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    public class PullDiagnosticTests : AbstractLanguageServerProtocolTests
    {
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

        private void VerifyContent(LSP.VSHover result, string expectedContent)
        {
            var containerElement = (ContainerElement)result.RawContent;
            using var _ = ArrayBuilder<ClassifiedTextElement>.GetInstance(out var classifiedTextElements);
            GetClassifiedTextElements(containerElement, classifiedTextElements);
            Assert.False(classifiedTextElements.SelectMany(classifiedTextElements => classifiedTextElements.Runs).Any(run => run.NavigationAction != null));
            var content = string.Join("|", classifiedTextElements.Select(cte => string.Join(string.Empty, cte.Runs.Select(ctr => ctr.Text))));
            Assert.Equal(expectedContent, content);
        }

        private void GetClassifiedTextElements(ContainerElement container, ArrayBuilder<ClassifiedTextElement> classifiedTextElements)
        {
            foreach (var element in container.Elements)
            {
                if (element is ClassifiedTextElement classifiedTextElement)
                {
                    classifiedTextElements.Add(classifiedTextElement);
                }
                else if (element is ContainerElement containerElement)
                {
                    GetClassifiedTextElements(containerElement, classifiedTextElements);
                }
            }
        }

        [Fact]
        public async Task TestGetDocumentPullDiagnostics1()
        {
            var markup =
@"class A";
            using var workspace = CreateTestWorkspaceWithDiagnostics(markup);

            var results = await RunGetDocumentPullDiagnosticsAsync(
                workspace, workspace.CurrentSolution.Projects.Single().Documents.Single());

            Console.WriteLine(results);
        }

        private TestWorkspace CreateTestWorkspaceWithDiagnostics(string markup)
        {
            var workspace = CreateTestWorkspace(markup, out _);

            //var threadingContext = workspace.GetService<IThreadingContext>();
            //var listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            var analyzerReference = new TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var registrationService = (SolutionCrawlerRegistrationService)workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registrationService.EnsureRegistration(workspace, initializeLazily: false);

            //Dim analyzerService = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.ExportProvider.GetExportedValue(Of IDiagnosticAnalyzerService)())

            //Dim service = DirectCast(workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)(), SolutionCrawlerRegistrationService)
            //service.Register(workspace)

            if (!registrationService.GetTestAccessor().TryGetWorkCoordinator(workspace, out _))
                throw new InvalidOperationException();

            var analyzerService = (DiagnosticAnalyzerService)registrationService.GetTestAccessor().AnalyzerProviders.SelectMany(pair => pair.Value).SingleOrDefault(lazyProvider => lazyProvider.Metadata.Name == WellKnownSolutionCrawlerAnalyzers.Diagnostic && lazyProvider.Metadata.HighPriorityForActiveFile)?.Value!;
            var diagnosticService = (DiagnosticService)workspace.ExportProvider.GetExportedValue<IDiagnosticService>();
            diagnosticService.Register(new TestHostDiagnosticUpdateSource(workspace));

            registrationService.GetTestAccessor().WaitUntilCompletion(workspace, ImmutableArray.Create<IIncrementalAnalyzer>(analyzerService.CreateIncrementalAnalyzer(workspace)));

            return workspace;
        }
    }
}
