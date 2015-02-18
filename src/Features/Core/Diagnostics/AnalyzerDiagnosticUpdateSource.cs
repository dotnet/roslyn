// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSource))]
    [Export(typeof(AnalyzerDiagnosticUpdateSource))]
    [Shared]
    internal sealed class AnalyzerDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> _analyzerExceptionDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.Empty;

        [ImportingConstructor]
        public AnalyzerDiagnosticUpdateSource()
        {
            BaseDiagnosticIncrementalAnalyzer.AnalyzerExceptionDiagnostic += OnAnalyzerExceptionDiagnostic;
        }

        ~AnalyzerDiagnosticUpdateSource()
        {
            BaseDiagnosticIncrementalAnalyzer.AnalyzerExceptionDiagnostic -= OnAnalyzerExceptionDiagnostic;
        }

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

        private void OnAnalyzerExceptionDiagnostic(object sender, WorkspaceAnalyzerExceptionDiagnosticArgs args)
        {
            Contract.ThrowIfFalse(AnalyzerDriverHelper.IsAnalyzerExceptionDiagnostic(args.Diagnostic));
            
            var diagnosticData = DiagnosticData.Create(args.Workspace, args.Diagnostic);
            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerExceptionDiagnosticsMap,
                args.FaultedAnalyzer,
                ImmutableArray.Create(diagnosticData),
                (a, existing) => existing.Add(diagnosticData).Distinct());

            RaiseDiagnosticsUpdated(MakeArgs(args.FaultedAnalyzer, dxs, args.Workspace));
        }

        public void ClearDiagnostics(DiagnosticAnalyzer analyzer, Workspace workspace)
        {
            ImmutableArray<DiagnosticData> existing;
            if (ImmutableInterlocked.TryRemove(ref _analyzerExceptionDiagnosticsMap, analyzer, out existing))
            {
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableArray<DiagnosticData>.Empty, workspace));
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> items, Workspace workspace)
        {
            var id = WorkspaceAnalyzerManager.GetUniqueIdForAnalyzer(analyzer);

            return new DiagnosticsUpdatedArgs(
                id: Tuple.Create(this, id),
                workspace: workspace,
                solution: null,
                projectId: null,
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
