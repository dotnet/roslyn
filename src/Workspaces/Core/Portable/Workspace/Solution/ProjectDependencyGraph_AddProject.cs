// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public partial class ProjectDependencyGraph
    {
        internal ProjectDependencyGraph WithAdditionalProject(ProjectId projectId)
        {
            // Track the existence of some new projects. Note this call only adds new ProjectIds, but doesn't add any references. Any caller who wants to add a new project
            // with references will first call this, and then call WithAdditionalProjectReferences to add references as well.

            // Since we're adding a new project here, there aren't any references to it or from it (any references will be added
            // later with WithAdditionalProjectReferences). Thus, the new projects aren't topologically sorted relative to any other project
            // and form their own dependency set. Thus, sticking them at the end is fine.
            var newTopologicallySortedProjects = _lazyTopologicallySortedProjects;

            if (!newTopologicallySortedProjects.IsDefault)
            {
                newTopologicallySortedProjects = newTopologicallySortedProjects.Add(projectId);
            }

            var newDependencySets = _lazyDependencySets;

            if (!newDependencySets.IsDefault)
            {
                var builder = newDependencySets.ToBuilder();
                builder.Add(ImmutableArray.Create(projectId));
                newDependencySets = builder.ToImmutable();
            }

            // The rest of the references map is unchanged, since no new references are added in this call.
            return new ProjectDependencyGraph(
                _projectIds.Add(projectId),
                referencesMap: _referencesMap,
                reverseReferencesMap: _lazyReverseReferencesMap,
                transitiveReferencesMap: _transitiveReferencesMap,
                reverseTransitiveReferencesMap: _reverseTransitiveReferencesMap,
                topologicallySortedProjects: newTopologicallySortedProjects,
                dependencySets: newDependencySets);
        }
    }
}
