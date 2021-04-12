﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.DiagnosticService, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousDiagnosticListTableWorkspaceEventListener : IEventListener<IDiagnosticService>
    {
        internal const string IdentifierString = nameof(MiscellaneousDiagnosticListTable);

        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscellaneousDiagnosticListTableWorkspaceEventListener(ITableManagerProvider tableManagerProvider)
            => _tableManagerProvider = tableManagerProvider;

        public void StartListening(Workspace workspace, IDiagnosticService diagnosticService)
            => new MiscellaneousDiagnosticListTable(workspace, diagnosticService, _tableManagerProvider);

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
                => _source.Shutdown();
        }
    }
}
