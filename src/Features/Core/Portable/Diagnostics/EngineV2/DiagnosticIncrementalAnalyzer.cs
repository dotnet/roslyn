// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Diagnostic Analyzer Engine V2
    /// 
    /// This one follows pattern compiler has set for diagnostic analyzer.
    /// </summary>
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private readonly int _correlationId;

        private readonly StateManager _stateManager;
        private readonly Executor _executor;
        private readonly CompilationManager _compilationManager;

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService owner,
            int correlationId,
            Workspace workspace,
            HostAnalyzerManager hostAnalyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(owner, workspace, hostAnalyzerManager, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;

            _stateManager = new StateManager(hostAnalyzerManager);
            _stateManager.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;

            _executor = new Executor(this);
            _compilationManager = new CompilationManager(this);
        }

        public override bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
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

        private bool SupportAnalysisKind(DiagnosticAnalyzer analyzer, string language, AnalysisKind kind)
        {
            // compiler diagnostic analyzer always support all kinds
            if (HostAnalyzerManager.IsCompilerDiagnosticAnalyzer(language, analyzer))
            {
                return true;
            }

            switch (kind)
            {
                case AnalysisKind.Syntax:
                    return analyzer.SupportsSyntaxDiagnosticAnalysis();
                case AnalysisKind.Semantic:
                    return analyzer.SupportsSemanticDiagnosticAnalysis();
                default:
                    return Contract.FailWithReturn<bool>("shouldn't reach here");
            }
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

        private void ClearAllDiagnostics(ImmutableArray<StateSet> stateSets, ProjectId projectId)
        {
            Owner.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                var handleActiveFile = true;
                foreach (var stateSet in stateSets)
                {
                    // PERF: don't fire events for ones that we dont have any diagnostics on
                    if (!stateSet.ContainsAnyDocumentOrProjectDiagnostics(projectId))
                    {
                        continue;
                    }

                    RaiseProjectDiagnosticsRemoved(stateSet, projectId, stateSet.GetDocumentsWithDiagnostics(projectId), handleActiveFile, raiseEvents);
                }
            });
        }

        private void RaiseDiagnosticsCreated(
            Project project, StateSet stateSet, ImmutableArray<DiagnosticData> items, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(project.Solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(stateSet.Analyzer, project.Id, AnalysisKind.NonLocal, stateSet.ErrorSourceName),
                project.Solution.Workspace,
                project.Solution,
                project.Id,
                documentId: null,
                diagnostics: items));
        }

        private void RaiseDiagnosticsRemoved(
            ProjectId projectId, Solution solution, StateSet stateSet, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(stateSet.Analyzer, projectId, AnalysisKind.NonLocal, stateSet.ErrorSourceName),
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
                CreateId(stateSet.Analyzer, document.Id, kind, stateSet.ErrorSourceName),
                document.Project.Solution.Workspace,
                document.Project.Solution,
                document.Project.Id,
                document.Id,
                items));
        }

        private void RaiseDiagnosticsRemoved(
            DocumentId documentId, Solution solution, StateSet stateSet, AnalysisKind kind, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Contract.ThrowIfFalse(solution == null || solution.Workspace == Workspace);

            raiseEvents(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(stateSet.Analyzer, documentId, kind, stateSet.ErrorSourceName),
                Workspace,
                solution,
                documentId.ProjectId,
                documentId));
        }

        private object CreateId(DiagnosticAnalyzer analyzer, DocumentId key, AnalysisKind kind, string errorSourceName)
        {
            return CreateIdInternal(analyzer, key, kind, errorSourceName);
        }

        private object CreateId(DiagnosticAnalyzer analyzer, ProjectId key, AnalysisKind kind, string errorSourceName)
        {
            return CreateIdInternal(analyzer, key, kind, errorSourceName);
        }

        private static object CreateIdInternal(DiagnosticAnalyzer analyzer, object key, AnalysisKind kind, string errorSourceName)
        {
            return new LiveDiagnosticUpdateArgsId(analyzer, key, (int)kind, errorSourceName);
        }

        public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
        {
            return project.GetDependentVersionAsync(cancellationToken);
        }

        private static AnalysisResult GetResultOrEmpty(ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> map, DiagnosticAnalyzer analyzer, ProjectId projectId, VersionStamp version)
        {
            AnalysisResult result;
            if (map.TryGetValue(analyzer, out result))
            {
                return result;
            }

            return new AnalysisResult(projectId, version);
        }

        private static ImmutableArray<DiagnosticData> GetResult(AnalysisResult result, AnalysisKind kind, DocumentId id)
        {
            if (result.IsEmpty || !result.DocumentIds.Contains(id) || result.IsAggregatedForm)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            switch (kind)
            {
                case AnalysisKind.Syntax:
                    return result.GetResultOrEmpty(result.SyntaxLocals, id);
                case AnalysisKind.Semantic:
                    return result.GetResultOrEmpty(result.SemanticLocals, id);
                case AnalysisKind.NonLocal:
                    return result.GetResultOrEmpty(result.NonLocals, id);
                default:
                    return Contract.FailWithReturn<ImmutableArray<DiagnosticData>>("shouldn't reach here");
            }
        }
    }
}
