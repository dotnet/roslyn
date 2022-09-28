// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
        public RemoteDiagnosticListTable(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            RemoteLanguageServiceWorkspace workspace,
            IDiagnosticService diagnosticService,
            ITableManagerProvider provider)
            : base(workspace, provider)
        {
            _source = new LiveTableDataSource(workspace, threadingContext, diagnosticService, IdentifierString);
            AddInitialTableSource(workspace.CurrentSolution, _source);

            ConnectWorkspaceEvents();
        }

        public IGlobalOptionService GlobalOptions
            => _source.GlobalOptions;

        public void UpdateWorkspaceDiagnosticsPresent(bool diagnosticsPresent)
            => _workspaceDiagnosticsPresent = diagnosticsPresent;

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
            => _source.Shutdown();
    }
}
