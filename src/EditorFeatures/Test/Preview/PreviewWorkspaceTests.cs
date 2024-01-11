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
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923121")]
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
            var registrationService = Assert.IsType<SolutionCrawlerRegistrationService>(service);
            Assert.False(registrationService.Register(previewWorkspace));

            var persistentService = previewWorkspace.Services.SolutionServices.GetPersistentStorageService();

            await using var storage = await persistentService.GetStorageAsync(SolutionKey.ToSolutionKey(previewWorkspace.CurrentSolution), CancellationToken.None);
            Assert.IsType<NoOpPersistentStorage>(storage);
        }

        [WpfFact]
        public async Task TestPreviewDiagnosticTagger()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("class { }", composition: EditorTestCompositions.EditorFeatures);
            using var previewWorkspace = new PreviewWorkspace(workspace.CurrentSolution);

            // preview workspace and owner of the solution now share solution and its underlying text buffer
            var hostDocument = workspace.Projects.First().Documents.First();

            previewWorkspace.TryApplyChanges(previewWorkspace.CurrentSolution.WithAnalyzerReferences(new[] { DiagnosticExtensions.GetCompilerDiagnosticAnalyzerReference(LanguageNames.CSharp) }));

            // enable preview diagnostics
            previewWorkspace.EnableSolutionCrawler();

            var diagnosticsAndErrorsSpans = await SquiggleUtilities.GetDiagnosticsAndErrorSpansAsync<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace);
            const string AnalyzerCount = "Analyzer Count: ";
            Assert.Equal(AnalyzerCount + 1, AnalyzerCount + diagnosticsAndErrorsSpans.Item1.Length);

            const string SquigglesCount = "Squiggles Count: ";
            Assert.Equal(SquigglesCount + 1, SquigglesCount + diagnosticsAndErrorsSpans.Item2.Length);
        }

        [WpfFact]
        public async Task TestPreviewDiagnosticTaggerInPreviewPane()
        {
            // TODO: WPF required due to https://github.com/dotnet/roslyn/issues/46153
            using var workspace = TestWorkspace.CreateCSharp("class { }", composition: EditorTestCompositions.EditorFeaturesWpf);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { DiagnosticExtensions.GetCompilerDiagnosticAnalyzerReference(LanguageNames.CSharp) }));

            var hostDocument = workspace.Projects.First().Documents.First();

            // make a change to remove squiggle
            var oldDocument = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
            var oldText = oldDocument.GetTextAsync().Result;

            var newDocument = oldDocument.WithText(oldText.WithChanges(new TextChange(new TextSpan(0, oldText.Length), "class C { }")));

            // create a diff view
            WpfTestRunner.RequireWpfFact($"{nameof(TestPreviewDiagnosticTaggerInPreviewPane)} creates a {nameof(DifferenceViewerPreview)}");

            var previewFactoryService = (PreviewFactoryService)workspace.ExportProvider.GetExportedValue<IPreviewFactoryService>();
            using var diffView = await previewFactoryService.CreateChangedDocumentPreviewViewAsync(oldDocument, newDocument, CancellationToken.None);
            AssertEx.NotNull(diffView);

            var listenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();

            var provider = workspace.ExportProvider.GetExportedValues<ITaggerProvider>().OfType<DiagnosticsSquiggleTaggerProvider>().Single();

            // set up tagger for both buffers
            var leftBuffer = diffView.Viewer.LeftView.BufferGraph.GetTextBuffers(t => t.ContentType.IsOfType(ContentTypeNames.CSharpContentType)).First();
            var rightBuffer = diffView.Viewer.RightView.BufferGraph.GetTextBuffers(t => t.ContentType.IsOfType(ContentTypeNames.CSharpContentType)).First();

            var leftDocument = leftBuffer.GetRelatedDocuments().Single();
            var rightDocument = rightBuffer.GetRelatedDocuments().Single();

            // Diagnostic analyzer service, which provides pull capabilities (and not to be confused with
            // IDiagnosticService, which is push), doesn't normally register for test workspace.  So do it explicitly.
            var diagnosticAnalyzer = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticAnalyzer;
            incrementalAnalyzer.CreateIncrementalAnalyzer(leftDocument.Project.Solution.Workspace);
            incrementalAnalyzer.CreateIncrementalAnalyzer(rightDocument.Project.Solution.Workspace);

            var leftTagger = provider.CreateTagger<IErrorTag>(leftBuffer);
            var rightTagger = provider.CreateTagger<IErrorTag>(rightBuffer);
            Contract.ThrowIfNull(leftTagger);
            Contract.ThrowIfNull(rightTagger);

            using var leftDisposable = leftTagger as IDisposable;
            using var rightDisposable = rightTagger as IDisposable;

            // wait for diagnostics and taggers
            await listenerProvider.WaitAllDispatcherOperationAndTasksAsync(workspace, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles);

            // check left buffer
            var leftSnapshot = leftBuffer.CurrentSnapshot;
            var leftSpans = leftTagger.GetTags(leftSnapshot.GetSnapshotSpanCollection()).ToList();
            Assert.Equal(1, leftSpans.Count);

            // check right buffer
            var rightSnapshot = rightBuffer.CurrentSnapshot;
            var rightSpans = rightTagger.GetTags(rightSnapshot.GetSnapshotSpanCollection()).ToList();
            Assert.Equal(0, rightSpans.Count);
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/28639")]
        [Fact]
        public void TestPreviewWorkspaceDoesNotLeakSolution()
        {
            // Verify that analyzer execution doesn't leak solution instances from the preview workspace.

            var previewWorkspace = new PreviewWorkspace();
            Assert.NotNull(previewWorkspace.CurrentSolution);
            var solutionObjectReference = ObjectReference.CreateFromFactory(
                static previewWorkspace =>
                {
                    var project = previewWorkspace.CurrentSolution.AddProject("project", "project.dll", LanguageNames.CSharp);
                    Assert.True(previewWorkspace.TryApplyChanges(project.Solution));
                    return previewWorkspace.CurrentSolution;
                },
                previewWorkspace);

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new CommonDiagnosticAnalyzers.NotConfigurableDiagnosticAnalyzer());
            ExecuteAnalyzers(previewWorkspace, analyzers);

            previewWorkspace.Dispose();
            solutionObjectReference.AssertReleased();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/pull/67142")]
        public void TestPreviewWorkspaceDoesNotLeakItself()
        {
            var composition = EditorTestCompositions.EditorFeatures;
            var exportProvider = composition.ExportProviderFactory.CreateExportProvider();
            var previewWorkspaceReference = ObjectReference.CreateFromFactory(
                static composition => new PreviewWorkspace(composition.GetHostServices()),
                composition);

            // Verify the GC can reclaim member for a workspace which has not been disposed.
            previewWorkspaceReference.AssertReleased();

            // Keep the export provider alive longer than the workspace to further ensure that the workspace is not GC
            // rooted within the export provider instance.
            GC.KeepAlive(exportProvider);
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
