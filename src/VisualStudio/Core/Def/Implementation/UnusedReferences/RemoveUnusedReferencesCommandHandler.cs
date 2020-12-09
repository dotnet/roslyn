// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    [Export(typeof(RemoveUnusedReferencesCommandHandler)), Shared]
    internal sealed class RemoveUnusedReferencesCommandHandler
    {
        private const string ProjectAssetsFilePropertyName = "ProjectAssetsFile";

        private readonly IReferenceCleanupService _referenceCleanupService;
        private readonly IUnusedReferencesService _unusedReferencesService;
        private readonly RemoveUnusedReferencesDialogProvider _unusedReferenceDialogProvider;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private IServiceProvider? _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveUnusedReferencesCommandHandler(
            RemoveUnusedReferencesDialogProvider unusedReferenceDialogProvider,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IUIThreadOperationExecutor threadOperationExecutor,
            VisualStudioWorkspaceImpl workspace)
        {
            _unusedReferenceDialogProvider = unusedReferenceDialogProvider;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _threadOperationExecutor = threadOperationExecutor;
            _workspace = workspace;

            _referenceCleanupService = workspace.Services.GetRequiredService<IReferenceCleanupService>();
            _unusedReferencesService = workspace.Services.GetRequiredService<IUnusedReferencesService>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            Contract.ThrowIfNull(_serviceProvider);

            // Hook up the "Run Code Analysis" menu command for CPS based managed projects.
            var menuCommandService = (IMenuCommandService)_serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                AddCommand(menuCommandService, ID.RoslynCommands.RemoveUnusedReferences, Guids.RoslynGroupId, OnRemoveUnusedReferencesForSelectedProject, OnRemoveUnusedReferencesForSelectedProjectStatus);
            }

            return;

            // Local functions
            static OleMenuCommand AddCommand(
                IMenuCommandService menuCommandService,
                int commandId,
                Guid commandGroup,
                EventHandler invokeHandler,
                EventHandler beforeQueryStatus)
            {
                var commandIdWithGroupId = new CommandID(commandGroup, commandId);
                var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
                menuCommandService.AddCommand(command);
                return command;
            }
        }

        private void OnRemoveUnusedReferencesForSelectedProjectStatus(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;

            // We hook up the "Run Code Analysis" menu commands for CPS based managed projects.
            // These commands are already hooked up for csproj based projects in StanCore, but those will eventually go away.
            var visible = TryGetSelectedProjectHierarchy(out var hierarchy) &&
                hierarchy.IsCapabilityMatch("CPS") &&
                hierarchy.IsCapabilityMatch(".NET");
            var enabled = false;

            if (visible)
            {
                if (hierarchy!.TryGetProject(out _))
                {
                    // Change to show the name of the project as part of the menu item display text.
                    // command.Text = string.Format("Remove Unused References for {0}", project!.Name);
                }

                enabled = !IsBuildActive();
            }

            if (command.Visible != visible)
            {
                command.Visible = visible;
            }
            if (command.Enabled != enabled)
            {
                command.Enabled = enabled;
            }
        }

        private void OnRemoveUnusedReferencesForSelectedProject(object sender, EventArgs args)
        {
            if (TryGetSelectedProjectHierarchy(out var hierarchy))
            {
                Project? project = null;
                ImmutableArray<ReferenceUpdate> referenceUpdates;
                _threadOperationExecutor.Execute(ServicesVSResources.Remove_Unused_References, ServicesVSResources.Analyzing_project_references, allowCancellation: true, showProgress: true, (operationContext) =>
                {
                    (project, referenceUpdates) = GetUnusedReferencesForProjectHierarchy(hierarchy, operationContext.UserCancellationToken);
                });

                if (project is null ||
                    referenceUpdates.IsEmpty)
                {
                    return;
                }

                var dialog = GetUnusedReferencesDialog(project, referenceUpdates);
                if (dialog.ShowDialog() == false)
                {
                    return;
                }

                _threadOperationExecutor.Execute(ServicesVSResources.Remove_Unused_References, ServicesVSResources.Updating_project_references, allowCancellation: false, showProgress: true, (operationContext) =>
                {
                    ApplyUnusedReferenceUpdates(project, referenceUpdates, CancellationToken.None);
                });
            }

            return;
        }

        private (Project?, ImmutableArray<ReferenceUpdate>) GetUnusedReferencesForProjectHierarchy(IVsHierarchy projectHierarchy, CancellationToken cancellationToken)
        {
            if (!TryGetPropertyValue(projectHierarchy, ProjectAssetsFilePropertyName, out var projectAssetsFile) ||
                !projectHierarchy.TryGetTargetFrameworkMoniker((uint)VSConstants.VSITEMID.Root, out var targetFrameworkMoniker))
            {
                return (null, ImmutableArray<ReferenceUpdate>.Empty);
            }

            var projectMap = _workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
            var projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(projectHierarchy, VSConstants.VSITEMID_ROOT);

            if (!projectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out var projectId))
            {
                return (null, ImmutableArray<ReferenceUpdate>.Empty);
            }

            var project = _workspace.CurrentSolution.GetProject(projectId)!;
            var unusedReferences = GetUnusedReferencesForProject(project, projectAssetsFile!, targetFrameworkMoniker, cancellationToken);

            return (project, unusedReferences);
        }

        private ImmutableArray<ReferenceUpdate> GetUnusedReferencesForProject(Project project, string projectAssetsFile, string targetFrameworkMoniker, CancellationToken cancellationToken)
        {
            ImmutableArray<ReferenceInfo> unusedReferences = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var projectReferences = await _referenceCleanupService.GetProjectReferencesAsync(project.FilePath, cancellationToken).ConfigureAwait(true);
                var references = ProjectAssetsReader.ReadReferences(projectReferences, projectAssetsFile, targetFrameworkMoniker);

                return await _unusedReferencesService.GetUnusedReferencesAsync(project, references, targetFrameworkMoniker, cancellationToken).ConfigureAwait(true);
            });

            var referenceUpdates = unusedReferences
                .Select(reference => new ReferenceUpdate(reference.TreatAsUsed ? UpdateAction.TreatAsUsed : UpdateAction.Remove, reference))
                .ToImmutableArray();

            return referenceUpdates;
        }

        private Window GetUnusedReferencesDialog(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates)
        {
            var uiShell = _serviceProvider.GetService<SVsUIShell, IVsUIShell>();
            uiShell.GetDialogOwnerHwnd(out var ownerHwnd);

            var dialog = _unusedReferenceDialogProvider.CreateDialog(project, referenceUpdates);

            var windowHelper = new WindowInteropHelper(dialog)
            {
                Owner = ownerHwnd
            };

            uiShell.CenterDialogOnWindow(windowHelper.Handle, IntPtr.Zero);

            return dialog;
        }

        private void ApplyUnusedReferenceUpdates(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            ThreadHelper.JoinableTaskFactory.Run(
                () => _unusedReferencesService.UpdateReferencesAsync(project, referenceUpdates, cancellationToken));
        }

        private static bool TryGetPropertyValue(IVsHierarchy hierarchy, string propertyName, out string? propertyValue)
        {
            if (hierarchy is not IVsBuildPropertyStorage storage)
            {
                propertyValue = null;
                return false;
            }

            return ErrorHandler.Succeeded(storage.GetPropertyValue(propertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out propertyValue));
        }

        private bool TryGetSelectedProjectHierarchy([NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy)
        {
            hierarchy = null;

            // Get the DTE service and make sure there is an open solution
            if (_serviceProvider?.GetService(typeof(EnvDTE.DTE)) is not EnvDTE.DTE dte ||
                dte.Solution == null)
            {
                return false;
            }

            var selectionHierarchy = IntPtr.Zero;
            var selectionContainer = IntPtr.Zero;

            // Get the current selection in the shell
            if (_serviceProvider.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection)
            {
                try
                {
                    monitorSelection.GetCurrentSelection(out selectionHierarchy, out var itemId, out var multiSelect, out selectionContainer);
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        hierarchy = Marshal.GetObjectForIUnknown(selectionHierarchy) as IVsHierarchy;
                        Debug.Assert(hierarchy != null);
                        return hierarchy != null;
                    }
                }
                catch (Exception)
                {
                    // If anything went wrong, just ignore it
                }
                finally
                {
                    // Make sure we release the COM pointers in any case
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        Marshal.Release(selectionHierarchy);
                    }

                    if (selectionContainer != IntPtr.Zero)
                    {
                        Marshal.Release(selectionContainer);
                    }
                }
            }

            return false;
        }

        private bool IsBuildActive()
        {
            // Using KnownUIContexts is faster in case when SBM's package was not loaded yet
            if (KnownUIContexts.SolutionBuildingContext != null)
            {
                return KnownUIContexts.SolutionBuildingContext.IsActive;
            }
            else
            {
                // Unlikely case that above service is not available, let's try Solution Build Manager
                if (_serviceProvider?.GetService(typeof(SVsSolutionBuildManager)) is IVsSolutionBuildManager buildManager)
                {
                    buildManager.QueryBuildManagerBusy(out var buildBusy);
                    return buildBusy != 0;
                }
                else
                {
                    Debug.Fail("Unable to determine whether build is active or not");
                    return true;
                }
            }
        }
    }
}
