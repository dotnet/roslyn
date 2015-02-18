// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSource))]
    [Export(typeof(AnalyzerExceptionDiagnosticUpdateSource))]
    [Shared]
    internal sealed class AnalyzerExceptionDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> _analyzerExceptionDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.Empty;

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

        private void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
        {
            var updated = this.DiagnosticsUpdated;
            if (updated != null)
            {
                updated(this, args);
            }
        }

        public void ReportDiagnostics(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics, Project project)
        {
            Contract.ThrowIfFalse(diagnostics.All(AnalyzerDriverHelper.IsAnalyzerExceptionDiagnostic));

            var dxs = diagnostics.Select(d => DiagnosticData.Create(project, d)).Distinct().ToImmutableArray();
            dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerExceptionDiagnosticsMap,
                analyzer,
                dxs,
                (a, existing) => existing.AddRange(dxs).Distinct());

            RaiseDiagnosticsUpdated(MakeArgs(analyzer, dxs, project.Solution.Workspace, project));
        }

        public void ClearDiagnostics(DiagnosticAnalyzer analyzer, Workspace workspace)
        {
            ImmutableArray<DiagnosticData> existing;
            if (ImmutableInterlocked.TryRemove(ref _analyzerExceptionDiagnosticsMap, analyzer, out existing))
            {
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableArray<DiagnosticData>.Empty, workspace, project: null));
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> items, Workspace workspace, Project project)
        {
            var id = WorkspaceAnalyzerManager.GetUniqueIdForAnalyzer(analyzer);

            return new DiagnosticsUpdatedArgs(
                id: Tuple.Create(this, id),
                workspace: workspace,
                solution: project != null ? project.Solution : workspace.CurrentSolution,
                projectId: project?.Id,
                documentId: null,
                diagnostics: items);
        }

        internal ImmutableArray<DiagnosticData> TestOnly_GetExceptionDiagnostics(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticData> diagnostics;
            if (!_analyzerExceptionDiagnosticsMap.TryGetValue(analyzer, out diagnostics))
            {
                diagnostics = ImmutableArray<DiagnosticData>.Empty;
            }

            return diagnostics;
        }
    }
}
