// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;

using DocumentDiagnosticPartialReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>;
using WorkspaceDiagnosticPartialReport = SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>;

public abstract class AbstractPullDiagnosticTestsBase(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
        builder.Add(LanguageNames.CSharp,
        [
            DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp),
            new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer(),
            new CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer(),
            new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(),
            new CSharpRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer(),
            new CSharpUseImplicitObjectCreationDiagnosticAnalyzer(),
        ]);
        builder.Add(LanguageNames.VisualBasic, [DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.VisualBasic)]);
        builder.Add(InternalLanguageNames.TypeScript, [new MockTypescriptDiagnosticAnalyzer()]);
        return new(builder.ToImmutableDictionary());
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(MockTypescriptDiagnosticAnalyzer));

    private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunGetWorkspacePullDiagnosticsAsync(
        TestLspServer testLspServer,
        bool useVSDiagnostics,
        ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
        bool useProgress = false,
        bool includeTaskListItems = false,
        string? category = null,
        bool triggerConnectionClose = true)
    {
        var optionService = testLspServer.TestWorkspace.GetService<IGlobalOptionService>();
        optionService.SetGlobalOption(TaskListOptionsStorage.ComputeTaskListItemsForClosedFiles, includeTaskListItems);

        if (useVSDiagnostics)
        {
            return await RunVSGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults, useProgress, category, triggerConnectionClose);
        }
        else
        {
            return await RunPublicGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults, useProgress, category, triggerConnectionClose);
        }
    }

    private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunVSGetWorkspacePullDiagnosticsAsync(
        TestLspServer testLspServer,
        ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults,
        bool useProgress,
        string? category,
        bool triggerConnectionClose)
    {
        await testLspServer.WaitForDiagnosticsAsync();

        BufferedProgress<VSInternalWorkspaceDiagnosticReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalWorkspaceDiagnosticReport[]>(null) : null;
        var diagnosticsTask = testLspServer.ExecuteRequestAsync<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]>(
            VSInternalMethods.WorkspacePullDiagnosticName,
            CreateWorkspaceDiagnosticParams(previousResults, progress, category),
            CancellationToken.None).ConfigureAwait(false);

        if (triggerConnectionClose)
        {
            // Workspace diagnostics wait for a change before closing the connection so we manually tell it to close here to let the test finish.
            var service = testLspServer.GetRequiredLspService<WorkspacePullDiagnosticHandler>();
            service.GetTestAccessor().TriggerConnectionClose();
        }

        var diagnostics = await diagnosticsTask;

        if (useProgress)
        {
            Assert.Null(diagnostics);
            diagnostics = progress!.Value.GetFlattenedValues();
        }

        AssertEx.NotNull(diagnostics);
        return [.. diagnostics.Select(d => new TestDiagnosticResult(d.TextDocument!, d.ResultId!, d.Diagnostics))];
    }

    private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunPublicGetWorkspacePullDiagnosticsAsync(
        TestLspServer testLspServer,
        ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults,
        bool useProgress,
        string? category,
        bool triggerConnectionClose)
    {
        await testLspServer.WaitForDiagnosticsAsync();

        BufferedProgress<WorkspaceDiagnosticPartialReport>? progress = useProgress ? BufferedProgress.Create<WorkspaceDiagnosticPartialReport>(null) : null;
        var diagnosticsTask = testLspServer.ExecuteRequestAsync<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport?>(
            Methods.WorkspaceDiagnosticName,
            CreateProposedWorkspaceDiagnosticParams(previousResults, progress, category),
            CancellationToken.None).ConfigureAwait(false);

        if (triggerConnectionClose)
        {
            // Workspace diagnostics wait for a change before closing the connection so we manually tell it to close here to let the test finish.
            var service = testLspServer.GetRequiredLspService<PublicWorkspacePullDiagnosticsHandler>();
            service.GetTestAccessor().TriggerConnectionClose();
        }

        var returnedResult = await diagnosticsTask;

        if (useProgress)
        {
            Assert.Empty(returnedResult!.Items);
            var progressValues = progress!.Value.GetValues();
            Assert.NotNull(progressValues);
            return [.. progressValues.SelectMany(value => value.Match(v => v.Items, v => v.Items)).Select(diagnostics => ConvertWorkspaceDiagnosticResult(diagnostics))];

        }

        AssertEx.NotNull(returnedResult);
        return [.. returnedResult.Items.Select(diagnostics => ConvertWorkspaceDiagnosticResult(diagnostics))];
    }

    private static WorkspaceDiagnosticParams CreateProposedWorkspaceDiagnosticParams(
            ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults,
            IProgress<WorkspaceDiagnosticPartialReport>? progress,
            string? category)
    {
        var previousResultsLsp = previousResults?.Select(r => new PreviousResultId
        {
            Uri = r.identifier.DocumentUri,
            Value = r.resultId
        }).ToArray() ?? [];
        return new WorkspaceDiagnosticParams
        {
            PreviousResultId = previousResultsLsp,
            PartialResultToken = progress,
            Identifier = category
        };
    }

    private static TestDiagnosticResult ConvertWorkspaceDiagnosticResult(SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport> workspaceReport)
    {
        if (workspaceReport.Value is WorkspaceFullDocumentDiagnosticReport fullReport)
        {
            return new TestDiagnosticResult(new TextDocumentIdentifier { DocumentUri = fullReport.Uri }, fullReport.ResultId, fullReport.Items);
        }
        else
        {
            var unchangedReport = (WorkspaceUnchangedDocumentDiagnosticReport)workspaceReport.Value!;
            return new TestDiagnosticResult(new TextDocumentIdentifier { DocumentUri = unchangedReport.Uri }, unchangedReport.ResultId, null);
        }
    }

    private protected static Task CloseDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.CloseDocumentAsync(document.GetURI());

    private protected static ImmutableArray<(string resultId, TextDocumentIdentifier identifier)> CreateDiagnosticParamsFromPreviousReports(ImmutableArray<TestDiagnosticResult> results)
    {
        // If there was no resultId provided in the response, we cannot create previous results for it.
        return results.SelectAsArray(r => r.ResultId != null, r => (r.ResultId!, r.TextDocument));
    }

    private protected static VSInternalDocumentDiagnosticsParams CreateDocumentDiagnosticParams(
        VSTextDocumentIdentifier vsTextDocumentIdentifier,
        string? previousResultId = null,
        IProgress<VSInternalDiagnosticReport[]>? progress = null,
        string? category = null)
    {
        return new VSInternalDocumentDiagnosticsParams
        {
            TextDocument = vsTextDocumentIdentifier,
            PreviousResultId = previousResultId,
            PartialResultToken = progress,
            QueryingDiagnosticKind = category == null ? null : new(category),
        };
    }

    private protected static VSInternalWorkspaceDiagnosticsParams CreateWorkspaceDiagnosticParams(
        ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
        IProgress<VSInternalWorkspaceDiagnosticReport[]>? progress = null,
        string? category = null)
    {
        return new VSInternalWorkspaceDiagnosticsParams
        {
            PreviousResults = previousResults?.Select(r => new VSInternalDiagnosticParams { PreviousResultId = r.resultId, TextDocument = r.identifier }).ToArray(),
            PartialResultToken = progress,
            QueryingDiagnosticKind = category == null ? null : new(category),
        };
    }

    private protected static async Task InsertTextAsync(
        TestLspServer testLspServer,
        Document document,
        int position,
        string text)
    {
        var sourceText = await document.GetTextAsync();
        var lineInfo = sourceText.Lines.GetLinePositionSpan(new TextSpan(position, 0));

        await testLspServer.InsertTextAsync(document.GetURI(), (lineInfo.Start.Line, lineInfo.Start.Character, text));
    }

    private protected static Task OpenDocumentAsync(TestLspServer testLspServer, TextDocument document) => testLspServer.OpenDocumentAsync(document.GetURI());

    private protected static Task<ImmutableArray<TestDiagnosticResult>> RunGetDocumentPullDiagnosticsAsync(
        TestLspServer testLspServer,
        DocumentUri uri,
        bool useVSDiagnostics,
        string? previousResultId = null,
        bool useProgress = false,
        string? category = null)
    {
        return RunGetDocumentPullDiagnosticsAsync(testLspServer, new VSTextDocumentIdentifier { DocumentUri = uri }, useVSDiagnostics, previousResultId, useProgress, category);
    }

    private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunGetDocumentPullDiagnosticsAsync(
        TestLspServer testLspServer,
        VSTextDocumentIdentifier vsTextDocumentIdentifier,
        bool useVSDiagnostics,
        string? previousResultId = null,
        bool useProgress = false,
        string? category = null)
    {
        await testLspServer.WaitForDiagnosticsAsync();

        if (useVSDiagnostics)
        {
            Assert.False(category == PublicDocumentNonLocalDiagnosticSourceProvider.NonLocal, "NonLocalDiagnostics are only supported for public DocumentPullHandler");
            BufferedProgress<VSInternalDiagnosticReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalDiagnosticReport[]>(null) : null;
            var diagnostics = await testLspServer.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(
                VSInternalMethods.DocumentPullDiagnosticName,
                CreateDocumentDiagnosticParams(vsTextDocumentIdentifier, previousResultId, progress, category),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(diagnostics);
                diagnostics = progress!.Value.GetFlattenedValues();
            }

            AssertEx.NotNull(diagnostics);
            return [.. diagnostics.Select(d => new TestDiagnosticResult(vsTextDocumentIdentifier, d.ResultId!, d.Diagnostics))];
        }
        else
        {
            BufferedProgress<DocumentDiagnosticPartialReport>? progress = useProgress ? BufferedProgress.Create<DocumentDiagnosticPartialReport>(null) : null;
            var diagnostics = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticParams, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>(
                Methods.TextDocumentDiagnosticName,
                CreateProposedDocumentDiagnosticParams(vsTextDocumentIdentifier, previousResultId, category, progress),
                CancellationToken.None).ConfigureAwait(false);
            if (useProgress)
            {
                Assert.IsType<FullDocumentDiagnosticReport>(diagnostics);
                Assert.Empty(diagnostics.First.Items);
                AssertEx.NotNull(progress);
                diagnostics = progress.Value.GetValues()!.Single().First;
            }

            if (diagnostics.Value is UnchangedDocumentDiagnosticReport)
            {
                // The public LSP spec returns different types when unchanged in contrast to VS which just returns null diagnostic array.
                return [new TestDiagnosticResult(vsTextDocumentIdentifier, diagnostics.Second.ResultId!, null)];
            }
            else
            {
                return [new TestDiagnosticResult(vsTextDocumentIdentifier, diagnostics.First.ResultId!, diagnostics.First.Items)];
            }
        }

        static DocumentDiagnosticParams CreateProposedDocumentDiagnosticParams(
            VSTextDocumentIdentifier vsTextDocumentIdentifier,
            string? previousResultId,
            string? category,
            IProgress<DocumentDiagnosticPartialReport>? progress)
        {
            return new DocumentDiagnosticParams
            {
                Identifier = category,
                PreviousResultId = previousResultId,
                PartialResultToken = progress,
                TextDocument = vsTextDocumentIdentifier,
            };
        }
    }

    private protected Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string markup, bool mutatingLspWorkspace, BackgroundAnalysisScope analyzerDiagnosticsScope, bool useVSDiagnostics, CompilerDiagnosticsScope? compilerDiagnosticsScope = null, IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
        => CreateTestLspServerAsync(markup, mutatingLspWorkspace, GetInitializationOptions(analyzerDiagnosticsScope, compilerDiagnosticsScope, useVSDiagnostics, additionalAnalyzers: additionalAnalyzers));

    private protected Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string[] markups, bool mutatingLspWorkspace, BackgroundAnalysisScope analyzerDiagnosticsScope, bool useVSDiagnostics, CompilerDiagnosticsScope? compilerDiagnosticsScope = null, IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
        => CreateTestLspServerAsync(markups, mutatingLspWorkspace, GetInitializationOptions(analyzerDiagnosticsScope, compilerDiagnosticsScope, useVSDiagnostics, additionalAnalyzers: additionalAnalyzers));

    private protected Task<TestLspServer> CreateTestWorkspaceFromXmlAsync(string xmlMarkup, bool mutatingLspWorkspace, BackgroundAnalysisScope analyzerDiagnosticsScope, bool useVSDiagnostics, CompilerDiagnosticsScope? compilerDiagnosticsScope = null, IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
        => CreateXmlTestLspServerAsync(xmlMarkup, mutatingLspWorkspace, initializationOptions: GetInitializationOptions(analyzerDiagnosticsScope, compilerDiagnosticsScope, useVSDiagnostics, additionalAnalyzers: additionalAnalyzers));

    private protected static InitializationOptions GetInitializationOptions(
        BackgroundAnalysisScope analyzerDiagnosticsScope,
        CompilerDiagnosticsScope? compilerDiagnosticsScope,
        bool useVSDiagnostics,
        WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer,
        string[]? sourceGeneratedMarkups = null,
        IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
    {
        // If no explicit compiler diagnostics scope has been provided, match it with the provided analyzer diagnostics scope
        compilerDiagnosticsScope ??= analyzerDiagnosticsScope switch
        {
            BackgroundAnalysisScope.None => CompilerDiagnosticsScope.None,
            BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics => CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics,
            BackgroundAnalysisScope.OpenFiles => CompilerDiagnosticsScope.OpenFiles,
            BackgroundAnalysisScope.FullSolution => CompilerDiagnosticsScope.FullSolution,
            _ => throw ExceptionUtilities.UnexpectedValue(analyzerDiagnosticsScope),
        };

        return new InitializationOptions
        {
            ClientCapabilities = useVSDiagnostics ? CapabilitiesWithVSExtensions : new LSP.ClientCapabilities(),
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, analyzerDiagnosticsScope);
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, analyzerDiagnosticsScope);
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript, analyzerDiagnosticsScope);
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, compilerDiagnosticsScope.Value);
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.VisualBasic, compilerDiagnosticsScope.Value);
                globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, InternalLanguageNames.TypeScript, compilerDiagnosticsScope.Value);
            },
            ServerKind = serverKind,
            SourceGeneratedMarkups = sourceGeneratedMarkups ?? [],
            AdditionalAnalyzers = additionalAnalyzers
        };
    }

    /// <summary>
    /// Helper type to store unified LSP diagnostic results.
    /// Diagnostics are null when unchanged.
    /// </summary>
    private protected sealed record TestDiagnosticResult(TextDocumentIdentifier TextDocument, string? ResultId, LSP.Diagnostic[]? Diagnostics)
    {
        public DocumentUri Uri { get; } = TextDocument.DocumentUri;
    }

    [DiagnosticAnalyzer(InternalLanguageNames.TypeScript), PartNotDiscoverable]
    private sealed class MockTypescriptDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Descriptor = new(
            "TS01", "TS error", "TS error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument document, SyntaxTree? tree, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create(
                Diagnostic.Create(Descriptor, Location.Create(document.FilePath!, default, default))));
        }
    }
}
