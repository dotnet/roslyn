// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;

public sealed class AdditionalFileDiagnosticsTests : AbstractPullDiagnosticTestsBase
{
    public AdditionalFileDiagnosticsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2531252")]
    public async Task TestDocumentDiagnosticsReportsAdditionalFileDiagnostic(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\C.cs"></Document>
                    <AdditionalDocument FilePath="C:\Test.xaml"></AdditionalDocument>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var additionalDocument = testLspServer.GetCurrentSolution().Projects.Single().AdditionalDocuments.Single();
        await testLspServer.OpenDocumentAsync(additionalDocument.GetURI());

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, additionalDocument.GetURI(), useVSDiagnostics, category: TestAdditionalFileDocumentSourceProvider.DiagnosticSourceProviderName);
        Assert.NotEmpty(results);
        AssertEx.Equal(
        [
            @$"C:\Test.xaml: [{MockAdditionalFileDiagnosticAnalyzer.Id}]",
        ], results.Select(r => $"{r.Uri.GetRequiredParsedUri().LocalPath}: [{string.Join(", ", r.Diagnostics!.Select(d => d.Code?.Value?.ToString()))}]"));
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

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsReportsSourceGeneratorDiagnosticInAdditionalFile(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var additionaFilePath = @"C:\File.razor";
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\C.cs"></Document>
                    <AdditionalDocument FilePath="{additionaFilePath}">Hello</AdditionalDocument>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        // Add a generator to the solution that reports a source generator diagnostic in an additional file.
        var generator = new DiagnosticProducingGenerator(context =>
        {
            return Location.Create(additionaFilePath, TextSpan.FromBounds(0, 1), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 1)));
        });

        testLspServer.TestWorkspace.OnAnalyzerReferenceAdded(
            testLspServer.GetCurrentSolution().Projects.Single().Id,
            new TestGeneratorReference(generator));
        await testLspServer.WaitForSourceGeneratorsAsync();

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.Equal(
        [
            @"C:\C.cs: []",
            @$"C:\File.razor: [{DiagnosticProducingGenerator.Descriptor.Id}, {MockAdditionalFileDiagnosticAnalyzer.Id}]",
            @"C:\CSProj1.csproj: []"
        ], results.Select(r => $"{r.Uri.GetRequiredParsedUri().LocalPath}: [{string.Join(", ", r.Diagnostics!.Select(d => d.Code?.Value?.ToString()))}]"));
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(MockAdditionalFileDiagnosticAnalyzer), typeof(TestAdditionalFileDocumentSourceProvider));

    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        => new(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty.Add(LanguageNames.CSharp, [DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp), new MockAdditionalFileDiagnosticAnalyzer()]));

    [DiagnosticAnalyzer(LanguageNames.CSharp), PartNotDiscoverable]
    private sealed class MockAdditionalFileDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "MockAdditionalDiagnostic";
        internal static readonly DiagnosticDescriptor Descriptor = new(Id, "MockAdditionalDiagnostic", "MockAdditionalDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "https://github.com/dotnet/roslyn");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => [Descriptor];

        public override void Initialize(AnalysisContext context)
            => context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);

        public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            => context.RegisterAdditionalFileAction(AnalyzeCompilation);

        public void AnalyzeCompilation(AdditionalFileAnalysisContext context)
            => context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                location: Location.Create(context.AdditionalFile.Path, Text.TextSpan.FromBounds(0, 0), new Text.LinePositionSpan(new Text.LinePosition(0, 0), new Text.LinePosition(0, 0))), "args"));
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared, PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestAdditionalFileDocumentSourceProvider() : IDiagnosticSourceProvider
    {
        internal const string DiagnosticSourceProviderName = "TestAdditionalFileSource";

        bool IDiagnosticSourceProvider.IsDocument => true;

        string IDiagnosticSourceProvider.Name => DiagnosticSourceProviderName;

        bool IDiagnosticSourceProvider.IsEnabled(LSP.ClientCapabilities clientCapabilities) => true;

        async ValueTask<ImmutableArray<IDiagnosticSource>> IDiagnosticSourceProvider.CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        {
            if (context.TextDocument is not null and not Document)
            {
                return [new TestAdditionalFileDocumentSource(context.TextDocument)];
            }

            return [];
        }

        private class TestAdditionalFileDocumentSource(TextDocument textDocument) : IDiagnosticSource
        {
            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
            {
                var diagnostic = Diagnostic.Create(MockAdditionalFileDiagnosticAnalyzer.Descriptor,
                    location: Location.Create(context.TextDocument!.FilePath!, Text.TextSpan.FromBounds(0, 0), new Text.LinePositionSpan(new Text.LinePosition(0, 0), new Text.LinePosition(0, 0))), "args");
                return [DiagnosticData.Create(diagnostic, context.TextDocument.Project)];
            }

            public LSP.TextDocumentIdentifier? GetDocumentIdentifier() => new()
            {
                DocumentUri = textDocument.GetURI()
            };

            public ProjectOrDocumentId GetId() => new(textDocument.Id);

            public Project GetProject() => textDocument.Project;

            public string ToDisplayString() => textDocument.ToString()!;
        }
    }
}
