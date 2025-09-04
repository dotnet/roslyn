// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class ProjectDependencyGraph
{
    internal ProjectDependencyGraph WithProjectsRemoved(ArrayBuilder<ProjectId> removedProjectIds)
    {
        // Project ID set and direct forward references are trivially updated by removing the key corresponding to
        // the project getting removed.
        var projectIdsBuilder = ProjectIds.ToBuilder();
        foreach (var projectId in removedProjectIds)
            Contract.ThrowIfFalse(projectIdsBuilder.Remove(projectId));
        var projectIds = projectIdsBuilder.ToImmutable();

        var referencesMap = ComputeNewReferencesMapForRemovedProject(
            existingForwardReferencesMap: _referencesMap,
            existingReverseReferencesMap: _lazyReverseReferencesMap,
            removedProjectIds);

        // The direct reverse references map is updated by removing the key for the project getting removed, and
        // also updating any direct references to the removed project.
        var reverseReferencesMap = ComputeNewReverseReferencesMapForRemovedProject(
            existingForwardReferencesMap: _referencesMap,
            existingReverseReferencesMap: _lazyReverseReferencesMap,
            removedProjectIds);
        var transitiveReferencesMap = ComputeNewTransitiveReferencesMapForRemovedProject(_transitiveReferencesMap, removedProjectIds);
        var reverseTransitiveReferencesMap = ComputeNewReverseTransitiveReferencesMapForRemovedProject(_reverseTransitiveReferencesMap, removedProjectIds);
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
    /// <param name="removedProjectIds">IDs of projects which are being removed.</param>
    /// <returns>The <see cref="_referencesMap"/> for the project dependency graph once the project is removed.</returns>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
        ArrayBuilder<ProjectId> removedProjectIds)
    {
        var builder = existingForwardReferencesMap.ToBuilder();

        // TODO: Consider optimizing when a large number of projects are being removed simultaneously
        if (existingReverseReferencesMap is not null)
        {
            foreach (var removedProjectId in removedProjectIds)
            {
                // We know all the projects directly referencing 'projectId', so remove 'projectId' from the set of
                // references in each of those cases directly.
                if (existingReverseReferencesMap.TryGetValue(removedProjectId, out var referencingProjects))
                {
                    foreach (var id in referencingProjects)
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
                foreach (var removedProjectId in removedProjectIds)
                    builder.MultiRemove(id, removedProjectId);
            }
        }

        // Finally, remove each item in removedProjectIds
        foreach (var removedProjectId in removedProjectIds)
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
        ArrayBuilder<ProjectId> removedProjectIds)
    {
        // If the map was never calculated for the previous graph, so there is nothing to update.
        if (existingReverseReferencesMap is null)
            return null;

        // TODO: Consider optimizing when a large number of projects are being removed simultaneously
        var builder = existingReverseReferencesMap.ToBuilder();
        foreach (var removedProjectId in removedProjectIds)
        {
            // If the removed project did not reference any other projects, nothing to do for this project.
            if (!existingForwardReferencesMap.TryGetValue(removedProjectId, out var forwardReferences))
                continue;

            // Iterate over each project referenced by 'removedProjectId', which is now being removed. Update the
            // reverse references map for the project to no longer include 'removedProjectId' in the list.
            foreach (var referencedProjectId in forwardReferences)
                builder.MultiRemove(referencedProjectId, removedProjectId);
        }

        // Finally, remove each item in removedProjectIds
        foreach (var removedProjectId in removedProjectIds)
            builder.Remove(removedProjectId);

        return builder.ToImmutable();
    }

    /// <summary>
    /// Computes a new <see cref="_transitiveReferencesMap"/> for the removal of a project.
    /// </summary>
    /// <seealso cref="ComputeNewReverseTransitiveReferencesMapForRemovedProject"/>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewTransitiveReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingTransitiveReferencesMap,
        ArrayBuilder<ProjectId> removedProjectIds)
    {
        var builder = existingTransitiveReferencesMap.ToBuilder();

        // Iterate over each project and invalidate the transitive references for the project if the project has an
        // existing transitive reference to 'removedProjectId'.
        foreach (var (project, references) in existingTransitiveReferencesMap)
        {
            foreach (var removedProjectId in removedProjectIds)
            {
                if (references.Contains(removedProjectId))
                {
                    // The project transitively referenced 'removedProjectId', so any transitive references brought in
                    // exclusively through this reference are no longer valid. Remove the project from the map and the
                    // new transitive references will be recomputed the first time they are needed.
                    builder.Remove(project);
                    break;
                }
            }
        }

        // Finally, remove each item in removedProjectIds
        foreach (var removedProjectId in removedProjectIds)
            builder.Remove(removedProjectId);

        return builder.ToImmutable();
    }

    /// <summary>
    /// Computes a new <see cref="_reverseTransitiveReferencesMap"/> for the removal of a project.
    /// </summary>
    /// <seealso cref="ComputeNewTransitiveReferencesMapForRemovedProject"/>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedProject(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseTransitiveReferencesMap,
        ArrayBuilder<ProjectId> removedProjectIds)
    {
        var builder = existingReverseTransitiveReferencesMap.ToBuilder();

        // Iterate over each project and invalidate the transitive reverse references for the project if the project
        // has an existing transitive reverse reference to 'removedProjectId'.
        foreach (var (project, references) in existingReverseTransitiveReferencesMap)
        {
            foreach (var removedProjectId in removedProjectIds)
            {
                if (references.Contains(removedProjectId))
                {
                    // 'removedProjectId' transitively referenced the project, so any transitive reverse references
                    // brought in exclusively through this reverse reference are no longer valid. Remove the project
                    // from the map and the new transitive reverse references will be recomputed the first time they are
                    // needed.
                    builder.Remove(project);
                    break;
                }
            }
        }

        foreach (var removedProjectId in removedProjectIds)
            builder.Remove(removedProjectId);

        return builder.ToImmutable();
    }
}
