// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
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
        var configuration = this.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
        {
            // If we're in automatic mode, we don't need to do anything *unless* the host has asked us to
            // force-regenerate something.  In that case we're literally going to drop our generator drivers and
            // regenerate the code, so we can't depend on automatic running generators normally.
            if (!projectIds.Any(t => t.forceRegeneration))
                return;
        }

        await this.SetCurrentSolutionAsync(
            useAsync: true,
            oldSolution =>
            {
                var updates = GetUpdatedSourceGeneratorVersions(oldSolution, projectIds);
                return oldSolution.UpdateSpecificSourceGeneratorExecutionVersions(updates);
            },
            static (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        return;

        static SourceGeneratorExecutionVersionMap GetUpdatedSourceGeneratorVersions(
            Solution solution, ImmutableSegmentedList<(ProjectId? projectId, bool forceRegeneration)> projectIds)
        {
            // For all the projects explicitly requested, update their source generator version.  Do this for all
            // projects that transitively depend on that project, so that their generators will run as well when next
            // asked.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            var result = ImmutableSortedDictionary.CreateBuilder<ProjectId, SourceGeneratorExecutionVersion>();

            // Determine if we want a major solution change, forcing regeneration of all projects.
            var solutionMajor = projectIds.Any(t => t.projectId is null && t.forceRegeneration);

            // If it's not a major solution change, then go update the versions for all projects requested.
            if (!solutionMajor)
            {
                // Do a pass where we update minor versions if requested.
                PopulateSourceGeneratorExecutionVersions(major: false);

                // Then update major versions.  We do this after the minor-version pass so that major version updates
                // overwrite minor-version updates.
                PopulateSourceGeneratorExecutionVersions(major: true);
            }

            // Now, if we've been asked to do an entire solution update, get any projects we didn't already mark, and
            // update their execution version as well.
            if (projectIds.Any(t => t.projectId is null))
            {
                foreach (var projectId in solution.ProjectIds)
                {
                    if (!result.ContainsKey(projectId))
                    {
                        result.Add(
                            projectId,
                            Increment(solution.GetSourceGeneratorExecutionVersion(projectId), solutionMajor));
                    }
                }
            }

            return new(result.ToImmutable());

            void PopulateSourceGeneratorExecutionVersions(bool major)
            {
                foreach (var (projectId, forceRegeneration) in projectIds)
                {
                    if (projectId is null)
                        continue;

                    if (forceRegeneration != major)
                        continue;

                    // We may have been asked to rerun generators for a project that is no longer around.  So make sure
                    // we still have this project.
                    var requestedProject = solution.GetProject(projectId);
                    if (requestedProject != null)
                    {
                        result[projectId] = Increment(solution.GetSourceGeneratorExecutionVersion(projectId), major);

                        foreach (var transitiveProjectId in dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(projectId))
                            result[transitiveProjectId] = Increment(solution.GetSourceGeneratorExecutionVersion(transitiveProjectId), major);
                    }
                }
            }

            static SourceGeneratorExecutionVersion Increment(SourceGeneratorExecutionVersion version, bool major)
                => major ? version.IncrementMajorVersion() : version.IncrementMinorVersion();
        }
    }
}
