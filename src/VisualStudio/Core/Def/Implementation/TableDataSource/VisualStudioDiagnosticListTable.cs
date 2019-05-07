// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(VisualStudioDiagnosticListTable);

        private readonly LiveTableDataSource _liveTableSource;
        private readonly BuildTableDataSource _buildTableSource;

        private IErrorList _errorList;

        public static async Task RegisterAsync(
            Shell.IAsyncServiceProvider asyncServiceProvider,
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticService diagnosticService,
            ITableManagerProvider provider)
        {
            var table = new VisualStudioDiagnosticListTable(workspace, diagnosticService, workspace.ExternalErrorDiagnosticUpdateSource, provider);

            table.SetErrorList((IErrorList)await asyncServiceProvider.GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(false));
        }

        private VisualStudioDiagnosticListTable(
            Workspace workspace,
            IDiagnosticService diagnosticService,
            ExternalErrorDiagnosticUpdateSource errorSource,
            ITableManagerProvider provider) :
            base(workspace, provider)
        {
            _liveTableSource = new LiveTableDataSource(workspace, diagnosticService, IdentifierString);
            _buildTableSource = new BuildTableDataSource(workspace, errorSource);

            ConnectWorkspaceEvents();
        }

        private void SetErrorList(IErrorList errorList)
        {
            _errorList = errorList;
            _errorList.PropertyChanged += OnErrorListPropertyChanged;

            AddInitialTableSource(Workspace.CurrentSolution, GetCurrentDataSource());
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
            base(workspace, provider)
        {
            AddInitialTableSource(workspace.CurrentSolution, new LiveTableDataSource(workspace, diagnosticService, IdentifierString));
        }

        /// this is for test only
        internal VisualStudioDiagnosticListTable(Workspace workspace, ExternalErrorDiagnosticUpdateSource errorSource, ITableManagerProvider provider) :
            base(workspace, provider)
        {
            AddInitialTableSource(workspace.CurrentSolution, new BuildTableDataSource(workspace, errorSource));
        }

        protected override void AddTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count == 0)
            {
                // whenever there is a change in solution, make sure we refresh static info
                // of build errors so that things like project name correctly refreshed
                _buildTableSource.RefreshAllFactories();
                return;
            }

            RemoveTableSourcesIfNecessary();
            AddTableSource(GetCurrentDataSource());
        }

        protected override void RemoveTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0)
            {
                // whenever there is a change in solution, make sure we refresh static info
                // of build errors so that things like project name correctly refreshed
                _buildTableSource.RefreshAllFactories();
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
