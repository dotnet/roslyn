// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTable))]
    internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(VisualStudioDiagnosticListTable);

        private readonly IErrorList _errorList;
        private readonly LiveTableDataSource _liveTableSource;
        private readonly BuildTableDataSource _buildTableSource;

        [ImportingConstructor]
        public VisualStudioDiagnosticListTable(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            ExternalErrorDiagnosticUpdateSource errorSource,
            ITableManagerProvider provider) :
            this(serviceProvider, (Workspace)workspace, diagnosticService, errorSource, provider)
        {
            ConnectWorkspaceEvents();

            _errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            if (_errorList == null)
            {
                AddInitialTableSource(workspace.CurrentSolution, _liveTableSource);
                return;
            }

            _errorList.PropertyChanged += OnErrorListPropertyChanged;
            AddInitialTableSource(workspace.CurrentSolution, GetCurrentDataSource());
            SuppressionStateColumnDefinition.SetDefaultFilter(_errorList.TableControl);
        }

        private ITableDataSource GetCurrentDataSource()
        {
            if (_errorList == null)
            {
                return _liveTableSource;
            }

            return _errorList.AreOtherErrorSourceEntriesShown ? (ITableDataSource)_liveTableSource : _buildTableSource;
        }

        /// this is for test only
        internal VisualStudioDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            this(null, workspace, diagnosticService, null, provider)
        {
            AddInitialTableSource(workspace.CurrentSolution, _liveTableSource);
        }

        private VisualStudioDiagnosticListTable(
            SVsServiceProvider serviceProvider,
            Workspace workspace,
            IDiagnosticService diagnosticService,
            ExternalErrorDiagnosticUpdateSource errorSource,
            ITableManagerProvider provider) :
            base(serviceProvider, workspace, diagnosticService, provider)
        {
            _liveTableSource = new LiveTableDataSource(serviceProvider, workspace, diagnosticService, IdentifierString);
            _buildTableSource = new BuildTableDataSource(workspace, errorSource);
        }

        protected override void AddTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count == 0)
            {
                return;
            }

            RemoveTableSourcesIfNecessary();
            AddTableSource(GetCurrentDataSource());
        }

        protected override void RemoveTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0)
            {
                return;
            }

            RemoveTableSourcesIfNecessary();
        }

        private void RemoveTableSourcesIfNecessary()
        {
            RemoveTableSourceIfNecessary(_buildTableSource);
            RemoveTableSourceIfNecessary(_liveTableSource);
        }

        private void RemoveTableSourceIfNecessary(ITableDataSource source)
        {
            if (!this.TableManager.Sources.Any(s => s == source))
            {
                return;
            }

            this.TableManager.RemoveSource(source);
        }

        protected override void ShutdownSource()
        {
            _liveTableSource.Shutdown();
            _buildTableSource.Shutdown();
        }

        private void OnErrorListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IErrorList.AreOtherErrorSourceEntriesShown))
            {
                AddTableSourceIfNecessary(this.Workspace.CurrentSolution);
            }
        }
    }
}
