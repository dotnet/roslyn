﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal abstract partial class AbstractHostDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>> _analyzerHostDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>>.Empty;

        protected abstract Workspace Workspace { get; }

        public bool SupportGetDiagnostics
        {
            get
            {
                return false;
            }
        }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, CancellationToken cancellationToken)
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        protected void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
        {
            var updated = this.DiagnosticsUpdated;
            if (updated != null)
            {
                updated(this, args);
            }
        }

        private void OnAnalyzerExceptionDiagnostic(object sender, WorkspaceAnalyzerExceptionDiagnosticArgs args)
        {
            if (this.Workspace != args.Workspace)
            {
                return;
            }

            Contract.ThrowIfFalse(AnalyzerDriverHelper.IsAnalyzerExceptionDiagnostic(args.Diagnostic));
            
            bool raiseDiagnosticsUpdated = true;
            var diagnosticData = args.ProjectOpt != null ?
                DiagnosticData.Create(args.ProjectOpt, args.Diagnostic) :
                DiagnosticData.Create(args.Workspace, args.Diagnostic);

            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerHostDiagnosticsMap,
                args.FaultedAnalyzer,
                ImmutableHashSet.Create(diagnosticData),
                (a, existing) =>
                {
                    var newDiags = existing.Add(diagnosticData);
                    raiseDiagnosticsUpdated = newDiags.Count > existing.Count;
                    return newDiags;
                });

            if (raiseDiagnosticsUpdated)
            {
                RaiseDiagnosticsUpdated(MakeArgs(args.FaultedAnalyzer, dxs, args.ProjectOpt));
            }
        }

        public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference analyzerReference, string language, ProjectId projectId)
        {
            foreach (var analyzer in analyzerReference.GetAnalyzers(language))
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
                    RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project));
                }
            }
            else if (ImmutableInterlocked.TryRemove(ref _analyzerHostDiagnosticsMap, analyzer, out existing))
            {
                var project = this.Workspace.CurrentSolution.GetProject(projectId);
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project));

                if (existing.Any(d => d.ProjectId == null))
                {
                    RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableHashSet<DiagnosticData>.Empty, project: null));
                }
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(DiagnosticAnalyzer analyzer, ImmutableHashSet<DiagnosticData> items, Project project)
        {
            var id = WorkspaceAnalyzerManager.GetUniqueIdForAnalyzer(analyzer);

            return new DiagnosticsUpdatedArgs(
                id: Tuple.Create(this, id, project?.Id),
                workspace: this.Workspace,
                solution: project?.Solution,
                projectId: project?.Id,
                documentId: null,
                diagnostics: items.ToImmutableArray());
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
    }
}
