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
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;
public class AdditionalFileDiagnosticsTests : AbstractPullDiagnosticTestsBase
{
    public AdditionalFileDiagnosticsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsReportsAdditionalFileDiagnostic(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""C:\C.cs""></Document>
        <AdditionalDocument FilePath=""C:\Test.txt""></AdditionalDocument>
    </Project>
</Workspace>";

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.Equal(new[]
        {
            @"C:\C.cs: []",
            @$"C:\Test.txt: [{MockAdditionalFileDiagnosticAnalyzer.Id}]",
            @"C:\CSProj1.csproj: []"
        }, results.Select(r => $"{r.Uri.LocalPath}: [{string.Join(", ", r.Diagnostics.Select(d => d.Code?.Value?.ToString()))}]"));

        // Asking again should give us back an unchanged diagnostic.
        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Null(results2[0].Diagnostics);
        Assert.Null(results2[1].Diagnostics);
        Assert.Equal(results[1].ResultId, results2[1].ResultId);
        Assert.Null(results2[2].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithRemovedAdditionalFile(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""C:\C.cs""></Document>
        <AdditionalDocument FilePath=""C:\Test.txt""></AdditionalDocument>
    </Project>
</Workspace>";

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        Assert.Equal(3, results.Length);

        Assert.Empty(results[0].Diagnostics);
        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[1].Diagnostics.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[1].Uri.LocalPath);
        Assert.Empty(results[2].Diagnostics);

        var initialSolution = testLspServer.GetCurrentSolution();
        var newSolution = initialSolution.RemoveAdditionalDocument(initialSolution.Projects.Single().AdditionalDocumentIds.Single());
        await testLspServer.TestWorkspace.ChangeSolutionAsync(newSolution);

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Equal(3, results2.Length);

        // The first report is the report for the removed additional file.
        Assert.Equal(useVSDiagnostics ? null : Array.Empty<LSP.Diagnostic>(), results2[0].Diagnostics);
        Assert.Null(results2[0].ResultId);

        // The other files should have new results since the solution changed.
        Assert.Empty(results2[1].Diagnostics);
        Assert.NotNull(results2[1].ResultId);
        Assert.Empty(results2[2].Diagnostics);
        Assert.NotNull(results2[2].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithAdditionalFileInMultipleProjects(bool mutatingLspWorkspace)
    {
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""C:\A.cs""></Document>
        <AdditionalDocument FilePath=""C:\Test.txt""></AdditionalDocument>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj2"" FilePath=""C:\CSProj1.csproj"">
        <Document FilePath=""C:\B.cs""></Document>
        <AdditionalDocument FilePath=""C:\Test.txt""></AdditionalDocument>
    </Project>
</Workspace>";

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true);
        Assert.Equal(6, results.Length);

        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[1].Diagnostics.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[1].Uri.LocalPath);
        Assert.Equal("CSProj1", ((LSP.VSDiagnostic)results[1].Diagnostics.Single()).Projects.First().ProjectName);
        Assert.Equal(MockAdditionalFileDiagnosticAnalyzer.Id, results[4].Diagnostics.Single().Code);
        Assert.Equal(@"C:\Test.txt", results[4].Uri.LocalPath);
        Assert.Equal("CSProj2", ((LSP.VSDiagnostic)results[4].Diagnostics.Single()).Projects.First().ProjectName);

        // Asking again should give us back an unchanged diagnostic.
        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, previousResults: CreateDiagnosticParamsFromPreviousReports(results));
        Assert.Equal(results[1].ResultId, results2[1].ResultId);
        Assert.Equal(results[4].ResultId, results2[4].ResultId);
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(MockAdditionalFileDiagnosticAnalyzer));

    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        => new(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(
                DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp),
                new MockAdditionalFileDiagnosticAnalyzer())));

    [DiagnosticAnalyzer(LanguageNames.CSharp), PartNotDiscoverable]
    private class MockAdditionalFileDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "MockAdditionalDiagnostic";
        private readonly DiagnosticDescriptor _descriptor = new(Id, "MockAdditionalDiagnostic", "MockAdditionalDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "https://github.com/dotnet/roslyn");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(_descriptor);

        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);

        public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            => context.RegisterAdditionalFileAction(AnalyzeCompilation);

        public void AnalyzeCompilation(AdditionalFileAnalysisContext context)
            => context.ReportDiagnostic(Diagnostic.Create(_descriptor,
                location: Location.Create(context.AdditionalFile.Path, Text.TextSpan.FromBounds(0, 0), new Text.LinePositionSpan(new Text.LinePosition(0, 0), new Text.LinePosition(0, 0))), "args"));
    }
}
