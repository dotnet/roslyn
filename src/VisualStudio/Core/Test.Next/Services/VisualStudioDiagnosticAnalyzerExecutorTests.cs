// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class VisualStudioDiagnosticAnalyzerExecutorTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCSharpAnalyzerOptions()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";

            using (var workspace = CreateWorkspace(LanguageNames.CSharp, code))
            {
                var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);
                var analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType);

                Assert.True(analyzerResult.IsEmpty);

                // set option
                workspace.Options = workspace.Options.WithChangedOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion));
                analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType);

                var diagnostics = analyzerResult.SemanticLocals[analyzerResult.DocumentIds.First()];
                Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestVisualBasicAnalyzerOptions()
        {
            var code = @"Class Test
    Sub Method()
        Dim b = Nothing
        Dim a = If(b Is Nothing, Nothing, b.ToString())
    End Sub
End Class";

            using (var workspace = CreateWorkspace(LanguageNames.VisualBasic, code))
            {
                // set option
                workspace.Options = workspace.Options.WithChangedOption(CodeStyleOptions.PreferNullPropagation, LanguageNames.VisualBasic, new CodeStyleOption<bool>(false, NotificationOption.None));

                var analyzerType = typeof(VisualBasicUseNullPropagationDiagnosticAnalyzer);
                var analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType);

                Assert.True(analyzerResult.IsEmpty);

                // set option
                workspace.Options = workspace.Options.WithChangedOption(CodeStyleOptions.PreferNullPropagation, LanguageNames.VisualBasic, new CodeStyleOption<bool>(true, NotificationOption.Error));
                analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType);

                var diagnostics = analyzerResult.SemanticLocals[analyzerResult.DocumentIds.First()];
                Assert.Equal(IDEDiagnosticIds.UseNullPropagationDiagnosticId, diagnostics[0].Id);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCancellation()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = CreateWorkspace(LanguageNames.CSharp, code))
            {
                var analyzerType = typeof(MyAnalyzer);

                for (var i = 0; i < 5; i++)
                {
                    var source = new CancellationTokenSource();

                    try
                    {
                        var task = Task.Run(() => AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType, source.Token));

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
                        Assert.True(ex is OperationCanceledException, $"cancellationToken : {source.Token.IsCancellationRequested}/r/n{ex.ToString()}");
                    }
                }
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestHostAnalyzers()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";

            using (var workspace = CreateWorkspace(LanguageNames.CSharp, code))
            {
                var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);
                var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());

                // add host analyzer as global assets
                var snapshotService = workspace.Services.GetService<IRemotableDataService>();
                var assetBuilder = new CustomAssetBuilder(workspace);

                var asset = assetBuilder.Build(analyzerReference, CancellationToken.None);
                snapshotService.AddGlobalAsset(analyzerReference, asset, CancellationToken.None);

                var client = await workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(CancellationToken.None);
                await client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService, workspace.CurrentSolution,
                    nameof(IRemoteHostService.SynchronizeGlobalAssetsAsync), (object)(new Checksum[] { asset.Checksum }), CancellationToken.None);

                // set option
                workspace.Options = workspace.Options.WithChangedOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion));

                // run analysis
                var project = workspace.CurrentSolution.Projects.First();

                var executor = (ICodeAnalysisDiagnosticAnalyzerExecutor)new DiagnosticAnalyzerExecutor(new MyUpdateSource(workspace)).CreateService(workspace.Services);
                var analyzerDriver = (await project.GetCompilationAsync()).WithAnalyzers(
                        analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray(),
                        new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution));

                var result = await executor.AnalyzeAsync(analyzerDriver, project, forcedAnalysis: false, cancellationToken: CancellationToken.None);

                var analyzerResult = result.AnalysisResult[analyzerDriver.Analyzers[0]];

                // check result
                var diagnostics = analyzerResult.SemanticLocals[analyzerResult.DocumentIds.First()];
                Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
            }
        }

        private static async Task<DiagnosticAnalysisResult> AnalyzeAsync(TestWorkspace workspace, ProjectId projectId, Type analyzerType, CancellationToken cancellationToken = default(CancellationToken))
        {
            var executor = (ICodeAnalysisDiagnosticAnalyzerExecutor)new DiagnosticAnalyzerExecutor(new MyUpdateSource(workspace)).CreateService(workspace.Services);

            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());
            var project = workspace.CurrentSolution.GetProject(projectId).AddAnalyzerReference(analyzerReference);

            var analyzerDriver = (await project.GetCompilationAsync()).WithAnalyzers(
                    analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray(),
                    new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution));

            var result = await executor.AnalyzeAsync(analyzerDriver, project, forcedAnalysis: true, cancellationToken: cancellationToken);

            return result.AnalysisResult[analyzerDriver.Analyzers[0]];
        }

        private TestWorkspace CreateWorkspace(string language, string code, ParseOptions options = null)
        {
            var workspace = (language == LanguageNames.CSharp) ?
                TestWorkspace.CreateCSharp(code, parseOptions: options, exportProvider: TestHostServices.SharedExportProvider) :
                TestWorkspace.CreateVisualBasic(code, parseOptions: options, exportProvider: TestHostServices.SharedExportProvider);

            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                     .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true)
                                     .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, true);

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

        private class MyUpdateSource : AbstractHostDiagnosticUpdateSource
        {
            private readonly Workspace _workspace;

            public MyUpdateSource(Workspace workspace)
            {
                _workspace = workspace;
            }

            public override Workspace Workspace => _workspace;
        }
    }
}
