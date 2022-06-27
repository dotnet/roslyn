// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    public class DiagnosticAnalyzerServiceTests
    {
        private static readonly TestComposition s_featuresCompositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures
            .AddExcludedPartTypes(typeof(IDiagnosticUpdateSourceRegistrationService))
            .AddParts(typeof(MockDiagnosticUpdateSourceRegistrationService))
            .AddParts(typeof(TestDocumentTrackingService));

        private static readonly TestComposition s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures
            .AddExcludedPartTypes(typeof(IDiagnosticUpdateSourceRegistrationService))
            .AddParts(typeof(MockDiagnosticUpdateSourceRegistrationService));

        private static AdhocWorkspace CreateWorkspace(Type[] additionalParts = null)
            => new(s_featuresCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(additionalParts).GetHostServices());

        private static IGlobalOptionService GetGlobalOptions(Workspace workspace)
            => ((IMefHostExportProvider)workspace.Services.HostServices).GetExportedValue<IGlobalOptionService>();

        private static void OpenDocumentAndMakeActive(Document document, Workspace workspace)
        {
            workspace.OpenDocument(document.Id);

            var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetRequiredService<IDocumentTrackingService>();
            documentTrackingService.SetActiveDocument(document.Id);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalse()
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var document = GetDocumentFromIncompleteProject(workspace);

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var analyzer = service.CreateIncrementalAnalyzer(workspace);
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            // listen to events
            // check empty since this could be called to clear up existing diagnostics
            service.DiagnosticsUpdated += (s, a) =>
            {
                var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                Assert.Empty(diagnostics);
            };

            // now call each analyze method. none of them should run.
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseFSAOn()
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzer()));

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));
            var document = GetDocumentFromIncompleteProject(workspace);

            OpenDocumentAndMakeActive(document, workspace);

            await TestAnalyzerAsync(workspace, document, AnalyzerResultSetter, expectedSyntax: true, expectedSemantic: true);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWhenFileOpened()
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var document = GetDocumentFromIncompleteProject(workspace);

            OpenDocumentAndMakeActive(document, workspace);

            await TestAnalyzerAsync(workspace, document, AnalyzerResultSetter, expectedSyntax: true, expectedSemantic: true);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWhenFileOpenedWithCompilerAnalyzer()
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var document = GetDocumentFromIncompleteProject(workspace);

            // open document
            workspace.OpenDocument(document.Id);

            await TestAnalyzerAsync(workspace, document, CompilerAnalyzerResultSetter, expectedSyntax: true, expectedSemantic: false);
        }

        [Fact]
        public async Task TestHasSuccessfullyLoadedBeingFalseWithCompilerAnalyzerFSAOn()
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer()));

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var document = GetDocumentFromIncompleteProject(workspace);

            await TestAnalyzerAsync(workspace, document, CompilerAnalyzerResultSetter, expectedSyntax: true, expectedSemantic: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestDisabledByDefaultAnalyzerEnabledWithEditorConfig(bool enabledWithEditorconfig)
        {
            using var workspace = CreateWorkspace();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new DisabledByDefaultAnalyzer()));

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    filePath: "z:\\CSharpProject.csproj"));

            if (enabledWithEditorconfig)
            {
                var editorconfigText = @$"
[*.cs]
dotnet_diagnostic.{DisabledByDefaultAnalyzer.s_syntaxRule.Id}.severity = warning
dotnet_diagnostic.{DisabledByDefaultAnalyzer.s_semanticRule.Id}.severity = warning
dotnet_diagnostic.{DisabledByDefaultAnalyzer.s_compilationRule.Id}.severity = warning";

                project = project.AddAnalyzerConfigDocument(".editorconfig", filePath: "z:\\.editorconfig", text: SourceText.From(editorconfigText)).Project;
            }

            var document = project.AddDocument("test.cs", SourceText.From("class A {}"), filePath: "z:\\test.cs");
            var applied = workspace.TryApplyChanges(document.Project.Solution);
            Assert.True(applied);

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            // listen to events
            var syntaxDiagnostic = false;
            var semanticDiagnostic = false;
            var compilationDiagnostic = false;
            service.DiagnosticsUpdated += (s, a) =>
            {
                var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                var diagnostic = Assert.Single(diagnostics);
                Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

                if (diagnostic.Id == DisabledByDefaultAnalyzer.s_syntaxRule.Id)
                {
                    syntaxDiagnostic = true;
                }
                else if (diagnostic.Id == DisabledByDefaultAnalyzer.s_semanticRule.Id)
                {
                    semanticDiagnostic = true;
                }
                else if (diagnostic.Id == DisabledByDefaultAnalyzer.s_compilationRule.Id)
                {
                    compilationDiagnostic = true;
                }
            };

            // open document
            workspace.OpenDocument(document.Id);
            await analyzer.DocumentOpenAsync(document, CancellationToken.None).ConfigureAwait(false);

            // run analysis
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync().ConfigureAwait(false);

            Assert.Equal(enabledWithEditorconfig, syntaxDiagnostic);
            Assert.Equal(enabledWithEditorconfig, semanticDiagnostic);
            Assert.Equal(enabledWithEditorconfig, compilationDiagnostic);
        }

        private static async Task TestAnalyzerAsync(
            AdhocWorkspace workspace,
            Document document,
            Func<bool, bool, ImmutableArray<DiagnosticData>, (bool, bool)> resultSetter,
            bool expectedSyntax, bool expectedSemantic)
        {
            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            var syntax = false;
            var semantic = false;

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                (syntax, semantic) = resultSetter(syntax, semantic, diagnostics);
            };

            // now call each analyze method. none of them should run.
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync().ConfigureAwait(false);

            // two should have been called.
            Assert.Equal(expectedSyntax, syntax);
            Assert.Equal(expectedSemantic, semantic);
        }

        [Fact]
        public async Task TestOpenFileOnlyAnalyzerDiagnostics()
        {
            using var workspace = CreateWorkspace();

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new OpenFileOnlyAnalyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.AddProject(
                           ProjectInfo.Create(
                               ProjectId.CreateNewId(),
                               VersionStamp.Create(),
                               "CSharpProject",
                               "CSharpProject",
                               LanguageNames.CSharp));

            var document = workspace.AddDocument(project.Id, "Empty.cs", SourceText.From(""));

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var analyzer = service.CreateIncrementalAnalyzer(workspace);

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                if (workspace.IsDocumentOpen(a.DocumentId))
                {
                    var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                    // check the diagnostics are reported
                    Assert.Equal(document.Id, a.DocumentId);
                    Assert.Equal(1, diagnostics.Length);
                    Assert.Equal(OpenFileOnlyAnalyzer.s_syntaxRule.Id, diagnostics[0].Id);
                }

                if (a.DocumentId == document.Id && !workspace.IsDocumentOpen(a.DocumentId))
                {
                    // check the diagnostics reported are cleared
                    var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                    Assert.Equal(0, diagnostics.Length);
                }
            };

            // open document
            workspace.OpenDocument(document.Id);
            await analyzer.DocumentOpenAsync(document, CancellationToken.None).ConfigureAwait(false);

            // cause analysis
            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // close document
            workspace.CloseDocument(document.Id);
            await analyzer.DocumentCloseAsync(document, CancellationToken.None).ConfigureAwait(false);

            await RunAllAnalysisAsync(analyzer, document).ConfigureAwait(false);

            // wait for all events to raised
            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSynchronizeWithBuild()
        {
            using var workspace = CreateWorkspace(new[] { typeof(NoCompilationLanguageServiceFactory) });

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new NoNameAnalyzer()));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var language = NoCompilationConstants.LanguageName;

            var project = workspace.AddProject(
                           ProjectInfo.Create(
                               ProjectId.CreateNewId(),
                               VersionStamp.Create(),
                               "NoNameProject",
                               "NoNameProject",
                               language));

            var filePath = "NoNameDoc.other";
            var document = workspace.AddDocument(
                DocumentInfo.Create(
                    DocumentId.CreateNewId(project.Id),
                    "Empty",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create(), filePath)),
                    filePath: filePath));

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var analyzer = service.CreateIncrementalAnalyzer(workspace);
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var syntax = false;

            // listen to events
            service.DiagnosticsUpdated += (s, a) =>
            {
                var diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                switch (diagnostics.Length)
                {
                    case 0:
                        return;
                    case 1:
                        syntax |= diagnostics[0].Id == NoNameAnalyzer.s_syntaxRule.Id;
                        return;
                    default:
                        AssertEx.Fail("shouldn't reach here");
                        return;
                }
            };

            // cause analysis
            var location = Location.Create(document.FilePath, textSpan: default, lineSpan: default);

            await service.SynchronizeWithBuildAsync(
                workspace,
                ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>>.Empty.Add(
                    document.Project.Id,
                    ImmutableArray.Create(DiagnosticData.Create(Diagnostic.Create(NoNameAnalyzer.s_syntaxRule, location), document.Project))),
                new TaskQueue(service.Listener, TaskScheduler.Default),
                onBuildCompleted: true,
                CancellationToken.None);

            // wait for all events to raised
            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync().ConfigureAwait(false);

            // two should have been called.
            Assert.True(syntax);

            // we should reach here without crashing
        }

        [Fact]
        public void TestHostAnalyzerOrdering()
        {
            using var workspace = CreateWorkspace();
            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(
                new Priority20Analyzer(),
                new Priority15Analyzer(),
                new Priority10Analyzer(),
                new Priority1Analyzer(),
                new Priority0Analyzer(),
                new CSharpCompilerDiagnosticAnalyzer(),
                new Analyzer()
            ));

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.AddProject(
                          ProjectInfo.Create(
                              ProjectId.CreateNewId(),
                              VersionStamp.Create(),
                              "Dummy",
                              "Dummy",
                              LanguageNames.CSharp));

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);
            var analyzers = incrementalAnalyzer.GetAnalyzersTestOnly(project).ToArray();

            AssertEx.Equal(new[]
            {
                typeof(FileContentLoadAnalyzer),
                typeof(CSharpCompilerDiagnosticAnalyzer),
                typeof(Analyzer),
                typeof(Priority0Analyzer),
                typeof(Priority1Analyzer),
                typeof(Priority10Analyzer),
                typeof(Priority15Analyzer),
                typeof(Priority20Analyzer)
            }, analyzers.Select(a => a.GetType()));
        }

        [Fact]
        public async Task TestHostAnalyzerErrorNotLeaking()
        {
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution;

            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(
                new LeakDocumentAnalyzer(), new LeakProjectAnalyzer()));

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);

            workspace.TryApplyChanges(solution.WithAnalyzerReferences(new[] { analyzerReference }));

            var projectId = ProjectId.CreateNewId();
            var project = workspace.AddProject(
                          ProjectInfo.Create(
                              projectId,
                              VersionStamp.Create(),
                              "Dummy",
                              "Dummy",
                              LanguageNames.CSharp,
                              documents: new[] {
                                  DocumentInfo.Create(
                                      DocumentId.CreateNewId(projectId),
                                      "test.cs",
                                      loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class A {}"), VersionStamp.Create(), filePath: "test.cs")),
                                      filePath: "test.cs")}));

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

            var called = false;
            service.DiagnosticsUpdated += (s, e) =>
            {
                var diagnostics = e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                if (diagnostics.Length == 0)
                {
                    return;
                }

                var liveId = (LiveDiagnosticUpdateArgsId)e.Id;
                Assert.False(liveId.Analyzer is ProjectDiagnosticAnalyzer);

                called = true;
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);
            await incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            Assert.True(called);
        }

        [Fact, WorkItem(42353, "https://github.com/dotnet/roslyn/issues/42353")]
        public async Task TestFullSolutionAnalysisForHiddenAnalyzers()
        {
            // By default, hidden analyzer does not execute in full solution analysis.
            using var workspace = CreateWorkspaceWithProjectAndAnalyzer(new NamedTypeAnalyzer(DiagnosticSeverity.Hidden));
            var project = workspace.CurrentSolution.Projects.Single();

            await TestFullSolutionAnalysisForProjectAsync(workspace, project, expectAnalyzerExecuted: false);
        }

        [Fact, WorkItem(42353, "https://github.com/dotnet/roslyn/issues/42353")]
        public async Task TestFullSolutionAnalysisForHiddenAnalyzers_SeverityInCompilationOptions()
        {
            // Escalating the analyzer to non-hidden effective severity through compilation options
            // ensures that analyzer executes in full solution analysis.
            using var workspace = CreateWorkspaceWithProjectAndAnalyzer(new NamedTypeAnalyzer(DiagnosticSeverity.Hidden));
            var project = workspace.CurrentSolution.Projects.Single();

            var newSpecificOptions = project.CompilationOptions.SpecificDiagnosticOptions.Add(NamedTypeAnalyzer.DiagnosticId, ReportDiagnostic.Warn);
            project = project.WithCompilationOptions(project.CompilationOptions.WithSpecificDiagnosticOptions(newSpecificOptions));
            await TestFullSolutionAnalysisForProjectAsync(workspace, project, expectAnalyzerExecuted: true);
        }

        [Fact, WorkItem(42353, "https://github.com/dotnet/roslyn/issues/42353")]
        public async Task TestFullSolutionAnalysisForHiddenAnalyzers_SeverityInAnalyzerConfigOptions()
        {
            using var workspace = CreateWorkspaceWithProjectAndAnalyzer(new NamedTypeAnalyzer(DiagnosticSeverity.Hidden));
            var project = workspace.CurrentSolution.Projects.Single();

            // Escalating the analyzer to non-hidden effective severity through analyzer config options
            // ensures that analyzer executes in full solution analysis.
            var analyzerConfigText = $@"
[*.cs]
dotnet_diagnostic.{NamedTypeAnalyzer.DiagnosticId}.severity = warning
";

            project = project.AddAnalyzerConfigDocument(
                ".editorconfig",
                text: SourceText.From(analyzerConfigText),
                filePath: "z:\\.editorconfig").Project;

            await TestFullSolutionAnalysisForProjectAsync(workspace, project, expectAnalyzerExecuted: true);
        }

        private static AdhocWorkspace CreateWorkspaceWithProjectAndAnalyzer(DiagnosticAnalyzer analyzer)
        {
            var workspace = CreateWorkspace();

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);

            var projectId = ProjectId.CreateNewId();
            var solution = workspace.CurrentSolution;

            solution = solution
                .AddAnalyzerReference(new AnalyzerImageReference(ImmutableArray.Create(analyzer)))
                .AddProject(
                    ProjectInfo.Create(
                        projectId,
                        VersionStamp.Create(),
                        "Dummy",
                        "Dummy",
                        LanguageNames.CSharp,
                        filePath: "z:\\Dummy.csproj",
                        documents: new[] {
                            DocumentInfo.Create(
                                DocumentId.CreateNewId(projectId),
                                "test.cs",
                                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class A {}"), VersionStamp.Create(), filePath: "test.cs")),
                                filePath: "z:\\test.cs")}));

            Assert.True(workspace.TryApplyChanges(solution));

            return workspace;
        }

        private static async Task TestFullSolutionAnalysisForProjectAsync(AdhocWorkspace workspace, Project project, bool expectAnalyzerExecuted)
        {
            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var called = false;
            service.DiagnosticsUpdated += (s, e) =>
            {
                var diagnostics = e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                if (diagnostics.Length == 0)
                {
                    return;
                }

                var liveId = (LiveDiagnosticUpdateArgsId)e.Id;
                Assert.True(liveId.Analyzer is NamedTypeAnalyzer);

                called = true;
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(project.Solution.Workspace);
            await incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            Assert.Equal(expectAnalyzerExecuted, called);
        }

        [Theory, CombinatorialData]
        internal async Task TestAdditionalFileAnalyzer(bool registerFromInitialize, bool testMultiple, BackgroundAnalysisScope analysisScope)
        {
            using var workspace = CreateWorkspace();

            var globalOptions = GetGlobalOptions(workspace);
            globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), analysisScope);

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "CSharpProject", "CSharpProject", LanguageNames.CSharp);
            var project = workspace.AddProject(projectInfo);

            var diagnosticSpan = new TextSpan(2, 2);
            var analyzer = new AdditionalFileAnalyzer(registerFromInitialize, diagnosticSpan, id: "ID0001");
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
            if (testMultiple)
            {
                analyzer = new AdditionalFileAnalyzer2(registerFromInitialize, diagnosticSpan, id: "ID0002");
                analyzers = analyzers.Add(analyzer);
            }

            var analyzerReference = new AnalyzerImageReference(analyzers);
            project = project.WithAnalyzerReferences(new[] { analyzerReference })
                .AddAdditionalDocument(name: "dummy.txt", text: "Additional File Text", filePath: "dummy.txt").Project;
            if (testMultiple)
            {
                project = project.AddAdditionalDocument(name: "dummy2.txt", text: "Additional File2 Text", filePath: "dummy2.txt").Project;
            }

            var applied = workspace.TryApplyChanges(project.Solution);
            Assert.True(applied);

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(exportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

            var diagnostics = new ConcurrentSet<DiagnosticData>();
            service.DiagnosticsUpdated += (s, e) =>
            {
                diagnostics.AddRange(e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode));
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);
            var firstAdditionalDocument = project.AdditionalDocuments.FirstOrDefault();

            switch (analysisScope)
            {
                case BackgroundAnalysisScope.None:
                case BackgroundAnalysisScope.ActiveFile:
                case BackgroundAnalysisScope.OpenFiles:
                    workspace.OpenAdditionalDocument(firstAdditionalDocument.Id);
                    await incrementalAnalyzer.AnalyzeNonSourceDocumentAsync(firstAdditionalDocument, InvocationReasons.SyntaxChanged, CancellationToken.None);
                    break;

                case BackgroundAnalysisScope.FullSolution:
                    await incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(analysisScope);
            }

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            var expectedCount = (analysisScope, testMultiple) switch
            {
                (BackgroundAnalysisScope.ActiveFile or BackgroundAnalysisScope.None, _) => 0,
                (BackgroundAnalysisScope.OpenFiles or BackgroundAnalysisScope.FullSolution, false) => 1,
                (BackgroundAnalysisScope.OpenFiles, true) => 2,
                (BackgroundAnalysisScope.FullSolution, true) => 4,
                _ => throw ExceptionUtilities.Unreachable,
            };

            Assert.Equal(expectedCount, diagnostics.Count);

            for (var i = 0; i < analyzers.Length; i++)
            {
                analyzer = (AdditionalFileAnalyzer)analyzers[i];
                foreach (var additionalDoc in project.AdditionalDocuments)
                {
                    var applicableDiagnostics = diagnostics.Where(
                        d => d.Id == analyzer.Descriptor.Id && d.DataLocation.OriginalFilePath == additionalDoc.FilePath);

                    if (analysisScope is BackgroundAnalysisScope.ActiveFile or BackgroundAnalysisScope.None)
                    {
                        Assert.Empty(applicableDiagnostics);
                    }
                    else if (analysisScope == BackgroundAnalysisScope.OpenFiles &&
                        firstAdditionalDocument != additionalDoc)
                    {
                        Assert.Empty(applicableDiagnostics);
                    }
                    else
                    {
                        var diagnostic = Assert.Single(applicableDiagnostics);
                        Assert.Equal(diagnosticSpan, diagnostic.GetTextSpan());
                        diagnostics.Remove(diagnostic);
                    }
                }
            }

            Assert.Empty(diagnostics);
        }

        private class AdditionalFileAnalyzer2 : AdditionalFileAnalyzer
        {
            public AdditionalFileAnalyzer2(bool registerFromInitialize, TextSpan diagnosticSpan, string id)
                : base(registerFromInitialize, diagnosticSpan, id)
            {
            }
        }

        [Theory, CombinatorialData]
        internal async Task TestDiagnosticSuppressor(bool includeAnalyzer, bool includeSuppressor, BackgroundAnalysisScope analysisScope)
        {
            var analyzers = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
            if (includeAnalyzer)
            {
                analyzers.Add(new NamedTypeAnalyzer());
            }

            if (includeSuppressor)
            {
                analyzers.Add(new DiagnosticSuppressorForId(NamedTypeAnalyzer.DiagnosticId));
            }

            var analyzerReference = new AnalyzerImageReference(analyzers.ToImmutableArray());

            using var workspace = TestWorkspace.CreateCSharp("class A {}", composition: s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(typeof(TestDocumentTrackingService)));

            workspace.GlobalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), analysisScope);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());
            var globalOptions = workspace.GetService<IGlobalOptionService>();

            DiagnosticData diagnostic = null;
            service.DiagnosticsUpdated += (s, e) =>
            {
                var diagnostics = e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                if (diagnostics.Length == 0)
                {
                    return;
                }

                diagnostic = Assert.Single(diagnostics);
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);

            switch (analysisScope)
            {
                case BackgroundAnalysisScope.None:
                case BackgroundAnalysisScope.ActiveFile:
                    workspace.OpenDocument(document.Id);
                    var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetService<IDocumentTrackingService>();
                    documentTrackingService.SetActiveDocument(document.Id);
                    await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, CancellationToken.None);
                    break;

                case BackgroundAnalysisScope.OpenFiles:
                    workspace.OpenDocument(document.Id);
                    await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, CancellationToken.None);
                    break;

                case BackgroundAnalysisScope.FullSolution:
                    await incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(analysisScope);
            }

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            if (includeAnalyzer && analysisScope != BackgroundAnalysisScope.None)
            {
                Assert.True(diagnostic != null);
                Assert.Equal(NamedTypeAnalyzer.DiagnosticId, diagnostic.Id);
                Assert.Equal(includeSuppressor, diagnostic.IsSuppressed);
            }
            else
            {
                Assert.True(diagnostic == null);
            }
        }

        [Theory, CombinatorialData]
        internal async Task TestRemoveUnnecessaryInlineSuppressionsAnalyzer(BackgroundAnalysisScope analysisScope, bool isSourceGenerated, bool testPragma)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new CSharpCompilerDiagnosticAnalyzer(),
                new NamedTypeAnalyzer(),
                new CSharpRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer());

            var analyzerReference = new AnalyzerImageReference(analyzers);

            string code;
            if (testPragma)
            {
                code = $@"
#pragma warning disable {NamedTypeAnalyzer.DiagnosticId} // Unnecessary
#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary

#pragma warning disable {NamedTypeAnalyzer.DiagnosticId} // Necessary
class A
{{
    void M()
    {{
#pragma warning disable CS0168 // Variable is declared but never used - Necessary
        int x;
    }}
}}
";
            }
            else
            {
                code = $@"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category1"", ""{NamedTypeAnalyzer.DiagnosticId}"")] // Necessary
class A
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""{NamedTypeAnalyzer.DiagnosticId}"")] // Unnecessary
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Category3"", ""CS0168"")] // Unnecessary
    void M()
    {{
#pragma warning disable CS0168 // Variable is declared but never used - Necessary
        int x;
    }}
}}
";
            }

            string[] files;
            string[] sourceGeneratedFiles;
            if (isSourceGenerated)
            {
                files = Array.Empty<string>();
                sourceGeneratedFiles = new[] { code };
            }
            else
            {
                files = new[] { code };
                sourceGeneratedFiles = Array.Empty<string>();
            }

            using var workspace = TestWorkspace.CreateCSharp(files, sourceGeneratedFiles, composition: s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(typeof(TestDocumentTrackingService), typeof(TestSyntaxTreeConfigurationService)));
            var syntaxTreeConfigurationService = workspace.GetService<TestSyntaxTreeConfigurationService>();
            syntaxTreeConfigurationService.EnableOpeningSourceGeneratedFilesInWorkspace = true;

            workspace.GlobalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), analysisScope);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.CurrentSolution.Projects.Single();
            var document = isSourceGenerated ? (await project.GetSourceGeneratedDocumentsAsync(CancellationToken.None)).Single() : project.Documents.Single();
            if (isSourceGenerated)
                Assert.IsType<SourceGeneratedDocument>(document);
            else
                Assert.IsType<Document>(document);

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());

            var diagnostics = ArrayBuilder<DiagnosticData>.GetInstance();
            service.DiagnosticsUpdated += (s, e) =>
            {
                diagnostics.AddRange(
                    e.GetPushDiagnostics(workspace.GlobalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)
                     .Where(d => d.Id == IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId)
                     .OrderBy(d => d.GetTextSpan()));
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);

            switch (analysisScope)
            {
                case BackgroundAnalysisScope.None:
                case BackgroundAnalysisScope.ActiveFile:
                    if (isSourceGenerated)
                        workspace.OpenSourceGeneratedDocument(document.Id);
                    else
                        workspace.OpenDocument(document.Id);

                    var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetRequiredService<IDocumentTrackingService>();
                    documentTrackingService.SetActiveDocument(document.Id);
                    await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, CancellationToken.None);
                    break;

                case BackgroundAnalysisScope.OpenFiles:
                    if (isSourceGenerated)
                        workspace.OpenSourceGeneratedDocument(document.Id);
                    else
                        workspace.OpenDocument(document.Id);

                    await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, CancellationToken.None);
                    break;

                case BackgroundAnalysisScope.FullSolution:
                    await incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged: true, InvocationReasons.Reanalyze, CancellationToken.None);
                    break;
            }

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            var root = await document.GetSyntaxRootAsync();
            if (analysisScope == BackgroundAnalysisScope.None)
            {
                // Anayzers are disabled for BackgroundAnalysisScope.None.
                Assert.Empty(diagnostics);
            }
            else
            {
                Assert.Equal(2, diagnostics.Count);
                if (testPragma)
                {
                    var pragma1 = root.FindTrivia(diagnostics[0].GetTextSpan().Start).ToString();
                    Assert.Equal($"#pragma warning disable {NamedTypeAnalyzer.DiagnosticId} // Unnecessary", pragma1);
                    var pragma2 = root.FindTrivia(diagnostics[1].GetTextSpan().Start).ToString();
                    Assert.Equal($"#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary", pragma2);
                }
                else
                {
                    var attribute1 = root.FindNode(diagnostics[0].GetTextSpan()).ToString();
                    Assert.Equal($@"System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""{NamedTypeAnalyzer.DiagnosticId}"")", attribute1);
                    var attribute2 = root.FindNode(diagnostics[1].GetTextSpan()).ToString();
                    Assert.Equal($@"System.Diagnostics.CodeAnalysis.SuppressMessage(""Category3"", ""CS0168"")", attribute2);
                }
            }
        }

        [Theory, CombinatorialData]
        internal async Task TestCancellationDuringDiagnosticComputation_InProc(AnalyzerRegisterActionKind actionKind)
        {
            // This test verifies that we do no attempt to re-use CompilationWithAnalyzers instance in IDE in-proc diagnostic computation in presence of an OperationCanceledException during analysis.
            // Attempting to do so has led to large number of reliability issues and flakiness in diagnostic computation, which we want to avoid.

            var source = @"
class A
{
    void M()
    {
        int x = 0;
    }
}";

            using var workspace = TestWorkspace.CreateCSharp(source,
                composition: s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(typeof(TestDocumentTrackingService)));

            var analyzer = new CancellationTestAnalyzer(actionKind);
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();

            Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.GetService<IDiagnosticUpdateSourceRegistrationService>());
            var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());
            var globalOptions = workspace.GetService<IGlobalOptionService>();

            DiagnosticData diagnostic = null;
            service.DiagnosticsUpdated += (s, e) =>
            {
                var diagnostics = e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                if (diagnostics.IsEmpty)
                {
                    return;
                }

                Assert.Null(diagnostic);
                diagnostic = Assert.Single(diagnostics);
            };

            var incrementalAnalyzer = (DiagnosticIncrementalAnalyzer)service.CreateIncrementalAnalyzer(workspace);

            OpenDocumentAndMakeActive(document, workspace);

            // First invoke analysis with cancellation token, and verify canceled compilation and no reported diagnostics.
            Assert.Empty(analyzer.CanceledCompilations);
            try
            {
                if (actionKind == AnalyzerRegisterActionKind.SyntaxTree)
                {
                    await incrementalAnalyzer.AnalyzeSyntaxAsync(document, InvocationReasons.SyntaxChanged, analyzer.CancellationToken);
                }
                else
                {
                    await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, analyzer.CancellationToken);
                }

                throw ExceptionUtilities.Unreachable;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == analyzer.CancellationToken)
            {
            }

            Assert.Single(analyzer.CanceledCompilations);
            Assert.Null(diagnostic);

            // Then invoke analysis without cancellation token, and verify non-cancelled diagnostic.
            if (actionKind == AnalyzerRegisterActionKind.SyntaxTree)
            {
                await incrementalAnalyzer.AnalyzeSyntaxAsync(document, InvocationReasons.SyntaxChanged, CancellationToken.None);
            }
            else
            {
                await incrementalAnalyzer.AnalyzeDocumentAsync(document, bodyOpt: null, InvocationReasons.SemanticChanged, CancellationToken.None);
            }

            await ((AsynchronousOperationListener)service.Listener).ExpeditedWaitAsync();

            Assert.True(diagnostic != null);
            Assert.Equal(CancellationTestAnalyzer.NonCanceledDiagnosticId, diagnostic.Id);
        }

        [Theory, CombinatorialData]
        [WorkItem(49698, "https://github.com/dotnet/roslyn/issues/49698")]
        internal async Task TestOnlyRequiredAnalyzerExecutedDuringDiagnosticComputation(bool documentAnalysis)
        {
            using var workspace = TestWorkspace.CreateCSharp("class A { }");

            // Verify that requesting analyzer diagnostics for analyzer1 does not lead to invoking analyzer2.
            var analyzer1 = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, DiagnosticSeverity.Warning, throwOnAllNamedTypes: false);
            var analyzer1Id = analyzer1.GetAnalyzerId();
            var analyzer2 = new NamedTypeAnalyzer();
            var analyzerIdsToRequestDiagnostics = new[] { analyzer1Id };
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer1, analyzer2));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));
            var project = workspace.CurrentSolution.Projects.Single();
            var document = documentAnalysis ? project.Documents.Single() : null;
            var diagnosticComputer = new DiagnosticComputer(document, project, IdeAnalyzerOptions.Default, span: null, AnalysisKind.Semantic, new DiagnosticAnalyzerInfoCache());
            var diagnosticsMapResults = await diagnosticComputer.GetDiagnosticsAsync(analyzerIdsToRequestDiagnostics, reportSuppressedDiagnostics: false,
                logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);
            Assert.False(analyzer2.ReceivedSymbolCallback);

            Assert.Equal(1, diagnosticsMapResults.Diagnostics.Length);
            var (actualAnalyzerId, diagnosticMap) = diagnosticsMapResults.Diagnostics.Single();
            Assert.Equal(analyzer1Id, actualAnalyzerId);
            Assert.Equal(1, diagnosticMap.Semantic.Length);
            var semanticDiagnostics = diagnosticMap.Semantic.Single().Item2;
            var diagnostic = Assert.Single(semanticDiagnostics);
            Assert.Equal(analyzer1.Descriptor.Id, diagnostic.Id);

            Assert.Empty(diagnosticMap.Syntax);
            Assert.Empty(diagnosticMap.NonLocal);
            Assert.Empty(diagnosticMap.Other);
        }

        [Theory, CombinatorialData]
        internal async Task TestCancellationDuringDiagnosticComputation_OutOfProc(AnalyzerRegisterActionKind actionKind)
        {
            // This test verifies that we do no attempt to re-use CompilationWithAnalyzers instance in IDE OutOfProc diagnostic computation in presence of an OperationCanceledException during analysis.
            // Attempting to do so has led to large number of reliability issues and flakiness in diagnostic computation, which we want to avoid.
            // NOTE: Unfortunately, we cannot perform an end-to-end OutOfProc test, similar to the InProc test above because AnalyzerImageReference is not serializable.
            //       So, we perform a very targeted test which directly uses the 'DiagnosticComputer' type that is used for all OutOfProc diagnostic computation.

            var source = @"
class A
{
    void M()
    {
        int x = 0;
    }
}";

            using var workspace = TestWorkspace.CreateCSharp(source);

            var analyzer = new CancellationTestAnalyzer(actionKind);
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();
            var diagnosticAnalyzerInfoCache = new DiagnosticAnalyzerInfoCache();

            var kind = actionKind == AnalyzerRegisterActionKind.SyntaxTree ? AnalysisKind.Syntax : AnalysisKind.Semantic;
            var diagnosticComputer = new DiagnosticComputer(document, project, IdeAnalyzerOptions.Default, span: null, kind, diagnosticAnalyzerInfoCache);
            var analyzerIds = new[] { analyzer.GetAnalyzerId() };

            // First invoke analysis with cancellation token, and verify canceled compilation and no reported diagnostics.
            Assert.Empty(analyzer.CanceledCompilations);
            try
            {
                _ = await diagnosticComputer.GetDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics: false,
                    logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: analyzer.CancellationToken);

                throw ExceptionUtilities.Unreachable;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == analyzer.CancellationToken)
            {
            }

            Assert.Single(analyzer.CanceledCompilations);

            // Then invoke analysis without cancellation token, and verify non-cancelled diagnostic.
            var diagnosticsMap = await diagnosticComputer.GetDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics: false,
                logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);
            var builder = diagnosticsMap.Diagnostics.Single().diagnosticMap;
            var diagnostic = kind == AnalysisKind.Syntax ? builder.Syntax.Single().Item2.Single() : builder.Semantic.Single().Item2.Single();
            Assert.Equal(CancellationTestAnalyzer.NonCanceledDiagnosticId, diagnostic.Id);
        }

        internal enum AnalyzerRegisterActionKind
        {
            SyntaxTree,
            SyntaxNode,
            Symbol,
            Operation,
            SemanticModel,
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal sealed class CancellationTestAnalyzer : DiagnosticAnalyzer
        {
            public const string CanceledDiagnosticId = "CanceledId";
            public const string NonCanceledDiagnosticId = "NonCanceledId";
            private readonly DiagnosticDescriptor s_canceledDescriptor =
                new DiagnosticDescriptor(CanceledDiagnosticId, "test", "test", "test", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            private readonly DiagnosticDescriptor s_nonCanceledDescriptor =
                new DiagnosticDescriptor(NonCanceledDiagnosticId, "test", "test", "test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly AnalyzerRegisterActionKind _actionKind;
            private readonly CancellationTokenSource _cancellationTokenSource;

            public CancellationTestAnalyzer(AnalyzerRegisterActionKind actionKind)
            {
                _actionKind = actionKind;
                _cancellationTokenSource = new CancellationTokenSource();
                CanceledCompilations = new ConcurrentSet<Compilation>();
            }

            public CancellationToken CancellationToken => _cancellationTokenSource.Token;
            public ConcurrentSet<Compilation> CanceledCompilations { get; }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_canceledDescriptor, s_nonCanceledDescriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(OnCompilationStart);
            }

            private void OnCompilationStart(CompilationStartAnalysisContext context)
            {
                switch (_actionKind)
                {
                    case AnalyzerRegisterActionKind.SyntaxTree:
                        context.RegisterSyntaxTreeAction(syntaxContext => HandleCallback(syntaxContext.Tree.GetRoot().GetLocation(), context.Compilation, syntaxContext.ReportDiagnostic, syntaxContext.CancellationToken));
                        break;
                    case AnalyzerRegisterActionKind.SyntaxNode:
                        context.RegisterSyntaxNodeAction(context => HandleCallback(context.Node.GetLocation(), context.Compilation, context.ReportDiagnostic, context.CancellationToken), CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
                        break;
                    case AnalyzerRegisterActionKind.Symbol:
                        context.RegisterSymbolAction(context => HandleCallback(context.Symbol.Locations[0], context.Compilation, context.ReportDiagnostic, context.CancellationToken), SymbolKind.NamedType);
                        break;
                    case AnalyzerRegisterActionKind.Operation:
                        context.RegisterOperationAction(context => HandleCallback(context.Operation.Syntax.GetLocation(), context.Compilation, context.ReportDiagnostic, context.CancellationToken), OperationKind.VariableDeclaration);
                        break;
                    case AnalyzerRegisterActionKind.SemanticModel:
                        context.RegisterSemanticModelAction(context => HandleCallback(context.SemanticModel.SyntaxTree.GetRoot().GetLocation(), context.SemanticModel.Compilation, context.ReportDiagnostic, context.CancellationToken));
                        break;
                }
            }

            private void HandleCallback(Location analysisLocation, Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                // Mimic cancellation by throwing an OperationCanceledException in first callback.
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    CanceledCompilations.Add(compilation);

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    throw ExceptionUtilities.Unreachable;
                }

                // Report diagnostic in the second callback.
                var descriptor = CanceledCompilations.Contains(compilation) ? s_canceledDescriptor : s_nonCanceledDescriptor;
                reportDiagnostic(Diagnostic.Create(descriptor, analysisLocation));
            }
        }

        private static Document GetDocumentFromIncompleteProject(AdhocWorkspace workspace)
        {
            var project = workspace.AddProject(
                            ProjectInfo.Create(
                                ProjectId.CreateNewId(),
                                VersionStamp.Create(),
                                "CSharpProject",
                                "CSharpProject",
                                LanguageNames.CSharp).WithHasAllInformation(hasAllInformation: false));

            return workspace.AddDocument(project.Id, "Empty.cs", SourceText.From("class A { B B {get} }"));
        }

        private static (bool, bool) AnalyzerResultSetter(bool syntax, bool semantic, ImmutableArray<DiagnosticData> diagnostics)
        {
            switch (diagnostics.Length)
            {
                case 0:
                    break;
                case 1:
                    syntax |= diagnostics[0].Id == Analyzer.s_syntaxRule.Id;
                    semantic |= diagnostics[0].Id == Analyzer.s_semanticRule.Id;
                    break;
                default:
                    AssertEx.Fail("shouldn't reach here");
                    break;
            }

            return (syntax, semantic);
        }

        private static (bool, bool) CompilerAnalyzerResultSetter(bool syntax, bool semantic, ImmutableArray<DiagnosticData> diagnostics)
        {
            syntax |= diagnostics.Any(d => d.Properties["Origin"] == "Syntactic");
            semantic |= diagnostics.Any(d => d.Properties["Origin"] != "Syntactic");

            return (syntax, semantic);
        }

        private static async Task RunAllAnalysisAsync(IIncrementalAnalyzer analyzer, TextDocument textDocument)
        {
            if (textDocument is Document document)
            {
                await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None).ConfigureAwait(false);
                await analyzer.AnalyzeDocumentAsync(document, bodyOpt: null, reasons: InvocationReasons.Empty, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await analyzer.AnalyzeNonSourceDocumentAsync(textDocument, InvocationReasons.Empty, CancellationToken.None).ConfigureAwait(false);
            }

            await analyzer.AnalyzeProjectAsync(textDocument.Project, semanticsChanged: true, reasons: InvocationReasons.Empty, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);
            internal static readonly DiagnosticDescriptor s_semanticRule = new DiagnosticDescriptor("semantic", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);
            internal static readonly DiagnosticDescriptor s_compilationRule = new DiagnosticDescriptor("compilation", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule, s_semanticRule, s_compilationRule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));
                context.RegisterSemanticModelAction(c => c.ReportDiagnostic(Diagnostic.Create(s_semanticRule, c.SemanticModel.SyntaxTree.GetRoot().GetLocation())));
                context.RegisterCompilationAction(c => c.ReportDiagnostic(Diagnostic.Create(s_compilationRule, c.Compilation.SyntaxTrees.First().GetRoot().GetLocation())));
            }
        }

        private class DisabledByDefaultAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: false);
            internal static readonly DiagnosticDescriptor s_semanticRule = new DiagnosticDescriptor("semantic", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: false);
            internal static readonly DiagnosticDescriptor s_compilationRule = new DiagnosticDescriptor("compilation", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: false);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule, s_semanticRule, s_compilationRule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));
                context.RegisterSemanticModelAction(c => c.ReportDiagnostic(Diagnostic.Create(s_semanticRule, c.SemanticModel.SyntaxTree.GetRoot().GetLocation())));
                context.RegisterCompilationAction(c => c.ReportDiagnostic(Diagnostic.Create(s_compilationRule, c.Compilation.SyntaxTrees.First().GetRoot().GetLocation())));
            }
        }

        private class OpenFileOnlyAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

            public override void Initialize(AnalysisContext context)
                => context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));

            public DiagnosticAnalyzerCategory GetAnalyzerCategory()
                => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

            public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;

            public bool OpenFileOnly(CodeAnalysis.Options.OptionSet options)
                => true;
        }

        private class NoNameAnalyzer : DocumentDiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray.Create(Diagnostic.Create(s_syntaxRule, Location.Create(document.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0))))));

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                => SpecializedTasks.Default<ImmutableArray<Diagnostic>>();
        }

        private class Priority20Analyzer : PriorityTestDocumentDiagnosticAnalyzer
        {
            public Priority20Analyzer() : base(priority: 20) { }
        }

        private class Priority15Analyzer : PriorityTestProjectDiagnosticAnalyzer
        {
            public Priority15Analyzer() : base(priority: 15) { }
        }

        private class Priority10Analyzer : PriorityTestDocumentDiagnosticAnalyzer
        {
            public Priority10Analyzer() : base(priority: 10) { }
        }

        private class Priority1Analyzer : PriorityTestProjectDiagnosticAnalyzer
        {
            public Priority1Analyzer() : base(priority: 1) { }
        }

        private class Priority0Analyzer : PriorityTestDocumentDiagnosticAnalyzer
        {
            public Priority0Analyzer() : base(priority: -1) { }
        }

        private class PriorityTestDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
        {
            protected PriorityTestDocumentDiagnosticAnalyzer(int priority)
                => Priority = priority;

            public override int Priority { get; }
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<Diagnostic>.Empty);
        }

        private class PriorityTestProjectDiagnosticAnalyzer : ProjectDiagnosticAnalyzer
        {
            protected PriorityTestProjectDiagnosticAnalyzer(int priority)
                => Priority = priority;

            public override int Priority { get; }
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
            public override Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<Diagnostic>.Empty);
        }

        private class LeakDocumentAnalyzer : DocumentDiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("leak", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

            public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return ImmutableArray.Create(Diagnostic.Create(s_syntaxRule, root.GetLocation()));
            }

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                => SpecializedTasks.Default<ImmutableArray<Diagnostic>>();
        }

        private class LeakProjectAnalyzer : ProjectDiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor("project", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);
            public override Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken) => SpecializedTasks.Default<ImmutableArray<Diagnostic>>();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class NamedTypeAnalyzer : DiagnosticAnalyzer
        {
            public const string DiagnosticId = "test";
            private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

            public NamedTypeAnalyzer(DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Warning)
                => _supportedDiagnostics = ImmutableArray.Create(new DiagnosticDescriptor(DiagnosticId, "test", "test", "test", defaultSeverity, isEnabledByDefault: true));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;
            public bool ReceivedSymbolCallback { get; private set; }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(c =>
                {
                    ReceivedSymbolCallback = true;
                    c.ReportDiagnostic(Diagnostic.Create(_supportedDiagnostics[0], c.Symbol.Locations[0]));
                }, SymbolKind.NamedType);
            }
        }
    }
}
