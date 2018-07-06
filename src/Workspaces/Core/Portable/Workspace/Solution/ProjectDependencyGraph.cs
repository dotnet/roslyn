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
        private readonly ImmutableHashSet<ProjectId> _projectIds;
        private readonly ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _referencesMap;

        // guards lazy computed data
        private readonly NonReentrantLock _dataLock = new NonReentrantLock();

        // These are computed fully on demand. null or ImmutableArray.IsDefault indicates the item needs to be realized
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _lazyReverseReferencesMap;
        private ImmutableArray<ProjectId> _lazyTopologicallySortedProjects;
        private ImmutableArray<IEnumerable<ProjectId>> _lazyDependencySets;

        // These accumulate results on demand. They are never null, but a missing key/value pair indicates it needs to be computed.
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _transitiveReferencesMap;
        private ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> _reverseTransitiveReferencesMap;

        internal static readonly ProjectDependencyGraph Empty = new ProjectDependencyGraph(
            ImmutableHashSet<ProjectId>.Empty,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>>.Empty);

        internal ProjectDependencyGraph(
            ImmutableHashSet<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> referencesMap)
            : this(
                  projectIds,
                  referencesMap,
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
            ImmutableDictionary<ProjectId, ImmutableHashSet<ProjectId>> reverseReferencesMap,
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
        }

        internal ProjectDependencyGraph WithAdditionalProjects(IEnumerable<ProjectId> projectIds)
        {
            // Track the existence of some new projects. Note this call only adds new ProjectIds, but doesn't add any references. Any caller who wants to add a new project
            // with references will first call this, and then call WithAdditionalProjectReferences to add references as well.
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

            return new ProjectDependencyGraph(
                _projectIds.Union(projectIds),
                referencesMap: _referencesMap,
                reverseReferencesMap: _lazyReverseReferencesMap,
                transitiveReferencesMap: _transitiveReferencesMap,
                reverseTransitiveReferencesMap: _reverseTransitiveReferencesMap,
                topologicallySortedProjects: newTopologicallySortedProjects,
                dependencySets: newDependencySets);
        }

        internal ProjectDependencyGraph WithAdditionalProjectReferences(ProjectId projectId, IReadOnlyList<ProjectId> referencedProjectIds)
        {
            if (!_projectIds.Contains(projectId))
            {
                throw new InvalidOperationException($"{nameof(projectId)} isn't being tracked by this dependency graph.");
            }

            if (referencedProjectIds.Count == 0)
            {
                return this;
            }

            // Step #1: recompute our references map
            var newReferencesMap = _referencesMap;

            if (newReferencesMap.TryGetValue(projectId, out var existingReferences))
            {
                newReferencesMap = newReferencesMap.SetItem(projectId, existingReferences.Union(referencedProjectIds));
            }
            else
            {
                newReferencesMap = newReferencesMap.SetItem(projectId, referencedProjectIds.ToImmutableHashSet());
            }

            // Step #2: compute our reverse map, if we had one previously to build off of
            var newReverseReferencesMap = _lazyReverseReferencesMap;

            if (newReverseReferencesMap != null)
            {
                var builder = newReverseReferencesMap.ToBuilder();

                foreach (var referencedProject in referencedProjectIds)
                {
                    if (builder.TryGetValue(referencedProject, out var reverseReferences))
                    {
                        builder[referencedProject] = reverseReferences.Add(projectId);
                    }
                    else
                    {
                        builder[referencedProject] = ImmutableHashSet.Create(projectId);
                    }
                }

                newReverseReferencesMap = builder.ToImmutable();
            }

            // Step #3: update our forward transitive map, by combining the transitive references of the new projects into the transitive references of projectId.
            // If we didn't have transitive references computed for us, then we'll skip this because we won't be able to incrementally update what we don't have
            var newTransitiveReferencesMap = _transitiveReferencesMap;

            if (newTransitiveReferencesMap.TryGetValue(projectId, out var existingTransitiveReferences))
            {
                // We now will make a builder to compute the new data. If we discover that we don't know the transitive references for one of the new projects being added,
                // then we will null this local out and remove any cached data we have for projectId instead.
                var builder = existingTransitiveReferences.ToBuilder();

                // We already have some transitive references. We'll incorporate in transitive references of the projects we all have.
                // If any of our dependencies haven't already computed their transitive references, we'll just remove the entry on newTransitiveReferencesMap so
                // as to compute it lazily later.
                foreach (var referencedProjectId in referencedProjectIds)
                {
                    if (newTransitiveReferencesMap.TryGetValue(referencedProjectId, out var transitiveReferencesToUnion))
                    {
                        builder.UnionWith(transitiveReferencesToUnion);
                    }
                    else if (GetProjectsThatThisProjectDirectlyDependsOn(referencedProjectId).Count > 0)
                    {
                        // We have a problem: we know that referencedProjectId has at least one reference itself, but we don't know it's full transitive reference set. At this
                        // point, we'll just remove it from our cache, and lazy compute it later
                        builder = null;
                        break;
                    }
                }

                if (builder != null)
                {
                    newTransitiveReferencesMap = newTransitiveReferencesMap.SetItem(projectId, builder.ToImmutable());
                }
                else
                {
                    newTransitiveReferencesMap = newTransitiveReferencesMap.Remove(projectId);
                }
            }

            // Step 4: update our reverse transitive references map, by including the reverse transitive references of projectId into the referenced projects
            var newReverseTransitiveReferencesMapBuilder = _reverseTransitiveReferencesMap.ToBuilder();
            bool reverseTransitiveReferencesKnown = newReverseTransitiveReferencesMapBuilder.TryGetValue(projectId, out var reverseTransitiveReferencesToUnion);

            foreach (var referencedProjectId in referencedProjectIds)
            {
                if (!reverseTransitiveReferencesKnown)
                {
                    newReverseTransitiveReferencesMapBuilder.Remove(referencedProjectId);
                }
                else if (_reverseTransitiveReferencesMap.TryGetValue(referencedProjectId, out var existingReverseTransitiveReferences))
                {
                    newReverseTransitiveReferencesMapBuilder[referencedProjectId] = existingReverseTransitiveReferences.Union(reverseTransitiveReferencesToUnion);
                }
            }

            // Note: rather than updating our dependency sets and topologically sorted data, we'll throw that away since incremental update is
            // tricky, and those are rarely used.
            return new ProjectDependencyGraph(
                _projectIds,
                referencesMap: newReferencesMap,
                reverseReferencesMap: newReverseReferencesMap,
                transitiveReferencesMap: newTransitiveReferencesMap,
                reverseTransitiveReferencesMap: newReverseTransitiveReferencesMapBuilder.ToImmutable(),
                topologicallySortedProjects: default,
                dependencySets: default);
        }

        internal ProjectDependencyGraph WithProjectReferences(ProjectId projectId, IEnumerable<ProjectId> referencedProjectIds)
        {
            // This method we can't optimize very well: changing project references arbitrarily could invalidate pretty much anything. The only thing we can reuse is our
            // actual map of project references for all the other projects, so we'll do that
            return new ProjectDependencyGraph(_projectIds, _referencesMap.SetItem(projectId, referencedProjectIds.ToImmutableHashSet()));
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

            if (_referencesMap.TryGetValue(projectId, out var projectIds))
            {
                return projectIds;
            }
            else
            {
                return ImmutableHashSet<ProjectId>.Empty;
            }
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
            }

            if (_lazyReverseReferencesMap.TryGetValue(projectId, out var reverseReferences))
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
                    if (!reverseReferencesMap.TryGetValue(referencedId, out var reverseReferences))
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
                using (var seenProjects = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
                using (var resultList = SharedPools.Default<List<ProjectId>>().GetPooledObject())
                {
                    this.TopologicalSort(_projectIds, seenProjects.Object, resultList.Object, cancellationToken);
                    _lazyTopologicallySortedProjects = resultList.Object.ToImmutableArray();
                }
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
        /// and there is no dependency between sets.
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
