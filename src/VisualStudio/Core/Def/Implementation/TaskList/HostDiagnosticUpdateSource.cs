// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(IDiagnosticUpdateSource))]
    [Export(typeof(HostDiagnosticUpdateSource))]
    internal sealed class HostDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly AnalyzerDiagnosticUpdateSource _analyzerDiagnosticsSource;

        private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = new Dictionary<ProjectId, HashSet<object>>();

        [ImportingConstructor]
        public HostDiagnosticUpdateSource(
            VisualStudioWorkspaceImpl workspace,
            AnalyzerDiagnosticUpdateSource analyzerDiagnosticsSource)
        {
            _workspace = workspace;
            _analyzerDiagnosticsSource = analyzerDiagnosticsSource;
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public bool SupportGetDiagnostics { get { return false; } }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, CancellationToken cancellationToken)
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }

        private void RaiseDiagnosticsUpdatedForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            var diagnosticsUpdated = DiagnosticsUpdated;
            if (diagnosticsUpdated != null)
            {
                diagnosticsUpdated(this, new DiagnosticsUpdatedArgs(
                    id: Tuple.Create(this, projectId, key),
                    workspace: _workspace,
                    solution: null,
                    projectId: projectId,
                    documentId: null,
                    diagnostics: items.AsImmutableOrEmpty()));
            }
        }

        public void UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            Contract.ThrowIfNull(projectId);
            Contract.ThrowIfNull(key);
            Contract.ThrowIfNull(items);

            var projectDiagnosticKeys = _diagnosticMap.GetOrAdd(projectId, id => new HashSet<object>());

            projectDiagnosticKeys.Add(key);

            RaiseDiagnosticsUpdatedForProject(projectId, key, items);
        }

        public void ClearAllDiagnosticsForProject(ProjectId projectId)
        {
            Contract.ThrowIfNull(projectId);

            HashSet<object> projectDiagnosticKeys;
            if (_diagnosticMap.TryGetValue(projectId, out projectDiagnosticKeys))
            {
                _diagnosticMap.Remove(projectId);

                foreach (var key in projectDiagnosticKeys)
                {
                    RaiseDiagnosticsUpdatedForProject(projectId, key, SpecializedCollections.EmptyEnumerable<DiagnosticData>());
                }
            }            
        }

        public void ClearDiagnosticsForProject(ProjectId projectId, object key)
        {
            Contract.ThrowIfNull(projectId);
            Contract.ThrowIfNull(key);

            HashSet<object> projectDiagnosticKeys;
            if (_diagnosticMap.TryGetValue(projectId, out projectDiagnosticKeys))
            {
                if (projectDiagnosticKeys.Remove(key))
                {
                    RaiseDiagnosticsUpdatedForProject(projectId, key, SpecializedCollections.EmptyEnumerable<DiagnosticData>());
                }
            }
        }

        public void ClearAnalyzerSpecificDiagnostics(AnalyzerFileReference analyzerReference, string language)
        {
            foreach (var analyzer in analyzerReference.GetAnalyzers(language))
            {
                _analyzerDiagnosticsSource.ClearDiagnostics(analyzer, _workspace);
            }
        }
    }
}
