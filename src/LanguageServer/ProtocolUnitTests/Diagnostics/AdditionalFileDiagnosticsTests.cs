// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;
public sealed class AdditionalFileDiagnosticsTests : AbstractPullDiagnosticTestsBase
{
    public AdditionalFileDiagnosticsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsReportsAdditionalFileDiagnostic(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\C.cs"></Document>
                    <AdditionalDocument FilePath="C:\Test.txt"></AdditionalDocument>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.Equal(
        [
            @"C:\C.cs: []",
            @$"C:\Test.txt: [{MockAdditionalFileDiagnosticAnalyzer.Id}]",
            @"C:\CSProj1.csproj: []"
        ], results.Select(r => $"{r.Uri.GetRequiredParsedUri().LocalPath}: [{string.Join(", ", r.Diagnostics!.Select(d => d.Code?.Value?.ToString()))}]"));

        // Asking again should give us back an unchanged diagnostic.
        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Empty(results2);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithRemovedAdditionalFile(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\C.cs"></Document>
                    <AdditionalDocument FilePath="C:\Test.txt"></AdditionalDocument>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        Assert.Equal(3, results.Length);

        AssertEx.Empty(results[0].Diagnostics);
        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[1].Diagnostics!.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[1].Uri.GetRequiredParsedUri().LocalPath);
        AssertEx.Empty(results[2].Diagnostics);

        var initialSolution = testLspServer.GetCurrentSolution();
        var newSolution = initialSolution.RemoveAdditionalDocument(initialSolution.Projects.Single().AdditionalDocumentIds.Single());
        await testLspServer.TestWorkspace.ChangeSolutionAsync(newSolution);

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // We should get a single report for the removed additional file, the rest are unchanged and do not report.
        Assert.Equal(1, results2.Length);
        Assert.Equal(useVSDiagnostics ? null : [], results2[0].Diagnostics);
        Assert.Null(results2[0].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithAdditionalFileInMultipleProjects(bool mutatingLspWorkspace)
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs"></Document>
                    <AdditionalDocument FilePath="C:\Test.txt"></AdditionalDocument>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\B.cs"></Document>
                    <AdditionalDocument FilePath="C:\Test.txt"></AdditionalDocument>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true);
        Assert.Equal(6, results.Length);

        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[1].Diagnostics!.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[1].Uri.GetRequiredParsedUri().LocalPath);
        Assert.Equal("CSProj1", ((LSP.VSDiagnostic)results[1].Diagnostics!.Single()).Projects!.First().ProjectName);
        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[4].Diagnostics!.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[4].Uri.GetRequiredParsedUri().LocalPath);
        Assert.Equal("CSProj2", ((LSP.VSDiagnostic)results[4].Diagnostics!.Single()).Projects!.First().ProjectName);

        // Asking again should give us back an unchanged diagnostic.
        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        AssertEx.Empty(results2);
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(MockAdditionalFileDiagnosticAnalyzer));

    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        => new(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, [DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp), new MockAdditionalFileDiagnosticAnalyzer()]));

    [DiagnosticAnalyzer(LanguageNames.CSharp), PartNotDiscoverable]
    private sealed class MockAdditionalFileDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "MockAdditionalDiagnostic";
        private readonly DiagnosticDescriptor _descriptor = new(Id, "MockAdditionalDiagnostic", "MockAdditionalDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "https://github.com/dotnet/roslyn");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => [_descriptor];

        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);

        public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            => context.RegisterAdditionalFileAction(AnalyzeCompilation);

        public void AnalyzeCompilation(AdditionalFileAnalysisContext context)
            => context.ReportDiagnostic(Diagnostic.Create(_descriptor,
                location: Location.Create(context.AdditionalFile.Path, Text.TextSpan.FromBounds(0, 0), new Text.LinePositionSpan(new Text.LinePosition(0, 0), new Text.LinePosition(0, 0))), "args"));
    }
}
