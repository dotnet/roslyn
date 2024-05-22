// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.CodeStyle;
using Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class VisualStudioDiagnosticAnalyzerExecutorTests
    {
        [Fact]
        public async Task TestCSharpAnalyzerOptions()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";

            using var workspace = CreateWorkspace(LanguageNames.CSharp, code);
            var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);
            var analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType,
                IdeAnalyzerOptions.GetDefault(workspace.Services.SolutionServices.GetLanguageServices(LanguageNames.CSharp)));

            var diagnostics = analyzerResult.GetDocumentDiagnostics(analyzerResult.DocumentIds.First(), AnalysisKind.Semantic);
            Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
            Assert.Equal(DiagnosticSeverity.Hidden, diagnostics[0].Severity);

            var ideOptions = new IdeAnalyzerOptions()
            {
                CleanCodeGenerationOptions = new()
                {
                    GenerationOptions = CSharpCodeGenerationOptions.Default,
                    CleanupOptions = new()
                    {
                        FormattingOptions = CSharpSyntaxFormattingOptions.Default,
                        SimplifierOptions = new CSharpSimplifierOptions()
                        {
                            VarWhenTypeIsApparent = new CodeStyleOption2<bool>(false, NotificationOption2.Suggestion)
                        }
                    }
                }
            };

            analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType, ideOptions);

            diagnostics = analyzerResult.GetDocumentDiagnostics(analyzerResult.DocumentIds.First(), AnalysisKind.Semantic);
            Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
            Assert.Equal(DiagnosticSeverity.Info, diagnostics[0].Severity);
        }

        [Fact]
        public async Task TestVisualBasicAnalyzerOptions()
        {
            var code = @"Class Test
    Sub Method()
        Dim b = Nothing
        Dim a = If(b Is Nothing, Nothing, b.ToString())
    End Sub
End Class";

            using var workspace = CreateWorkspace(LanguageNames.VisualBasic, code);
            var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(workspace.Services.SolutionServices.GetLanguageServices(LanguageNames.VisualBasic));

            ideAnalyzerOptions = ideAnalyzerOptions with
            {
                CodeStyleOptions = new VisualBasicIdeCodeStyleOptions()
                {
                    PreferNullPropagation = new CodeStyleOption2<bool>(false, NotificationOption2.Silent)
                }
            };

            var analyzerType = typeof(VisualBasicUseNullPropagationDiagnosticAnalyzer);
            var analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType, ideAnalyzerOptions);

            Assert.True(analyzerResult.IsEmpty);

            ideAnalyzerOptions = ideAnalyzerOptions with
            {
                CodeStyleOptions = new VisualBasicIdeCodeStyleOptions()
                {
                    PreferNullPropagation = new CodeStyleOption2<bool>(true, NotificationOption2.Error)
                }
            };

            analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType, ideAnalyzerOptions);

            var diagnostics = analyzerResult.GetDocumentDiagnostics(analyzerResult.DocumentIds.First(), AnalysisKind.Semantic);
            Assert.Equal(IDEDiagnosticIds.UseNullPropagationDiagnosticId, diagnostics[0].Id);
        }

        [Fact]
        public async Task TestCancellation()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = CreateWorkspace(LanguageNames.CSharp, code);
            var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(workspace.Services.SolutionServices.GetLanguageServices(LanguageNames.CSharp));

            var analyzerType = typeof(MyAnalyzer);

            for (var i = 0; i < 5; i++)
            {
                var source = new CancellationTokenSource();

                try
                {
                    var task = Task.Run(() => AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType, ideAnalyzerOptions, source.Token));

                    // wait random milli-second
                    var random = new Random(Environment.TickCount);
                    var next = random.Next(1000);
                    await Task.Delay(next);

                    source.Cancel();

                    // let it throw
                    var result = await task;
                }
                catch (Exception ex)
                {
                    // only cancellation is expected
                    Assert.True(ex is OperationCanceledException, $"cancellationToken : {source.Token.IsCancellationRequested}/r/n{ex}");
                }
            }
        }

        [Fact]
        public async Task TestHostAnalyzers_OutOfProc()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";
            using var workspace = CreateWorkspace(LanguageNames.CSharp, code);
            var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);

            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(workspace.Services.SolutionServices.GetLanguageServices(LanguageNames.CSharp));
            workspace.GlobalOptions.SetGlobalOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion));

            // run analysis
            var project = workspace.CurrentSolution.Projects.First();

            var runner = CreateAnalyzerRunner();

            var compilationWithAnalyzers = (await project.GetCompilationAsync()).WithAnalyzers(
                analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray(),
                new WorkspaceAnalyzerOptions(project.AnalyzerOptions, ideAnalyzerOptions));

            var result = await runner.AnalyzeProjectAsync(project, compilationWithAnalyzers, logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);
            var analyzerResult = result.AnalysisResult[compilationWithAnalyzers.Analyzers[0]];

            // check result
            var diagnostics = analyzerResult.GetDocumentDiagnostics(analyzerResult.DocumentIds.First(), AnalysisKind.Semantic);
            Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
        }

        [Fact]
        public async Task TestDuplicatedAnalyzers()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";

            using var workspace = CreateWorkspace(LanguageNames.CSharp, code);
            var analyzerType = typeof(DuplicateAnalyzer);
            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());

            // add host analyzer as global assets
            var remotableDataService = workspace.Services.GetService<ISolutionAssetStorageProvider>();
            var serializer = workspace.Services.GetRequiredService<ISerializerService>();

            // run analysis
            var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(analyzerReference);

            var runner = CreateAnalyzerRunner();
            var analyzers = analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray();

            var ideAnalyzerOptions = IdeAnalyzerOptions.GetDefault(project.Services);

            var compilationWithAnalyzers = (await project.GetCompilationAsync())
                .WithAnalyzers(analyzers, new WorkspaceAnalyzerOptions(project.AnalyzerOptions, ideAnalyzerOptions));

            var result = await runner.AnalyzeProjectAsync(project, compilationWithAnalyzers,
                logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);

            var analyzerResult = result.AnalysisResult[compilationWithAnalyzers.Analyzers[0]];

            // check result
            var diagnostics = analyzerResult.GetDocumentDiagnostics(analyzerResult.DocumentIds.First(), AnalysisKind.Syntax);
            Assert.Equal("test", diagnostics[0].Id);
        }

        private static InProcOrRemoteHostAnalyzerRunner CreateAnalyzerRunner()
            => new(enabled: true, new DiagnosticAnalyzerInfoCache());

        private static async Task<DiagnosticAnalysisResult> AnalyzeAsync(TestWorkspace workspace, ProjectId projectId, Type analyzerType, IdeAnalyzerOptions ideOptions, CancellationToken cancellationToken = default)
        {
            var executor = CreateAnalyzerRunner();

            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());
            var project = workspace.CurrentSolution.GetProject(projectId).AddAnalyzerReference(analyzerReference);

            var analyzerDriver = (await project.GetCompilationAsync()).WithAnalyzers(
                    analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray(),
                    new WorkspaceAnalyzerOptions(project.AnalyzerOptions, ideOptions));

            var result = await executor.AnalyzeProjectAsync(
                project, analyzerDriver, logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken);

            return result.AnalysisResult[analyzerDriver.Analyzers[0]];
        }

        private static TestWorkspace CreateWorkspace(string language, string code, ParseOptions options = null)
        {
            var composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(TestHost.OutOfProcess);

            var workspace = (language == LanguageNames.CSharp)
                ? TestWorkspace.CreateCSharp(code, parseOptions: options, composition: composition)
                : TestWorkspace.CreateVisualBasic(code, parseOptions: options, composition: composition);

            workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);
            workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, BackgroundAnalysisScope.FullSolution);

            return workspace;
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class MyAnalyzer : DiagnosticAnalyzer
        {
            private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
                ImmutableArray.Create(new DiagnosticDescriptor("test", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    for (var i = 0; i < 10000; i++)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(_supportedDiagnostics[0], c.Tree.GetLocation(TextSpan.FromBounds(0, 1))));
                    }
                });
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class DuplicateAnalyzer : DiagnosticAnalyzer
        {
            private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
                ImmutableArray.Create(new DiagnosticDescriptor("test", "test", "test", "test", DiagnosticSeverity.Error, isEnabledByDefault: true));

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    c.ReportDiagnostic(Diagnostic.Create(_supportedDiagnostics[0], c.Tree.GetLocation(TextSpan.FromBounds(0, 1))));
                });
            }
        }
    }
}
