// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.Configuration;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTableCommandHandler))]
    internal partial class VisualStudioDiagnosticListTableCommandHandler
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly VisualStudioSuppressionFixService _suppressionFixService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
        private readonly IWaitIndicator _waitIndicator;
        private readonly IWpfTableControl _tableControl;

        [ImportingConstructor]
        public VisualStudioDiagnosticListTableCommandHandler(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IVisualStudioSuppressionFixService suppressionFixService,
            IVisualStudioDiagnosticListSuppressionStateService suppressionStateService,
            IWaitIndicator waitIndicator)
        {
            _workspace = workspace;
            _suppressionFixService = (VisualStudioSuppressionFixService)suppressionFixService;
            _suppressionStateService = (VisualStudioDiagnosticListSuppressionStateService)suppressionStateService;
            _waitIndicator = waitIndicator;

            var errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            _tableControl = errorList?.TableControl;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            // Add command handlers for bulk suppression commands.
            var menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                AddErrorListSetSeverityMenuHandlers(menuCommandService);

                // The Add/Remove suppression(s) have been moved to the VS code analysis layer, so we don't add the commands here.

                // TODO: Figure out how to access menu commands registered by CodeAnalysisPackage and 
                //       add the commands here if we cannot find the new command(s) in the code analysis layer.

                // AddSuppressionsCommandHandlers(menuCommandService);
            }
        }

        private void AddErrorListSetSeverityMenuHandlers(IMenuCommandService menuCommandService)
        {
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeveritySubMenu, delegate { }, OnErrorListSetSeveritySubMenuStatus);

            // Severity menu items
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityDefault, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityError, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityWarning, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityInfo, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityHidden, SetSeverityHandler, delegate { });
            AddCommand(menuCommandService, ID.RoslynCommands.ErrorListSetSeverityNone, SetSeverityHandler, delegate { });
        }

        private void AddSuppressionsCommandHandlers(IMenuCommandService menuCommandService)
        {
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressions, delegate { }, OnAddSuppressionsStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressionsInSource, OnAddSuppressionsInSource, OnAddSuppressionsInSourceStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressionsInSuppressionFile, OnAddSuppressionsInSuppressionFile, OnAddSuppressionsInSuppressionFileStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.RemoveSuppressions, OnRemoveSuppressions, OnRemoveSuppressionsStatus);
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
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnRemoveSuppressionsStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanRemoveSuppressionsSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSourceStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSource;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSuppressionFileStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSuppressionFiles;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSource(object sender, EventArgs e)
        {
            _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: true, projectHierarchyOpt: null);
        }

        private void OnAddSuppressionsInSuppressionFile(object sender, EventArgs e)
        {
            _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: false, projectHierarchyOpt: null);
        }

        private void OnRemoveSuppressions(object sender, EventArgs e)
        {
            _suppressionFixService.RemoveSuppressions(selectedErrorListEntriesOnly: true, projectHierarchyOpt: null);
        }

        private void OnErrorListSetSeveritySubMenuStatus(object sender, EventArgs e)
        {
            // For now, we only enable the Set severity menu when a single configurable diagnostic is selected in the error list
            // and we can update/create an editorconfig file for the configuration entry.
            // In future, we can enable support for configuring in presence of multi-selection. 
            var command = (MenuCommand)sender;
            var selectedEntry = TryGetSingleSelectedEntry();
            command.Visible = selectedEntry != null &&
                !SuppressionHelpers.IsNotConfigurableDiagnostic(selectedEntry) &&
                TryGetPathToAnalyzerConfigDoc(selectedEntry, out _) != null;
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

            var pathToAnalyzerConfigDoc = TryGetPathToAnalyzerConfigDoc(selectedDiagnostic, out var project);
            if (pathToAnalyzerConfigDoc != null)
            {
                _waitIndicator.Wait(
                    title: ServicesVSResources.Updating_severity,
                    message: ServicesVSResources.Updating_severity,
                    allowCancel: true,
                    action: waitContext =>
                    {
                        var newSolution = ConfigureSeverityAsync(waitContext).WaitAndGetResult(waitContext.CancellationToken);
                        _workspace.TryApplyChanges(newSolution, waitContext.ProgressTracker);
                    });
            }

            return;

            // Local functions.
            async System.Threading.Tasks.Task<Solution> ConfigureSeverityAsync(IWaitContext waitContext)
            {
                var diagnostic = await selectedDiagnostic.ToDiagnosticAsync(project, waitContext.CancellationToken).ConfigureAwait(false);
                return await ConfigurationUpdater.ConfigureSeverityAsync(reportDiagnostic.Value, diagnostic, project, waitContext.CancellationToken).ConfigureAwait(false);
            }
        }

        private DiagnosticData TryGetSingleSelectedEntry()
        {
            if (_tableControl?.SelectedEntries.Count() != 1)
            {
                return null;
            }

            if (!_tableControl.SelectedEntry.TryGetSnapshot(out var snapshot, out var index) ||
                !(snapshot is AbstractTableEntriesSnapshot<DiagnosticTableItem> roslynSnapshot))
            {
                return null;
            }

            return roslynSnapshot.GetItem(index)?.Data;
        }

        private string TryGetPathToAnalyzerConfigDoc(DiagnosticData selectedDiagnostic, out Project project)
        {
            project = _workspace.CurrentSolution.GetProject(selectedDiagnostic.ProjectId);
            return project?.TryGetAnalyzerConfigPathForProjectConfiguration();
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
