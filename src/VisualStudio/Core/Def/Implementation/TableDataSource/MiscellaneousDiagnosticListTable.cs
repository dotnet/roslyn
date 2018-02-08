// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(MiscellaneousDiagnosticListTable))]
    internal class MiscellaneousDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(MiscellaneousDiagnosticListTable);

        private readonly LiveTableDataSource _source;

        [ImportingConstructor]
        public MiscellaneousDiagnosticListTable(
            SVsServiceProvider serviceProvider, MiscellaneousFilesWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            this(serviceProvider, (Workspace)workspace, diagnosticService, provider)
        {
            ConnectWorkspaceEvents();
        }

        /// this is for test only
        internal MiscellaneousDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            this(null, workspace, diagnosticService, provider)
        {
        }

        private MiscellaneousDiagnosticListTable(
            SVsServiceProvider serviceProvider, Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            base(serviceProvider, workspace, diagnosticService, provider)
        {
            // we don't report progress for misc project
            _source = new LiveTableDataSource(workspace, diagnosticService, IdentifierString, reporterOpt: null);
            AddInitialTableSource(workspace.CurrentSolution, _source);
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
