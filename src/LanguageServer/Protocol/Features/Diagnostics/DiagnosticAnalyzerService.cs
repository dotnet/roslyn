// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService))]
    [Shared]
    internal partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

        public IAsynchronousOperationListener Listener { get; }
        public IGlobalOptionService GlobalOptions { get; }

        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> _map = new();
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;
        private readonly IDiagnosticsRefresher _diagnosticsRefresher;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticAnalyzerService(
            IAsynchronousOperationListenerProvider listenerProvider,
            DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
            IGlobalOptionService globalOptions,
            IDiagnosticsRefresher diagnosticsRefresher)
        {
            AnalyzerInfoCache = globalCache.AnalyzerInfoCache;
            Listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
            GlobalOptions = globalOptions;
            _diagnosticsRefresher = diagnosticsRefresher;
            _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;

            globalOptions.AddOptionChangedHandler(this, (_, _, e) =>
            {
                if (e.HasOption(IsGlobalOptionAffectingDiagnostics))
                {
                    RequestDiagnosticRefresh();
                }
            });
        }

        public static bool IsGlobalOptionAffectingDiagnostics(IOption2 option)
            => option == NamingStyleOptions.NamingPreferences ||
               option.Definition.Group.Parent == CodeStyleOptionGroups.CodeStyle ||
               option == SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption ||
               option == SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption ||
               option == SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption;

        public void RequestDiagnosticRefresh()
            => _diagnosticsRefresher?.RequestWorkspaceRefresh();

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
            TextDocument document,
            TextSpan? range,
            Func<string, bool>? shouldIncludeDiagnostic,
            bool includeCompilerDiagnostics,
            bool includeSuppressedDiagnostics,
            ICodeActionRequestPriorityProvider priorityProvider,
            DiagnosticKind diagnosticKinds,
            bool isExplicit,
            CancellationToken cancellationToken)
        {
            var analyzer = CreateIncrementalAnalyzer(document.Project.Solution.Workspace);

            // always make sure that analyzer is called on background thread.
            await TaskScheduler.Default;
            priorityProvider ??= new DefaultCodeActionRequestPriorityProvider();

            return await analyzer.GetDiagnosticsForSpanAsync(
                document, range, shouldIncludeDiagnostic, includeSuppressedDiagnostics, includeCompilerDiagnostics,
                priorityProvider, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            var analyzer = CreateIncrementalAnalyzer(workspace);
            return analyzer.GetCachedDiagnosticsAsync(workspace.CurrentSolution, projectId, documentId, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
        }

        public async Task ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var analyzer = CreateIncrementalAnalyzer(project.Solution.Workspace);
            await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, Func<Project, DocumentId?, IReadOnlyList<DocumentId>>? getDocuments, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            var analyzer = CreateIncrementalAnalyzer(solution.Workspace);
            return analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, getDocuments, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
            Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics,
            bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            var analyzer = CreateIncrementalAnalyzer(solution.Workspace);
            return analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, shouldIncludeAnalyzer, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
        }
    }
}
