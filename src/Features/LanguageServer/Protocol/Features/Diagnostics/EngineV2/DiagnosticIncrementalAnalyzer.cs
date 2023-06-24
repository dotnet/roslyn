// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Diagnostic Analyzer Engine V2
    /// 
    /// This one follows pattern compiler has set for diagnostic analyzer.
    /// </summary>
    internal partial class DiagnosticIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private readonly int _correlationId;
        private readonly DiagnosticAnalyzerTelemetry _telemetry = new();
        private readonly StateManager _stateManager;
        private readonly InProcOrRemoteHostAnalyzerRunner _diagnosticAnalyzerRunner;
        private readonly IDocumentTrackingService _documentTrackingService;
        private readonly IncrementalMemberEditAnalyzer _incrementalMemberEditAnalyzer = new();

#if NETSTANDARD
        private ConditionalWeakTable<Project, CompilationWithAnalyzers?> _projectCompilationsWithAnalyzers = new();
#else
        private readonly ConditionalWeakTable<Project, CompilationWithAnalyzers?> _projectCompilationsWithAnalyzers = new();
#endif

        internal DiagnosticAnalyzerService AnalyzerService { get; }
        internal Workspace Workspace { get; }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService analyzerService,
            int correlationId,
            Workspace workspace,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            Contract.ThrowIfNull(analyzerService);

            AnalyzerService = analyzerService;
            Workspace = workspace;

            _documentTrackingService = workspace.Services.GetRequiredService<IDocumentTrackingService>();

            _correlationId = correlationId;

            _stateManager = new StateManager(workspace, analyzerInfoCache);
            _stateManager.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;

            _diagnosticAnalyzerRunner = new InProcOrRemoteHostAnalyzerRunner(analyzerInfoCache, analyzerService.Listener);

            GlobalOptions.AddOptionChangedHandler(this, OnGlobalOptionChanged);
        }

        private void OnGlobalOptionChanged(object? sender, OptionChangedEventArgs e)
        {
            if (DiagnosticAnalyzerService.IsGlobalOptionAffectingDiagnostics(e.Option) &&
                GlobalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
            {
                var service = Workspace.Services.GetService<ISolutionCrawlerService>();
                service?.Reanalyze(Workspace, this, projectIds: null, documentIds: null, highPriority: false);
            }
        }

        internal IGlobalOptionService GlobalOptions => AnalyzerService.GlobalOptions;
        internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache => _diagnosticAnalyzerRunner.AnalyzerInfoCache;

        private void OnProjectAnalyzerReferenceChanged(object? sender, ProjectAnalyzerReferenceChangedEventArgs e)
        {
            if (e.Removed.Length == 0)
            {
                // nothing to refresh
                return;
            }

            // events will be automatically serialized.
            var project = e.Project;
            var stateSets = e.Removed;

            // make sure we drop cache related to the analyzers
            foreach (var stateSet in stateSets)
            {
                stateSet.OnRemoved();
            }

            ClearAllDiagnostics(stateSets, project.Id);
        }

        public void Shutdown()
        {
            GlobalOptions.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);

            var stateSets = _stateManager.GetAllStateSets();

            AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                var handleActiveFile = true;
                using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentSet);

                foreach (var stateSet in stateSets)
                {
                    var projectIds = stateSet.GetProjectsWithDiagnostics();
                    foreach (var projectId in projectIds)
                    {
                        stateSet.CollectDocumentsWithDiagnostics(projectId, documentSet);
                        RaiseProjectDiagnosticsRemoved(stateSet, projectId, documentSet, handleActiveFile, raiseEvents);
                        documentSet.Clear();
                    }
                }
            });
        }

        private void ClearAllDiagnostics(ImmutableArray<StateSet> stateSets, ProjectId projectId)
        {
            AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentSet);

                foreach (var stateSet in stateSets)
                {
                    Debug.Assert(documentSet.Count == 0);

                    stateSet.CollectDocumentsWithDiagnostics(projectId, documentSet);

                    // PERF: don't fire events for ones that we dont have any diagnostics on
                    if (documentSet.Count > 0)
                    {
                        RaiseProjectDiagnosticsRemoved(stateSet, projectId, documentSet, handleActiveFile: true, raiseEvents);
                        documentSet.Clear();
                    }
                }
            });
        }

        private void RaiseDiagnosticsCreated(
            Project project, DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> items, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(project.Solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(analyzer, project.Id, AnalysisKind.NonLocal),
                project.Solution.Workspace,
                project.Solution,
                project.Id,
                documentId: null,
                diagnostics: items));
        }

        private void RaiseDiagnosticsRemoved(
            ProjectId projectId, Solution? solution, DiagnosticAnalyzer analyzer, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(analyzer, projectId, AnalysisKind.NonLocal),
                Workspace,
                solution,
                projectId,
                documentId: null));
        }

        private void RaiseDiagnosticsCreated(
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind, ImmutableArray<DiagnosticData> items, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(document.Project.Solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(analyzer, document.Id, kind),
                document.Project.Solution.Workspace,
                document.Project.Solution,
                document.Project.Id,
                document.Id,
                items));
        }

        private void RaiseDiagnosticsRemoved(
            DocumentId documentId, Solution? solution, DiagnosticAnalyzer analyzer, AnalysisKind kind, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(analyzer, documentId, kind),
                Workspace,
                solution,
                documentId.ProjectId,
                documentId));
        }

        private static object CreateId(DiagnosticAnalyzer analyzer, DocumentId documentId, AnalysisKind kind)
            => new LiveDiagnosticUpdateArgsId(analyzer, documentId, kind);

        private static object CreateId(DiagnosticAnalyzer analyzer, ProjectId projectId, AnalysisKind kind)
            => new LiveDiagnosticUpdateArgsId(analyzer, projectId, kind);

        public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
            => project.GetDependentVersionAsync(cancellationToken);

        private static DiagnosticAnalysisResult GetResultOrEmpty(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> map, DiagnosticAnalyzer analyzer, ProjectId projectId, VersionStamp version)
        {
            if (map.TryGetValue(analyzer, out var result))
            {
                return result;
            }

            return DiagnosticAnalysisResult.CreateEmpty(projectId, version);
        }

        public void LogAnalyzerCountSummary()
            => _telemetry.ReportAndClear(_correlationId);

        /// <summary>
        /// The highest priority (lowest value) amongst all incremental analyzers (others have priority 1).
        /// </summary>
        public int Priority => 0;

        internal IEnumerable<DiagnosticAnalyzer> GetAnalyzersTestOnly(Project project)
            => _stateManager.GetOrCreateStateSets(project).Select(s => s.Analyzer);

        private static string GetDocumentLogMessage(string title, TextDocument document, DiagnosticAnalyzer analyzer)
            => $"{title}: ({document.Id}, {document.Project.Id}), ({analyzer})";

        private static string GetProjectLogMessage(Project project, ImmutableArray<StateSet> stateSets)
            => $"project: ({project.Id}), ({string.Join(Environment.NewLine, stateSets.Select(s => s.Analyzer.ToString()))})";

        private static string GetResetLogMessage(TextDocument document)
            => $"document close/reset: ({document.FilePath ?? document.Name})";

        private static string GetOpenLogMessage(TextDocument document)
            => $"document open: ({document.FilePath ?? document.Name})";

        private static string GetRemoveLogMessage(DocumentId id)
            => $"document remove: {id.ToString()}";

        private static string GetRemoveLogMessage(ProjectId id)
            => $"project remove: {id.ToString()}";
    }
}
