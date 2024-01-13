// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.DiagnosticService, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousDiagnosticListTableWorkspaceEventListener : IEventListener<IDiagnosticService>
    {
        internal const string IdentifierString = nameof(MiscellaneousDiagnosticListTable);
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscellaneousDiagnosticListTableWorkspaceEventListener(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            ITableManagerProvider tableManagerProvider)
        {
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, IDiagnosticService diagnosticService)
            => new MiscellaneousDiagnosticListTable(workspace, _globalOptions, _threadingContext, diagnosticService, _tableManagerProvider);

        private sealed class MiscellaneousDiagnosticListTable : VisualStudioBaseDiagnosticListTable
        {
            private readonly LiveTableDataSource _source;

            public MiscellaneousDiagnosticListTable(
                Workspace workspace,
                IGlobalOptionService globalOptions,
                IThreadingContext threadingContext,
                IDiagnosticService diagnosticService,
                ITableManagerProvider provider)
                : base(workspace, provider)
            {
                _source = new LiveTableDataSource(workspace, globalOptions, threadingContext, diagnosticService, IdentifierString);

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
