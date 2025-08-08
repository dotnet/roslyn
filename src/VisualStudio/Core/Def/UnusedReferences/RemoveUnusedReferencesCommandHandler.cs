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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences;

[Export(typeof(RemoveUnusedReferencesCommandHandler)), Shared]
internal sealed class RemoveUnusedReferencesCommandHandler
{
    private const string ProjectAssetsFilePropertyName = "ProjectAssetsFile";
    private readonly IThreadingContext _threadingContext;
    private readonly RemoveUnusedReferencesDialogProvider _unusedReferenceDialogProvider;
    private readonly VisualStudioWorkspace _workspace;
    private readonly IGlobalOptionService _globalOptions;
    private readonly IUIThreadOperationExecutor _threadOperationExecutor;
    private IServiceProvider? _serviceProvider;

    private IReferenceCleanupService ReferenceCleanupService
        => _workspace.Services.GetRequiredService<IReferenceCleanupService>();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RemoveUnusedReferencesCommandHandler(
        IThreadingContext threadingContext,
        RemoveUnusedReferencesDialogProvider unusedReferenceDialogProvider,
        IUIThreadOperationExecutor threadOperationExecutor,
        VisualStudioWorkspace workspace,
        IGlobalOptionService globalOptions)
    {
        _threadingContext = threadingContext;
        _unusedReferenceDialogProvider = unusedReferenceDialogProvider;
        _threadOperationExecutor = threadOperationExecutor;
        _workspace = workspace;
        _globalOptions = globalOptions;
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(serviceProvider);

        _serviceProvider = (IServiceProvider)serviceProvider;

        // Hook up the "Remove Unused References" menu command for CPS based managed projects.
        var menuCommandService = await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        if (menuCommandService != null)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.RemoveUnusedReferences, Guids.RoslynGroupId, OnRemoveUnusedReferencesForSelectedProject, OnRemoveUnusedReferencesForSelectedProjectStatus);
        }
    }

    private void OnRemoveUnusedReferencesForSelectedProjectStatus(object sender, EventArgs e)
    {
        var command = (OleMenuCommand)sender;

        // If the value is null it means user loads the value from previous build (at that moment it is in experiment)
        // Since the feature is on by default now, just set it to true
        var isOptionEnabled = _globalOptions.GetOption(FeatureOnOffOptions.OfferRemoveUnusedReferences) ?? true;

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
            if (dialog.ShowModal(_threadingContext.JoinableTaskFactory, solution, projectFilePath, referenceUpdates) == false)
            {
                return;
            }

            // If we are removing, then that is a change or if we are newly marking a reference as TreatAsUsed,
            // then that is a change.
            var referenceChanges = referenceUpdates
                .WhereAsArray(update => update.Action != UpdateAction.TreatAsUsed || !update.ReferenceInfo.TreatAsUsed);

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
                ApplyUnusedReferenceUpdates(_threadingContext.JoinableTaskFactory, solution, projectFilePath, referenceChanges, CancellationToken.None);
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
            return (null, null, []);
        }

        var projectFilePath = projectHierarchy.TryGetProjectFilePath();
        if (string.IsNullOrEmpty(projectFilePath))
        {
            return (null, null, []);
        }

        var solution = _workspace.CurrentSolution;

        var unusedReferences = GetUnusedReferencesForProject(solution, projectFilePath!, projectAssetsFile, cancellationToken);

        return (solution, projectFilePath, unusedReferences);
    }

    private ImmutableArray<ReferenceUpdate> GetUnusedReferencesForProject(Solution solution, string projectFilePath, string projectAssetsFile, CancellationToken cancellationToken)
    {
        var unusedReferences = _threadingContext.JoinableTaskFactory.Run(async () =>
        {
            var projectReferences = await this.ReferenceCleanupService.GetProjectReferencesAsync(projectFilePath, cancellationToken).ConfigureAwait(true);
            var unusedReferenceAnalysisService = solution.Services.GetRequiredService<IUnusedReferenceAnalysisService>();
            return await unusedReferenceAnalysisService.GetUnusedReferencesAsync(solution, projectFilePath, projectAssetsFile, projectReferences, cancellationToken).ConfigureAwait(true);
        });

        var referenceUpdates = unusedReferences
            .SelectAsArray(reference => new ReferenceUpdate(reference.TreatAsUsed ? UpdateAction.TreatAsUsed : UpdateAction.Remove, reference));

        return referenceUpdates;
    }

    private static void ApplyUnusedReferenceUpdates(JoinableTaskFactory joinableTaskFactory, Solution solution, string projectFilePath, ImmutableArray<ReferenceUpdate> referenceUpdates, CancellationToken cancellationToken)
    {
        joinableTaskFactory.Run(
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
