// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// An error list provider that gets diagnostics from the Roslyn diagnostics service.
    /// </summary>
    [Export]
    internal class RemoteDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(RemoteDiagnosticListTable);

        private readonly LiveTableDataSource _source;

        private bool _workspaceDiagnosticsPresent = false;

        [ImportingConstructor]
        public RemoteDiagnosticListTable(
            SVsServiceProvider serviceProvider, RemoteLanguageServiceWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            this(workspace, diagnosticService, provider)
        {
            ConnectWorkspaceEvents();
        }

        private RemoteDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider)
            : base(workspace, provider)
        {
            _source = new LiveTableDataSource(workspace, diagnosticService, IdentifierString);
            AddInitialTableSource(workspace.CurrentSolution, _source);
        }

        public void UpdateWorkspaceDiagnosticsPresent(bool diagnosticsPresent)
        {
            _workspaceDiagnosticsPresent = diagnosticsPresent;
        }

        protected override void AddTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count == 0 || TableManager.Sources.Any(s => s == _source))
            {
                return;
            }
            // If there's no workspace diagnostic service, we should populate the diagnostics table via language services.
            // Otherwise, the workspace diagnostic service will handle it.
            if (_workspaceDiagnosticsPresent)
            {
                return;
            }
            AddTableSource(_source);
        }

        protected override void RemoveTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0 || !TableManager.Sources.Any(s => s == _source))
            {
                return;
            }

            TableManager.RemoveSource(_source);
        }

        protected override void ShutdownSource()
        {
            _source.Shutdown();
        }
    }
}
