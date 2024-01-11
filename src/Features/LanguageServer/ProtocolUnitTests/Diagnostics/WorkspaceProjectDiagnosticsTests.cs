// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;
public class WorkspaceProjectDiagnosticsTests : AbstractPullDiagnosticTestsBase
{
    public WorkspaceProjectDiagnosticsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsReportsProjectDiagnostic(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(string.Empty, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(2, results.Length);
        Assert.Empty(results[0].Diagnostics);
        Assert.Equal(MockProjectDiagnosticAnalyzer.Id, results[1].Diagnostics.Single().Code);
        Assert.Equal(ProtocolConversions.CreateAbsoluteUri(testLspServer.GetCurrentSolution().Projects.First().FilePath!), results[1].Uri);

        // Asking again should give us back an unchanged diagnostic.
        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Null(results2[0].Diagnostics);
        Assert.Null(results2[1].Diagnostics);
        Assert.Equal(results[1].ResultId, results2[1].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithRemovedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(string.Empty, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(2, results.Length);
        Assert.Empty(results[0].Diagnostics);
        Assert.Equal(MockProjectDiagnosticAnalyzer.Id, results[1].Diagnostics.Single().Code);
        Assert.Equal(ProtocolConversions.CreateAbsoluteUri(testLspServer.GetCurrentSolution().Projects.First().FilePath!), results[1].Uri);

        var initialSolution = testLspServer.GetCurrentSolution();
        var newSolution = initialSolution.RemoveProject(initialSolution.Projects.First().Id);
        await testLspServer.TestWorkspace.ChangeSolutionAsync(newSolution);

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Equal(2, results2.Length);
        Assert.Equal(useVSDiagnostics ? null : Array.Empty<LSP.Diagnostic>(), results2[0].Diagnostics);
        Assert.Null(results2[0].ResultId);
        Assert.Equal(useVSDiagnostics ? null : Array.Empty<LSP.Diagnostic>(), results2[1].Diagnostics);
        Assert.Null(results2[1].ResultId);
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(MockProjectDiagnosticAnalyzer));

    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        => new(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(
            DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp),
            new MockProjectDiagnosticAnalyzer())));

    [DiagnosticAnalyzer(LanguageNames.CSharp), PartNotDiscoverable]
    private class MockProjectDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "MockProjectDiagnostic";
        private readonly DiagnosticDescriptor _descriptor = new(Id, "MockProjectDiagnostic", "MockProjectDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "https://github.com/dotnet/roslyn");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(_descriptor);

        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);

        public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            => context.RegisterCompilationEndAction(AnalyzeCompilation);

        public void AnalyzeCompilation(CompilationAnalysisContext context)
            => context.ReportDiagnostic(Diagnostic.Create(_descriptor, location: null, "args"));
    }
}
