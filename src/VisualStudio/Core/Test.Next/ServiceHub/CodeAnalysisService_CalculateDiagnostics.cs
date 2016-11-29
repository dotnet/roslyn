// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class CalculateDiagnosticsTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAnalyzerOptions()
        {
            var code = @"class Test
{
    void Method()
    {
        var t = new Test();
    }
}";

            using (var workspace = await CreateWorkspaceAsync(code))
            {
                // set option
                workspace.Options = workspace.Options.WithChangedOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion));

                var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);
                var analyzerResult = await AnalyzeAsync(workspace, workspace.CurrentSolution.ProjectIds.First(), analyzerType);

                var diagnostics = analyzerResult.SemanticLocals[analyzerResult.DocumentIds.First()];
                Assert.Equal(IDEDiagnosticIds.UseExplicitTypeDiagnosticId, diagnostics[0].Id);
            }
        }

        private static async Task<DiagnosticAnalysisResult> AnalyzeAsync(TestWorkspace workspace, ProjectId projectId, Type analyzerType)
        {
            var diagnosticService = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>();
            var executor = new VisualStudioDiagnosticAnalyzerExecutor(diagnosticService, new MyUpdateSource(workspace));

            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());
            var project = workspace.CurrentSolution.GetProject(projectId).AddAnalyzerReference(analyzerReference);

            var analyzerDriver = (await project.GetCompilationAsync()).WithAnalyzers(analyzerReference.GetAnalyzers(project.Language).Where(a => a.GetType() == analyzerType).ToImmutableArray());
            var result = await executor.AnalyzeAsync(analyzerDriver, project, CancellationToken.None);

            var analyzerResult = result.AnalysisResult[analyzerDriver.Analyzers[0]];
            return analyzerResult;
        }

        private async Task<TestWorkspace> CreateWorkspaceAsync(string code)
        {
            var workspace = await TestWorkspace.CreateCSharpAsync(code, exportProvider: TestHostServices.SharedExportProvider);

            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                     .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true)
                                     .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, true);

            return workspace;
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

        private class TestAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
