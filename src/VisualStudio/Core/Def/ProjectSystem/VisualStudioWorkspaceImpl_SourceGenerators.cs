// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract partial class VisualStudioWorkspaceImpl
{
    /// <summary>
    /// Used for batching up a lot of events and only combining them into a single request to update generators.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<ProjectId?> _updateSourceGeneratorsQueue;
    private bool _isSubscribedToSourceGeneratorImpactingEvents;

    public void SubscribeToSourceGeneratorImpactingEvents()
    {
        _foregroundObject.AssertIsForeground();
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
                    this.EnqueueUpdateSourceGeneratorVersion(projectId: null);
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
                    this.EnqueueUpdateSourceGeneratorVersion(projectId: null);
                }
            };
        });

        // Whenever the workspace status changes, go attempt to update generators.
        var workspaceStatusService = this.Services.GetRequiredService<IWorkspaceStatusService>();
        workspaceStatusService.StatusChanged += (_, _) => EnqueueUpdateSourceGeneratorVersion(projectId: null);

        // Now kick off at least the initial work to run generators.
        this.EnqueueUpdateSourceGeneratorVersion(projectId: null);
    }

    private void EnqueueUpdateSourceGeneratorVersion(ProjectId? projectId)
        => _updateSourceGeneratorsQueue.AddWork(projectId);

    private async ValueTask ProcessUpdateSourceGeneratorRequestAsync(
        ImmutableSegmentedList<ProjectId?> projectIds, CancellationToken cancellationToken)
    {
        // Only need to do this if we're not in automatic mode.
        var configuration = this.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
            return;

        // Ensure we're fully loaded before rerunning generators.
        var workspaceStatusService = this.Services.GetRequiredService<IWorkspaceStatusService>();
        await workspaceStatusService.WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

        var projectIdSet = projectIds.Contains(null) ? null : (ImmutableHashSet<ProjectId>)projectIds.ToImmutableHashSet()!;
        await this.SetCurrentSolutionAsync(
            oldSolution =>
            {
                var updates = GetUpdatedSourceGeneratorVersions(oldSolution, projectIdSet);
                return oldSolution.WithSourceGeneratorVersions(updates, cancellationToken);
            },
            static (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        return;

        static FrozenDictionary<ProjectId, int> GetUpdatedSourceGeneratorVersions(Solution solution, ImmutableHashSet<ProjectId>? projectIdSet)
        {
            // If the entire solution needs to be regenerated, then take every project and increase its source generator version.
            if (projectIdSet is null)
            {
                return solution.ProjectIds.ToFrozenDictionary(
                    p => solution.GetRequiredProject(p).Id,
                    p => solution.GetRequiredProject(p).SourceGeneratorVersion + 1);
            }

            // Otherwise, for all the projects involved in the save, update its source generator version.  Also do this
            // for all projects that transitively depend on that project, so that their generators will run as well when
            // next asked.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            using var _ = CodeAnalysis.PooledObjects.PooledDictionary<ProjectId, int>.GetInstance(out var result);

            foreach (var savedProjectId in projectIdSet)
            {
                var savedProject = solution.GetProject(savedProjectId);
                if (savedProject != null && !result.ContainsKey(savedProjectId))
                {
                    result[savedProjectId] = savedProject.SourceGeneratorVersion + 1;

                    foreach (var transitiveProjectId in dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(savedProjectId))
                        result[transitiveProjectId] = solution.GetRequiredProject(transitiveProjectId).SourceGeneratorVersion + 1;
                }
            }

            return result.ToFrozenDictionary();
        }
    }

    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.SourceGeneratorSave)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class SaveCommandHandler() : IChainedCommandHandler<SaveCommandArgs>
    {
        public string DisplayName => ServicesVSResources.Roslyn_save_command_handler;

        public CommandState GetCommandState(SaveCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(SaveCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            nextCommandHandler();

            // After a save happens, enqueue a request to run generators on the projects impacted by the save.
            foreach (var group in args.SubjectBuffer.GetRelatedDocuments().GroupBy(d => d.Project.Solution.Workspace))
            {
                if (group.Key is VisualStudioWorkspaceImpl visualStudioWorkspace)
                {
                    foreach (var projectGroup in group.GroupBy(d => d.Project))
                        visualStudioWorkspace.EnqueueUpdateSourceGeneratorVersion(projectGroup.Key.Id);
                }
            }
        }
    }
}
