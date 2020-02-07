// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    partial class ProjectDependencyGraph
    {
        internal ProjectDependencyGraph WithAdditionalProjects(IEnumerable<ProjectId> projectIds)
        {
            // Track the existence of some new projects. Note this call only adds new ProjectIds, but doesn't add any references. Any caller who wants to add a new project
            // with references will first call this, and then call WithAdditionalProjectReferences to add references as well.

            // Since we're adding a new project here, there aren't any references to it, or at least not yet. (If there are, they'll be added
            // later with WithAdditionalProjectReferences). Thus, the new projects aren't topologically sorted relative to any other project
            // and form their own dependency set. Thus, sticking them at the end is fine.
            var newTopologicallySortedProjects = _lazyTopologicallySortedProjects;

            if (!newTopologicallySortedProjects.IsDefault)
            {
                newTopologicallySortedProjects = newTopologicallySortedProjects.AddRange(projectIds);
            }

            var newDependencySets = _lazyDependencySets;

            if (!newDependencySets.IsDefault)
            {
                var builder = newDependencySets.ToBuilder();

                foreach (var projectId in projectIds)
                {
                    builder.Add(ImmutableArray.Create(projectId));
                }

                newDependencySets = builder.ToImmutable();
            }

            // The rest of the references map is unchanged, since no new references are added in this call.
            return new ProjectDependencyGraph(
                _projectIds.Union(projectIds),
                referencesMap: _referencesMap,
                reverseReferencesMap: _lazyReverseReferencesMap,
                transitiveReferencesMap: _transitiveReferencesMap,
                reverseTransitiveReferencesMap: _reverseTransitiveReferencesMap,
                topologicallySortedProjects: newTopologicallySortedProjects,
                dependencySets: newDependencySets);
        }
    }
}
