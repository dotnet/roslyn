// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Diagnostic update source for reporting workspace host specific diagnostics,
    /// which may not be related to any given project/document in the solution.
    /// For example, these include diagnostics generated for exceptions from third party analyzers.
    /// </summary>
    internal abstract class AbstractHostDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>> _analyzerHostDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>>.Empty;

        internal abstract Workspace Workspace { get; }

        public bool SupportGetDiagnostics
        {
            get
            {
                return false;
            }
        }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
        {
            var updated = this.DiagnosticsUpdated;
            if (updated != null)
            {
                updated(this, args);
            }
        }

        internal void ReportAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Workspace workspace, ProjectId projectIdOpt)
        {
            if (workspace != this.Workspace)
            {
                return;
            }

            // check whether we are reporting project specific diagnostic or workspace wide diagnostic
            var project = projectIdOpt != null ? workspace.CurrentSolution.GetProject(projectIdOpt) : null;
            
            // check whether project the diagnostic belong to still exist
            if (projectIdOpt != null && project == null)
            {
                // project the diagnostic belong to already removed from the solution.
                // ignore the diagnostic
                return;
            }

            bool raiseDiagnosticsUpdated = true;
            var diagnosticData = project != null ?
                DiagnosticData.Create(project, diagnostic) :
                DiagnosticData.Create(this.Workspace, diagnostic);

            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerHostDiagnosticsMap,
                analyzer,
                ImmutableHashSet.Create(diagnosticData),
                (a, existing) =>
                {
                    var newDiags = existing.Add(diagnosticData);
                    raiseDiagnosticsUpdated = newDiags.Count > existing.Count;
                    return newDiags;
                });

            if (raiseDiagnosticsUpdated)
            {
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, dxs, project, DiagnosticsUpdatedKind.DiagnosticsCreated));
            }
        }

        public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference analyzerReference, string language, ProjectId projectId)
        {
            var analyzers = analyzerReference.GetAnalyzers(language);
            ClearAnalyzerDiagnostics(analyzers, projectId);
            CompilationWithAnalyzers.ClearAnalyzerState(analyzers);
        }

        internal void ClearAnalyzerDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, ProjectId projectId)
        {
            foreach (var analyzer in analyzers)
            {
                ClearAnalyzerDiagnostics(analyzer, projectId);
            }
        }

        private void ClearAnalyzerDiagnostics(DiagnosticAnalyzer analyzer, ProjectId projectId)
        {
            ImmutableHashSet<DiagnosticData> existing;
            if (!_analyzerHostDiagnosticsMap.TryGetValue(analyzer, out existing))
            {
                return;
            }

            // Check if analyzer is shared by analyzer references from different projects.
            var sharedAnalyzer = existing.Contains(d => d.ProjectId != null && d.ProjectId != projectId);
            if (sharedAnalyzer)
            {
                var newDiags = existing.Where(d => d.ProjectId != projectId).ToImmutableHashSet();
                if (newDiags.Count < existing.Count &&
                    ImmutableInterlocked.TryUpdate(ref _analyzerHostDiagnosticsMap, analyzer, newDiags, existing))
                {
                    var project = this.Workspace.CurrentSolution.GetProject(projectId);
                    RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project,
                        DiagnosticsUpdatedKind.DiagnosticsRemoved));
                }
            }
            else if (ImmutableInterlocked.TryRemove(ref _analyzerHostDiagnosticsMap, analyzer, out existing))
            {
                var project = this.Workspace.CurrentSolution.GetProject(projectId);
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project,
                    DiagnosticsUpdatedKind.DiagnosticsRemoved));

                if (existing.Any(d => d.ProjectId == null))
                {
                    RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project: null,
                        kind: DiagnosticsUpdatedKind.DiagnosticsRemoved));
                }
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(DiagnosticAnalyzer analyzer, ImmutableHashSet<DiagnosticData> items, Project project,
            DiagnosticsUpdatedKind kind)
        {
            return new DiagnosticsUpdatedArgs(
                kind: kind,
                id: new HostArgsId(this, analyzer, project?.Id),
                workspace: this.Workspace,
                solution: project?.Solution,
                projectId: project?.Id,
                documentId: null,
                diagnostics: items.ToImmutableArray());
        }

        internal ImmutableArray<DiagnosticData> TestOnly_GetReportedDiagnostics()
        {
            return _analyzerHostDiagnosticsMap.Values.Flatten().ToImmutableArray();
        }

        internal ImmutableHashSet<DiagnosticData> TestOnly_GetReportedDiagnostics(DiagnosticAnalyzer analyzer)
        {
            ImmutableHashSet<DiagnosticData> diagnostics;
            if (!_analyzerHostDiagnosticsMap.TryGetValue(analyzer, out diagnostics))
            {
                diagnostics = ImmutableHashSet<DiagnosticData>.Empty;
            }

            return diagnostics;
        }

        private class HostArgsId : AnalyzerUpdateArgsId
        {
            private readonly AbstractHostDiagnosticUpdateSource _source;
            private readonly ProjectId _projectIdOpt;

            public HostArgsId(AbstractHostDiagnosticUpdateSource source, DiagnosticAnalyzer analyzer, ProjectId projectIdOpt) : base(analyzer)
            {
                this._source = source;
                this._projectIdOpt = projectIdOpt;
            }

            public override bool Equals(object obj)
            {
                var other = obj as HostArgsId;
                if (other == null)
                {
                    return false;
                }

                return _source == other._source && _projectIdOpt == other._projectIdOpt && base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_source.GetHashCode(), Hash.Combine(_projectIdOpt == null ? 1 : _projectIdOpt.GetHashCode(), base.GetHashCode()));
            }
        }
    }
}
