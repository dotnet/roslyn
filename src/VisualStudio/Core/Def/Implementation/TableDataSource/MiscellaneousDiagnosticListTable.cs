// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportWorkspaceEventListener(WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousDiagnosticListTableWorkspaceEventListener : IWorkspaceEventListener
    {
        internal const string IdentifierString = nameof(MiscellaneousDiagnosticListTable);

        private readonly IDiagnosticService _diagnosticService;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public MiscellaneousDiagnosticListTableWorkspaceEventListener(
            IDiagnosticService diagnosticService, ITableManagerProvider tableManagerProvider)
        {
            _diagnosticService = diagnosticService;
            _tableManagerProvider = tableManagerProvider;
        }

        public void Listen(Workspace workspace)
        {
            new MiscellaneousDiagnosticListTable(workspace, _diagnosticService, _tableManagerProvider);
        }

        public void Stop(Workspace workspace)
        {
            // nothing to do on stop
        }

        private sealed class MiscellaneousDiagnosticListTable : VisualStudioBaseDiagnosticListTable
        {
            private readonly LiveTableDataSource _source;

            public MiscellaneousDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
                base(workspace, provider)
            {
                _source = new LiveTableDataSource(workspace, diagnosticService, IdentifierString);
                AddInitialTableSource(workspace.CurrentSolution, _source);

                ConnectWorkspaceEvents();
            }

            protected override void AddTableSourceIfNecessary(Solution solution)
            {
                if (solution.ProjectIds.Count == 0 || this.TableManager.Sources.Any(s => s == _source))
                {
                    return;
                }

                AddTableSource(_source);
            }

            protected override void RemoveTableSourceIfNecessary(Solution solution)
            {
                if (solution.ProjectIds.Count > 0 || !this.TableManager.Sources.Any(s => s == _source))
                {
                    return;
                }

                this.TableManager.RemoveSource(_source);
            }

            protected override void ShutdownSource()
            {
                _source.Shutdown();
            }
        }
    }
}
