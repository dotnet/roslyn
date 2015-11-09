// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    // exporting both Abstract and HostDiagnosticUpdateSource is just to make testing easier.
    // use HostDiagnosticUpdateSource when abstract one is not needed for testing purpose
    [Export(typeof(AbstractHostDiagnosticUpdateSource))]
    [Export(typeof(HostDiagnosticUpdateSource))]
    internal sealed class HostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        private readonly object _gate = new object();
        private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = new Dictionary<ProjectId, HashSet<object>>();

        [ImportingConstructor]
        public HostDiagnosticUpdateSource(VisualStudioWorkspaceImpl workspace, IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            _workspace = workspace;

            registrationService.Register(this);
        }

        internal override Workspace Workspace
        {
            get
            {
                return _workspace;
            }
        }

        private void RaiseDiagnosticsCreatedForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(projectId, key),
                _workspace,
                solution: null,
                projectId: projectId,
                documentId: null,
                diagnostics: items.AsImmutableOrEmpty());

            RaiseDiagnosticsUpdated(args);
        }

        private void RaiseDiagnosticsRemovedForProject(ProjectId projectId, object key)
        {
            var args = DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(projectId, key),
                _workspace,
                solution: null,
                projectId: projectId,
                documentId: null);

            RaiseDiagnosticsUpdated(args);
        }

        private object CreateId(ProjectId projectId, object key) => Tuple.Create(this, projectId, key);

        public void UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            Contract.ThrowIfNull(projectId);
            Contract.ThrowIfNull(key);
            Contract.ThrowIfNull(items);

            lock (_gate)
            {
                _diagnosticMap.GetOrAdd(projectId, id => new HashSet<object>()).Add(key);
            }

            RaiseDiagnosticsCreatedForProject(projectId, key, items);
        }

        public void ClearAllDiagnosticsForProject(ProjectId projectId)
        {
            Contract.ThrowIfNull(projectId);

            HashSet<object> projectDiagnosticKeys;
            lock (_gate)
            {
                if (_diagnosticMap.TryGetValue(projectId, out projectDiagnosticKeys))
                {
                    _diagnosticMap.Remove(projectId);
                }
            }

            if (projectDiagnosticKeys != null)
            {
                foreach (var key in projectDiagnosticKeys)
                {
                    RaiseDiagnosticsRemovedForProject(projectId, key);
                }
            }
        }

        public void ClearDiagnosticsForProject(ProjectId projectId, object key)
        {
            Contract.ThrowIfNull(projectId);
            Contract.ThrowIfNull(key);

            var raiseEvent = false;
            lock (_gate)
            {
                HashSet<object> projectDiagnosticKeys;
                if (_diagnosticMap.TryGetValue(projectId, out projectDiagnosticKeys))
                {
                    raiseEvent = projectDiagnosticKeys.Remove(key);
                }
            }

            if (raiseEvent)
            {
                RaiseDiagnosticsRemovedForProject(projectId, key);
            }
        }
    }
}
