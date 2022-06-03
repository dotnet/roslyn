// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A <see cref="ProjectDependencyGraph"/> models the dependencies between projects in a solution.
    /// </summary>
    public partial class ProjectDependencyGraph
    {
        private readonly ImmutableHashSet<ProjectId> _projectIds;

        /// <summary>
        /// The map of projects to dependencies. This field is always fully initialized. Projects which do not reference
        /// any other projects do not have a key in this map (i.e. they are omitted, as opposed to including them with
        /// an empty value).
        ///
        /// <list type="bullet">
        /// <item><description>This field is always fully initialized</description></item>
        /// <item><description>Projects which do not reference any other projects do not have a key in this map (i.e.
        /// they are omitted, as opposed to including them with an empty value)</description></item>
        /// <item><description>The keys and values in this map are always contained in
        /// <see cref="_projectIds"/></description></item>
        /// </list>
        /// </summary>
        private readonly ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _referencesMap;

        // guards lazy computed data
        private readonly NonReentrantLock _dataLock = new();

        /// <summary>
        /// The lazily-initialized map of projects to projects which reference them. This field is either null, or
        /// fully-computed. Projects which are not referenced by any other project do not have a key in this map (i.e.
        /// they are omitted, as opposed to including them with an empty value).
        /// </summary>
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? _lazyReverseReferencesMap;

        // These are computed fully on demand. ImmutableArray.IsDefault indicates the item needs to be realized
        private ImmutableArray<ProjectId> _lazyTopologicallySortedProjects;

        // This is not typed ImmutableArray<ImmutableArray<...>> because GetDependencySets() wants to return
        // an IEnumerable<IEnumerable<...>>, and ImmutableArray<ImmutableArray<...>> can't be converted
        // to an IEnumerable<IEnumerable<...>> without a bunch of boxing.
        private ImmutableArray<IEnumerable<ProjectId>> _lazyDependencySets;

        // These accumulate results on demand. They are never null, but a missing key/value pair indicates it needs to be computed.
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _transitiveReferencesMap;
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _reverseTransitiveReferencesMap;

        internal static readonly ProjectDependencyGraph Empty = new(
            ImmutableHashSet<ProjectId>.Empty,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
            ImmutableArray<ProjectId>.Empty,
            ImmutableArray<IEnumerable<ProjectId>>.Empty);

        internal ProjectDependencyGraph(
            ImmutableHashSet<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap)
            : this(
                  projectIds,
                  RemoveItemsWithEmptyValues(referencesMap),
                  reverseReferencesMap: null,
                  transitiveReferencesMap: ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
                  reverseTransitiveReferencesMap: ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty,
                  default,
                  default)
        {
        }

        // This constructor is private to prevent other Roslyn code from producing this type with inconsistent inputs.
        private ProjectDependencyGraph(
            ImmutableHashSet<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? reverseReferencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> transitiveReferencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> reverseTransitiveReferencesMap,
            ImmutableArray<ProjectId> topologicallySortedProjects,
            ImmutableArray<IEnumerable<ProjectId>> dependencySets)
        {
            Contract.ThrowIfNull(transitiveReferencesMap);
            Contract.ThrowIfNull(reverseTransitiveReferencesMap);

            _projectIds = projectIds;
            _referencesMap = referencesMap;
            _lazyReverseReferencesMap = reverseReferencesMap;
            _transitiveReferencesMap = transitiveReferencesMap;
            _reverseTransitiveReferencesMap = reverseTransitiveReferencesMap;
            _lazyTopologicallySortedProjects = topologicallySortedProjects;
            _lazyDependencySets = dependencySets;

            ValidateForwardReferences(_projectIds, _referencesMap);
            ValidateReverseReferences(_projectIds, _referencesMap, _lazyReverseReferencesMap);
        }

        internal ImmutableHashSet<ProjectId> ProjectIds => _projectIds;

        private static ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> RemoveItemsWithEmptyValues(
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> map)
        {
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Builder? builder = null;
            foreach (var (key, value) in map)
            {
                if (!value.IsEmpty)
                {
                    continue;
                }

                builder ??= map.ToBuilder();
                builder.Remove(key);
            }

            return builder?.ToImmutable() ?? map;
        }

        internal ProjectDependencyGraph WithProjectReferences(ProjectId projectId, IReadOnlyList<ProjectReference> projectReferences)
        {
            Contract.ThrowIfFalse(_projectIds.Contains(projectId));

            // This method we can't optimize very well: changing project references arbitrarily could invalidate pretty much anything.
            // The only thing we can reuse is our actual map of project references for all the other projects, so we'll do that.

            // only include projects contained in the solution:
            var referencedProjectIds = projectReferences.IsEmpty() ? ImmutableHashSet<ProjectId>.Empty :
                projectReferences
                    .Where(r => _projectIds.Contains(r.ProjectId))
                    .Select(r => r.ProjectId)
                    .ToImmutableHashSet();

            var referencesMap = referencedProjectIds.IsEmpty ?
                _referencesMap.Remove(projectId) : _referencesMap.SetItem(projectId, referencedProjectIds);

            return new ProjectDependencyGraph(_projectIds, referencesMap);
        }

        /// <summary>
        /// Gets the list of projects that this project directly depends on.
        /// </summary>
        public IImmutableSet<ProjectId> GetProjectsThatThisProjectDirectlyDependsOn(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            return _referencesMap.GetValueOrDefault(projectId, ImmutableHashSet<ProjectId>.Empty);
        }

        /// <summary>
        /// Gets the list of projects that directly depend on this project.
        /// </summary> 
        public IImmutableSet<ProjectId> GetProjectsThatDirectlyDependOnThisProject(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (_lazyReverseReferencesMap == null)
            {
                using (_dataLock.DisposableWait())
                {
                    return this.GetProjectsThatDirectlyDependOnThisProject_NoLock(projectId);
                }
            }
            else
            {
                // okay, because its only ever going to be computed and assigned once
                return this.GetProjectsThatDirectlyDependOnThisProject_NoLock(projectId);
            }
        }

        private ImmutableHashSet<ProjectId> GetProjectsThatDirectlyDependOnThisProject_NoLock(ProjectId projectId)
        {
            if (_lazyReverseReferencesMap == null)
            {
                _lazyReverseReferencesMap = this.ComputeReverseReferencesMap();
                ValidateReverseReferences(_projectIds, _referencesMap, _lazyReverseReferencesMap);
            }

            return _lazyReverseReferencesMap.GetValueOrDefault(projectId, ImmutableHashSet<ProjectId>.Empty);
        }

        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeReverseReferencesMap()
        {
            var reverseReferencesMap = new Dictionary<ProjectId, HashSet<ProjectId>>();

            foreach (var (projectId, references) in _referencesMap)
            {
                foreach (var referencedId in references)
                    reverseReferencesMap.MultiAdd(referencedId, projectId);
            }

            return reverseReferencesMap
                .Select(kvp => new KeyValuePair<ProjectId, ImmutableHashSet<ProjectId>>(kvp.Key, kvp.Value.ToImmutableHashSet()))
                .ToImmutableDictionary();
        }

        /// <summary>
        /// Gets the list of projects that directly or transitively this project depends on, if it has already been
        /// cached.
        /// </summary>
        internal ImmutableHashSet<ProjectId>? TryGetProjectsThatThisProjectTransitivelyDependsOn(ProjectId projectId)
        {
            if (projectId is null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            _transitiveReferencesMap.TryGetValue(projectId, out var projects);
            return projects;
        }

        /// <summary>
        /// Gets the list of projects that directly or transitively this project depends on
        /// </summary>
        public IImmutableSet<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            // first try without lock for speed
            var currentMap = _transitiveReferencesMap;
            if (currentMap.TryGetValue(projectId, out var transitiveReferences))
            {
                return transitiveReferences;
            }
            else
            {
                using (_dataLock.DisposableWait())
                {
                    return GetProjectsThatThisProjectTransitivelyDependsOn_NoLock(projectId);
                }
            }
        }

        private ImmutableHashSet<ProjectId> GetProjectsThatThisProjectTransitivelyDependsOn_NoLock(ProjectId projectId)
        {
            if (!_transitiveReferencesMap.TryGetValue(projectId, out var transitiveReferences))
            {
                using var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                var results = pooledObject.Object;
                this.ComputeTransitiveReferences(projectId, results);
                transitiveReferences = results.ToImmutableHashSet();
                _transitiveReferencesMap = _transitiveReferencesMap.Add(projectId, transitiveReferences);
            }

            return transitiveReferences;
        }

        private void ComputeTransitiveReferences(ProjectId project, HashSet<ProjectId> result)
        {
            var otherProjects = this.GetProjectsThatThisProjectDirectlyDependsOn(project);

            foreach (var other in otherProjects)
            {
                if (result.Add(other))
                {
                    ComputeTransitiveReferences(other, result);
                }
            }
        }

        /// <summary>
        /// Gets the list of projects that directly or transitively depend on this project.
        /// </summary>
        public IEnumerable<ProjectId> GetProjectsThatTransitivelyDependOnThisProject(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            // first try without lock for speed
            var currentMap = _reverseTransitiveReferencesMap;
            if (currentMap.TryGetValue(projectId, out var reverseTransitiveReferences))
            {
                return reverseTransitiveReferences;
            }
            else
            {
                using (_dataLock.DisposableWait())
                {
                    return this.GetProjectsThatTransitivelyDependOnThisProject_NoLock(projectId);
                }
            }
        }

        private ImmutableHashSet<ProjectId> GetProjectsThatTransitivelyDependOnThisProject_NoLock(ProjectId projectId)
        {
            if (!_reverseTransitiveReferencesMap.TryGetValue(projectId, out var reverseTransitiveReferences))
            {
                using var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                var results = pooledObject.Object;

                ComputeReverseTransitiveReferences(projectId, results);
                reverseTransitiveReferences = results.ToImmutableHashSet();
                _reverseTransitiveReferencesMap = _reverseTransitiveReferencesMap.Add(projectId, reverseTransitiveReferences);
            }

            return reverseTransitiveReferences;
        }

        private void ComputeReverseTransitiveReferences(ProjectId project, HashSet<ProjectId> results)
        {
            var otherProjects = this.GetProjectsThatDirectlyDependOnThisProject_NoLock(project);
            foreach (var other in otherProjects)
            {
                if (results.Add(other))
                {
                    ComputeReverseTransitiveReferences(other, results);
                }
            }
        }

        /// <summary>
        /// Returns all the projects for the solution in a topologically sorted order with respect
        /// to their dependencies. Projects that depend on other projects will always show up later in this sequence
        /// than the projects they depend on.
        /// </summary>
        public IEnumerable<ProjectId> GetTopologicallySortedProjects(CancellationToken cancellationToken = default)
        {
            if (_lazyTopologicallySortedProjects.IsDefault)
            {
                using (_dataLock.DisposableWait(cancellationToken))
                {
                    this.GetTopologicallySortedProjects_NoLock(cancellationToken);
                }
            }

            return _lazyTopologicallySortedProjects;
        }

        private void GetTopologicallySortedProjects_NoLock(CancellationToken cancellationToken)
        {
            if (_lazyTopologicallySortedProjects.IsDefault)
            {
                using var seenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                using var resultList = SharedPools.Default<List<ProjectId>>().GetPooledObject();
                this.TopologicalSort(_projectIds, seenProjects.Object, resultList.Object, cancellationToken);
                _lazyTopologicallySortedProjects = resultList.Object.ToImmutableArray();
            }
        }

        private void TopologicalSort(
            IEnumerable<ProjectId> projectIds,
            HashSet<ProjectId> seenProjects,
            List<ProjectId> resultList,
            CancellationToken cancellationToken)
        {
            foreach (var projectId in projectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Make sure we only ever process a project once.
                if (seenProjects.Add(projectId))
                {
                    // Recurse and add anything this project depends on before adding the project itself.
                    if (_referencesMap.TryGetValue(projectId, out var projectReferenceIds))
                    {
                        TopologicalSort(projectReferenceIds, seenProjects, resultList, cancellationToken);
                    }

                    resultList.Add(projectId);
                }
            }
        }

        /// <summary>
        /// Returns a sequence of sets, where each set contains items with shared interdependency,
        /// and there is no dependency between sets.  Each set returned will sorted in topological order.
        /// </summary>
        public IEnumerable<IEnumerable<ProjectId>> GetDependencySets(CancellationToken cancellationToken = default)
        {
            if (_lazyDependencySets == null)
            {
                using (_dataLock.DisposableWait(cancellationToken))
                {
                    this.GetDependencySets_NoLock(cancellationToken);
                }
            }

            return _lazyDependencySets;
        }

        private ImmutableArray<IEnumerable<ProjectId>> GetDependencySets_NoLock(CancellationToken cancellationToken)
        {
            if (_lazyDependencySets == null)
            {
                using var seenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                using var results = SharedPools.Default<List<IEnumerable<ProjectId>>>().GetPooledObject();
                this.ComputeDependencySets(seenProjects.Object, results.Object, cancellationToken);
                _lazyDependencySets = results.Object.ToImmutableArray();
            }

            return _lazyDependencySets;
        }

        private void ComputeDependencySets(HashSet<ProjectId> seenProjects, List<IEnumerable<ProjectId>> results, CancellationToken cancellationToken)
        {
            foreach (var project in _projectIds)
            {
                if (seenProjects.Add(project))
                {
                    // We've never seen this project before, so we have not yet dealt with any projects
                    // in its dependency set.
                    using var dependencySet = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                    ComputedDependencySet(project, dependencySet.Object);

                    // add all items in the dependency set to seen projects so we don't revisit any of them
                    seenProjects.UnionWith(dependencySet.Object);

                    // now make sure the items within the sets are topologically sorted.
                    using var topologicallySeenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                    using var sortedProjects = SharedPools.Default<List<ProjectId>>().GetPooledObject();
                    this.TopologicalSort(dependencySet.Object, topologicallySeenProjects.Object, sortedProjects.Object, cancellationToken);
                    results.Add(sortedProjects.Object.ToImmutableArrayOrEmpty());
                }
            }
        }

        private void ComputedDependencySet(ProjectId project, HashSet<ProjectId> result)
        {
            if (result.Add(project))
            {
                var otherProjects = this.GetProjectsThatDirectlyDependOnThisProject_NoLock(project).Concat(
                                    this.GetProjectsThatThisProjectDirectlyDependsOn(project));

                foreach (var other in otherProjects)
                {
                    ComputedDependencySet(other, result);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateForwardReferences(
            ImmutableHashSet<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap)
        {
            RoslynDebug.Assert(referencesMap is object);

            Debug.Assert(projectIds.Count >= referencesMap.Count);
            Debug.Assert(referencesMap.Keys.All(projectIds.Contains));

            foreach (var (_, referencedProjects) in referencesMap)
            {
                Debug.Assert(!referencedProjects.IsEmpty, "Unexpected empty value in the forward references map.");
                foreach (var referencedProject in referencedProjects)
                {
                    Debug.Assert(projectIds.Contains(referencedProject), "Unexpected reference to unknown project.");
                }
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateReverseReferences(
            ImmutableHashSet<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> forwardReferencesMap,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>? reverseReferencesMap)
        {
            if (reverseReferencesMap is null)
                return;

            Debug.Assert(projectIds.Count >= reverseReferencesMap.Count);
            Debug.Assert(reverseReferencesMap.Keys.All(projectIds.Contains));

            foreach (var (project, referencedProjects) in forwardReferencesMap)
            {
                foreach (var referencedProject in referencedProjects)
                {
                    Debug.Assert(reverseReferencesMap.ContainsKey(referencedProject));
                    Debug.Assert(reverseReferencesMap[referencedProject].Contains(project));
                }
            }

            foreach (var (project, referencingProjects) in reverseReferencesMap)
            {
                Debug.Assert(!referencingProjects.IsEmpty, "Unexpected empty value in the reverse references map.");
                foreach (var referencingProject in referencingProjects)
                {
                    Debug.Assert(forwardReferencesMap.ContainsKey(referencingProject));
                    Debug.Assert(forwardReferencesMap[referencingProject].Contains(project));
                }
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly ProjectDependencyGraph _instance;

            public TestAccessor(ProjectDependencyGraph instance)
                => _instance = instance;

            /// <summary>
            /// Gets the list of projects that directly or transitively depend on this project, if it has already been
            /// cached.
            /// </summary>
            public ImmutableHashSet<ProjectId>? TryGetProjectsThatTransitivelyDependOnThisProject(ProjectId projectId)
            {
                if (projectId is null)
                {
                    throw new ArgumentNullException(nameof(projectId));
                }

                _instance._reverseTransitiveReferencesMap.TryGetValue(projectId, out var projects);
                return projects;
            }
        }

        /// <summary>
        /// Checks whether <paramref name="id"/> depends on <paramref name="potentialDependency"/>.
        /// </summary>
        internal bool DoesProjectTransitivelyDependOnProject(ProjectId id, ProjectId potentialDependency)
        {
            // Check the dependency graph to see if project 'id' directly or transitively depends on 'projectId'.
            // If the information is not available, do not compute it.
            var forwardDependencies = TryGetProjectsThatThisProjectTransitivelyDependsOn(id);
            if (forwardDependencies is object && forwardDependencies.Contains(potentialDependency))
            {
                return true;
            }

            // Compute the set of all projects that depend on 'potentialDependency'. This information answers the same
            // question as the previous check, but involves at most one transitive computation within the
            // dependency graph when you are checking multiple projects against the same potentialDependency.
            return GetProjectsThatTransitivelyDependOnThisProject(potentialDependency).Contains(id);
        }
    }
}
