// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A <see cref="ProjectDependencyGraph"/> models the dependencies between projects in a solution.
    /// </summary>
    public class ProjectDependencyGraph
    {
        private readonly ImmutableArray<ProjectId> _projectIds;
        private readonly ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _referencesMap;

        // guards lazy computed data
        private readonly NonReentrantLock _dataLock = new NonReentrantLock();

        // these are computed fully on demand
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _lazyReverseReferencesMap;
        private ImmutableArray<ProjectId> _lazyTopologicallySortedProjects;
        private ImmutableArray<IEnumerable<ProjectId>> _lazyDependencySets;

        // these accumulate results on demand
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _transitiveReferencesMap = ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty;
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _reverseTransitiveReferencesMap = ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty;

        internal static readonly ProjectDependencyGraph Empty = new ProjectDependencyGraph(
            ImmutableArray.Create<ProjectId>(),
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty);

        internal ProjectDependencyGraph(
            ImmutableArray<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap)
        {
            _projectIds = projectIds;
            _referencesMap = referencesMap;
        }

        /// <summary>
        /// Gets the list of projects (topologically sorted) that this project directly depends on.
        /// </summary>
        public IImmutableSet<ProjectId> GetProjectsThatThisProjectDirectlyDependsOn(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            ImmutableHashSet<ProjectId> projectIds;
            if (_referencesMap.TryGetValue(projectId, out projectIds))
            {
                return projectIds;
            }
            else
            {
                return ImmutableHashSet<ProjectId>.Empty;
            }
        }

        /// <summary>
        /// Gets the list of projects (topologically sorted) that directly depend on this project.
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
            }

            ImmutableHashSet<ProjectId> reverseReferences;
            if (_lazyReverseReferencesMap.TryGetValue(projectId, out reverseReferences))
            {
                return reverseReferences;
            }
            else
            {
                return ImmutableHashSet<ProjectId>.Empty;
            }
        }

        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> ComputeReverseReferencesMap()
        {
            var reverseReferencesMap = new Dictionary<ProjectId, HashSet<ProjectId>>();

            foreach (var kvp in _referencesMap)
            {
                var references = kvp.Value;
                foreach (var referencedId in references)
                {
                    HashSet<ProjectId> reverseReferences;
                    if (!reverseReferencesMap.TryGetValue(referencedId, out reverseReferences))
                    {
                        reverseReferences = new HashSet<ProjectId>();
                        reverseReferencesMap.Add(referencedId, reverseReferences);
                    }

                    reverseReferences.Add(kvp.Key);
                }
            }

            return reverseReferencesMap
                .Select(kvp => new KeyValuePair<ProjectId, ImmutableHashSet<ProjectId>>(kvp.Key, kvp.Value.ToImmutableHashSet()))
                .ToImmutableDictionary();
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
            ImmutableHashSet<ProjectId> transitiveReferences;
            if (currentMap.TryGetValue(projectId, out transitiveReferences))
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
            ImmutableHashSet<ProjectId> transitiveReferences;
            if (!_transitiveReferencesMap.TryGetValue(projectId, out transitiveReferences))
            {
                using (var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                {
                    var results = pooledObject.Object;
                    this.ComputeTransitiveReferences(projectId, results);
                    transitiveReferences = results.ToImmutableHashSet();
                    _transitiveReferencesMap = _transitiveReferencesMap.Add(projectId, transitiveReferences);
                }
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
            ImmutableHashSet<ProjectId> reverseTransitiveReferences;
            if (currentMap.TryGetValue(projectId, out reverseTransitiveReferences))
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
            ImmutableHashSet<ProjectId> reverseTransitiveReferences;

            if (!_reverseTransitiveReferencesMap.TryGetValue(projectId, out reverseTransitiveReferences))
            {
                using (var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                {
                    var results = pooledObject.Object;

                    ComputeReverseTransitiveReferences(projectId, results);
                    reverseTransitiveReferences = results.ToImmutableHashSet();
                    _reverseTransitiveReferencesMap = _reverseTransitiveReferencesMap.Add(projectId, reverseTransitiveReferences);
                }
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
        public IEnumerable<ProjectId> GetTopologicallySortedProjects(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_lazyTopologicallySortedProjects == null)
            {
                using (_dataLock.DisposableWait(cancellationToken))
                {
                    this.GetTopologicallySortedProjects_NoLock(cancellationToken);
                }
            }

            return _lazyTopologicallySortedProjects;
        }

        private IEnumerable<ProjectId> GetTopologicallySortedProjects_NoLock(CancellationToken cancellationToken)
        {
            if (_lazyTopologicallySortedProjects == null)
            {
                using (var seenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                using (var resultList = SharedPools.Default<List<ProjectId>>().GetPooledObject())
                {
                    this.TopologicalSort(_projectIds, seenProjects.Object, resultList.Object, cancellationToken);
                    _lazyTopologicallySortedProjects = resultList.Object.ToImmutableArray();
                }
            }

            return _lazyTopologicallySortedProjects;
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
                    ImmutableHashSet<ProjectId> projectReferenceIds;
                    if (_referencesMap.TryGetValue(projectId, out projectReferenceIds))
                    {
                        TopologicalSort(projectReferenceIds, seenProjects, resultList, cancellationToken);
                    }

                    resultList.Add(projectId);
                }
            }
        }

        /// <summary>
        /// Returns a sequence of sets, where each set contains items with shared interdependency,
        /// and there is no dependency between sets.
        /// </summary>
        public IEnumerable<IEnumerable<ProjectId>> GetDependencySets(CancellationToken cancellationToken = default(CancellationToken))
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

        private IEnumerable<IEnumerable<ProjectId>> GetDependencySets_NoLock(CancellationToken cancellationToken)
        {
            if (_lazyDependencySets == null)
            {
                using (var seenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                using (var results = SharedPools.Default<List<IEnumerable<ProjectId>>>().GetPooledObject())
                {
                    this.ComputeDependencySets(seenProjects.Object, results.Object, cancellationToken);
                    _lazyDependencySets = results.Object.ToImmutableArray();
                }
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
                    using (var dependencySet = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                    {
                        ComputedDependencySet(project, dependencySet.Object);

                        // add all items in the dependency set to seen projects so we don't revisit any of them
                        seenProjects.UnionWith(dependencySet.Object);

                        // now make sure the items within the sets are topologically sorted.
                        using (var topologicallySeenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                        using (var sortedProjects = SharedPools.Default<List<ProjectId>>().GetPooledObject())
                        {
                            this.TopologicalSort(dependencySet.Object, topologicallySeenProjects.Object, sortedProjects.Object, cancellationToken);
                            results.Add(sortedProjects.Object.ToImmutableArrayOrEmpty());
                        }
                    }
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
    }
}
