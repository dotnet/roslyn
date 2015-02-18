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
            // Register for exception diagnostics from both engines.
            EngineV1.DiagnosticAnalyzerDriver.AnalyzerExceptionDiagnostic += OnAnalyzerExceptionDiagnostic;
            EngineV2.DiagnosticIncrementalAnalyzer.AnalyzerExceptionDiagnostic += OnAnalyzerExceptionDiagnostic;
        }

        ~AnalyzerDiagnosticUpdateSource()
        {
            // Unregister for exception diagnostics from both engines.
            EngineV1.DiagnosticAnalyzerDriver.AnalyzerExceptionDiagnostic -= OnAnalyzerExceptionDiagnostic;
            EngineV2.DiagnosticIncrementalAnalyzer.AnalyzerExceptionDiagnostic -= OnAnalyzerExceptionDiagnostic;
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
            ImmutableArray<DiagnosticData> existingDiagnostics;
            if (_analyzerExceptionDiagnosticsMap.TryGetValue(args.FaultedAnalyzer, out existingDiagnostics))
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

            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerExceptionDiagnosticsMap,
                args.FaultedAnalyzer,
                ImmutableArray.Create(diagnosticData),
                (a, existing) =>
                {
                    var newDiags = existing.Add(diagnosticData).Distinct();
                    return newDiags.Length == existing.Length ? existing : newDiags;
                });

            if (dxs.Length > existingDiagnostics.Length)
            {
                RaiseDiagnosticsUpdated(MakeArgs(args.FaultedAnalyzer, dxs, args.Workspace, args.Project));
            }
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
                solution: project?.Solution,
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
