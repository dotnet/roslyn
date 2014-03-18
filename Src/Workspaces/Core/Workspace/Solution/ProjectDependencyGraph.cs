// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class ProjectDependencyGraph : IObjectWritable
    {
        internal readonly Solution Solution;
        internal readonly VersionStamp Version;

        private readonly ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> projectToProjectsItReferencesMap;
        private readonly ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> projectToProjectsThatReferenceItMap;
        private readonly CancellableLazy<IEnumerable<ProjectId>> topologicallySortedProjects;

        private ProjectDependencyGraph(
            Solution solution,
            VersionStamp version,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> projectToProjectsItReferencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> projectToProjectsThatReferenceItMap)
        {
            this.Solution = solution;
            this.Version = version;
            this.projectToProjectsItReferencesMap = projectToProjectsItReferencesMap;
            this.projectToProjectsThatReferenceItMap = projectToProjectsThatReferenceItMap;

            this.topologicallySortedProjects = CancellableLazy.Create(
                c => TopologicalSort(this.projectToProjectsItReferencesMap.Keys.OrderBy(p => p.Id), c).AsEnumerable());
        }

        internal static ProjectDependencyGraph From(Solution solution, CancellationToken cancellationToken)
        {
            var map = ImmutableDictionary.Create<ProjectId, ImmutableHashSet<ProjectId>>();
            var reverseMap = ImmutableDictionary.Create<ProjectId, ImmutableHashSet<ProjectId>>();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                cancellationToken.ThrowIfCancellationRequested();

                var projectIds = project.ProjectReferences.Select(p => p.ProjectId);
                map = map.Add(project.Id, ImmutableHashSet.CreateRange<ProjectId>(projectIds));
                reverseMap = reverseMap.AddAll(projectIds, project.Id);
            }

            var version = solution.GetLatestProjectVersion();

            return new ProjectDependencyGraph(solution, version, map, reverseMap);
        }

        internal static ProjectDependencyGraph From(Solution newSolution, ProjectDependencyGraph oldGraph, CancellationToken cancellationToken)
        {
            var oldSolution = oldGraph.Solution;

            if (oldSolution == newSolution)
            {
                return oldGraph;
            }

            // in case old and new are incompatible just build it the hard way
            if (oldSolution.Id != newSolution.Id)
            {
                return From(newSolution, cancellationToken);
            }

            var map = oldGraph.projectToProjectsItReferencesMap;
            var reverseMap = oldGraph.projectToProjectsThatReferenceItMap;
            var differences = newSolution.GetChanges(oldSolution);

            // remove projects that no longer occur in new solution
            foreach (var project in differences.GetRemovedProjects())
            {
                cancellationToken.ThrowIfCancellationRequested();
                ImmutableHashSet<ProjectId> referencedProjectIds;
                if (oldGraph.projectToProjectsItReferencesMap.TryGetValue(project.Id, out referencedProjectIds))
                {
                    map = map.Remove(project.Id);
                    reverseMap = reverseMap.RemoveAll(referencedProjectIds, project.Id);
                }
            }

            // add projects that don't occur in old solution
            foreach (var project in differences.GetAddedProjects())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var referencedProjectIds = project.ProjectReferences.Select(p => p.ProjectId);
                map = map.Add(project.Id, ImmutableHashSet.CreateRange<ProjectId>(referencedProjectIds));
                reverseMap = reverseMap.AddAll(referencedProjectIds, project.Id);
            }

            // update projects that are changed.
            foreach (var projectChanges in differences.GetProjectChanges().Where(pc => pc.OldProject.AllProjectReferences != pc.NewProject.AllProjectReferences))
            {
                var projectId = projectChanges.ProjectId;

                cancellationToken.ThrowIfCancellationRequested();
                ImmutableHashSet<ProjectId> oldReferencedProjectIds;
                if (oldGraph.projectToProjectsItReferencesMap.TryGetValue(projectId, out oldReferencedProjectIds))
                {
                    map = map.Remove(projectId);
                    reverseMap = reverseMap.RemoveAll(oldReferencedProjectIds, projectId);
                }

                var newReferencedProjectIds = newSolution.GetProject(projectId).ProjectReferences.Select(p => p.ProjectId);
                map = map.Add(projectId, ImmutableHashSet.CreateRange<ProjectId>(newReferencedProjectIds));
                reverseMap = reverseMap.AddAll(newReferencedProjectIds, projectId);
            }

            var version = newSolution.GetLatestProjectVersion();

            return new ProjectDependencyGraph(newSolution, version, map, reverseMap);
        }

        /// <summary>
        /// Returns all the projects for the solution in a topologically sorted order with respect
        /// to their dependencies. That is, projects that depend on other projects will always show
        /// up later than them in this stream.
        /// </summary>
        public IEnumerable<ProjectId> GetTopologicallySortedProjects(CancellationToken cancellationToken = default(CancellationToken))
        {
            return topologicallySortedProjects.GetValue(cancellationToken);
        }

        /// <summary>
        /// Gets the list of projects (topologically sorted) that directly depend on this project.
        /// </summary> 
        public IEnumerable<ProjectId> GetProjectsThatDirectlyDependOnThisProject(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException("projectId");
            }

            // The list of projects that reference this project might be null if there are no
            // references.
            ImmutableHashSet<ProjectId> projectIds;
            return projectToProjectsThatReferenceItMap.TryGetValue(projectId, out projectIds)
                ? projectIds
                : SpecializedCollections.EmptyEnumerable<ProjectId>();
        }

        /// <summary>
        /// Gets the list of projects (topologically sorted) that this project directly depends on.
        /// </summary>
        public IEnumerable<ProjectId> GetProjectsThatThisProjectDirectlyDependsOn(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException("projectId");
            }

            ImmutableHashSet<ProjectId> projectIds;
            return projectToProjectsItReferencesMap.TryGetValue(projectId, out projectIds)
                ? projectIds
                : SpecializedCollections.EmptyEnumerable<ProjectId>();
        }

        internal ProjectDependencyGraph WithReference(Solution newSolution, ProjectId fromId, ProjectId toId)
        {
            return new ProjectDependencyGraph(
                newSolution,
                VersionStamp.Create(),
                this.projectToProjectsItReferencesMap.Add(fromId, toId),
                this.projectToProjectsThatReferenceItMap.Add(toId, fromId));
        }

        internal ProjectDependencyGraph WithoutReference(Solution newSolution, ProjectId fromId, ProjectId toId)
        {
            return new ProjectDependencyGraph(
                newSolution,
                VersionStamp.Create(),
                this.projectToProjectsItReferencesMap.Remove(fromId, toId),
                this.projectToProjectsThatReferenceItMap.Remove(toId, fromId));
        }

        /// <summary>
        /// Called when a solution is updated but when all the projects and project references
        /// haven't changed. We just update to the latest solution snapshot here to not hold the old
        /// solution in memory unnecessarily. Since we know that none of the relevant information
        /// has actually changed, there's no need to recalculate the graph.
        /// </summary>
        internal ProjectDependencyGraph WithNewSolution(Solution newSolution)
        {
            Contract.Requires(!this.projectToProjectsItReferencesMap.Keys.Any((projectId) => !newSolution.ContainsProject(projectId)));
            Contract.Requires(!this.projectToProjectsItReferencesMap.Values.Any((referencedProjectIds) =>
                referencedProjectIds.Any((projectId) => !newSolution.ContainsProject(projectId))));

            return new ProjectDependencyGraph(
                newSolution,
                VersionStamp.Create(),
                this.projectToProjectsItReferencesMap,
                this.projectToProjectsThatReferenceItMap);
        }

        private const string SerializationFormat = "1";

        internal static ProjectDependencyGraph ReadGraph(Solution solution, ObjectReader reader, CancellationToken cancellationToken)
        {
            var references = new List<ProjectId>();
            var map = ImmutableDictionary.Create<ProjectId, ImmutableHashSet<ProjectId>>();
            var reverseMap = ImmutableDictionary.Create<ProjectId, ImmutableHashSet<ProjectId>>();

            var format = reader.ReadString();
            if (!string.Equals(format, SerializationFormat, StringComparison.Ordinal))
            {
                return null;
            }

            var graphVersion = VersionStamp.ReadFrom(reader);

            var projectCount = reader.ReadInt32();
            for (var i = 0; i < projectCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var referenceCount = reader.ReadInt32();
                if (referenceCount < 0)
                {
                    continue;
                }

                references.Clear();
                var projectFilePath = reader.ReadString();

                for (var j = 0; j < referenceCount; j++)
                {
                    var referenceFilePath = reader.ReadString();
                    var referenceProject = GetProject(solution, referenceFilePath);
                    if (referenceProject == null)
                    {
                        return null;
                    }

                    references.Add(referenceProject.Id);
                }

                var project = GetProject(solution, projectFilePath);
                if (project == null)
                {
                    return null;
                }

                map = map.Add(project.Id, ImmutableHashSet.CreateRange<ProjectId>(references));
                reverseMap = reverseMap.AddAll(references, project.Id);
            }

            return new ProjectDependencyGraph(solution, graphVersion, map, reverseMap);
        }

        private static Project GetProject(Solution solution, string filePath)
        {
            return solution.Projects.FirstOrDefault(p => p.FilePath == filePath);
        }

        internal static ProjectDependencyGraph From(Solution solution, ObjectReader reader, CancellationToken cancellationToken)
        {
            try
            {
                var graph = ReadGraph(solution, reader, cancellationToken);
                if (graph == null)
                {
                    return null;
                }

                var latestProjectVersion = solution.GetLatestProjectVersion();

                // check whether the loaded graph is already out of date.
                if (!VersionStamp.CanReusePersistedVersion(latestProjectVersion, graph.Version))
                {
                    return null;
                }

                // make sure loaded graph is consistent with solution.
                if (!CheckGraph(solution, graph.projectToProjectsItReferencesMap, cancellationToken))
                {
                    return null;
                }

                return graph;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static bool CheckGraph(Solution solution, ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> projectsInGraph, CancellationToken cancellationToken)
        {
            // check whether graph de-serialized from the persistence service is valid or not.
            var projectsInSolution = solution.ProjectIds.ToSet();

            if (projectsInGraph.Count != projectsInSolution.Count)
            {
                return false;
            }

            return projectsInSolution.SetEquals(projectsInGraph.Keys, projectsInGraph.KeyComparer);
        }

        // TODO: WriteAsync?
        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            this.Version.WriteTo(writer);

            writer.WriteInt32(this.projectToProjectsItReferencesMap.Count);

            foreach (var id in this.projectToProjectsItReferencesMap.Keys.OrderBy(p => p.Id))
            {
                var project = this.Solution.GetProject(id);
                if (project != null && !string.IsNullOrEmpty(project.FilePath))
                {
                    ImmutableHashSet<ProjectId> projRefs;
                    if (this.projectToProjectsItReferencesMap.TryGetValue(id, out projRefs))
                    {
                        var referencedProjects = projRefs.OrderBy(r => r.Id)
                                                         .Select(r => this.Solution.GetProject(r))
                                                         .Where(p => p != null && !string.IsNullOrEmpty(p.FilePath))
                                                         .ToList();

                        writer.WriteInt32(referencedProjects.Count);
                        writer.WriteString(project.FilePath);

                        if (referencedProjects.Count > 0)
                        {
                            // project references
                            foreach (var referencedProject in referencedProjects)
                            {
                                writer.WriteString(referencedProject.FilePath);
                            }
                        }

                        continue;
                    }
                }

                // invalid project
                writer.WriteInt32(-1);
            }
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            WriteTo(writer);
        }

        private IList<ProjectId> TopologicalSort(
            IEnumerable<ProjectId> projectIds,
            CancellationToken cancellationToken,
            HashSet<ProjectId> seenProjects = null,
            List<ProjectId> resultList = null)
        {
            seenProjects = seenProjects ?? new HashSet<ProjectId>();
            resultList = resultList ?? new List<ProjectId>();

            foreach (var projectId in projectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Make sure we only ever process a project once.
                if (seenProjects.Add(projectId))
                {
                    // Recurse and add anything this project depends on before adding the project
                    // itself.
                    ImmutableHashSet<ProjectId> projectReferenceIds;
                    if (this.projectToProjectsItReferencesMap.TryGetValue(projectId, out projectReferenceIds))
                    {
                        TopologicalSort(projectReferenceIds, cancellationToken, seenProjects, resultList);
                    }

                    resultList.Add(projectId);
                }
            }

            return resultList;
        }

        public IEnumerable<IEnumerable<ProjectId>> GetConnectedProjects(CancellationToken cancellationToken)
        {
            var topologicallySortedProjects = this.GetTopologicallySortedProjects(cancellationToken);
            var result = new List<IEnumerable<ProjectId>>();

            var seenProjects = new HashSet<ProjectId>();

#if false
            Example:

            A <-- B
              |-- C

            D <-- E
              |-- F
#endif

            // Process the projects in topological order (i.e. build order).  This means that the
            // project that has things depending on it will show up first.  Given the above example
            // the topological sort will produce something like:
#if false
            A B D C E F
#endif
            foreach (var project in topologicallySortedProjects)
            {
                if (seenProjects.Add(project))
                {
                    // We've never seen this project before.  That means it's either A or D.  Walk
                    // that project to see all the things that depend on it transitively and make a
                    // connected component out of that.  If it's A then we'll add 'B' and 'C' and
                    // consider them 'seen'.  Then we'll ignore those projects in the outer loop
                    // until we get to 'D'.
                    var connectedGroup = new HashSet<ProjectId>();
                    connectedGroup.AddAll(GetTransitivelyConnectedProjects(project));

                    seenProjects.AddAll(connectedGroup);

                    result.Add(connectedGroup);
                }
            }

#if false
            Other case:
            A<--B
            C<-/
#endif

            return result;
        }

        private IEnumerable<ProjectId> GetTransitivelyConnectedProjects(ProjectId project, HashSet<ProjectId> visited = null)
        {
            visited = visited ?? new HashSet<ProjectId>();
            if (visited.Add(project))
            {
                var otherProjects = this.GetProjectsThatDirectlyDependOnThisProject(project).Concat(
                                    this.GetProjectsThatThisProjectDirectlyDependsOn(project));
                foreach (var other in otherProjects)
                {
                    GetTransitivelyConnectedProjects(other, visited);
                }
            }

            return visited;
        }

        /// <summary>
        /// Gets the list of projects that directly or transitively this project depends on
        /// </summary>
        public IEnumerable<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn(ProjectId projectId)
        {
            return GetProjectsThatThisProjectTransitivelyDependsOn(projectId, visited: null);
        }

        private IEnumerable<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn(ProjectId project, HashSet<ProjectId> visited)
        {
            visited = visited ?? new HashSet<ProjectId>();

            var otherProjects = this.GetProjectsThatThisProjectDirectlyDependsOn(project);
            foreach (var other in otherProjects)
            {
                if (visited.Add(other))
                {
                    GetProjectsThatThisProjectTransitivelyDependsOn(other, visited);
                }
            }

            return visited;
        }

        /// <summary>
        /// Gets the list of projects that directly or transitively depend on this project.
        /// </summary>
        public IEnumerable<ProjectId> GetProjectsThatTransitivelyDependOnThisProject(ProjectId projectId)
        {
            return GetProjectsThatTransitivelyDependOnThisProject(projectId, visited: null);
        }

        private IEnumerable<ProjectId> GetProjectsThatTransitivelyDependOnThisProject(ProjectId project, HashSet<ProjectId> visited)
        {
            visited = visited ?? new HashSet<ProjectId>();

            var otherProjects = this.GetProjectsThatDirectlyDependOnThisProject(project);
            foreach (var other in otherProjects)
            {
                if (visited.Add(other))
                {
                    GetProjectsThatTransitivelyDependOnThisProject(other, visited);
                }
            }

            return visited;
        }
    }
}