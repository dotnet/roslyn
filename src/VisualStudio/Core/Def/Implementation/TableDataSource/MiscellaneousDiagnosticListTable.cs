// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class MiscellaneousDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(MiscellaneousDiagnosticListTable);

        private readonly LiveTableDataSource _source;

        public static void Register(MiscellaneousFilesWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider)
        {
            new MiscellaneousDiagnosticListTable(workspace, diagnosticService, provider);
        }

        private MiscellaneousDiagnosticListTable(MiscellaneousFilesWorkspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
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
