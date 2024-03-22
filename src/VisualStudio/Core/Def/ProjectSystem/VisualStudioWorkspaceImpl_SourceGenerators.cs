// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract partial class VisualStudioWorkspaceImpl
{
    private bool _isSubscribedToSourceGeneratorImpactingEvents;

    public void SubscribeToSourceGeneratorImpactingEvents()
    {
        _foregroundObject.AssertIsForeground();
        if (_isSubscribedToSourceGeneratorImpactingEvents)
            return;

        // UIContextImpl requires IVsMonitorSelection service:
        if (ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) == null)
            return;

        // This pattern ensures that we are called whenever the build starts/completes even if it is already in progress.
        KnownUIContexts.SolutionBuildingContext.WhenActivated(() =>
        {
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += (_, e) =>
            {
                if (!e.Activated)
                {
                    // After a build occurs, transition the solution to a new source generator version.  This will
                    // ensure that any cached SG documents will be re-generated.
                    this.UpdateSourceGeneratorVersion();
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
                    this.UpdateSourceGeneratorVersion();
                }
            };

            this.UpdateSourceGeneratorVersion();
        });

        _isSubscribedToSourceGeneratorImpactingEvents = true;
    }

    private void UpdateSourceGeneratorVersion()
    {
        _foregroundObject.AssertIsForeground();

        // Only need to do this if we're not in automatic mode.
        var configuration = this.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
            return;

        this.SetCurrentSolution(
            oldSolution => oldSolution.WithSourceGeneratorVersion(oldSolution.SourceGeneratorVersion + 1),
            static (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null);
    }
}
