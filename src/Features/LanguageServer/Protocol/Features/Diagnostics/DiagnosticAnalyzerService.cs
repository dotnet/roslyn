// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService))]
    [Shared]
    internal partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        // use eventMap and taskQueue to serialize events
        private readonly EventMap _eventMap = new();
        private readonly TaskQueue _eventQueue;

        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

        public IAsynchronousOperationListener Listener { get; }
        public IGlobalOptionService GlobalOptions { get; }

        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> _map = new();
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticAnalyzerService(
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
            IGlobalOptionService globalOptions,
            IDiagnosticsRefresher diagnosticsRefresher)
        {
            AnalyzerInfoCache = globalCache.AnalyzerInfoCache;
            Listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
            GlobalOptions = globalOptions;

            _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;

            _eventQueue = new TaskQueue(Listener, TaskScheduler.Default);

            registrationService.Register(this);

            globalOptions.AddOptionChangedHandler(this, (_, e) =>
            {
                if (IsGlobalOptionAffectingDiagnostics(e.Option))
                {
                    diagnosticsRefresher.RequestWorkspaceRefresh();
                }
            });
        }

        public static bool IsGlobalOptionAffectingDiagnostics(IOption2 option)
            => option == NamingStyleOptions.NamingPreferences ||
               option.Definition.Group.Parent == CodeStyleOptionGroups.CodeStyle ||
               option == SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption ||
               option == SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption ||
               option == SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption;

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds, bool highPriority)
        {
            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            if (service != null && _map.TryGetValue(workspace, out var analyzer))
            {
                service.Reanalyze(workspace, analyzer, projectIds, documentIds, highPriority);
            }
        }

        public Task<(ImmutableArray<DiagnosticData> diagnostics, bool upToDate)> TryGetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            DiagnosticKind diagnosticKinds,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                // always make sure that analyzer is called on background thread.
                return Task.Run(async () =>
                {
                    priorityProvider ??= new DefaultCodeActionRequestPriorityProvider();

                    using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);
                    var upToDate = await analyzer.TryAppendDiagnosticsForSpanAsync(
                        document, range, diagnostics, shouldIncludeDiagnostic,
                        includeSuppressedDiagnostics, true, priorityProvider, blockForData: false,
                        addOperationScope: null, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
                    return (diagnostics.ToImmutable(), upToDate);
                }, cancellationToken);
            }

            return Task.FromResult((ImmutableArray<DiagnosticData>.Empty, upToDate: false));
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeCompilerDiagnostics,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            Func<string, IDisposable?>? addOperationScope,
            DiagnosticKind diagnosticKinds,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(document.Project.Solution.Workspace, out var analyzer))
            {
                priorityProvider ??= new DefaultCodeActionRequestPriorityProvider();

                // always make sure that analyzer is called on background thread.
                return Task.Run(() => analyzer.GetDiagnosticsForSpanAsync(
                    document, range, shouldIncludeDiagnostic, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                    priorityProvider, blockForData: true, addOperationScope, diagnosticKinds, isExplicit, cancellationToken), cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.GetCachedDiagnosticsAsync(workspace.CurrentSolution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Workspace workspace, object id, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.GetSpecificCachedDiagnosticsAsync(workspace.CurrentSolution, id, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public async Task ForceAnalyzeAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                if (projectId != null)
                {
                    var project = solution.GetProject(projectId);
                    if (project != null)
                    {
                        await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                        onProjectAnalyzed(project);
                    }
                }
                else
                {
                    var tasks = new Task[solution.ProjectIds.Count];
                    var index = 0;
                    foreach (var project in solution.Projects)
                    {
                        tasks[index++] = Task.Run(async () =>
                            {
                                await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                                onProjectAnalyzed(project);
                            }, cancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics,
            bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(solution.Workspace, out var analyzer))
            {
                return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, shouldIncludeAnalyzer, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
        }
    }
}
