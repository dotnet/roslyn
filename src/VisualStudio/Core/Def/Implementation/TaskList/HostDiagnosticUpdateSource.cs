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
    [Export(typeof(AbstractHostDiagnosticUpdateSource))]
    [Export(typeof(HostDiagnosticUpdateSource))]
    internal sealed class HostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = new Dictionary<ProjectId, HashSet<object>>();

        [ImportingConstructor]
        public HostDiagnosticUpdateSource(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        internal override Workspace Workspace
        {
            get
            {
                return _workspace;
            }
        }

        private void RaiseDiagnosticsUpdatedForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            var args = new DiagnosticsUpdatedArgs(
                id: Tuple.Create(this, projectId, key),
                workspace: _workspace,
                solution: null,
                projectId: projectId,
                documentId: null,
                diagnostics: items.AsImmutableOrEmpty());

            RaiseDiagnosticsUpdated(args);
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
    }
}
