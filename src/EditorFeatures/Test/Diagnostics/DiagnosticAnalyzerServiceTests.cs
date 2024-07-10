// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;

[UseExportProvider]
public class DiagnosticAnalyzerServiceTests
{
    private static readonly TestComposition s_featuresCompositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures
        .AddParts(typeof(TestDocumentTrackingService));

    private static readonly TestComposition s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures;

    private static AdhocWorkspace CreateWorkspace(Type[] additionalParts = null)
        => new AdhocWorkspace(s_featuresCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(additionalParts).GetHostServices());

    private static IGlobalOptionService GetGlobalOptions(Workspace workspace)
        => workspace.Services.SolutionServices.ExportProvider.GetExportedValue<IGlobalOptionService>();

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

        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
        var analyzer = service.CreateIncrementalAnalyzer(workspace);
        var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

        var diagnostics = await analyzer.GetDiagnosticsForIdsAsync(
            workspace.CurrentSolution, projectId: null, documentId: null, diagnosticIds: null, shouldIncludeAnalyzer: null, getDocuments: null,
            includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: false, CancellationToken.None);
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public async Task TestHasSuccessfullyLoadedBeingFalseFSAOn()
    {
        using var workspace = CreateWorkspace();

        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzer()));

        var globalOptions = GetGlobalOptions(workspace);
        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

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
        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

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
        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

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

        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
        var analyzer = service.CreateIncrementalAnalyzer(workspace);

        // listen to events
        var syntaxDiagnostic = false;
        var semanticDiagnostic = false;
        var compilationDiagnostic = false;

        // open document
        workspace.OpenDocument(document.Id);

        var diagnostics = await analyzer.ForceAnalyzeProjectAsync(document.Project, CancellationToken.None);

        foreach (var diagnostic in diagnostics)
        {
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
        }

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
        var exportProvider = workspace.Services.SolutionServices.ExportProvider;

        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

        var analyzer = service.CreateIncrementalAnalyzer(workspace);

        var syntax = false;
        var semantic = false;

        var diagnostics = await analyzer.ForceAnalyzeProjectAsync(document.Project, CancellationToken.None);

        (syntax, semantic) = resultSetter(syntax, semantic, diagnostics);

        // two should have been called.
        Assert.Equal(expectedSyntax, syntax);
        Assert.Equal(expectedSemantic, semantic);
    }

    [Fact]
    public async Task TestHostAnalyzerOrderingAsync()
    {
        using var workspace = CreateWorkspace();
        var exportProvider = workspace.Services.SolutionServices.ExportProvider;

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

        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);
        var analyzers = await incrementalAnalyzer.GetAnalyzersTestOnlyAsync(project, CancellationToken.None).ConfigureAwait(false);
        var analyzersArray = analyzers.ToArray();

        AssertEx.Equal(new[]
        {
            typeof(FileContentLoadAnalyzer),
            typeof(GeneratorDiagnosticsPlaceholderAnalyzer),
            typeof(CSharpCompilerDiagnosticAnalyzer),
            typeof(Analyzer),
            typeof(Priority0Analyzer),
            typeof(Priority1Analyzer),
            typeof(Priority10Analyzer),
            typeof(Priority15Analyzer),
            typeof(Priority20Analyzer)
        }, analyzersArray.Select(a => a.GetType()));
    }

    [Fact]
    public async Task TestHostAnalyzerErrorNotLeaking()
    {
        using var workspace = CreateWorkspace();

        var solution = workspace.CurrentSolution;

        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(
            new LeakDocumentAnalyzer(), new LeakProjectAnalyzer()));

        var globalOptions = GetGlobalOptions(workspace);
        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

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

        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);
        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);
        Assert.NotEmpty(diagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42353")]
    public async Task TestFullSolutionAnalysisForHiddenAnalyzers()
    {
        // By default, hidden analyzer does not execute in full solution analysis.
        using var workspace = CreateWorkspaceWithProjectAndAnalyzer(new NamedTypeAnalyzer(DiagnosticSeverity.Hidden));
        var project = workspace.CurrentSolution.Projects.Single();

        await TestFullSolutionAnalysisForProjectAsync(workspace, project, expectAnalyzerExecuted: false);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42353")]
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42353")]
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
        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);

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
        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(project.Solution.Workspace);
        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);

        if (expectAnalyzerExecuted)
        {
            Assert.NotEmpty(diagnostics);
        }
        else
        {
            Assert.Empty(diagnostics);
        }
    }

    [Theory, CombinatorialData]
    internal async Task TestAdditionalFileAnalyzer(bool registerFromInitialize, bool testMultiple)
    {
        using var workspace = CreateWorkspace();

        var globalOptions = GetGlobalOptions(workspace);

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

        var exportProvider = workspace.Services.SolutionServices.ExportProvider;
        var service = Assert.IsType<DiagnosticAnalyzerService>(exportProvider.GetExportedValue<IDiagnosticAnalyzerService>());

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);
        var firstAdditionalDocument = project.AdditionalDocuments.FirstOrDefault();

        workspace.OpenAdditionalDocument(firstAdditionalDocument.Id);

        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);

        var expectedCount = testMultiple ? 4 : 1;

        Assert.Equal(expectedCount, diagnostics.Length);

        for (var i = 0; i < analyzers.Length; i++)
        {
            analyzer = (AdditionalFileAnalyzer)analyzers[i];
            foreach (var additionalDoc in project.AdditionalDocuments)
            {
                var applicableDiagnostics = diagnostics.Where(
                    d => d.Id == analyzer.Descriptor.Id && d.DataLocation.UnmappedFileSpan.Path == additionalDoc.FilePath);

                var text = await additionalDoc.GetTextAsync();

                var diagnostic = Assert.Single(applicableDiagnostics);
                Assert.Equal(diagnosticSpan, diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text));
                diagnostics = diagnostics.Remove(diagnostic);
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

        using var workspace = EditorTestWorkspace.CreateCSharp("class A {}", composition: s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(typeof(TestDocumentTrackingService)));

        workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, analysisScope);

        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

        var project = workspace.CurrentSolution.Projects.Single();
        var document = project.Documents.Single();

        var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());
        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);

        switch (analysisScope)
        {
            case BackgroundAnalysisScope.None:
            case BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics:
                workspace.OpenDocument(document.Id);
                var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetService<IDocumentTrackingService>();
                documentTrackingService.SetActiveDocument(document.Id);
                break;

            case BackgroundAnalysisScope.OpenFiles:
                workspace.OpenDocument(document.Id);
                break;

            case BackgroundAnalysisScope.FullSolution:
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(analysisScope);
        }

        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);

        var diagnostic = diagnostics.SingleOrDefault();
        if (includeAnalyzer)
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
            files = [];
            sourceGeneratedFiles = [code];
        }
        else
        {
            files = [code];
            sourceGeneratedFiles = [];
        }

        var composition = s_editorFeaturesCompositionWithMockDiagnosticUpdateSourceRegistrationService.AddParts(
            typeof(TestDocumentTrackingService));

        using var workspace = new EditorTestWorkspace(composition);

        workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, analysisScope);
        workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles, isSourceGenerated);

        var compilerDiagnosticsScope = analysisScope.ToEquivalentCompilerDiagnosticsScope();
        workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, compilerDiagnosticsScope);

        workspace.InitializeDocuments(TestWorkspace.CreateWorkspaceElement(LanguageNames.CSharp, files: files, sourceGeneratedFiles: sourceGeneratedFiles), openDocuments: false);
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

        var project = workspace.CurrentSolution.Projects.Single();
        var document = isSourceGenerated ? (await project.GetSourceGeneratedDocumentsAsync(CancellationToken.None)).Single() : project.Documents.Single();
        if (isSourceGenerated)
            Assert.IsType<SourceGeneratedDocument>(document);
        else
            Assert.IsType<Document>(document);

        var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());

        var text = await document.GetTextAsync();

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);

        switch (analysisScope)
        {
            case BackgroundAnalysisScope.None:
            case BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics:
                if (isSourceGenerated)
                    workspace.OpenSourceGeneratedDocument(document.Id);
                else
                    workspace.OpenDocument(document.Id);

                var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetRequiredService<IDocumentTrackingService>();
                documentTrackingService.SetActiveDocument(document.Id);
                break;

            case BackgroundAnalysisScope.OpenFiles:
                if (isSourceGenerated)
                    workspace.OpenSourceGeneratedDocument(document.Id);
                else
                    workspace.OpenDocument(document.Id);

                break;

            case BackgroundAnalysisScope.FullSolution:
                break;
        }

        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);

        diagnostics = diagnostics
            .Where(d => d.Id == IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId)
            .OrderBy(d => d.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text))
            .ToImmutableArray();

        var root = await document.GetSyntaxRootAsync();
        text = await document.GetTextAsync();

        Assert.Equal(2, diagnostics.Length);
        if (testPragma)
        {
            var pragma1 = root.FindTrivia(diagnostics[0].DataLocation.UnmappedFileSpan.GetClampedTextSpan(text).Start).ToString();
            Assert.Equal($"#pragma warning disable {NamedTypeAnalyzer.DiagnosticId} // Unnecessary", pragma1);
            var pragma2 = root.FindTrivia(diagnostics[1].DataLocation.UnmappedFileSpan.GetClampedTextSpan(text).Start).ToString();
            Assert.Equal($"#pragma warning disable CS0168 // Variable is declared but never used - Unnecessary", pragma2);
        }
        else
        {
            var attribute1 = root.FindNode(diagnostics[0].DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)).ToString();
            Assert.Equal($@"System.Diagnostics.CodeAnalysis.SuppressMessage(""Category2"", ""{NamedTypeAnalyzer.DiagnosticId}"")", attribute1);
            var attribute2 = root.FindNode(diagnostics[1].DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)).ToString();
            Assert.Equal($@"System.Diagnostics.CodeAnalysis.SuppressMessage(""Category3"", ""CS0168"")", attribute2);
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/49698")]
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
        var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(project.Services);
        var diagnosticsMapResults = await DiagnosticComputer.GetDiagnosticsAsync(
            document, project, Checksum.Null, ideAnalyzerOptions, span: null, analyzerIdsToRequestDiagnostics,
            AnalysisKind.Semantic, new DiagnosticAnalyzerInfoCache(), workspace.Services,
            isExplicit: false, reportSuppressedDiagnostics: false, logPerformanceInfo: false, getTelemetryInfo: false,
            cancellationToken: CancellationToken.None);
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

    [Theory, WorkItem(67257, "https://github.com/dotnet/roslyn/issues/67257")]
    [CombinatorialData]
    public async Task TestFilterSpanOnContextAsync(FilterSpanTestAnalyzer.AnalysisKind kind)
    {
        var source = @"
class B
{
    void M()
    {
        int x = 1;
    }
}";
        var additionalText = @"This is an additional file!";

        using var workspace = TestWorkspace.CreateCSharp(source);
        var project = workspace.CurrentSolution.Projects.Single();
        project = project.AddAdditionalDocument("additional.txt", additionalText).Project;

        var analyzer = new FilterSpanTestAnalyzer(kind);
        var analyzerId = analyzer.GetAnalyzerId();
        var analyzerIdsToRequestDiagnostics = new[] { analyzerId };
        var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        project = project.AddAnalyzerReference(analyzerReference);

        workspace.TryApplyChanges(project.Solution);

        project = workspace.CurrentSolution.Projects.Single();
        var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(project.Services);
        var document = project.Documents.Single();
        var additionalDocument = project.AdditionalDocuments.Single();

        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var localDeclaration = root.DescendantNodes().OfType<CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>().First();
        var filterSpan = kind == FilterSpanTestAnalyzer.AnalysisKind.AdditionalFile
            ? new TextSpan(0, 1)
            : localDeclaration.Span;
        // Invoke "GetDiagnosticsAsync" for a sub-span and then
        // for the entire document span and verify FilterSpan/FilterTree on the callback context.
        Assert.Null(analyzer.CallbackFilterSpan);
        Assert.Null(analyzer.CallbackFilterTree);
        await VerifyCallbackSpanAsync(filterSpan);
        await VerifyCallbackSpanAsync(filterSpan: null);

        async Task VerifyCallbackSpanAsync(TextSpan? filterSpan)
        {
            var analysisKind = kind is FilterSpanTestAnalyzer.AnalysisKind.SyntaxTree or FilterSpanTestAnalyzer.AnalysisKind.AdditionalFile
                ? AnalysisKind.Syntax
                : AnalysisKind.Semantic;
            var documentToAnalyze = kind == FilterSpanTestAnalyzer.AnalysisKind.AdditionalFile ? additionalDocument : document;
            _ = await DiagnosticComputer.GetDiagnosticsAsync(
                documentToAnalyze, project, Checksum.Null, ideAnalyzerOptions, filterSpan, analyzerIdsToRequestDiagnostics,
                analysisKind, new DiagnosticAnalyzerInfoCache(), workspace.Services,
                isExplicit: false, reportSuppressedDiagnostics: false, logPerformanceInfo: false, getTelemetryInfo: false,
                CancellationToken.None);
            Assert.Equal(filterSpan, analyzer.CallbackFilterSpan);
            if (kind == FilterSpanTestAnalyzer.AnalysisKind.AdditionalFile)
            {
                var expectedText = additionalDocument.GetTextSynchronously(CancellationToken.None).ToString();
                var actualText = analyzer.CallbackFilterFile.GetText().ToString();
                Assert.Equal(expectedText, actualText);
                Assert.Null(analyzer.CallbackFilterTree);
            }
            else
            {
                Assert.Equal(root.SyntaxTree, analyzer.CallbackFilterTree);
                Assert.Null(analyzer.CallbackFilterFile);
            }
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/67084")]
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

        var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(project.Services);
        var kind = actionKind == AnalyzerRegisterActionKind.SyntaxTree ? AnalysisKind.Syntax : AnalysisKind.Semantic;
        var analyzerIds = new[] { analyzer.GetAnalyzerId() };

        // First invoke analysis with cancellation token, and verify canceled compilation and no reported diagnostics.
        Assert.Empty(analyzer.CanceledCompilations);
        try
        {
            _ = await DiagnosticComputer.GetDiagnosticsAsync(document, project, Checksum.Null, ideAnalyzerOptions, span: null,
                analyzerIds, kind, diagnosticAnalyzerInfoCache, workspace.Services, isExplicit: false, reportSuppressedDiagnostics: false,
                logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: analyzer.CancellationToken);

            throw ExceptionUtilities.Unreachable();
        }
        catch (OperationCanceledException) when (analyzer.CancellationToken.IsCancellationRequested)
        {
        }

        Assert.Single(analyzer.CanceledCompilations);

        // Then invoke analysis without cancellation token, and verify non-cancelled diagnostic.
        var diagnosticsMap = await DiagnosticComputer.GetDiagnosticsAsync(document, project, Checksum.Null, ideAnalyzerOptions, span: null,
            analyzerIds, kind, diagnosticAnalyzerInfoCache, workspace.Services, isExplicit: false, reportSuppressedDiagnostics: false,
            logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);
        var builder = diagnosticsMap.Diagnostics.Single().diagnosticMap;
        var diagnostic = kind == AnalysisKind.Syntax ? builder.Syntax.Single().Item2.Single() : builder.Semantic.Single().Item2.Single();
        Assert.Equal(CancellationTestAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Theory, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1909806")]
    [CombinatorialData]
    internal async Task TestGeneratorProducedDiagnostics(bool fullSolutionAnalysis, bool analyzeProject, TestHost testHost)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("// This file will get a diagnostic", composition: s_featuresCompositionWithMockDiagnosticUpdateSourceRegistrationService.WithTestHostParts(testHost));

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var generator = new DiagnosticProducingGenerator(c => Location.Create(c.Compilation.SyntaxTrees.Single(), new TextSpan(0, 10)));
        Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(new TestGeneratorReference(generator)).Solution));

        var project = workspace.CurrentSolution.Projects.Single();
        var document = project.Documents.Single();

        globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp,
            fullSolutionAnalysis ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.OpenFiles);

        // If we aren't testing FSA or analyzing document diagnostics, then open the file.
        if (!fullSolutionAnalysis || !analyzeProject)
        {
            workspace.OpenDocument(document.Id);
        }

        var service = Assert.IsType<DiagnosticAnalyzerService>(workspace.GetService<IDiagnosticAnalyzerService>());

        var incrementalAnalyzer = service.CreateIncrementalAnalyzer(workspace);
        var diagnostics = await incrementalAnalyzer.ForceAnalyzeProjectAsync(project, CancellationToken.None);

        Assert.NotEmpty(diagnostics);
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
        foreach (var diagnostic in diagnostics)
        {
            syntax |= diagnostic.Id == Analyzer.s_syntaxRule.Id;
            semantic |= diagnostic.Id == Analyzer.s_semanticRule.Id;
        }

        return (syntax, semantic);
    }

    private static (bool, bool) CompilerAnalyzerResultSetter(bool syntax, bool semantic, ImmutableArray<DiagnosticData> diagnostics)
    {
        syntax |= diagnostics.Any(d => d.Properties["Origin"] == "Syntactic");
        semantic |= diagnostics.Any(d => d.Properties["Origin"] != "Syntactic");

        return (syntax, semantic);
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

    //private class OpenFileOnlyAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    //{
    //    internal static readonly DiagnosticDescriptor s_syntaxRule = new DiagnosticDescriptor("syntax", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true);

    //    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_syntaxRule);

    //    public override void Initialize(AnalysisContext context)
    //        => context.RegisterSyntaxTreeAction(c => c.ReportDiagnostic(Diagnostic.Create(s_syntaxRule, c.Tree.GetRoot().GetLocation())));

    //    public DiagnosticAnalyzerCategory GetAnalyzerCategory()
    //        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    //    public bool IsHighPriority => false;
    //}

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
