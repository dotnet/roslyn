// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class Workspace
{
    /// <summary>
    /// Used for batching up a lot of events and only combining them into a single request to update generators.  The
    /// <see cref="ProjectId"/> represents the projects that have changed, and which need their source-generators
    /// re-run.  <see langword="null"/> in the list indicates the entire solution has changed and all generators need to
    /// be rerun.  The <see cref="bool"/> represents if source generators should be fully rerun for the requested
    /// project or solution.  If <see langword="false"/>, the existing generator driver will be used, which may result
    /// in no actual changes to emitted source (as the driver may decide no inputs changed, and thus all outputs should
    /// be reused).  If <see langword="true"/>, the existing driver will be dropped, forcing all generation to be redone.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<(ProjectId? projectId, bool forceRegeneration)> _updateSourceGeneratorsQueue;

    private readonly CancellationTokenSource _updateSourceGeneratorsQueueTokenSource = new();

    internal void EnqueueUpdateSourceGeneratorVersion(ProjectId? projectId, bool forceRegeneration)
        => _updateSourceGeneratorsQueue.AddWork((projectId, forceRegeneration));

    private async ValueTask ProcessUpdateSourceGeneratorRequestAsync(
        ImmutableSegmentedList<(ProjectId? projectId, bool forceRegeneration)> projectIds, CancellationToken cancellationToken)
    {
        // Only need to do this if we're not in automatic mode.
        var configuration = this.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
            return;

        // Ensure we're fully loaded before rerunning generators.
        var workspaceStatusService = this.Services.GetRequiredService<IWorkspaceStatusService>();
        await workspaceStatusService.WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

        await this.SetCurrentSolutionAsync(
            oldSolution =>
            {
                var updates = GetUpdatedSourceGeneratorVersions(oldSolution, projectIds);
                return oldSolution.WithSourceGeneratorVersions(updates, cancellationToken);
            },
            static (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        return;

        static FrozenDictionary<ProjectId, SourceGeneratorExecutionVersion> GetUpdatedSourceGeneratorVersions(
            Solution solution, ImmutableSegmentedList<(ProjectId? projectId, bool forceRegeneration)> projectIds)
        {
            // First check if we're updating for the entire solution.

            if (projectIds.Any(t => t.projectId is null))
            {
                // Determine if we want a major/minor update depending on if we see `forceRegeneration: true` passed in.
                var major = projectIds.Any(t => t.projectId is null && t.forceRegeneration);

                return solution.ProjectIds.ToFrozenDictionary(
                    p => solution.GetRequiredProject(p).Id,
                    p => Increment(solution.GetRequiredProject(p).SourceGeneratorExecutionVersion, major));
            }

            // Otherwise, for all the projects involved requested, update their source generator version.  Do this for
            // all projects that transitively depend on that project, so that their generators will run as well when
            // next asked.
            var dependencyGraph = solution.GetProjectDependencyGraph();

            using var _ = CodeAnalysis.PooledObjects.PooledDictionary<ProjectId, SourceGeneratorExecutionVersion>.GetInstance(out var result);

            // Do a pass where we update minor versions if requested.
            PopulateSourceGeneratorExecutionVersions(major: false);

            // Then update major versions.  We do this after the minor-version pass so that major version updates
            // overwrite minor-version updates.
            PopulateSourceGeneratorExecutionVersions(major: true);

            return result.ToFrozenDictionary();

            void PopulateSourceGeneratorExecutionVersions(bool major)
            {
                foreach (var (projectId, forceRegeneration) in projectIds)
                {
                    Contract.ThrowIfNull(projectId);
                    if (forceRegeneration != major)
                        continue;

                    var requestedProject = solution.GetProject(projectId);
                    if (requestedProject != null && !result.ContainsKey(projectId))
                    {
                        result[projectId] = Increment(requestedProject.SourceGeneratorExecutionVersion, major);

                        foreach (var transitiveProjectId in dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectId))
                            result[transitiveProjectId] = Increment(solution.GetRequiredProject(transitiveProjectId).SourceGeneratorExecutionVersion, major);
                    }
                }
            }

            static SourceGeneratorExecutionVersion Increment(SourceGeneratorExecutionVersion version, bool major)
                => major ? version.IncrementMajorVersion() : version.IncrementMinorVersion();
        }
    }
}
