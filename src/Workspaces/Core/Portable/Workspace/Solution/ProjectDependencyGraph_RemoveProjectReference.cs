// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    partial class ProjectDependencyGraph
    {
        internal ProjectDependencyGraph WithProjectReferenceRemoved(ProjectId projectId, ProjectId referencedProjectId)
        {
            Contract.ThrowIfFalse(_projectIds.Contains(projectId));
            Contract.ThrowIfFalse(_referencesMap[projectId].Contains(referencedProjectId));

            // Removing a project reference doesn't change the set of projects
            var projectIds = _projectIds;

            // Incrementally update the graph
            var referencesMap = _referencesMap.SetItem(projectId, _referencesMap[projectId].Remove(referencedProjectId));
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

        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? ComputeNewReverseReferencesMapForRemovedProjectReference(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
            ProjectId projectId,
            ProjectId referencedProjectId)
        {
            if (existingReverseReferencesMap is null)
                return null;

            if (existingReverseReferencesMap.TryGetValue(referencedProjectId, out var referencingProjects))
            {
                return existingReverseReferencesMap.SetItem(referencedProjectId, referencingProjects.Remove(projectId));
            }

            return existingReverseReferencesMap;
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
                if (references.Contains(projectId))
                {
                    builder.Remove(project);
                }
            }

            // Invalidate the transitive references from the changed project
            builder.Remove(projectId);

            return builder.ToImmutable();
        }

        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedProjectReference(
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
                    //   A -> D
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
                    //   A -> D
                    //
                    // For this example, we do not hit this optimization because D contains project B in the set of
                    // reverse transitive references. Without more complicated checks, we cannot rule out the
                    // possibility that A may have been removed from the reverse transitive references of D by the
                    // removal of the A->B reference.
                    continue;
                }

                if (references.Contains(projectId))
                {
                    builder.Remove(project);
                }
            }

            // Invalidate the transitive references from the previously-referenced project
            builder.Remove(referencedProjectId);

            return builder.ToImmutable();
        }
    }
}
