// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    using DocumentDiagnosticPartialReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>;
    using WorkspaceDiagnosticPartialReport = SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>;

    public abstract class AbstractPullDiagnosticTestsBase : AbstractLanguageServerProtocolTests
    {
        protected AbstractPullDiagnosticTestsBase(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private protected override TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
            builder.Add(LanguageNames.CSharp, ImmutableArray.Create(
                DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp),
                new CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer(),
                new CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer(),
                new CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer(),
                new CSharpRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer()));
            builder.Add(LanguageNames.VisualBasic, ImmutableArray.Create(DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.VisualBasic)));
            builder.Add(InternalLanguageNames.TypeScript, ImmutableArray.Create<DiagnosticAnalyzer>(new MockTypescriptDiagnosticAnalyzer()));
            return new(builder.ToImmutableDictionary());
        }

        protected override TestComposition Composition => base.Composition.AddParts(typeof(MockTypescriptDiagnosticAnalyzer));

        private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunGetWorkspacePullDiagnosticsAsync(
            TestLspServer testLspServer,
            bool useVSDiagnostics,
            ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
            bool useProgress = false,
            bool includeTaskListItems = false,
            string? category = null)
        {
            var optionService = testLspServer.TestWorkspace.GetService<IGlobalOptionService>();
            optionService.SetGlobalOption(TaskListOptionsStorage.ComputeTaskListItemsForClosedFiles, includeTaskListItems);

            if (useVSDiagnostics)
            {
                return await RunVSGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults, useProgress, category);
            }
            else
            {
                return await RunPublicGetWorkspacePullDiagnosticsAsync(testLspServer, previousResults, useProgress, triggerConnectionClose: true);
            }
        }

        private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunVSGetWorkspacePullDiagnosticsAsync(
            TestLspServer testLspServer,
            ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
            bool useProgress = false,
            string? category = null)
        {
            await testLspServer.WaitForDiagnosticsAsync();

            BufferedProgress<VSInternalWorkspaceDiagnosticReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalWorkspaceDiagnosticReport[]>(null) : null;
            var diagnostics = await testLspServer.ExecuteRequestAsync<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]>(
                VSInternalMethods.WorkspacePullDiagnosticName,
                CreateWorkspaceDiagnosticParams(previousResults, progress, category),
                CancellationToken.None).ConfigureAwait(false);

            if (useProgress)
            {
                Assert.Null(diagnostics);
                diagnostics = progress!.Value.GetFlattenedValues();
            }

            AssertEx.NotNull(diagnostics);
            return diagnostics.Select(d => new TestDiagnosticResult(d.TextDocument!, d.ResultId!, d.Diagnostics)).ToImmutableArray();
        }

        private protected static async Task<ImmutableArray<TestDiagnosticResult>> RunPublicGetWorkspacePullDiagnosticsAsync(
            TestLspServer testLspServer,
            ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
            bool useProgress = false,
            bool triggerConnectionClose = true)
        {
            await testLspServer.WaitForDiagnosticsAsync();

            BufferedProgress<WorkspaceDiagnosticPartialReport>? progress = useProgress ? BufferedProgress.Create<WorkspaceDiagnosticPartialReport>(null) : null;
            var diagnosticsTask = testLspServer.ExecuteRequestAsync<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport?>(
                Methods.WorkspaceDiagnosticName,
                CreateProposedWorkspaceDiagnosticParams(previousResults, progress),
                CancellationToken.None).ConfigureAwait(false);

            if (triggerConnectionClose)
            {
                // Public spec diagnostics wait for a change before closing the connection so we manually tell it to close here to let the test finish.
                var service = testLspServer.GetRequiredLspService<PublicWorkspacePullDiagnosticsHandler>();
                service.GetTestAccessor().TriggerConnectionClose();
            }

            var returnedResult = await diagnosticsTask;

            if (useProgress)
            {
                Assert.Empty(returnedResult!.Items);
                var progressValues = progress!.Value.GetValues();
                Assert.NotNull(progressValues);
                return progressValues.SelectMany(value => value.Match(v => v.Items, v => v.Items)).Select(diagnostics => ConvertWorkspaceDiagnosticResult(diagnostics)).ToImmutableArray();

            }

            AssertEx.NotNull(returnedResult);
            return returnedResult.Items.Select(diagnostics => ConvertWorkspaceDiagnosticResult(diagnostics)).ToImmutableArray();
        }

        private static WorkspaceDiagnosticParams CreateProposedWorkspaceDiagnosticParams(
                ImmutableArray<(string resultId, TextDocumentIdentifier identifier)>? previousResults = null,
                IProgress<WorkspaceDiagnosticPartialReport>? progress = null)
        {
            var previousResultsLsp = previousResults?.Select(r => new PreviousResultId
            {
                Uri = r.identifier.Uri,
                Value = r.resultId
            }).ToArray() ?? Array.Empty<PreviousResultId>();
            return new WorkspaceDiagnosticParams
            {
                PreviousResultId = previousResultsLsp,
                PartialResultToken = progress
            };
        }

        private static TestDiagnosticResult ConvertWorkspaceDiagnosticResult(SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport> workspaceReport)
        {
            if (workspaceReport.Value is WorkspaceFullDocumentDiagnosticReport fullReport)
            {
                return new TestDiagnosticResult(new TextDocumentIdentifier { Uri = fullReport.Uri }, fullReport.ResultId!, fullReport.Items);
            }
            else
            {
                var unchangedReport = (WorkspaceUnchangedDocumentDiagnosticReport)workspaceReport.Value!;
                return new TestDiagnosticResult(new TextDocumentIdentifier { Uri = unchangedReport.Uri }, unchangedReport.ResultId!, null);
            }
        }

        private protected static Task CloseDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.CloseDocumentAsync(document.GetURI());

        private protected static ImmutableArray<(string resultId, TextDocumentIdentifier identifier)> CreateDiagnosticParamsFromPreviousReports(ImmutableArray<TestDiagnosticResult> results)
        {

            return results.Select(r => (r.ResultId, r.TextDocument)).ToImmutableArray();
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

        private protected static Task OpenDocumentAsync(TestLspServer testLspServer, Document document) => testLspServer.OpenDocumentAsync(document.GetURI());

        private protected static Task<ImmutableArray<TestDiagnosticResult>> RunGetDocumentPullDiagnosticsAsync(
            TestLspServer testLspServer,
            Uri uri,
            bool useVSDiagnostics,
            string? previousResultId = null,
            bool useProgress = false,
            string? category = null)
        {
            return RunGetDocumentPullDiagnosticsAsync(testLspServer, new VSTextDocumentIdentifier { Uri = uri }, useVSDiagnostics, previousResultId, useProgress, category);
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
                return diagnostics.Select(d => new TestDiagnosticResult(vsTextDocumentIdentifier, d.ResultId!, d.Diagnostics)).ToImmutableArray();
            }
            else
            {
                BufferedProgress<DocumentDiagnosticPartialReport>? progress = useProgress ? BufferedProgress.Create<DocumentDiagnosticPartialReport>(null) : null;
                var diagnostics = await testLspServer.ExecuteRequestAsync<DocumentDiagnosticParams, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>?>(
                    Methods.TextDocumentDiagnosticName,
                    CreateProposedDocumentDiagnosticParams(vsTextDocumentIdentifier, previousResultId, progress),
                    CancellationToken.None).ConfigureAwait(false);
                if (useProgress)
                {
                    Assert.Null(diagnostics);
                    diagnostics = progress!.Value.GetValues().Single().First;
                }

                if (diagnostics == null)
                {
                    // The public LSP spec returns null when no diagnostics are available for a document wheres VS returns an empty array.
                    return ImmutableArray<TestDiagnosticResult>.Empty;
                }
                else if (diagnostics.Value.Value is UnchangedDocumentDiagnosticReport)
                {
                    // The public LSP spec returns different types when unchanged in contrast to VS which just returns null diagnostic array.
                    return ImmutableArray.Create(new TestDiagnosticResult(vsTextDocumentIdentifier, diagnostics.Value.Second.ResultId!, null));
                }
                else
                {
                    return ImmutableArray.Create(new TestDiagnosticResult(vsTextDocumentIdentifier, diagnostics.Value.First.ResultId!, diagnostics.Value.First.Items));
                }
            }

            static DocumentDiagnosticParams CreateProposedDocumentDiagnosticParams(
                VSTextDocumentIdentifier vsTextDocumentIdentifier,
                string? previousResultId = null,
                IProgress<DocumentDiagnosticPartialReport>? progress = null)
            {
                return new DocumentDiagnosticParams
                {
                    Identifier = null,
                    PreviousResultId = previousResultId,
                    PartialResultToken = progress,
                    TextDocument = vsTextDocumentIdentifier,
                };
            }
        }

        private protected Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string markup, bool mutatingLspWorkspace, BackgroundAnalysisScope scope, bool useVSDiagnostics, bool pullDiagnostics = true)
            => CreateTestLspServerAsync(markup, mutatingLspWorkspace, GetInitializationOptions(scope, useVSDiagnostics, pullDiagnostics ? DiagnosticMode.LspPull : DiagnosticMode.SolutionCrawlerPush));

        private protected Task<TestLspServer> CreateTestWorkspaceWithDiagnosticsAsync(string[] markups, bool mutatingLspWorkspace, BackgroundAnalysisScope scope, bool useVSDiagnostics, bool pullDiagnostics = true)
            => CreateTestLspServerAsync(markups, mutatingLspWorkspace, GetInitializationOptions(scope, useVSDiagnostics, pullDiagnostics ? DiagnosticMode.LspPull : DiagnosticMode.SolutionCrawlerPush));

        private protected Task<TestLspServer> CreateTestWorkspaceFromXmlAsync(string xmlMarkup, bool mutatingLspWorkspace, BackgroundAnalysisScope scope, bool useVSDiagnostics, bool pullDiagnostics = true)
            => CreateXmlTestLspServerAsync(xmlMarkup, mutatingLspWorkspace, initializationOptions: GetInitializationOptions(scope, useVSDiagnostics, pullDiagnostics ? DiagnosticMode.LspPull : DiagnosticMode.SolutionCrawlerPush));

        private protected static InitializationOptions GetInitializationOptions(
            BackgroundAnalysisScope scope,
            bool useVSDiagnostics,
            DiagnosticMode mode,
            WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer,
            string[]? sourceGeneratedMarkups = null)
        {
            return new InitializationOptions
            {
                ClientCapabilities = useVSDiagnostics ? CapabilitiesWithVSExtensions : new LSP.ClientCapabilities(),
                OptionUpdater = (globalOptions) =>
                {
                    globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope);
                    globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope);
                    globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript, scope);
                    globalOptions.SetGlobalOption(InternalDiagnosticsOptionsStorage.NormalDiagnosticMode, mode);
                    globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.EnableDiagnosticsInSourceGeneratedFiles, true);
                },
                ServerKind = serverKind,
                SourceGeneratedMarkups = sourceGeneratedMarkups ?? Array.Empty<string>()
            };
        }

        /// <summary>
        /// Helper type to store unified LSP diagnostic results.
        /// Diagnostics are null when unchanged.
        /// </summary>
        private protected record TestDiagnosticResult(TextDocumentIdentifier TextDocument, string ResultId, LSP.Diagnostic[]? Diagnostics)
        {
            public Uri Uri { get; } = TextDocument.Uri;
        }

        [DiagnosticAnalyzer(InternalLanguageNames.TypeScript), PartNotDiscoverable]
        private class MockTypescriptDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            "TS01", "TS error", "TS error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
                => SpecializedTasks.EmptyImmutableArray<Diagnostic>();

            public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.FromResult(ImmutableArray.Create(
                    Diagnostic.Create(Descriptor, Location.Create(document.FilePath!, default, default))));
            }
        }
    }
}
