// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract partial class VisualStudioWorkspaceImpl
{
    private bool _isSubscribedToSourceGeneratorImpactingEvents;

    public void SubscribeToSourceGeneratorImpactingEvents()
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (_isSubscribedToSourceGeneratorImpactingEvents)
            return;

        // UIContextImpl requires IVsMonitorSelection service:
        if (ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) == null)
            return;

        _isSubscribedToSourceGeneratorImpactingEvents = true;

        // This pattern ensures that we are called whenever the build starts/completes even if it is already in progress.
        KnownUIContexts.SolutionBuildingContext.WhenActivated(() =>
        {
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += (_, e) =>
            {
                if (!e.Activated)
                {
                    // After a build occurs, transition the solution to a new source generator version.  This will
                    // ensure that any cached SG documents will be re-generated.
                    this.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: false);
                }
            };
        });

        KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(() =>
        {
            KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += (_, e) =>
            {
                if (e.Activated)
                {
                    // After the solution fully loads, transition the solution to a new source generator version.  This
                    // will ensure that we'll now produce correct SG docs with fully knowledge of all the user's state.
                    this.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: false);
                }
            };
        });

        // Whenever the workspace status changes, go attempt to update generators.
        var workspaceStatusService = this.Services.GetRequiredService<IWorkspaceStatusService>();
        workspaceStatusService.StatusChanged += (_, _) => EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: false);

        // Now kick off at least the initial work to run generators.
        this.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: false);
    }

    public sealed partial class OpenFileTracker
    {
        void IOpenTextBufferEventListener.OnSaveDocument(string moniker)
        {
            // Note: this will find docs, additional docs, and analyzer config docs.  Thats good. We do want changing
            // any of those to cause rerunning generators in any affected project.
            var documentIds = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(moniker);

            foreach (var projectId in documentIds.Select(i => i.ProjectId).Distinct())
                _workspace.EnqueueUpdateSourceGeneratorVersion(projectId, forceRegeneration: false);
        }
    }
}
