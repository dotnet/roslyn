// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Configuration;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTableCommandHandler))]
    internal partial class VisualStudioDiagnosticListTableCommandHandler
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspace _workspace;
        private readonly VisualStudioSuppressionFixService _suppressionFixService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly ICodeActionEditHandlerService _editHandlerService;
        private readonly IAsynchronousOperationListener _listener;

        private IWpfTableControl? _tableControl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDiagnosticListTableCommandHandler(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IVisualStudioSuppressionFixService suppressionFixService,
            VisualStudioDiagnosticListSuppressionStateService suppressionStateService,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IDiagnosticAnalyzerService diagnosticService,
            ICodeActionEditHandlerService editHandlerService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _suppressionFixService = (VisualStudioSuppressionFixService)suppressionFixService;
            _suppressionStateService = suppressionStateService;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _diagnosticService = diagnosticService;
            _editHandlerService = editHandlerService;
            _listener = listenerProvider.GetListener(FeatureAttribute.ErrorList);
        }

        public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var errorList = await serviceProvider.GetServiceAsync<SVsErrorList, IErrorList>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false);
            _tableControl = errorList?.TableControl;

            // Add command handlers for bulk suppression commands.
            var menuCommandService = await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false);
            if (menuCommandService != null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                AddErrorListSetSeverityMenuHandlers(menuCommandService);

                // The Add/Remove suppression(s) have been moved to the VS code analysis layer, so we don't add the commands here.

                // TODO: Figure out how to access menu commands registered by CodeAnalysisPackage and 
                //       add the commands here if we cannot find the new command(s) in the code analysis layer.

                // AddSuppressionsCommandHandlers(menuCommandService);
            }
        }

        private void AddErrorListSetSeverityMenuHandlers(IMenuCommandService menuCommandService)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);

            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeveritySubMenu, delegate { }, OnErrorListSetSeveritySubMenuStatus);

            // Severity menu items
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityDefault, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityError, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityWarning, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityInfo, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityHidden, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityNone, SetSeverityHandler, delegate { });
        }

        /// <summary>
        /// Add a command handler and status query handler for a menu item
        /// </summary>
        private static OleMenuCommand AddCommand(
            IMenuCommandService menuCommandService,
            int commandId,
            EventHandler invokeHandler,
            EventHandler beforeQueryStatus)
        {
            var commandIdWithGroupId = new CommandID(Guids.RoslynGroupId, commandId);
            var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
            menuCommandService.AddCommand(command);
            return command;
        }

        private void OnAddSuppressionsStatus(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnRemoveSuppressionsStatus(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Visible = _suppressionStateService.CanRemoveSuppressionsSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSourceStatus(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSource;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSuppressionFileStatus(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSuppressionFiles;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSource(object sender, EventArgs e)
            => _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: true, projectHierarchy: null);

        private void OnAddSuppressionsInSuppressionFile(object sender, EventArgs e)
            => _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: false, projectHierarchy: null);

        private void OnRemoveSuppressions(object sender, EventArgs e)
            => _suppressionFixService.RemoveSuppressions(selectedErrorListEntriesOnly: true, projectHierarchy: null);

        private void OnErrorListSetSeveritySubMenuStatus(object sender, EventArgs e)
        {
            // For now, we only enable the Set severity menu when a single configurable diagnostic is selected in the error list
            // and we can update/create an editorconfig file for the configuration entry.
            // In future, we can enable support for configuring in presence of multi-selection. 
            var command = (MenuCommand)sender;
            var selectedEntry = TryGetSingleSelectedEntry();
            command.Visible = selectedEntry != null &&
                !SuppressionHelpers.IsNotConfigurableDiagnostic(selectedEntry) &&
                TryGetPathToAnalyzerConfigDoc(selectedEntry, out _, out _);
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void SetSeverityHandler(object sender, EventArgs args)
        {
            var selectedItem = (MenuCommand)sender;
            var reportDiagnostic = TryMapSelectedItemToReportDiagnostic(selectedItem);
            if (reportDiagnostic == null)
            {
                return;
            }

            var selectedDiagnostic = TryGetSingleSelectedEntry();
            if (selectedDiagnostic == null)
            {
                return;
            }

            if (TryGetPathToAnalyzerConfigDoc(selectedDiagnostic, out var project, out _))
            {
                // Fire and forget.
                _ = SetSeverityHandlerAsync(reportDiagnostic.Value, selectedDiagnostic, project);
            }
        }

        private async Task SetSeverityHandlerAsync(ReportDiagnostic reportDiagnostic, DiagnosticData selectedDiagnostic, Project project)
        {
            try
            {
                using var token = _listener.BeginAsyncOperation(nameof(SetSeverityHandlerAsync));
                using var context = _uiThreadOperationExecutor.BeginExecute(
                    title: ServicesVSResources.Updating_severity,
                    defaultDescription: ServicesVSResources.Updating_severity,
                    allowCancellation: true,
                    showProgress: true);

                var newSolution = await ConfigureSeverityAsync(context.UserCancellationToken).ConfigureAwait(false);
                var operations = ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
                using var scope = context.AddScope(allowCancellation: true, ServicesVSResources.Updating_severity);
                await _editHandlerService.ApplyAsync(
                    _workspace,
                    fromDocument: null,
                    operations: operations,
                    title: ServicesVSResources.Updating_severity,
                    progressTracker: new UIThreadOperationContextProgressTracker(scope),
                    cancellationToken: context.UserCancellationToken).ConfigureAwait(false);

                if (selectedDiagnostic.DocumentId != null)
                {
                    // Kick off diagnostic re-analysis for affected document so that the configured diagnostic gets refreshed.
                    _ = Task.Run(() =>
                    {
                        _diagnosticService.Reanalyze(_workspace, documentIds: SpecializedCollections.SingletonEnumerable(selectedDiagnostic.DocumentId), highPriority: true);
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }

            return;

            // Local functions.
            async System.Threading.Tasks.Task<Solution> ConfigureSeverityAsync(CancellationToken cancellationToken)
            {
                var diagnostic = await selectedDiagnostic.ToDiagnosticAsync(project, cancellationToken).ConfigureAwait(false);
                return await ConfigurationUpdater.ConfigureSeverityAsync(reportDiagnostic, diagnostic, project, cancellationToken).ConfigureAwait(false);
            }
        }

        private DiagnosticData? TryGetSingleSelectedEntry()
        {
            if (_tableControl?.SelectedEntries.Count() != 1)
            {
                return null;
            }

            if (!_tableControl.SelectedEntry.TryGetSnapshot(out var snapshot, out var index) ||
                snapshot is not AbstractTableEntriesSnapshot<DiagnosticTableItem> roslynSnapshot)
            {
                return null;
            }

            return roslynSnapshot.GetItem(index)?.Data;
        }

        private bool TryGetPathToAnalyzerConfigDoc(DiagnosticData selectedDiagnostic, [NotNullWhen(true)] out Project? project, [NotNullWhen(true)] out string? pathToAnalyzerConfigDoc)
        {
            project = _workspace.CurrentSolution.GetProject(selectedDiagnostic.ProjectId);
            pathToAnalyzerConfigDoc = project?.TryGetAnalyzerConfigPathForProjectConfiguration();
            return pathToAnalyzerConfigDoc is not null;
        }

        private static ReportDiagnostic? TryMapSelectedItemToReportDiagnostic(MenuCommand selectedItem)
        {
            if (selectedItem.CommandID.Guid == Guids.RoslynGroupId)
            {
                return selectedItem.CommandID.ID switch
                {
                    ID.RoslynCommands.ErrorListSetSeverityDefault => ReportDiagnostic.Default,
                    ID.RoslynCommands.ErrorListSetSeverityError => ReportDiagnostic.Error,
                    ID.RoslynCommands.ErrorListSetSeverityWarning => ReportDiagnostic.Warn,
                    ID.RoslynCommands.ErrorListSetSeverityInfo => ReportDiagnostic.Info,
                    ID.RoslynCommands.ErrorListSetSeverityHidden => ReportDiagnostic.Hidden,
                    ID.RoslynCommands.ErrorListSetSeverityNone => ReportDiagnostic.Suppress,
                    _ => (ReportDiagnostic?)null
                };
            }

            return null;
        }
    }
}
