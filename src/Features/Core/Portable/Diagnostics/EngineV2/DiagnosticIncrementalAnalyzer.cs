// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Options;
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

        private readonly StateManager _stateManager;
        private readonly InProcOrRemoteHostAnalyzerRunner _diagnosticAnalyzerRunner;
        private ConditionalWeakTable<Project, CompilationWithAnalyzers?> _projectCompilationsWithAnalyzers;

        internal DiagnosticAnalyzerService AnalyzerService { get; }
        internal Workspace Workspace { get; }
        internal IPersistentStorageService PersistentStorageService { get; }
        internal AbstractHostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; }
        internal DiagnosticAnalyzerInfoCache DiagnosticAnalyzerInfoCache { get; }
        internal DiagnosticLogAggregator DiagnosticLogAggregator { get; private set; }

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService analyzerService,
            int correlationId,
            Workspace workspace,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            Contract.ThrowIfNull(analyzerService);

            AnalyzerService = analyzerService;
            Workspace = workspace;
            DiagnosticAnalyzerInfoCache = analyzerInfoCache;
            HostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            DiagnosticLogAggregator = new DiagnosticLogAggregator(analyzerService);
            PersistentStorageService = workspace.Services.GetRequiredService<IPersistentStorageService>();

            _correlationId = correlationId;

            _stateManager = new StateManager(analyzerInfoCache, PersistentStorageService);
            _stateManager.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;

            _diagnosticAnalyzerRunner = new InProcOrRemoteHostAnalyzerRunner(AnalyzerService, HostDiagnosticUpdateSource);
            _projectCompilationsWithAnalyzers = new ConditionalWeakTable<Project, CompilationWithAnalyzers?>();
        }

        public bool IsCompilationEndAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer, Project project, Compilation compilation)
        {
            var stateSet = _stateManager.GetOrCreateStateSet(project, diagnosticAnalyzer);
            return stateSet != null && stateSet.IsCompilationEndAnalyzer(project, compilation);
        }

        public bool ContainsDiagnostics(ProjectId projectId)
        {
            foreach (var stateSet in _stateManager.GetStateSets(projectId))
            {
                if (stateSet.ContainsAnyDocumentOrProjectDiagnostics(projectId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return e.Option.Feature == nameof(SimplificationOptions) ||
                   e.Option.Feature == nameof(CodeStyleOptions) ||
                   e.Option == SolutionCrawlerOptions.BackgroundAnalysisScopeOption ||
#pragma warning disable CS0618 // Type or member is obsolete - TypeScript and F# are still on the older ClosedFileDiagnostic option.
                   e.Option == ServiceFeatureOnOffOptions.ClosedFileDiagnostic;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void OnProjectAnalyzerReferenceChanged(object sender, ProjectAnalyzerReferenceChangedEventArgs e)
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
            var stateSets = _stateManager.GetAllStateSets();

            AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                var handleActiveFile = true;
                var documentSet = PooledHashSet<DocumentId>.GetInstance();

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

                documentSet.Free();
            });
        }

        private void ClearAllDiagnostics(ImmutableArray<StateSet> stateSets, ProjectId projectId)
        {
            AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                var documentSet = PooledHashSet<DocumentId>.GetInstance();

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

                documentSet.Free();
            });
        }

        private void RaiseDiagnosticsCreated(
            Project project, StateSet stateSet, ImmutableArray<DiagnosticData> items, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(project.Solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(stateSet, project.Id, AnalysisKind.NonLocal),
                project.Solution.Workspace,
                project.Solution,
                project.Id,
                documentId: null,
                diagnostics: items));
        }

        private void RaiseDiagnosticsRemoved(
            ProjectId projectId, Solution? solution, StateSet stateSet, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(stateSet, projectId, AnalysisKind.NonLocal),
                Workspace,
                solution,
                projectId,
                documentId: null));
        }

        private void RaiseDiagnosticsCreated(
            Document document, StateSet stateSet, AnalysisKind kind, ImmutableArray<DiagnosticData> items, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(document.Project.Solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(stateSet, document.Id, kind),
                document.Project.Solution.Workspace,
                document.Project.Solution,
                document.Project.Id,
                document.Id,
                items));
        }

        private void RaiseDiagnosticsRemoved(
            DocumentId documentId, Solution? solution, StateSet stateSet, AnalysisKind kind, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(stateSet, documentId, kind),
                Workspace,
                solution,
                documentId.ProjectId,
                documentId));
        }

        private static object CreateId(StateSet stateSet, DocumentId documentId, AnalysisKind kind)
            => new LiveDiagnosticUpdateArgsId(stateSet.Analyzer, documentId, (int)kind, stateSet.ErrorSourceName);

        private static object CreateId(StateSet stateSet, ProjectId projectId, AnalysisKind kind)
            => new LiveDiagnosticUpdateArgsId(stateSet.Analyzer, projectId, (int)kind, stateSet.ErrorSourceName);

        public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
        {
            return project.GetDependentVersionAsync(cancellationToken);
        }

        private static DiagnosticAnalysisResult GetResultOrEmpty(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> map, DiagnosticAnalyzer analyzer, ProjectId projectId, VersionStamp version)
        {
            if (map.TryGetValue(analyzer, out var result))
            {
                return result;
            }

            return DiagnosticAnalysisResult.CreateEmpty(projectId, version);
        }

        public void LogAnalyzerCountSummary()
        {
            DiagnosticAnalyzerLogger.LogAnalyzerCrashCountSummary(_correlationId, DiagnosticLogAggregator);
            DiagnosticAnalyzerLogger.LogAnalyzerTypeCountSummary(_correlationId, DiagnosticLogAggregator);

            // reset the log aggregator
            ResetDiagnosticLogAggregator();
        }

        private void ResetDiagnosticLogAggregator()
        {
            DiagnosticLogAggregator = new DiagnosticLogAggregator(AnalyzerService);
        }

        internal IEnumerable<DiagnosticAnalyzer> GetAnalyzersTestOnly(Project project)
        {
            return _stateManager.GetOrCreateStateSets(project).Select(s => s.Analyzer);
        }

        private static string GetDocumentLogMessage(string title, Document document, DiagnosticAnalyzer analyzer)
        {
            return $"{title}: ({document.Id}, {document.Project.Id}), ({analyzer.ToString()})";
        }

        private static string GetProjectLogMessage(Project project, IEnumerable<StateSet> stateSets)
        {
            return $"project: ({project.Id}), ({string.Join(Environment.NewLine, stateSets.Select(s => s.Analyzer.ToString()))})";
        }

        private static string GetResetLogMessage(Document document)
        {
            return $"document close/reset: ({document.FilePath ?? document.Name})";
        }

        private static string GetOpenLogMessage(Document document)
        {
            return $"document open: ({document.FilePath ?? document.Name})";
        }

        private static string GetRemoveLogMessage(DocumentId id)
        {
            return $"document remove: {id.ToString()}";
        }

        private static string GetRemoveLogMessage(ProjectId id)
        {
            return $"project remove: {id.ToString()}";
        }
    }
}
