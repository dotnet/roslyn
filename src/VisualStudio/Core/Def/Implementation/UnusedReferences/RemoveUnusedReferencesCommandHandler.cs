// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.PlatformUI;
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

        private readonly Lazy<IReferenceCleanupService> _lazyReferenceCleanupService;
        private readonly RemoveUnusedReferencesDialogProvider _unusedReferenceDialogProvider;
        private readonly VisualStudioWorkspace _workspace;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private IServiceProvider? _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveUnusedReferencesCommandHandler(
            RemoveUnusedReferencesDialogProvider unusedReferenceDialogProvider,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IUIThreadOperationExecutor threadOperationExecutor,
            VisualStudioWorkspace workspace)
        {
            _unusedReferenceDialogProvider = unusedReferenceDialogProvider;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _threadOperationExecutor = threadOperationExecutor;
            _workspace = workspace;

            _lazyReferenceCleanupService = new(() => workspace.Services.GetRequiredService<IReferenceCleanupService>());
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            Contract.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;

            // Hook up the "Remove Unused References" menu command for CPS based managed projects.
            var menuCommandService = (IMenuCommandService)_serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.RemoveUnusedReferences, Guids.RoslynGroupId, OnRemoveUnusedReferencesForSelectedProject, OnRemoveUnusedReferencesForSelectedProjectStatus);
            }
        }

        private void OnRemoveUnusedReferencesForSelectedProjectStatus(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;

            // Only show the "Remove Unused Reference" menu commands for CPS based managed projects.
            var visible = VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var hierarchy) &&
                hierarchy.IsCapabilityMatch("CPS") &&
                hierarchy.IsCapabilityMatch(".NET") &&
                _workspace.Options.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferences);
            var enabled = false;

            if (visible)
            {
                enabled = !VisualStudioCommandHandlerHelpers.IsBuildActive(_serviceProvider);
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
            if (VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var hierarchy))
            {
                Project? project = null;
                ImmutableArray<ReferenceUpdate> referenceUpdates = default;
                var status = _threadOperationExecutor.Execute(ServicesVSResources.Remove_Unused_References, ServicesVSResources.Analyzing_project_references, allowCancellation: true, showProgress: true, (operationContext) =>
                {
                    (project, referenceUpdates) = GetUnusedReferencesForProjectHierarchy(hierarchy, operationContext.UserCancellationToken);
                });

                if (status == UIThreadOperationStatus.Canceled)
                {
                    return;
                }

                if (project is null ||
                    referenceUpdates.IsEmpty)
                {
                    MessageDialog.Show(ServicesVSResources.Remove_Unused_References, ServicesVSResources.No_unused_references_were_found, MessageDialogCommandSet.Ok);
                    return;
                }

                var dialog = GetUnusedReferencesDialog(project, referenceUpdates);
                if (dialog.ShowDialog() == false)
                {
                    return;
                }

                // If we are removing, then that is a change or if we are newly marking a reference as TreatAsUsed,
                // then that is a change.
                var referenceChanges = referenceUpdates
                    .Where(update => update.Action != UpdateAction.TreatAsUsed || !update.ReferenceInfo.TreatAsUsed)
                    .ToImmutableArray();

                // If there are no changes, then we can return
                if (referenceChanges.IsEmpty)
                {
                    return;
                }

                // Since undo/redo is not supported, get confirmation that we should apply these changes.
                var result = MessageDialog.Show(ServicesVSResources.Remove_Unused_References, ServicesVSResources.This_action_cannot_be_undone_Do_you_wish_to_continue, MessageDialogCommandSet.YesNo);
                if (result == MessageDialogCommand.No)
                {
                    return;
                }

                _threadOperationExecutor.Execute(ServicesVSResources.Remove_Unused_References, ServicesVSResources.Updating_project_references, allowCancellation: false, showProgress: true, (operationContext) =>
                {
                    ApplyUnusedReferenceUpdates(project, referenceChanges, CancellationToken.None);
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

            var project = _workspace.CurrentSolution.GetRequiredProject(projectId);
            var unusedReferences = GetUnusedReferencesForProject(project, projectAssetsFile, targetFrameworkMoniker, cancellationToken);

            return (project, unusedReferences);
        }

        private ImmutableArray<ReferenceUpdate> GetUnusedReferencesForProject(Project project, string projectAssetsFile, string targetFrameworkMoniker, CancellationToken cancellationToken)
        {
            ImmutableArray<ReferenceInfo> unusedReferences = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var projectReferences = await _lazyReferenceCleanupService.Value.GetProjectReferencesAsync(project.FilePath!, cancellationToken).ConfigureAwait(true);
                var references = ProjectAssetsReader.ReadReferences(projectReferences, projectAssetsFile, targetFrameworkMoniker);

                return await UnusedReferencesService.GetUnusedReferencesAsync(project, references, cancellationToken).ConfigureAwait(true);
            });

            var referenceUpdates = unusedReferences
                .Select(reference => new ReferenceUpdate(reference.TreatAsUsed ? UpdateAction.TreatAsUsed : UpdateAction.Remove, reference))
                .ToImmutableArray();

            return referenceUpdates;
        }

        private Window GetUnusedReferencesDialog(Project project, ImmutableArray<ReferenceUpdate> referenceUpdates)
        {
            var dialog = _unusedReferenceDialogProvider.CreateDialog(project, referenceUpdates);

            var uiShell = _serviceProvider?.GetService<SVsUIShell, IVsUIShell>();
            if (uiShell is null)
            {
                return dialog;
            }

            uiShell.GetDialogOwnerHwnd(out var ownerHwnd);

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
                () => UnusedReferencesService.UpdateReferencesAsync(project, referenceUpdates, cancellationToken));
        }

        private static bool TryGetPropertyValue(IVsHierarchy hierarchy, string propertyName, [NotNullWhen(returnValue: true)] out string? propertyValue)
        {
            if (hierarchy is not IVsBuildPropertyStorage storage)
            {
                propertyValue = null;
                return false;
            }

            return ErrorHandler.Succeeded(storage.GetPropertyValue(propertyName, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out propertyValue));
        }
    }
}
