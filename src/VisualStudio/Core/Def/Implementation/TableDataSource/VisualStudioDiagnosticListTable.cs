// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.DiagnosticService, WorkspaceKind.Host), Shared]
    internal partial class VisualStudioDiagnosticListTableWorkspaceEventListener : IEventListener<IDiagnosticService>
    {
        internal const string IdentifierString = nameof(VisualStudioDiagnosticListTable);

        private readonly Shell.IAsyncServiceProvider _asyncServiceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public VisualStudioDiagnosticListTableWorkspaceEventListener(
            [Import("Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider")] object asyncServiceProvider,
            IThreadingContext threadingContext,
            ITableManagerProvider tableManagerProvider)
        {
            // MEFv2 doesn't support type based contract for Import above and for this particular contract (SAsyncServiceProvider)
            // actual type cast doesn't work. (https://github.com/microsoft/vs-mef/issues/138)
            // workaround by getting the service as object and cast to actual interface
            _asyncServiceProvider = (Shell.IAsyncServiceProvider)asyncServiceProvider;
            _threadingContext = threadingContext;
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, IDiagnosticService diagnosticService)
        {
            var errorList = _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                return await _asyncServiceProvider.GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(true) as IErrorList;
            });

            if (errorList == null)
            {
                // nothing to do when there is no error list. 
                // it can happen if VS ran in command line mode
                return;
            }

            var table = new VisualStudioDiagnosticListTable(
                (VisualStudioWorkspaceImpl)workspace,
                diagnosticService,
                _tableManagerProvider,
                errorList);
        }

        internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
        {
            private readonly LiveTableDataSource _liveTableSource;
            private readonly BuildTableDataSource _buildTableSource;

            private readonly IErrorList _errorList;

            public VisualStudioDiagnosticListTable(
                VisualStudioWorkspaceImpl workspace,
                IDiagnosticService diagnosticService,
                ITableManagerProvider provider,
                IErrorList errorList) :
                base(workspace, provider)
            {
                _errorList = errorList;

                _liveTableSource = new LiveTableDataSource(workspace, diagnosticService, IdentifierString);
                _buildTableSource = new BuildTableDataSource(workspace, workspace.ExternalErrorDiagnosticUpdateSource);

                AddInitialTableSource(Workspace.CurrentSolution, GetCurrentDataSource());
                ConnectWorkspaceEvents();

                _errorList.PropertyChanged += OnErrorListPropertyChanged;
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
}
