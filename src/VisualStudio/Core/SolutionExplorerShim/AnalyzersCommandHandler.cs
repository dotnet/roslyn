// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj140;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export]
    internal class AnalyzersCommandHandler : IVsUpdateSolutionEvents
    {
        [Import]
        private AnalyzerItemsTracker _tracker = null;

        [Import]
        private AnalyzerReferenceManager _analyzerReferenceManager = null;

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider _serviceProvider = null;

        private MenuCommand _addMenuItem;
        private MenuCommand _projectAddMenuItem;
        private MenuCommand _projectContextAddMenuItem;
        private MenuCommand _referencesContextAddMenuItem;
        private MenuCommand _removeMenuItem;
        private MenuCommand _openRuleSetMenuItem;

        private MenuCommand _setSeverityErrorMenuItem;
        private MenuCommand _setSeverityWarningMenuItem;
        private MenuCommand _setSeverityInfoMenuItem;
        private MenuCommand _setSeverityHiddenMenuItem;
        private MenuCommand _setSeverityNoneMenuItem;

        private MenuCommand _openHelpLinkMenuItem;

        private Workspace _workspace;

        private ImmutableArray<DiagnosticItem> _selectedDiagnosticItems = ImmutableArray<DiagnosticItem>.Empty;

        /// <summary>
        /// Hook up the context menu handlers.
        /// </summary>
        public void Initialize(IMenuCommandService menuCommandService)
        {
            if (menuCommandService != null)
            {
                _addMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.AddAnalyzer, AddAnalyzerHandler);
                _projectAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ProjectAddAnalyzer, AddAnalyzerHandler);
                _projectContextAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ProjectContextAddAnalyzer, AddAnalyzerHandler);
                _referencesContextAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ReferencesContextAddAnalyzer, AddAnalyzerHandler);

                _removeMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.RemoveAnalyzer, RemoveAnalyzerHandler);

                _openRuleSetMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.OpenRuleSet, OpenRuleSetHandler);

                _setSeverityErrorMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityError, SetSeverityHandler);
                _setSeverityWarningMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityWarning, SetSeverityHandler);
                _setSeverityInfoMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityInfo, SetSeverityHandler);
                _setSeverityHiddenMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityHidden, SetSeverityHandler);
                _setSeverityNoneMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityNone, SetSeverityHandler);

                _openHelpLinkMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.OpenDiagnosticHelpLink, OpenDiagnosticHelpLinkHandler);

                UpdateMenuItemVisibility();
                UpdateMenuItemsChecked();

                if (_tracker != null)
                {
                    _tracker.SelectedHierarchyChanged += SelectedHierarchyChangedHandler;
                    _tracker.SelectedDiagnosticItemsChanged += SelectedDiagnosticItemsChangedHandler;
                    _tracker.SelectedItemIdChanged += SelectedItemIdChangedHandler;
                }

                var buildManager = (IVsSolutionBuildManager)_serviceProvider.GetService(typeof(SVsSolutionBuildManager));
                uint cookie;
                buildManager.AdviseUpdateSolutionEvents(this, out cookie);
            }
        }

        private MenuCommand AddCommandHandler(IMenuCommandService menuCommandService, int roslynCommand, EventHandler handler)
        {
            var commandID = new CommandID(Guids.RoslynGroupId, roslynCommand);
            var menuCommand = new MenuCommand(handler, commandID);
            menuCommandService.AddCommand(menuCommand);

            return menuCommand;
        }

        private void SelectedDiagnosticItemsChangedHandler(object sender, EventArgs e)
        {
            foreach (var item in _selectedDiagnosticItems)
            {
                item.PropertyChanged -= DiagnosticItemPropertyChangedHandler;
            }

            _selectedDiagnosticItems = _tracker.SelectedDiagnosticItems;

            foreach (var item in _selectedDiagnosticItems)
            {
                item.PropertyChanged += DiagnosticItemPropertyChangedHandler;
            }

            UpdateMenuItemsChecked();
        }

        private void DiagnosticItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiagnosticItem.EffectiveSeverity))
            {
                UpdateMenuItemsChecked();
            }
        }

        private void SelectedHierarchyChangedHandler(object sender, EventArgs e)
        {
            UpdateMenuItemVisibility();
        }

        private void SelectedItemIdChangedHandler(object sender, EventArgs e)
        {
            UpdateMenuItemVisibility();
        }

        private void UpdateMenuItemVisibility()
        {
            bool selectedProjectSupportsAnalyzers = SelectedProjectSupportAnalyzers();
            _addMenuItem.Visible = selectedProjectSupportsAnalyzers;
            _projectAddMenuItem.Visible = selectedProjectSupportsAnalyzers;
            _projectContextAddMenuItem.Visible = selectedProjectSupportsAnalyzers && _tracker.SelectedItemId == VSConstants.VSITEMID_ROOT;
            _referencesContextAddMenuItem.Visible = selectedProjectSupportsAnalyzers;

            _openHelpLinkMenuItem.Visible = _tracker.SelectedDiagnosticItems.Length == 1 &&
                                            !string.IsNullOrWhiteSpace(_tracker.SelectedDiagnosticItems[0].Descriptor.HelpLinkUri);
        }

        private void UpdateMenuItemsChecked()
        {
            _setSeverityErrorMenuItem.Checked = AnyDiagnosticsWithSeverity(ReportDiagnostic.Error);
            _setSeverityWarningMenuItem.Checked = AnyDiagnosticsWithSeverity(ReportDiagnostic.Warn);
            _setSeverityInfoMenuItem.Checked = AnyDiagnosticsWithSeverity(ReportDiagnostic.Info);
            _setSeverityHiddenMenuItem.Checked = AnyDiagnosticsWithSeverity(ReportDiagnostic.Hidden);
            _setSeverityNoneMenuItem.Checked = AnyDiagnosticsWithSeverity(ReportDiagnostic.Suppress);
        }

        private bool AnyDiagnosticsWithSeverity(ReportDiagnostic severity)
        {
            return _selectedDiagnosticItems.Any(item => item.EffectiveSeverity == severity);
        }

        private bool SelectedProjectSupportAnalyzers()
        {
            EnvDTE.Project project;
            return _tracker != null &&
                   _tracker.SelectedHierarchy != null &&
                   _tracker.SelectedHierarchy.TryGetProject(out project) &&
                   project.Object is VSProject3;
        }

        /// <summary>
        /// Handler for "Add Analyzer..." context menu on Analyzers folder node.
        /// </summary>
        internal void AddAnalyzerHandler(object sender, EventArgs args)
        {
            if (_analyzerReferenceManager != null)
            {
                _analyzerReferenceManager.ShowDialog();
            }
        }

        /// <summary>
        /// Handler for "Remove" context menu on individual Analyzer items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void RemoveAnalyzerHandler(object sender, EventArgs args)
        {
            foreach (var item in _tracker.SelectedAnalyzerItems)
            {
                item.Remove();
            }
        }

        internal void OpenRuleSetHandler(object sender, EventArgs args)
        {
            if (_tracker.SelectedFolder != null &&
                _serviceProvider != null)
            {
                var workspace = _tracker.SelectedFolder.Workspace as VisualStudioWorkspaceImpl;
                var projectId = _tracker.SelectedFolder.ProjectId;
                if (workspace != null)
                {
                    var project = (AbstractProject)workspace.GetHostProject(projectId);

                    if (project == null)
                    {
                        SendUnableToOpenRuleSetNotification(workspace, string.Format(SolutionExplorerShim.AnalyzersCommandHandler_CouldNotFindProject, projectId));
                        return;
                    }

                    if (project.RuleSetFile == null)
                    {
                        SendUnableToOpenRuleSetNotification(workspace, SolutionExplorerShim.AnalyzersCommandHandler_NoRuleSetFile);
                        return;
                    }

                    try
                    {
                        EnvDTE.DTE dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                        dte.ItemOperations.OpenFile(project.RuleSetFile.FilePath);
                    }
                    catch (Exception e)
                    {
                        SendUnableToOpenRuleSetNotification(workspace, e.Message);
                    }
                }
            }
        }

        private void SetSeverityHandler(object sender, EventArgs args)
        {
            var selectedItem = (MenuCommand)sender;
            ReportDiagnostic? selectedAction = MapSelectedItemToReportDiagnostic(selectedItem);

            if (!selectedAction.HasValue)
            {
                return;
            }

            var workspace = TryGetWorkspace() as VisualStudioWorkspaceImpl;

            if (workspace == null)
            {
                return;
            }

            foreach (var selectedDiagnostic in _tracker.SelectedDiagnosticItems)
            {
                var projectId = selectedDiagnostic.AnalyzerItem.AnalyzersFolder.ProjectId;
                var project = (AbstractProject)workspace.GetHostProject(projectId);

                if (project == null)
                {
                    SendUnableToUpdateRuleSetNotification(workspace, string.Format(SolutionExplorerShim.AnalyzersCommandHandler_CouldNotFindProject, projectId));
                    continue;
                }

                var pathToRuleSet = project.RuleSetFile?.FilePath;

                if (pathToRuleSet == null)
                {
                    SendUnableToUpdateRuleSetNotification(workspace, SolutionExplorerShim.AnalyzersCommandHandler_NoRuleSetFile);
                    continue;
                }

                try
                {
                    EnvDTE.Project envDteProject;
                    project.Hierarchy.TryGetProject(out envDteProject);

                    if (SdkUiUtilities.IsBuiltInRuleSet(pathToRuleSet, _serviceProvider))
                    {
                        pathToRuleSet = CreateCopyOfRuleSetForProject(pathToRuleSet, envDteProject);
                        if (pathToRuleSet == null)
                        {
                            SendUnableToUpdateRuleSetNotification(workspace, string.Format(SolutionExplorerShim.AnalyzersCommandHandler_CouldNotCreateRuleSetFile, envDteProject.Name));
                            continue;
                        }

                        var fileInfo = new FileInfo(pathToRuleSet);
                        fileInfo.IsReadOnly = false;
                    }

                    var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                    var waitIndicator = componentModel.GetService<IWaitIndicator>();
                    waitIndicator.Wait(
                        title: SolutionExplorerShim.AnalyzersCommandHandler_RuleSet,
                        message: string.Format(SolutionExplorerShim.AnalyzersCommandHandler_CheckingOutRuleSet, Path.GetFileName(pathToRuleSet)),
                        allowCancel: false,
                        action: c =>
                        {
                            if (envDteProject.DTE.SourceControl.IsItemUnderSCC(pathToRuleSet))
                            {
                                envDteProject.DTE.SourceControl.CheckOutItem(pathToRuleSet);
                            }
                        });

                    selectedDiagnostic.SetSeverity(selectedAction.Value, pathToRuleSet);
                }
                catch (Exception e)
                {
                    SendUnableToUpdateRuleSetNotification(workspace, e.Message);
                }
            }
        }

        private void OpenDiagnosticHelpLinkHandler(object sender, EventArgs e)
        {
            if (_tracker.SelectedDiagnosticItems.Length != 1 ||
                string.IsNullOrWhiteSpace(_tracker.SelectedDiagnosticItems[0].Descriptor.HelpLinkUri))
            {
                return;
            }

            string link = _tracker.SelectedDiagnosticItems[0].Descriptor.HelpLinkUri;

            Uri uri;
            if (BrowserHelper.TryGetUri(link, out uri))
            {
                BrowserHelper.StartBrowser(_serviceProvider, uri);
            }
        }

        private string CreateCopyOfRuleSetForProject(string pathToRuleSet, EnvDTE.Project envDteProject)
        {
            string fileName = GetNewRuleSetFileNameForProject(envDteProject);
            string projectDirectory = Path.GetDirectoryName(envDteProject.FullName);
            string fullFilePath = Path.Combine(projectDirectory, fileName);
            File.Copy(pathToRuleSet, fullFilePath);
            UpdateProjectConfigurationsToUseRuleSetFile(envDteProject, fileName);
            envDteProject.ProjectItems.AddFromFile(fullFilePath);

            return fullFilePath;
        }

        private void UpdateProjectConfigurationsToUseRuleSetFile(EnvDTE.Project envDteProject, string fileName)
        {
            foreach (EnvDTE.Configuration config in envDteProject.ConfigurationManager)
            {
                EnvDTE.Properties properties = config.Properties;

                try
                {
                    EnvDTE.Property codeAnalysisRuleSetFileProperty = properties.Item("CodeAnalysisRuleSet");

                    if (codeAnalysisRuleSetFileProperty != null)
                    {
                        codeAnalysisRuleSetFileProperty.Value = fileName;
                    }
                }
                catch (ArgumentException)
                {
                    // Unfortunately the properties collection sometimes throws an ArgumentException
                    // instead of returning null if the current configuration doesn't support CodeAnalysisRuleSet.
                    // Ignore it and move on.
                }
            }
        }

        private string GetNewRuleSetFileNameForProject(EnvDTE.Project envDteProject)
        {
            string projectName = envDteProject.Name;

            HashSet<string> projectItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ProjectItem item in envDteProject.ProjectItems)
            {
                projectItemNames.Add(item.Name);
            }

            string ruleSetName = projectName + ".ruleset";
            if (!projectItemNames.Contains(ruleSetName))
            {
                return ruleSetName;
            }

            for (int i = 1; i < int.MaxValue; i++)
            {
                ruleSetName = projectName + i + ".ruleset";
                if (!projectItemNames.Contains(ruleSetName))
                {
                    return ruleSetName;
                }
            }

            return null;
        }

        private static ReportDiagnostic? MapSelectedItemToReportDiagnostic(MenuCommand selectedItem)
        {
            ReportDiagnostic? selectedAction = null;

            if (selectedItem.CommandID.Guid == Guids.RoslynGroupId)
            {
                switch (selectedItem.CommandID.ID)
                {
                    case ID.RoslynCommands.SetSeverityError:
                        selectedAction = ReportDiagnostic.Error;
                        break;

                    case ID.RoslynCommands.SetSeverityWarning:
                        selectedAction = ReportDiagnostic.Warn;
                        break;

                    case ID.RoslynCommands.SetSeverityInfo:
                        selectedAction = ReportDiagnostic.Info;
                        break;

                    case ID.RoslynCommands.SetSeverityHidden:
                        selectedAction = ReportDiagnostic.Hidden;
                        break;

                    case ID.RoslynCommands.SetSeverityNone:
                        selectedAction = ReportDiagnostic.Suppress;
                        break;

                    default:
                        selectedAction = null;
                        break;
                }
            }

            return selectedAction;
        }

        private void SendUnableToOpenRuleSetNotification(Workspace workspace, string message)
        {
            SendErrorNotification(
                workspace,
                SolutionExplorerShim.AnalyzersCommandHandler_RuleSetFileCouldNotBeOpened,
                message);
        }

        private void SendUnableToUpdateRuleSetNotification(Workspace workspace, string message)
        {
            SendErrorNotification(
                workspace,
                SolutionExplorerShim.AnalyzersCommandHandler_RuleSetFileCouldNotBeUpdated,
                message);
        }

        private void SendErrorNotification(Workspace workspace, string title, string message)
        {
            var notificationService = workspace.Services.GetService<INotificationService>();

            notificationService.SendNotification(message, title, NotificationSeverity.Error);
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            DisableMenuItems();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            EnableMenuItems();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            EnableMenuItems();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        private void DisableMenuItems()
        {
            _addMenuItem.Enabled = false;
            _projectAddMenuItem.Enabled = false;
            _projectContextAddMenuItem.Enabled = false;
            _referencesContextAddMenuItem.Enabled = false;
            _removeMenuItem.Enabled = false;
        }

        private void EnableMenuItems()
        {
            _addMenuItem.Enabled = true;
            _projectAddMenuItem.Enabled = true;
            _projectContextAddMenuItem.Enabled = true;
            _referencesContextAddMenuItem.Enabled = true;
            _removeMenuItem.Enabled = true;
        }

        private Workspace TryGetWorkspace()
        {
            if (_workspace == null)
            {
                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                var provider = componentModel.DefaultExportProvider.GetExportedValueOrDefault<ISolutionExplorerWorkspaceProvider>();
                if (provider != null)
                {
                    _workspace = provider.GetWorkspace();
                }
            }

            return _workspace;
        }
    }
}
