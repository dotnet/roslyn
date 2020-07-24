// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using VSLangProj140;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export]
    internal class AnalyzersCommandHandler : IAnalyzersCommandHandler, IVsUpdateSolutionEvents
    {
        private readonly AnalyzerItemsTracker _tracker;
        private readonly AnalyzerReferenceManager _analyzerReferenceManager;
        private readonly IServiceProvider _serviceProvider;

        private ContextMenuController _analyzerFolderContextMenuController;
        private ContextMenuController _analyzerContextMenuController;
        private ContextMenuController _diagnosticContextMenuController;

        // Analyzers folder context menu items
        private MenuCommand _addMenuItem;

        // Analyzer context menu items
        private MenuCommand _removeMenuItem;

        // Diagnostic context menu items
        private MenuCommand _setSeverityDefaultMenuItem;
        private MenuCommand _setSeverityErrorMenuItem;
        private MenuCommand _setSeverityWarningMenuItem;
        private MenuCommand _setSeverityInfoMenuItem;
        private MenuCommand _setSeverityHiddenMenuItem;
        private MenuCommand _setSeverityNoneMenuItem;
        private MenuCommand _openHelpLinkMenuItem;

        // Other menu items
        private MenuCommand _projectAddMenuItem;
        private MenuCommand _projectContextAddMenuItem;
        private MenuCommand _referencesContextAddMenuItem;
        private MenuCommand _setActiveRuleSetMenuItem;

        private Workspace _workspace;

        private bool _allowProjectSystemOperations = true;
        private bool _initialized;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public AnalyzersCommandHandler(
            AnalyzerItemsTracker tracker,
            AnalyzerReferenceManager analyzerReferenceManager,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _tracker = tracker;
            _analyzerReferenceManager = analyzerReferenceManager;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Hook up the context menu handlers.
        /// </summary>
        public void Initialize(IMenuCommandService menuCommandService)
        {
            if (menuCommandService != null)
            {
                // Analyzers folder context menu items
                _addMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.AddAnalyzer, AddAnalyzerHandler);
                _ = AddCommandHandler(menuCommandService, ID.RoslynCommands.OpenRuleSet, OpenRuleSetHandler);

                // Analyzer context menu items
                _removeMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.RemoveAnalyzer, RemoveAnalyzerHandler);

                // Diagnostic context menu items
                _setSeverityDefaultMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityDefault, SetSeverityHandler);
                _setSeverityErrorMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityError, SetSeverityHandler);
                _setSeverityWarningMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityWarning, SetSeverityHandler);
                _setSeverityInfoMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityInfo, SetSeverityHandler);
                _setSeverityHiddenMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityHidden, SetSeverityHandler);
                _setSeverityNoneMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetSeverityNone, SetSeverityHandler);
                _openHelpLinkMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.OpenDiagnosticHelpLink, OpenDiagnosticHelpLinkHandler);

                // Other menu items
                _projectAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ProjectAddAnalyzer, AddAnalyzerHandler);
                _projectContextAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ProjectContextAddAnalyzer, AddAnalyzerHandler);
                _referencesContextAddMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.ReferencesContextAddAnalyzer, AddAnalyzerHandler);
                _setActiveRuleSetMenuItem = AddCommandHandler(menuCommandService, ID.RoslynCommands.SetActiveRuleSet, SetActiveRuleSetHandler);

                UpdateOtherMenuItemsVisibility();

                if (_tracker != null)
                {
                    _tracker.SelectedHierarchyItemChanged += SelectedHierarchyItemChangedHandler;
                }

                var buildManager = (IVsSolutionBuildManager)_serviceProvider.GetService(typeof(SVsSolutionBuildManager));
                buildManager.AdviseUpdateSolutionEvents(this, out var cookie);

                _initialized = true;
            }
        }

        public IContextMenuController AnalyzerFolderContextMenuController
        {
            get
            {
                if (_analyzerFolderContextMenuController == null)
                {
                    _analyzerFolderContextMenuController = new ContextMenuController(
                        ID.RoslynCommands.AnalyzerFolderContextMenu,
                        ShouldShowAnalyzerFolderContextMenu,
                        UpdateAnalyzerFolderContextMenu);
                }

                return _analyzerFolderContextMenuController;
            }
        }

        private bool ShouldShowAnalyzerFolderContextMenu(IEnumerable<object> items)
        {
            return items.Count() == 1;
        }

        private void UpdateAnalyzerFolderContextMenu()
        {
            if (_addMenuItem != null)
            {
                _addMenuItem.Visible = SelectedProjectSupportsAnalyzers();
                _addMenuItem.Enabled = _allowProjectSystemOperations;
            }
        }

        public IContextMenuController AnalyzerContextMenuController
        {
            get
            {
                if (_analyzerContextMenuController == null)
                {
                    _analyzerContextMenuController = new ContextMenuController(
                        ID.RoslynCommands.AnalyzerContextMenu,
                        ShouldShowAnalyzerContextMenu,
                        UpdateAnalyzerContextMenu);
                }

                return _analyzerContextMenuController;
            }
        }

        private bool ShouldShowAnalyzerContextMenu(IEnumerable<object> items)
        {
            return items.All(item => item is AnalyzerItem);
        }

        private void UpdateAnalyzerContextMenu()
        {
            if (_removeMenuItem != null)
            {
                _removeMenuItem.Enabled = _allowProjectSystemOperations;
            }
        }

        public IContextMenuController DiagnosticContextMenuController
        {
            get
            {
                if (_diagnosticContextMenuController == null)
                {
                    _diagnosticContextMenuController = new ContextMenuController(
                        ID.RoslynCommands.DiagnosticContextMenu,
                        ShouldShowDiagnosticContextMenu,
                        UpdateDiagnosticContextMenu);
                }

                return _diagnosticContextMenuController;
            }
        }

        private bool ShouldShowDiagnosticContextMenu(IEnumerable<object> items)
        {
            return _initialized && items.All(item => item is BaseDiagnosticItem);
        }

        private void UpdateDiagnosticContextMenu()
        {
            Debug.Assert(_initialized);

            UpdateSeverityMenuItemsChecked();
            UpdateSeverityMenuItemsEnabled();
            UpdateOpenHelpLinkMenuItemVisibility();
        }

        private MenuCommand AddCommandHandler(IMenuCommandService menuCommandService, int roslynCommand, EventHandler handler)
        {
            var commandID = new CommandID(Guids.RoslynGroupId, roslynCommand);
            var menuCommand = new MenuCommand(handler, commandID);
            menuCommandService.AddCommand(menuCommand);

            return menuCommand;
        }

        private void SelectedHierarchyItemChangedHandler(object sender, EventArgs e)
        {
            UpdateOtherMenuItemsVisibility();
        }

        private void UpdateOtherMenuItemsVisibility()
        {
            var selectedProjectSupportsAnalyzers = SelectedProjectSupportsAnalyzers();
            _projectAddMenuItem.Visible = selectedProjectSupportsAnalyzers;
            _projectContextAddMenuItem.Visible = selectedProjectSupportsAnalyzers && _tracker.SelectedItemId == VSConstants.VSITEMID_ROOT;
            _referencesContextAddMenuItem.Visible = selectedProjectSupportsAnalyzers;
            _setActiveRuleSetMenuItem.Visible = selectedProjectSupportsAnalyzers &&
                                                _tracker.SelectedHierarchy.TryGetItemName(_tracker.SelectedItemId, out var itemName) &&
                                                Path.GetExtension(itemName).Equals(".ruleset", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateOtherMenuItemsEnabled()
        {
            _projectAddMenuItem.Enabled = _allowProjectSystemOperations;
            _projectContextAddMenuItem.Enabled = _allowProjectSystemOperations;
            _referencesContextAddMenuItem.Enabled = _allowProjectSystemOperations;
            _removeMenuItem.Enabled = _allowProjectSystemOperations;
        }

        private void UpdateOpenHelpLinkMenuItemVisibility()
        {
            _openHelpLinkMenuItem.Visible = _tracker.SelectedDiagnosticItems.Length == 1 &&
                                            _tracker.SelectedDiagnosticItems[0].GetHelpLink() != null;
        }

        private void UpdateSeverityMenuItemsChecked()
        {
            _setSeverityDefaultMenuItem.Checked = false;
            _setSeverityErrorMenuItem.Checked = false;
            _setSeverityWarningMenuItem.Checked = false;
            _setSeverityInfoMenuItem.Checked = false;
            _setSeverityHiddenMenuItem.Checked = false;
            _setSeverityNoneMenuItem.Checked = false;

            var workspace = TryGetWorkspace() as VisualStudioWorkspace;
            if (workspace == null)
            {
                return;
            }

            var selectedItemSeverities = new HashSet<ReportDiagnostic>();

            var groups = _tracker.SelectedDiagnosticItems.GroupBy(item => item.ProjectId);

            foreach (var group in groups)
            {
                var project = workspace.CurrentSolution.GetProject(group.Key);
                if (project == null)
                {
                    continue;
                }

                var analyzerConfigSpecificDiagnosticOptions = project.GetAnalyzerConfigSpecialDiagnosticOptions();

                foreach (var diagnosticItem in group)
                {
                    var severity = ReportDiagnostic.Default;
                    if (project.CompilationOptions.SpecificDiagnosticOptions.ContainsKey(diagnosticItem.Descriptor.Id) ||
                        analyzerConfigSpecificDiagnosticOptions.ContainsKey(diagnosticItem.Descriptor.Id))
                    {
                        // Severity is overridden by end user.
                        severity = diagnosticItem.Descriptor.GetEffectiveSeverity(project.CompilationOptions, analyzerConfigSpecificDiagnosticOptions);
                    }

                    selectedItemSeverities.Add(severity);
                }
            }

            if (selectedItemSeverities.Count != 1)
            {
                return;
            }

            switch (selectedItemSeverities.Single())
            {
                case ReportDiagnostic.Default:
                    _setSeverityDefaultMenuItem.Checked = true;
                    break;
                case ReportDiagnostic.Error:
                    _setSeverityErrorMenuItem.Checked = true;
                    break;
                case ReportDiagnostic.Warn:
                    _setSeverityWarningMenuItem.Checked = true;
                    break;
                case ReportDiagnostic.Info:
                    _setSeverityInfoMenuItem.Checked = true;
                    break;
                case ReportDiagnostic.Hidden:
                    _setSeverityHiddenMenuItem.Checked = true;
                    break;
                case ReportDiagnostic.Suppress:
                    _setSeverityNoneMenuItem.Checked = true;
                    break;
                default:
                    break;
            }
        }

        private void UpdateSeverityMenuItemsEnabled()
        {
            var configurable = !_tracker.SelectedDiagnosticItems.Any(item => item.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable));

            _setSeverityDefaultMenuItem.Enabled = configurable;
            _setSeverityErrorMenuItem.Enabled = configurable;
            _setSeverityWarningMenuItem.Enabled = configurable;
            _setSeverityInfoMenuItem.Enabled = configurable;
            _setSeverityHiddenMenuItem.Enabled = configurable;
            _setSeverityNoneMenuItem.Enabled = configurable;
        }

        private bool SelectedProjectSupportsAnalyzers()
        {
            return _tracker != null &&
                   _tracker.SelectedHierarchy != null &&
                   _tracker.SelectedHierarchy.TryGetProject(out var project) &&
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
                var workspace = _tracker.SelectedFolder.Workspace as VisualStudioWorkspace;
                var projectId = _tracker.SelectedFolder.ProjectId;
                if (workspace != null)
                {
                    var ruleSetFile = workspace.TryGetRuleSetPathForProject(projectId);

                    if (ruleSetFile == null)
                    {
                        SendUnableToOpenRuleSetNotification(workspace, SolutionExplorerShim.No_rule_set_file_is_specified_or_the_file_does_not_exist);
                        return;
                    }

                    try
                    {
                        var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                        dte.ItemOperations.OpenFile(ruleSetFile);
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
            var selectedAction = MapSelectedItemToReportDiagnostic(selectedItem);

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
                var projectId = selectedDiagnostic.ProjectId;
                var pathToRuleSet = workspace.TryGetRuleSetPathForProject(projectId);

                var project = workspace.CurrentSolution.GetProject(projectId);
                var pathToAnalyzerConfigDoc = project?.TryGetAnalyzerConfigPathForProjectConfiguration();

                if (pathToRuleSet == null && pathToAnalyzerConfigDoc == null)
                {
                    SendUnableToUpdateRuleSetNotification(workspace, SolutionExplorerShim.No_rule_set_file_is_specified_or_the_file_does_not_exist);
                    continue;
                }

                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                var waitIndicator = componentModel.GetService<IWaitIndicator>();
                var editHandlerService = componentModel.GetService<ICodeActionEditHandlerService>();

                try
                {
                    var envDteProject = workspace.TryGetDTEProject(projectId);

                    if (pathToRuleSet == null || SdkUiUtilities.IsBuiltInRuleSet(pathToRuleSet, _serviceProvider))
                    {
                        // If project is using the default built-in ruleset or no ruleset, then prefer .editorconfig for severity configuration.
                        if (pathToAnalyzerConfigDoc != null)
                        {
                            waitIndicator.Wait(
                                title: ServicesVSResources.Updating_severity,
                                message: ServicesVSResources.Updating_severity,
                                allowCancel: true,
                                action: waitContext =>
                                {
                                    var newSolution = selectedDiagnostic.GetSolutionWithUpdatedAnalyzerConfigSeverityAsync(selectedAction.Value, project, waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);
                                    var operations = ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
                                    editHandlerService.Apply(
                                        _workspace,
                                        fromDocument: null,
                                        operations: operations,
                                        title: ServicesVSResources.Updating_severity,
                                        progressTracker: waitContext.ProgressTracker,
                                        cancellationToken: waitContext.CancellationToken);
                                });
                            continue;
                        }

                        // Otherwise, fall back to using ruleset.
                        if (pathToRuleSet == null)
                        {
                            SendUnableToUpdateRuleSetNotification(workspace, SolutionExplorerShim.No_rule_set_file_is_specified_or_the_file_does_not_exist);
                            continue;
                        }

                        pathToRuleSet = CreateCopyOfRuleSetForProject(pathToRuleSet, envDteProject);
                        if (pathToRuleSet == null)
                        {
                            SendUnableToUpdateRuleSetNotification(workspace, string.Format(SolutionExplorerShim.Could_not_create_a_rule_set_for_project_0, envDteProject.Name));
                            continue;
                        }

                        var fileInfo = new FileInfo(pathToRuleSet);
                        fileInfo.IsReadOnly = false;
                    }

                    waitIndicator.Wait(
                        title: SolutionExplorerShim.Rule_Set,
                        message: string.Format(SolutionExplorerShim.Checking_out_0_for_editing, Path.GetFileName(pathToRuleSet)),
                        allowCancel: false,
                        action: c =>
                        {
                            if (envDteProject.DTE.SourceControl.IsItemUnderSCC(pathToRuleSet))
                            {
                                envDteProject.DTE.SourceControl.CheckOutItem(pathToRuleSet);
                            }
                        });

                    selectedDiagnostic.SetRuleSetSeverity(selectedAction.Value, pathToRuleSet);
                }
                catch (Exception e)
                {
                    SendUnableToUpdateRuleSetNotification(workspace, e.Message);
                }
            }
        }

        private void OpenDiagnosticHelpLinkHandler(object sender, EventArgs e)
        {
            if (_tracker.SelectedDiagnosticItems.Length != 1)
            {
                return;
            }

            var uri = _tracker.SelectedDiagnosticItems[0].GetHelpLink();
            if (uri != null)
            {
                BrowserHelper.StartBrowser(uri);
            }
        }

        private void SetActiveRuleSetHandler(object sender, EventArgs e)
        {
            if (_tracker.SelectedHierarchy.TryGetProject(out var project) &&
                _tracker.SelectedHierarchy.TryGetCanonicalName(_tracker.SelectedItemId, out var ruleSetFileFullPath))
            {
                var projectDirectoryFullPath = Path.GetDirectoryName(project.FullName);
                var ruleSetFileRelativePath = PathUtilities.GetRelativePath(projectDirectoryFullPath, ruleSetFileFullPath);

                UpdateProjectConfigurationsToUseRuleSetFile(project, ruleSetFileRelativePath);
            }
        }

        private string CreateCopyOfRuleSetForProject(string pathToRuleSet, EnvDTE.Project envDteProject)
        {
            var fileName = GetNewRuleSetFileNameForProject(envDteProject);
            var projectDirectory = Path.GetDirectoryName(envDteProject.FullName);
            var fullFilePath = Path.Combine(projectDirectory, fileName);
            File.Copy(pathToRuleSet, fullFilePath);
            UpdateProjectConfigurationsToUseRuleSetFile(envDteProject, fileName);
            envDteProject.ProjectItems.AddFromFile(fullFilePath);

            return fullFilePath;
        }

        private void UpdateProjectConfigurationsToUseRuleSetFile(EnvDTE.Project envDteProject, string fileName)
        {
            foreach (EnvDTE.Configuration config in envDteProject.ConfigurationManager)
            {
                var properties = config.Properties;

                try
                {
                    var codeAnalysisRuleSetFileProperty = properties?.Item("CodeAnalysisRuleSet");

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
            var projectName = envDteProject.Name;

            var projectItemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ProjectItem item in envDteProject.ProjectItems)
            {
                projectItemNames.Add(item.Name);
            }

            var ruleSetName = projectName + ".ruleset";
            if (!projectItemNames.Contains(ruleSetName))
            {
                return ruleSetName;
            }

            for (var i = 1; i < int.MaxValue; i++)
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
                    case ID.RoslynCommands.SetSeverityDefault:
                        selectedAction = ReportDiagnostic.Default;
                        break;

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
                SolutionExplorerShim.The_rule_set_file_could_not_be_opened,
                message);
        }

        private void SendUnableToUpdateRuleSetNotification(Workspace workspace, string message)
        {
            SendErrorNotification(
                workspace,
                SolutionExplorerShim.The_rule_set_file_could_not_be_updated,
                message);
        }

        private void SendErrorNotification(Workspace workspace, string message1, string message2)
        {
            var notificationService = workspace.Services.GetService<INotificationService>();

            notificationService.SendNotification(message1 + Environment.NewLine + Environment.NewLine + message2, severity: NotificationSeverity.Error);
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            _allowProjectSystemOperations = false;
            UpdateOtherMenuItemsEnabled();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            _allowProjectSystemOperations = true;
            UpdateOtherMenuItemsEnabled();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            _allowProjectSystemOperations = true;
            UpdateOtherMenuItemsEnabled();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
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
