// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;

public sealed class DiagnosticsPullCacheTests(ITestOutputHelper testOutputHelper)
    : AbstractPullDiagnosticTestsBase(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsCallsDiagnosticSourceWhenVersionChanges(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
@"class A { }";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var testProvider = (TestDiagnosticSourceProvider)testLspServer.TestWorkspace.ExportProvider.GetExportedValues<IDiagnosticSourceProvider>().Single(d => d is TestDiagnosticSourceProvider);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal(TestDiagnosticSource.Id, results[0].Diagnostics!.Single().Code);
        Assert.Equal(1, testProvider.DiagnosticsRequestedCount);

        // Make a change that modifies the versions we use to cache.
        await InsertTextAsync(testLspServer, document, position: 0, text: " ");

        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(),
            useVSDiagnostics,
            previousResultId: results[0].ResultId);

        // Assert diagnostics were calculated again even though we got an unchanged result.
        Assert.Null(results.Single().Diagnostics);
        Assert.Equal(results[0].ResultId, results.Single().ResultId);
        Assert.Equal(2, testProvider.DiagnosticsRequestedCount);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsCallsDiagnosticSourceWhenGlobalVersionChanges(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
@"class A { }";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var testProvider = (TestDiagnosticSourceProvider)testLspServer.TestWorkspace.ExportProvider.GetExportedValues<IDiagnosticSourceProvider>().Single(d => d is TestDiagnosticSourceProvider);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal(TestDiagnosticSource.Id, results[0].Diagnostics!.Single().Code);
        Assert.Equal(1, testProvider.DiagnosticsRequestedCount);

        // Make a global version change
        var refresher = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IDiagnosticsRefresher>();
        refresher.RequestWorkspaceRefresh();

        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(),
            useVSDiagnostics,
            previousResultId: results[0].ResultId);

        // Assert diagnostics were calculated again even though we got an unchanged result.
        Assert.Null(results.Single().Diagnostics);
        Assert.Equal(results[0].ResultId, results.Single().ResultId);
        Assert.Equal(2, testProvider.DiagnosticsRequestedCount);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsDoesNotCallDiagnosticSourceWhenVersionSame(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
@"class A { }";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var testProvider = (TestDiagnosticSourceProvider)testLspServer.TestWorkspace.ExportProvider.GetExportedValues<IDiagnosticSourceProvider>().Single(d => d is TestDiagnosticSourceProvider);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal(TestDiagnosticSource.Id, results[0].Diagnostics!.Single().Code);
        Assert.Equal(1, testProvider.DiagnosticsRequestedCount);

        // Make another request without modifying anything and assert we did not re-calculate anything.
        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(),
            useVSDiagnostics,
            previousResultId: results[0].ResultId);

        // Assert diagnostics were not recalculated.
        Assert.Null(results.Single().Diagnostics);
        Assert.Equal(results[0].ResultId, results.Single().ResultId);
        Assert.Equal(1, testProvider.DiagnosticsRequestedCount);
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestDiagnosticSourceProvider));

    private sealed class TestDiagnosticSource(Document document, TestDiagnosticSourceProvider provider) : AbstractDocumentDiagnosticSource<Document>(document)
    {
        public const string Id = "Id";

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref provider.DiagnosticsRequestedCount);
            return [new DiagnosticData(Id, category: "category", context.Document!.Name, DiagnosticSeverity.Error, DiagnosticSeverity.Error,
                isEnabledByDefault: true, warningLevel: 0, [], ImmutableDictionary<string, string?>.Empty,context.Document!.Project.Id,
                new DiagnosticDataLocation(new FileLinePositionSpan(context.Document!.FilePath!, new Text.LinePosition(0, 0), new Text.LinePosition(0, 0))))];
        }
    }

    [Export(typeof(IDiagnosticSourceProvider)), Shared, PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestDiagnosticSourceProvider() : IDiagnosticSourceProvider
    {
        public bool IsDocument => true;

        public string Name => nameof(TestDiagnosticSource);

        public int DiagnosticsRequestedCount = 0;

        public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return [new TestDiagnosticSource(context.Document!, this)];
        }

        public bool IsEnabled(LSP.ClientCapabilities clientCapabilities)
        {
            return true;
        }
    }
}
