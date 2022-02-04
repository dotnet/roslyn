// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    // exporting both Abstract and HostDiagnosticUpdateSource is just to make testing easier.
    // use HostDiagnosticUpdateSource when abstract one is not needed for testing purpose
    [Export(typeof(AbstractHostDiagnosticUpdateSource))]
    [Export(typeof(HostDiagnosticUpdateSource))]
    internal sealed class HostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource
    {
        private readonly Lazy<VisualStudioWorkspace> _workspace;

        private readonly object _gate = new();
        private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = new();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public HostDiagnosticUpdateSource(Lazy<VisualStudioWorkspace> workspace, IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            _workspace = workspace;

            registrationService.Register(this);
        }

        public override Workspace Workspace
        {
            get
            {
                return _workspace.Value;
            }
        }

        private void RaiseDiagnosticsCreatedForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        {
            var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(projectId, key),
                Workspace,
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
                Workspace,
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

            ClearAnalyzerDiagnostics(projectId);
        }

        public void ClearDiagnosticsForProject(ProjectId projectId, object key)
        {
            Contract.ThrowIfNull(projectId);
            Contract.ThrowIfNull(key);

            var raiseEvent = false;
            lock (_gate)
            {
                if (_diagnosticMap.TryGetValue(projectId, out var projectDiagnosticKeys))
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
