// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class AbstractHostDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> _analyzerHostDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>>.Empty;

        protected AbstractHostDiagnosticUpdateSource()
        {
            // Register for exception diagnostics from workspace's analyzer manager.
            WorkspaceAnalyzerManager.AnalyzerExceptionDiagnostic += OnAnalyzerExceptionDiagnostic;
        }

        ~AbstractHostDiagnosticUpdateSource()
        {
            // Unregister for exception diagnostics from workspace's analyzer manager.
            WorkspaceAnalyzerManager.AnalyzerExceptionDiagnostic -= OnAnalyzerExceptionDiagnostic;
        }

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
            
            var diagnosticData = DiagnosticData.Create(args.Workspace, args.Diagnostic);
            ImmutableArray<DiagnosticData> existingDiagnostics;
            if (_analyzerHostDiagnosticsMap.TryGetValue(args.FaultedAnalyzer, out existingDiagnostics))
            {
                if (existingDiagnostics.Contains(diagnosticData))
                {
                    // don't fire duplicate diagnostics.
                    return;
                }
            }
            else
            {
                existingDiagnostics = ImmutableArray<DiagnosticData>.Empty;
            }

            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerHostDiagnosticsMap,
                args.FaultedAnalyzer,
                ImmutableArray.Create(diagnosticData),
                (a, existing) =>
                {
                    var newDiags = existing.Add(diagnosticData).Distinct();
                    return newDiags.Length == existing.Length ? existing : newDiags;
                });

            if (dxs.Length > existingDiagnostics.Length)
            {
                RaiseDiagnosticsUpdated(MakeArgs(args.FaultedAnalyzer, dxs, args.ProjectOpt));
            }
        }

        public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference analyzerReference, string language)
        {
            foreach (var analyzer in analyzerReference.GetAnalyzers(language))
            {
                ClearAnalyzerDiagnostics(analyzer);
            }
        }

        private void ClearAnalyzerDiagnostics(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticData> existing;
            if (ImmutableInterlocked.TryRemove(ref _analyzerHostDiagnosticsMap, analyzer, out existing))
            {
                RaiseDiagnosticsUpdated(MakeArgs(analyzer, ImmutableArray<DiagnosticData>.Empty, project: null));
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> items, Project project)
        {
            var id = WorkspaceAnalyzerManager.GetUniqueIdForAnalyzer(analyzer);

            return new DiagnosticsUpdatedArgs(
                id: Tuple.Create(this, id),
                workspace: this.Workspace,
                solution: project?.Solution,
                projectId: project?.Id,
                documentId: null,
                diagnostics: items);
        }

        internal ImmutableArray<DiagnosticData> TestOnly_GetReportedDiagnostics(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticData> diagnostics;
            if (!_analyzerHostDiagnosticsMap.TryGetValue(analyzer, out diagnostics))
            {
                diagnostics = ImmutableArray<DiagnosticData>.Empty;
            }

            return diagnostics;
        }
    }
}
