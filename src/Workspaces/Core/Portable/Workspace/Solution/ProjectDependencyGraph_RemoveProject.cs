// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class ProjectDependencyGraph
{
    internal ProjectDependencyGraph WithProjectRemoved(ProjectId projectId)
    {
        Contract.ThrowIfFalse(_projectIds.Contains(projectId));

        // Project ID set and direct forward references are trivially updated by removing the key corresponding to
        // the project getting removed.
        var projectIds = _projectIds.Remove(projectId);
        var referencesMap = ComputeNewReferencesMapForRemovedProject(
            existingForwardReferencesMap: _referencesMap,
            existingReverseReferencesMap: _lazyReverseReferencesMap,
            projectId);

        // The direct reverse references map is updated by removing the key for the project getting removed, and
        // also updating any direct references to the removed project.
        var reverseReferencesMap = ComputeNewReverseReferencesMapForRemovedProject(
            existingForwardReferencesMap: _referencesMap,
            existingReverseReferencesMap: _lazyReverseReferencesMap,
            projectId);
        var transitiveReferencesMap = ComputeNewTransitiveReferencesMapForRemovedProject(_transitiveReferencesMap, projectId);
        var reverseTransitiveReferencesMap = ComputeNewReverseTransitiveReferencesMapForRemovedProject(_reverseTransitiveReferencesMap, projectId);
        return new ProjectDependencyGraph(
            projectIds,
            referencesMap,
            reverseReferencesMap,
            transitiveReferencesMap,
            reverseTransitiveReferencesMap,
            topologicallySortedProjects: default,
            dependencySets: default);
    }

    /// <summary>
    /// Computes a new <see cref="_referencesMap"/> for the removal of a project.
    /// </summary>
    /// <param name="existingForwardReferencesMap">The <see cref="_referencesMap"/> prior to the removal.</param>
    /// <param name="existingReverseReferencesMap">The <see cref="_lazyReverseReferencesMap"/> prior to the removal.
    /// This map serves as a hint to the removal process; i.e. it is assumed correct if it contains data, but may be
    /// omitted without impacting correctness.</param>
    /// <param name="removedProjectId">The ID of the project which is being removed.</param>
    /// <returns>The <see cref="_referencesMap"/> for the project dependency graph once the project is removed.</returns>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
        ProjectId removedProjectId)
    {
        var builder = existingForwardReferencesMap.ToBuilder();

        if (existingReverseReferencesMap is object)
        {
            // We know all the projects directly referencing 'projectId', so remove 'projectId' from the set of
            // references in each of those cases directly.
            if (existingReverseReferencesMap.TryGetValue(removedProjectId, out var referencingProjects))
            {
                foreach (var id in referencingProjects)
                {
                    builder.MultiRemove(id, removedProjectId);
                }
            }
        }
        else
        {
            // We don't know which projects reference 'projectId', so iterate over all known projects and remove
            // 'projectId' from the set of references if it exists.
            foreach (var (id, _) in existingForwardReferencesMap)
            {
                builder.MultiRemove(id, removedProjectId);
            }
        }

        // Finally, remove 'projectId' itself.
        builder.Remove(removedProjectId);
        return builder.ToImmutable();
    }

    /// <summary>
    /// Computes a new <see cref="_lazyReverseReferencesMap"/> for the removal of a project.
    /// </summary>
    /// <param name="existingReverseReferencesMap">The <see cref="_lazyReverseReferencesMap"/> prior to the removal,
    /// or <see langword="null"/> if the value prior to removal was not computed for the graph.</param>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? ComputeNewReverseReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
        ProjectId removedProjectId)
    {
        if (existingReverseReferencesMap is null)
        {
            // The map was never calculated for the previous graph, so there is nothing to update.
            return null;
        }

        if (!existingForwardReferencesMap.TryGetValue(removedProjectId, out var forwardReferences))
        {
            // The removed project did not reference any other projects, so we simply remove it.
            return existingReverseReferencesMap.Remove(removedProjectId);
        }

        var builder = existingReverseReferencesMap.ToBuilder();

        // Iterate over each project referenced by 'removedProjectId', which is now being removed. Update the
        // reverse references map for the project to no longer include 'removedProjectId' in the list.
        foreach (var referencedProjectId in forwardReferences)
        {
            builder.MultiRemove(referencedProjectId, removedProjectId);
        }

        // Finally, remove 'removedProjectId' itself.
        builder.Remove(removedProjectId);
        return builder.ToImmutable();
    }

    /// <summary>
    /// Computes a new <see cref="_transitiveReferencesMap"/> for the removal of a project.
    /// </summary>
    /// <seealso cref="ComputeNewReverseTransitiveReferencesMapForRemovedProject"/>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewTransitiveReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingTransitiveReferencesMap,
        ProjectId removedProjectId)
    {
        var builder = existingTransitiveReferencesMap.ToBuilder();

        // Iterate over each project and invalidate the transitive references for the project if the project has an
        // existing transitive reference to 'removedProjectId'.
        foreach (var (project, references) in existingTransitiveReferencesMap)
        {
            if (references.Contains(removedProjectId))
            {
                // The project transitively referenced 'removedProjectId', so any transitive references brought in
                // exclusively through this reference are no longer valid. Remove the project from the map and the
                // new transitive references will be recomputed the first time they are needed.
                builder.Remove(project);
            }
        }

        // Finally, remove 'projectId' itself.
        builder.Remove(removedProjectId);
        return builder.ToImmutable();
    }

    /// <summary>
    /// Computes a new <see cref="_reverseTransitiveReferencesMap"/> for the removal of a project.
    /// </summary>
    /// <seealso cref="ComputeNewTransitiveReferencesMapForRemovedProject"/>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseTransitiveReferencesMap,
        ProjectId removedProjectId)
    {
        var builder = existingReverseTransitiveReferencesMap.ToBuilder();

        // Iterate over each project and invalidate the transitive reverse references for the project if the project
        // has an existing transitive reverse reference to 'removedProjectId'.
        foreach (var (project, references) in existingReverseTransitiveReferencesMap)
        {
            if (references.Contains(removedProjectId))
            {
                // 'removedProjectId' transitively referenced the project, so any transitive reverse references
                // brought in exclusively through this reverse reference are no longer valid. Remove the project
                // from the map and the new transitive reverse references will be recomputed the first time they are
                // needed.
                builder.Remove(project);
            }
        }

        builder.Remove(removedProjectId);
        return builder.ToImmutable();
    }
}
