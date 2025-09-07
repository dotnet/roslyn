// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class ProjectDependencyGraph
{
    internal ProjectDependencyGraph WithAllProjectReferencesRemoved(ProjectId projectId)
    {
        Contract.ThrowIfFalse(ProjectIds.Contains(projectId));

        if (!_referencesMap.TryGetValue(projectId, out var referencedProjectIds))
            return this;

        // Removing a project reference doesn't change the set of projects
        var projectIds = ProjectIds;

        // Incrementally update the graph
        var referencesMap = ComputeNewReferencesMapForRemovedAllProjectReferences(_referencesMap, projectId);
        var reverseReferencesMap = ComputeNewReverseReferencesMapForRemovedAllProjectReferences(_lazyReverseReferencesMap, projectId, referencedProjectIds);
        var transitiveReferencesMap = ComputeNewTransitiveReferencesMapForRemovedAllProjectReferences(_transitiveReferencesMap, projectId, referencedProjectIds);
        var reverseTransitiveReferencesMap = ComputeNewReverseTransitiveReferencesMapForRemovedAllProjectReferences(_reverseTransitiveReferencesMap, projectId);

        return new ProjectDependencyGraph(
            projectIds,
            referencesMap,
            reverseReferencesMap,
            transitiveReferencesMap,
            reverseTransitiveReferencesMap,
            topologicallySortedProjects: default,
            dependencySets: default);
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReferencesMapForRemovedAllProjectReferences(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
        ProjectId projectId)
    {
        // Projects with no references do not have an entry in the forward references map
        return existingForwardReferencesMap.Remove(projectId);
    }

    /// <summary>
    /// Computes a new <see cref="_lazyReverseReferencesMap"/> for the removal of all project references from a
    /// project.
    /// </summary>
    /// <param name="existingReverseReferencesMap">The <see cref="_lazyReverseReferencesMap"/> prior to the removal,
    /// or <see langword="null"/> if the reverse references map was not computed for the prior graph.</param>
    /// <param name="projectId">The project ID from which a project reference is being removed.</param>
    /// <param name="referencedProjectIds">The targets of the project references which are being removed.</param>
    /// <returns>The updated (complete) reverse references map, or <see langword="null"/> if the reverse references
    /// map could not be incrementally updated.</returns>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? ComputeNewReverseReferencesMapForRemovedAllProjectReferences(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
        ProjectId projectId,
        ImmutableHashSet<ProjectId> referencedProjectIds)
    {
        if (existingReverseReferencesMap is null)
        {
            return null;
        }

        var builder = existingReverseReferencesMap.ToBuilder();
        foreach (var referencedProjectId in referencedProjectIds)
        {
            builder.MultiRemove(referencedProjectId, projectId);
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewTransitiveReferencesMapForRemovedAllProjectReferences(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingTransitiveReferencesMap,
        ProjectId projectId,
        ImmutableHashSet<ProjectId> referencedProjectIds)
    {
        var builder = existingTransitiveReferencesMap.ToBuilder();

        // Invalidate the transitive references from every project referencing the changed project (transitively)
        foreach (var (project, references) in existingTransitiveReferencesMap)
        {
            if (!references.Contains(projectId))
            {
                // This is the forward-references-equivalent of the optimization in the update of reverse transitive
                // references.
                continue;
            }

            Debug.Assert(references.IsSupersetOf(referencedProjectIds));
            builder.Remove(project);
        }

        // Invalidate the transitive references from the changed project
        builder.Remove(projectId);

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedAllProjectReferences(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseTransitiveReferencesMap,
        ProjectId projectId)
    {
        var builder = existingReverseTransitiveReferencesMap.ToBuilder();

        // Invalidate the transitive reverse references from every project previously referenced by the original
        // project (transitively).
        foreach (var (project, references) in existingReverseTransitiveReferencesMap)
        {
            if (!references.Contains(projectId))
            {
                continue;
            }

            builder.Remove(project);
        }

        return builder.ToImmutable();
    }
}
