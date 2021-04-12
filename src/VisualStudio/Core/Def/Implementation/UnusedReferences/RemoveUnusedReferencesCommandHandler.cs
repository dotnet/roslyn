﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
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

            var experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();

            // If the option hasn't been expicitly set then fallback to whether this is enabled as part of an experiment.
            var isOptionEnabled = _workspace.Options.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferences)
                ?? experimentationService.IsExperimentEnabled(WellKnownExperimentNames.RemoveUnusedReferences);

            var isDotNetCpsProject = VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var hierarchy) &&
                hierarchy.IsCapabilityMatch("CPS") &&
                hierarchy.IsCapabilityMatch(".NET");

            // Only show the "Remove Unused Reference" menu commands for CPS based managed projects.
            var visible = isOptionEnabled && isDotNetCpsProject;
            var enabled = false;

            if (visible)
            {
                enabled = !VisualStudioCommandHandlerHelpers.IsBuildActive();
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
                Solution? solution = null;
                string? projectFilePath = null;
                ImmutableArray<ReferenceUpdate> referenceUpdates = default;
                var status = _threadOperationExecutor.Execute(ServicesVSResources.Remove_Unused_References, ServicesVSResources.Analyzing_project_references, allowCancellation: true, showProgress: true, (operationContext) =>
                {
                    (solution, projectFilePath, referenceUpdates) = GetUnusedReferencesForProjectHierarchy(hierarchy, operationContext.UserCancellationToken);
                });

                if (status == UIThreadOperationStatus.Canceled)
                {
                    return;
                }

                if (solution is null ||
                    projectFilePath is not string { Length: > 0 } ||
                    referenceUpdates.IsEmpty)
                {
                    MessageDialog.Show(ServicesVSResources.Remove_Unused_References, ServicesVSResources.No_unused_references_were_found, MessageDialogCommandSet.Ok);
                    return;
                }

                var dialog = _unusedReferenceDialogProvider.CreateDialog();
                if (dialog.ShowModal(solution, projectFilePath, referenceUpdates) == false)
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
                    ApplyUnusedReferenceUpdates(solution, projectFilePath, referenceChanges, CancellationToken.None);
                });
            }

            return;
        }

        private (Solution?, string?, ImmutableArray<ReferenceUpdate>) GetUnusedReferencesForProjectHierarchy(
            IVsHierarchy projectHierarchy,
            CancellationToken cancellationToken)
        {
            if (!TryGetPropertyValue(projectHierarchy, ProjectAssetsFilePropertyName, out var projectAssetsFile))
            {
                return (null, null, ImmutableArray<ReferenceUpdate>.Empty);
            }

            var projectFilePath = projectHierarchy.TryGetProjectFilePath();
            if (string.IsNullOrEmpty(projectFilePath))
            {
                return (null, null, ImmutableArray<ReferenceUpdate>.Empty);
            }

            var solution = _workspace.CurrentSolution;

            var unusedReferences = GetUnusedReferencesForProject(solution, projectFilePath!, projectAssetsFile, cancellationToken);

            return (solution, projectFilePath, unusedReferences);
        }

        private ImmutableArray<ReferenceUpdate> GetUnusedReferencesForProject(Solution solution, string projectFilePath, string projectAssetsFile, CancellationToken cancellationToken)
        {
            ImmutableArray<ReferenceInfo> unusedReferences = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var projectReferences = await _lazyReferenceCleanupService.Value.GetProjectReferencesAsync(projectFilePath, cancellationToken).ConfigureAwait(true);
                var references = ProjectAssetsReader.ReadReferences(projectReferences, projectAssetsFile);

                return await UnusedReferencesRemover.GetUnusedReferencesAsync(solution, projectFilePath, references, cancellationToken).ConfigureAwait(true);
            });

            var referenceUpdates = unusedReferences
                .Select(reference => new ReferenceUpdate(reference.TreatAsUsed ? UpdateAction.TreatAsUsed : UpdateAction.Remove, reference))
                .ToImmutableArray();

            return referenceUpdates;
        }

        private void ApplyUnusedReferenceUpdates(Solution solution, string projectFilePath, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
        {
            ThreadHelper.JoinableTaskFactory.Run(
                () => UnusedReferencesRemover.UpdateReferencesAsync(solution, projectFilePath, referenceUpdates, cancellationToken));
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
