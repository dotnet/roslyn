// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    [UseExportProvider]
    [Trait(Traits.Editor, Traits.Editors.Preview), Trait(Traits.Feature, Traits.Features.Tagging)]
    public class PreviewWorkspaceTests
    {
        [Fact]
        public void TestPreviewCreationDefault()
        {
            using var previewWorkspace = new PreviewWorkspace();
            Assert.NotNull(previewWorkspace.CurrentSolution);
        }

        [Fact]
        public void TestPreviewCreationWithExplicitHostServices()
        {
            var hostServices = FeaturesTestCompositions.Features.GetHostServices();
            using var previewWorkspace = new PreviewWorkspace(hostServices);
            Assert.NotNull(previewWorkspace.CurrentSolution);
        }

        [Fact]
        public void TestPreviewCreationWithSolution()
        {
            using var custom = new AdhocWorkspace();
            using var previewWorkspace = new PreviewWorkspace(custom.CurrentSolution);
            Assert.NotNull(previewWorkspace.CurrentSolution);
        }

        [Fact]
        public void TestPreviewAddRemoveProject()
        {
            using var previewWorkspace = new PreviewWorkspace();
            var solution = previewWorkspace.CurrentSolution;
            var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
            Assert.True(previewWorkspace.TryApplyChanges(project.Solution));

            var newSolution = previewWorkspace.CurrentSolution.RemoveProject(project.Id);
            Assert.True(previewWorkspace.TryApplyChanges(newSolution));

            Assert.Equal(0, previewWorkspace.CurrentSolution.ProjectIds.Count);
        }

        [Fact]
        public void TestPreviewProjectChanges()
        {
            using var previewWorkspace = new PreviewWorkspace();
            var solution = previewWorkspace.CurrentSolution;
            var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
            Assert.True(previewWorkspace.TryApplyChanges(project.Solution));

            var addedSolution = previewWorkspace.CurrentSolution.Projects.First()
                                                .AddMetadataReference(TestMetadata.Net451.mscorlib)
                                                .AddDocument("document", "").Project.Solution;
            Assert.True(previewWorkspace.TryApplyChanges(addedSolution));
            Assert.Equal(1, previewWorkspace.CurrentSolution.Projects.First().MetadataReferences.Count);
            Assert.Equal(1, previewWorkspace.CurrentSolution.Projects.First().DocumentIds.Count);

            var text = "class C {}";
            var changedSolution = previewWorkspace.CurrentSolution.Projects.First().Documents.First().WithText(SourceText.From(text)).Project.Solution;
            Assert.True(previewWorkspace.TryApplyChanges(changedSolution));
            Assert.Equal(previewWorkspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync().Result.ToString(), text);

            var removedSolution = previewWorkspace.CurrentSolution.Projects.First()
                                                .RemoveMetadataReference(previewWorkspace.CurrentSolution.Projects.First().MetadataReferences[0])
                                                .RemoveDocument(previewWorkspace.CurrentSolution.Projects.First().DocumentIds[0]).Solution;

            Assert.True(previewWorkspace.TryApplyChanges(removedSolution));
            Assert.Equal(0, previewWorkspace.CurrentSolution.Projects.First().MetadataReferences.Count);
            Assert.Equal(0, previewWorkspace.CurrentSolution.Projects.First().DocumentIds.Count);
        }

        [WorkItem(923121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923121")]
        [WpfFact]
        public void TestPreviewOpenCloseFile()
        {
            using var previewWorkspace = new PreviewWorkspace();
            var solution = previewWorkspace.CurrentSolution;
            var project = solution.AddProject("project", "project.dll", LanguageNames.CSharp);
            var document = project.AddDocument("document", "");
            var sourceTextContainer = SourceText.From("Text").Container;

            Assert.True(previewWorkspace.TryApplyChanges(document.Project.Solution));

            previewWorkspace.OpenDocument(document.Id, sourceTextContainer);
            Assert.Equal(1, previewWorkspace.GetOpenDocumentIds().Count());
            Assert.True(previewWorkspace.IsDocumentOpen(document.Id));

            previewWorkspace.CloseDocument(document.Id);
            Assert.Equal(0, previewWorkspace.GetOpenDocumentIds().Count());
            Assert.False(previewWorkspace.IsDocumentOpen(document.Id));
        }

        [Fact]
        public async Task TestPreviewServices()
        {
            using var previewWorkspace = new PreviewWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices());
            var service = previewWorkspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            Assert.IsType<PreviewSolutionCrawlerRegistrationServiceFactory.Service>(service);

            var persistentService = previewWorkspace.Services.SolutionServices.GetPersistentStorageService();

            await using var storage = await persistentService.GetStorageAsync(SolutionKey.ToSolutionKey(previewWorkspace.CurrentSolution), CancellationToken.None);
            Assert.IsType<NoOpPersistentStorage>(storage);
        }

        [WorkItem(923196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923196")]
        [WpfFact]
        public async Task TestPreviewDiagnostic()
        {
            var hostServices = EditorTestCompositions.EditorFeatures.GetHostServices();
            var exportProvider = (IMefHostExportProvider)hostServices;

            var diagnosticService = (IDiagnosticUpdateSource)exportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            RoslynDebug.AssertNotNull(diagnosticService);
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var taskSource = new TaskCompletionSource<DiagnosticsUpdatedArgs>();
            diagnosticService.DiagnosticsUpdated += (s, a) => taskSource.TrySetResult(a);

            using var previewWorkspace = new PreviewWorkspace(hostServices);

            var solution = previewWorkspace.CurrentSolution
                .WithAnalyzerReferences(new[] { DiagnosticExtensions.GetCompilerDiagnosticAnalyzerReference(LanguageNames.CSharp) })
                .AddProject("project", "project.dll", LanguageNames.CSharp)
                .AddDocument("document", "class { }")
                .Project
                .Solution;

            Assert.True(previewWorkspace.TryApplyChanges(solution));

            var document = previewWorkspace.CurrentSolution.Projects.First().Documents.Single();

            previewWorkspace.OpenDocument(document.Id, (await document.GetTextAsync()).Container);
            previewWorkspace.EnableSolutionCrawler();

            // wait 20 seconds
            taskSource.Task.Wait(20000);
            Assert.True(taskSource.Task.IsCompleted);

            var args = taskSource.Task.Result;
            Assert.True(args.Diagnostics.Length > 0);
        }

        [WorkItem(28639, "https://github.com/dotnet/roslyn/issues/28639")]
        [ConditionalFact(typeof(Bitness32))]
        public void TestPreviewWorkspaceDoesNotLeakSolution()
        {
            // Verify that analyzer execution doesn't leak solution instances from the preview workspace.

            var previewWorkspace = new PreviewWorkspace();
            Assert.NotNull(previewWorkspace.CurrentSolution);
            var project = previewWorkspace.CurrentSolution.AddProject("project", "project.dll", LanguageNames.CSharp);
            Assert.True(previewWorkspace.TryApplyChanges(project.Solution));
            var solutionObjectReference = ObjectReference.Create(previewWorkspace.CurrentSolution);

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CommonDiagnosticAnalyzers.NotConfigurableDiagnosticAnalyzer());
            ExecuteAnalyzers(previewWorkspace, analyzers);

            previewWorkspace.Dispose();
            solutionObjectReference.AssertReleased();
        }

        private static void ExecuteAnalyzers(PreviewWorkspace previewWorkspace, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var analyzerOptions = new AnalyzerOptions(additionalFiles: ImmutableArray<AdditionalText>.Empty);
            var project = previewWorkspace.CurrentSolution.Projects.Single();
            var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(project.Services);
            var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(analyzerOptions, ideAnalyzerOptions);
            var compilationWithAnalyzersOptions = new CompilationWithAnalyzersOptions(workspaceAnalyzerOptions, onAnalyzerException: null, concurrentAnalysis: false, logAnalyzerExecutionTime: false);
            var compilation = project.GetRequiredCompilationAsync(CancellationToken.None).Result;
            var compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, compilationWithAnalyzersOptions);
            var result = compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).Result;
            Assert.Equal(1, result.CompilationDiagnostics.Count);
        }
    }
}
