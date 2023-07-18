// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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

        private readonly IAsyncServiceProvider _asyncServiceProvider;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IThreadingContext _threadingContext;
        private readonly ITableManagerProvider _tableManagerProvider;
        private readonly IAsynchronousOperationListener _asynchronousOperationListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDiagnosticListTableWorkspaceEventListener(
            [Import("Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider")] object asyncServiceProvider,
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            ITableManagerProvider tableManagerProvider,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            // MEFv2 doesn't support type based contract for Import above and for this particular contract (SAsyncServiceProvider)
            // actual type cast doesn't work. (https://github.com/microsoft/vs-mef/issues/138)
            // workaround by getting the service as object and cast to actual interface
            _asyncServiceProvider = (IAsyncServiceProvider)asyncServiceProvider;
            _globalOptions = globalOptions;
            _threadingContext = threadingContext;
            _tableManagerProvider = tableManagerProvider;
            _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
        }

        public void StartListening(Workspace workspace, IDiagnosticService diagnosticService)
        {
            _threadingContext.RunWithShutdownBlockAsync(async cancellationToken =>
            {
                using var asyncToken = _asynchronousOperationListener.BeginAsyncOperation(nameof(VisualStudioDiagnosticListTableWorkspaceEventListener) + "." + nameof(StartListening));
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var errorList = (await _asyncServiceProvider.GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(true)) as IErrorList;

                if (errorList == null)
                {
                    // nothing to do when there is no error list. 
                    // it can happen if VS ran in command line mode
                    return;
                }

                var table = new VisualStudioDiagnosticListTable(
                    (VisualStudioWorkspaceImpl)workspace,
                    _globalOptions,
                    _threadingContext,
                    diagnosticService,
                    _tableManagerProvider,
                    errorList);
            });
        }

        internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
        {
            private readonly LiveTableDataSource _liveTableSource;
            private readonly BuildTableDataSource _buildTableSource;

            private readonly IErrorList _errorList;

            public VisualStudioDiagnosticListTable(
                VisualStudioWorkspaceImpl workspace,
                IGlobalOptionService globalOptions,
                IThreadingContext threadingContext,
                IDiagnosticService diagnosticService,
                ITableManagerProvider provider,
                IErrorList errorList)
                : base(workspace, provider)
            {
                _errorList = errorList;

                _liveTableSource = new LiveTableDataSource(workspace, globalOptions, threadingContext, diagnosticService, IdentifierString, workspace.ExternalErrorDiagnosticUpdateSource);
                _buildTableSource = new BuildTableDataSource(workspace, threadingContext, workspace.ExternalErrorDiagnosticUpdateSource);

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

                return _errorList.AreOtherErrorSourceEntriesShown ? _liveTableSource : _buildTableSource;
            }

            /// this is for test only
            private VisualStudioDiagnosticListTable(
                Workspace workspace,
                IGlobalOptionService globalOptions,
                IThreadingContext threadingContext,
                IDiagnosticService diagnosticService,
                ITableManagerProvider provider)
                : base(workspace, provider)
            {
                _liveTableSource = null!;
                _buildTableSource = null!;
                _errorList = null!;

                AddInitialTableSource(workspace.CurrentSolution, new LiveTableDataSource(workspace, globalOptions, threadingContext, diagnosticService, IdentifierString));
            }

            /// this is for test only
            private VisualStudioDiagnosticListTable(Workspace workspace, IThreadingContext threadingContext, ExternalErrorDiagnosticUpdateSource errorSource, ITableManagerProvider provider)
                : base(workspace, provider)
            {
                _liveTableSource = null!;
                _buildTableSource = null!;
                _errorList = null!;

                AddInitialTableSource(workspace.CurrentSolution, new BuildTableDataSource(workspace, threadingContext, errorSource));
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

            internal static class TestAccessor
            {
                public static VisualStudioDiagnosticListTable Create(Workspace workspace, IGlobalOptionService globalOptions, IThreadingContext threadingContext, IDiagnosticService diagnosticService, ITableManagerProvider provider)
                    => new(workspace, globalOptions, threadingContext, diagnosticService, provider);

                public static VisualStudioDiagnosticListTable Create(Workspace workspace, IThreadingContext threadingContext, ExternalErrorDiagnosticUpdateSource errorSource, ITableManagerProvider provider)
                    => new(workspace, threadingContext, errorSource, provider);
            }
        }
    }
}
