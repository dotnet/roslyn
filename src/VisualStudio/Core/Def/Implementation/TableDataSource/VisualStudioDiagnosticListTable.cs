// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTable))]
    internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(VisualStudioDiagnosticListTable);

        private readonly IErrorList _errorList;
        private readonly LiveTableDataSource _liveTableSource;
        private readonly BuildTableDataSource _buildTableSource;
        
        private const string TypeScriptLanguageName = "TypeScript";

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

            if (ErrorListHasFullSolutionAnalysisButton())
            {
                SetupErrorListFullSolutionAnalysis(workspace);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetupErrorListFullSolutionAnalysis(Workspace workspace)
        {
            var errorList2 = _errorList as IErrorList2;
            if (errorList2 != null)
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
                errorList2.AnalysisToggleStateChanged += OnErrorListFullSolutionAnalysisToggled;
                workspace.Services.GetService<IOptionService>().OptionChanged += OnOptionChanged;
            }                

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

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            Contract.ThrowIfFalse(_errorList is IErrorList2);

            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectRemoved:
                    SetFullSolutionAnalysisState(Workspace, _errorList as IErrorList2);
                    return;
            }
        }

        private void OnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            Contract.ThrowIfFalse(_errorList is IErrorList2);

            if (e.Option == RuntimeOptions.FullSolutionAnalysis || e.Option == ServiceFeatureOnOffOptions.ClosedFileDiagnostic)
            {
                SetFullSolutionAnalysisState(Workspace, _errorList as IErrorList2);
            }
        }

        private void OnErrorListFullSolutionAnalysisToggled(object sender, AnalysisToggleStateChangedEventArgs e)
        {
            Workspace.Options = Workspace.Options
                .WithChangedOption(RuntimeOptions.FullSolutionAnalysis, e.NewState)
                .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, e.NewState)
                .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, e.NewState)
                .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, TypeScriptLanguageName, e.NewState);
        }

        private static void SetFullSolutionAnalysisState(Workspace workspace, IErrorList2 errorList2)
        {
            // Set error list toggle state based on current analysis state for all languages for projects in current solution.
            var fullAnalysisState = workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysis);
            if (fullAnalysisState)
            {
                var languages = workspace.CurrentSolution.Projects.Select(p => p.Language).Distinct();
                foreach (var language in languages)
                {
                    if (!ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(workspace, language))
                    {
                        fullAnalysisState = false;
                        break;
                    }
                }
            }

            errorList2.AnalysisToggleState = fullAnalysisState;
        }

        internal static bool ErrorListHasFullSolutionAnalysisButton()
        {
            try
            {
                // Full solution analysis option has been moved to the error list from Dev14 Update3.
                // Use reflection to check if the new interface "IErrorList2" exists in Microsoft.VisualStudio.Shell.XX.0.dll.
                return typeof(ErrorHandler).Assembly.GetType("Microsoft.Internal.VisualStudio.Shell.IErrorList2") != null;
            }
            catch (Exception)
            {
                // Ignore exceptions.
                return false;
            }
        }
    }
}
