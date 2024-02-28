// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class ProjectDependencyGraph
{
    internal ProjectDependencyGraph WithProjectReferenceRemoved(ProjectId projectId, ProjectId referencedProjectId)
    {
        Contract.ThrowIfFalse(_projectIds.Contains(projectId));
        Contract.ThrowIfFalse(_referencesMap[projectId].Contains(referencedProjectId));

        // Removing a project reference doesn't change the set of projects
        var projectIds = _projectIds;

        // Incrementally update the graph
        var referencesMap = ComputeNewReferencesMapForRemovedProjectReference(_referencesMap, projectId, referencedProjectId);
        var reverseReferencesMap = ComputeNewReverseReferencesMapForRemovedProjectReference(_lazyReverseReferencesMap, projectId, referencedProjectId);
        var transitiveReferencesMap = ComputeNewTransitiveReferencesMapForRemovedProjectReference(_transitiveReferencesMap, projectId, referencedProjectId);
        var reverseTransitiveReferencesMap = ComputeNewReverseTransitiveReferencesMapForRemovedProjectReference(_reverseTransitiveReferencesMap, projectId, referencedProjectId);

        return new ProjectDependencyGraph(
            projectIds,
            referencesMap,
            reverseReferencesMap,
            transitiveReferencesMap,
            reverseTransitiveReferencesMap,
            topologicallySortedProjects: default,
            dependencySets: default);
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReferencesMapForRemovedProjectReference(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
        ProjectId projectId,
        ProjectId referencedProjectId)
    {
        return existingForwardReferencesMap.MultiRemove(projectId, referencedProjectId);
    }

    /// <summary>
    /// Computes a new <see cref="_lazyReverseReferencesMap"/> for the removal of a project reference.
    /// </summary>
    /// <param name="existingReverseReferencesMap">The <see cref="_lazyReverseReferencesMap"/> prior to the removal,
    /// or <see langword="null"/> if the reverse references map was not computed for the prior graph.</param>
    /// <param name="projectId">The project ID from which a project reference is being removed.</param>
    /// <param name="referencedProjectId">The target of the project reference which is being removed.</param>
    /// <returns>The updated (complete) reverse references map, or <see langword="null"/> if the reverse references
    /// map could not be incrementally updated.</returns>
    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? ComputeNewReverseReferencesMapForRemovedProjectReference(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
        ProjectId projectId,
        ProjectId referencedProjectId)
    {
        if (existingReverseReferencesMap is null)
        {
            return null;
        }

        return existingReverseReferencesMap.MultiRemove(referencedProjectId, projectId);
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewTransitiveReferencesMapForRemovedProjectReference(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingTransitiveReferencesMap,
        ProjectId projectId,
        ProjectId referencedProjectId)
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

            Debug.Assert(references.Contains(referencedProjectId));
            builder.Remove(project);
        }

        // Invalidate the transitive references from the changed project
        builder.Remove(projectId);

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedProjectReference(
        ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseTransitiveReferencesMap,
        ProjectId projectId,
        ProjectId referencedProjectId)
    {
        var builder = existingReverseTransitiveReferencesMap.ToBuilder();

        // Invalidate the transitive reverse references from every project previously referenced by the original
        // project (transitively), except for cases where the removed project reference could not impact the result.
        foreach (var (project, references) in existingReverseTransitiveReferencesMap)
        {
            if (!references.Contains(referencedProjectId))
            {
                // If projectId references project, it isn't through referencedProjectId so the change doesn't
                // impact the dependency graph.
                //
                // Suppose we start with the following graph, and are removing the project reference A->B:
                //
                //   A -> B -> C
                //     \
                //      > D
                //
                // This case is not hit for project C. The reverse transitive references for C contains B, which is
                // the target project of the removed reference. We can see that project C is impacted by this
                // removal, and after the removal, project A will no longer be in the reverse transitive references
                // of C.
                //
                // This case is hit for project D. The reverse transitive references for D does not contain B, which
                // means project D cannot be "downstream" of the impact of removing the reference A->B. We can see
                // that project A will still be in the reverse transitive references of D.
                //
                // This optimization does not catch all cases. For example, consider the following graph where we
                // are removing the project reference A->B:
                //
                //   A -> B -> D
                //     \_______^
                //
                // For this example, we do not hit this optimization because D contains project B in the set of
                // reverse transitive references. Without more complicated checks, we cannot rule out the
                // possibility that A may have been removed from the reverse transitive references of D by the
                // removal of the A->B reference.
                continue;
            }

            Debug.Assert(references.Contains(projectId));
            builder.Remove(project);
        }

        // Invalidate the transitive references from the previously-referenced project
        builder.Remove(referencedProjectId);

        return builder.ToImmutable();
    }
}
