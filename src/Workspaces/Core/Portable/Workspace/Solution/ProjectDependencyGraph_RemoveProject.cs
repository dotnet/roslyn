// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    partial class ProjectDependencyGraph
    {
        internal ProjectDependencyGraph WithProjectRemoved(ProjectId projectId)
        {
            Contract.ThrowIfFalse(_projectIds.Contains(projectId));

            // Project ID set and direct forward references are trivially updated by removing the key corresponding to
            // the project getting removed.
            var projectIds = _projectIds.Remove(projectId);
            var referencesMap = ComputeNewReferencesMapForRemovedProject(
                referencesMap: _referencesMap,
                existingReverseReferencesMap: _lazyReverseReferencesMap,
                projectId);

            // The direct reverse references map is updated by removing the key for the project getting removed, and
            // also updating any direct references to the removed project.
            var reverseReferencesMap = _lazyReverseReferencesMap is object
                ? ComputeNewReverseReferencesMapForRemovedProject(
                    existingForwardReferencesMap: _referencesMap,
                    existingReverseReferencesMap: _lazyReverseReferencesMap,
                    projectId)
                : null;
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
        /// <param name="referencesMap">The <see cref="_referencesMap"/> prior to the removal.</param>
        /// <param name="existingReverseReferencesMap">The <see cref="_lazyReverseReferencesMap"/> prior to the removal.
        /// This map serves as a hint to the removal process; i.e. it is assumed correct if it contains data, but may be
        /// omitted without impacting correctness.</param>
        /// <param name="projectId">The ID of the project which is being removed.</param>
        /// <returns>The <see cref="_referencesMap"/> for the project dependency graph once the project is removed.</returns>
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReferencesMapForRemovedProject(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? existingReverseReferencesMap,
            ProjectId projectId)
        {
            var builder = referencesMap.ToBuilder();

            if (existingReverseReferencesMap is object && existingReverseReferencesMap.TryGetValue(projectId, out var referencingProjects))
            {
                // We know all the projects directly referencing 'projectId', so remove 'projectId' from the set of
                // references in each of those cases directly.
                foreach (var id in referencingProjects)
                {
                    builder[id] = builder[id].Remove(projectId);
                }
            }
            else
            {
                // We don't know which projects reference 'projectId', so iterate over all known projects and remove
                // 'projectId' from the set of references if it exists.
                foreach (var (id, references) in referencesMap)
                {
                    builder[id] = references.Remove(projectId);
                }
            }

            // Finally, remove 'projectId' itself.
            builder.Remove(projectId);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Computes a new <see cref="_lazyReverseReferencesMap"/> for the removal of a project.
        /// </summary>
        private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseReferencesMapForRemovedProject(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingForwardReferencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseReferencesMap,
            ProjectId projectId)
        {
            var builder = existingReverseReferencesMap.ToBuilder();

            // Iterate over each project referenced by 'projectId', which is now being removed. Update the reverse
            // references map for the project to no longer include 'projectId' in the list.
            if (existingForwardReferencesMap.TryGetValue(projectId, out var referencedProjectIds))
            {
                foreach (var referencedProjectId in referencedProjectIds)
                {
                    if (builder.TryGetValue(referencedProjectId, out var reverseDependencies))
                    {
                        builder[referencedProjectId] = reverseDependencies.Remove(projectId);
                    }
                }
            }

            // Finally, remove 'projectId' itself.
            builder.Remove(projectId);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Computes a new <see cref="_transitiveReferencesMap"/> for the removal of a project.
        /// </summary>
        /// <seealso cref="ComputeNewReverseTransitiveReferencesMapForRemovedProject"/>
        private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewTransitiveReferencesMapForRemovedProject(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingTransitiveReferencesMap,
            ProjectId projectId)
        {
            var builder = existingTransitiveReferencesMap.ToBuilder();

            // Iterate over each project and invalidate the transitive references for the project if the project has an
            // existing transitive reference to 'projectId'.
            foreach (var (project, references) in existingTransitiveReferencesMap)
            {
                if (references.Contains(projectId))
                {
                    // The project transitively referenced 'projectId', so any transitive references brought in
                    // exclusively through this reference are no longer valid. Remove the project from the map and the
                    // new transitive references will be recomputed the first time they are needed.
                    builder.Remove(project);
                }
            }

            // Finally, remove 'projectId' itself.
            builder.Remove(projectId);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Computes a new <see cref="_reverseTransitiveReferencesMap"/> for the removal of a project.
        /// </summary>
        /// <seealso cref="ComputeNewTransitiveReferencesMapForRemovedProject"/>
        private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeNewReverseTransitiveReferencesMapForRemovedProject(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> existingReverseTransitiveReferencesMap,
            ProjectId projectId)
        {
            var builder = existingReverseTransitiveReferencesMap.ToBuilder();

            foreach (var (project, references) in existingReverseTransitiveReferencesMap)
            {
                if (references.Contains(projectId))
                {
                    // Invalidate the cache for projects that reference the removed project
                    builder.Remove(project);
                }
            }

            builder.Remove(projectId);
            return builder.ToImmutable();
        }
    }
}
