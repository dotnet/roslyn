// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Services.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Services.Shared.Extensions
{
    internal static class IProjectDependencyGraphExtensions
    {
        /// <summary>
        /// Gets the list of projects that transitively depend on this project.
        /// </summary> 
        public static IEnumerable<ProjectId> GetProjectsThatTransitivelyDependOnThisProject(
            this IProjectDependencyGraph graph,
            ProjectId projectId)
        {
            return GetProjectsThatTransitivelyDependOnThisProject(graph, projectId, new HashSet<ProjectId>());
        }

        private static IEnumerable<ProjectId> GetProjectsThatTransitivelyDependOnThisProject(
            IProjectDependencyGraph graph,
            ProjectId projectId,
            HashSet<ProjectId> result)
        {
            if (result.Add(projectId))
            {
                foreach (var id in graph.GetProjectsThatDirectlyDependOnThisProject(projectId))
                {
                    GetProjectsThatTransitivelyDependOnThisProject(graph, id, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the list of projects that this project transitively depends on.
        /// </summary>
        public static IEnumerable<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn(
            this IProjectDependencyGraph graph,
            ProjectId projectId)
        {
            return GetProjectsThatThisProjectTransitivelyDependsOn(graph, projectId, new HashSet<ProjectId>());
        }

        private static IEnumerable<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn(
            IProjectDependencyGraph graph,
            ProjectId projectId,
            HashSet<ProjectId> result)
        {
            if (result.Add(projectId))
            {
                foreach (var id in graph.GetProjectsThatThisProjectDirectlyDependsOn(projectId))
                {
                    GetProjectsThatThisProjectTransitivelyDependsOn(graph, id, result);
                }
            }

            return result;
        }
    }
}
