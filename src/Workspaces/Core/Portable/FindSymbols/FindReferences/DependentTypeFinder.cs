// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using RelatedTypeCache = ConditionalWeakTable<Solution, ConcurrentDictionary<(SymbolKey, IImmutableSet<Project>), AsyncLazy<ImmutableArray<(SymbolKey, ProjectId)>>>>;
    using SymbolAndProjectIdSet = HashSet<SymbolAndProjectId<INamedTypeSymbol>>;

    /// <summary>
    /// Provides helper methods for finding dependent types (derivations, implementations, 
    /// etc.) across a solution.  The results found are returned in pairs of <see cref="ISymbol"/>s
    /// and <see cref="ProjectId"/>s.  The Ids specify what project we were searching in when
    /// we found the symbol.  That project has the compilation that we found the specific
    /// source or metadata symbol within.  Note that for metadata symbols there could be
    /// many projects where the same symbol could be found.  However, we only return the
    /// first instance we found.
    /// </summary>
    internal static partial class DependentTypeFinder
    {
        private static Func<Location, bool> s_isInMetadata = loc => loc.IsInMetadata;
        private static Func<Location, bool> s_isInSource = loc => loc.IsInSource;

        private static Func<INamedTypeSymbol, bool> s_isNonSealedClass =
            t => t?.TypeKind == TypeKind.Class && !t.IsSealed;

        private static readonly Func<INamedTypeSymbol, bool> s_isInterfaceOrNonSealedClass =
            t => t.TypeKind == TypeKind.Interface || s_isNonSealedClass(t);

        private static readonly ObjectPool<SymbolAndProjectIdSet> s_setPool = new ObjectPool<SymbolAndProjectIdSet>(
            () => new SymbolAndProjectIdSet(SymbolAndProjectIdComparer<INamedTypeSymbol>.SymbolEquivalenceInstance));

        // Caches from a types to their related types (in the context of a specific solution).
        // Kept as a cache so that clients who make many calls into us won't end up computing
        // the same data over and over again.  Will be let go the moment the solution they're
        // based off of is no longer alive.
        //
        // Importantly, the caches only store SymbolKeys and Ids.  As such, they will not hold
        // any Symbols or Compilations alive.

        private static readonly RelatedTypeCache s_typeToImmediatelyDerivedClassesMap = new RelatedTypeCache();
        private static readonly RelatedTypeCache s_typeToTransitivelyDerivedClassesMap = new RelatedTypeCache();
        private static readonly RelatedTypeCache s_typeToTransitivelyImplementingStructuresClassesAndInterfacesMap = new RelatedTypeCache();
        private static readonly RelatedTypeCache s_typeToImmediatelyDerivedAndImplementingTypesMap = new RelatedTypeCache();

        public static async Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTypesFromCacheOrComputeAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            RelatedTypeCache cache,
            Func<CancellationToken, Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>>> findAsync,
            CancellationToken cancellationToken)
        {
            var dictionary = cache.GetOrCreateValue(solution);

            var result = default(ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>);

            // Do a quick lookup first to avoid the allocation.  If it fails, go through the
            // slower allocating path.
            var key = (type.GetSymbolKey(), projects);
            if (!dictionary.TryGetValue(key, out var lazy))
            {
                lazy = dictionary.GetOrAdd(key,
                    new AsyncLazy<ImmutableArray<(SymbolKey, ProjectId)>>(
                        async c =>
                        {
                            // If we're the code that is actually computing the symbols, then just 
                            // take our result and store it in the outer frame.  That way the caller
                            // doesn't need to incur the cost of deserializing the symbol keys that
                            // we're create right below this.
                            result = await findAsync(c).ConfigureAwait(false);
                            return result.SelectAsArray(t => (t.Symbol.GetSymbolKey(), t.ProjectId));
                        },
                        cacheResult: true));
            }

            // If we were the caller that actually computed the symbols, then we can just return
            // the values we got.
            if (!result.IsDefault)
            {
                return result;
            }

            // Otherwise, someone else computed the symbols and cached the results as symbol 
            // keys.  Convert those symbol keys back to symbols and return.
            var symbolKeys = await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var builder = ArrayBuilder<SymbolAndProjectId<INamedTypeSymbol>>.GetInstance();

            // Group by projectId so that we only process one project/compilation at a time.
            // Also, process in dependency order so that previous compilations are ready if
            // they're referenced by later compilations.
            var dependencyOrder = solution.GetProjectDependencyGraph()
                                          .GetTopologicallySortedProjects()
                                          .Select((id, index) => (id, index))
                                          .ToDictionary(t => t.id, t => t.index);

            var orderedGroups = symbolKeys.GroupBy(t => t.Item2).OrderBy(g => dependencyOrder[g.Key]);
            foreach (var group in orderedGroups)
            {
                var project = solution.GetProject(group.Key);
                if (project.SupportsCompilation)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var (symbolKey, _) in group)
                    {
                        var resolvedSymbol = symbolKey.Resolve(compilation, cancellationToken: cancellationToken).GetAnySymbol();
                        if (resolvedSymbol is INamedTypeSymbol namedType)
                        {
                            builder.Add(new SymbolAndProjectId<INamedTypeSymbol>(namedType, project.Id));
                        }
                    }
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        public static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindImmediatelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects: null,
                cache: s_typeToImmediatelyDerivedClassesMap,
                findAsync: c => FindDerivedClassesAsync(
                    SymbolAndProjectId.Create(type, projectId: null), solution, projects: null,
                    transitive: false, cancellationToken: c),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync(SymbolAndProjectId{INamedTypeSymbol}, Solution, IImmutableSet{Project}, CancellationToken)"/>, which is a publically callable method.
        /// </summary>
        public static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTransitivelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyDerivedClassesMap,
                c => FindDerivedClassesAsync(
                    SymbolAndProjectId.Create(type, projectId: null), solution, projects,
                    transitive: true, cancellationToken: c),
                cancellationToken);
        }

        private static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindDerivedClassesAsync(
            SymbolAndProjectId<INamedTypeSymbol> type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            if (s_isNonSealedClass(type.Symbol))
            {
                bool metadataTypeMatches(SymbolAndProjectIdSet set, INamedTypeSymbol metadataType)
                    => TypeDerivesFrom(set, metadataType, transitive);

                bool sourceTypeImmediatelyMatches(SymbolAndProjectIdSet set, INamedTypeSymbol metadataType)
                    => set.Contains(SymbolAndProjectId.Create(metadataType.BaseType?.OriginalDefinition, projectId: null));

                return FindTypesAsync(type, solution, projects,
                    metadataTypeMatches: metadataTypeMatches,
                    sourceTypeImmediatelyMatches: sourceTypeImmediatelyMatches,
                    shouldContinueSearching: s_isNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>();
        }

        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync(SymbolAndProjectId, Solution, IImmutableSet{Project}, CancellationToken)"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static async Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTransitivelyImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var derivedAndImplementingTypes = await FindTransitivelyImplementingStructuresClassesAndInterfacesAsync(type, solution, projects, cancellationToken).ConfigureAwait(false);

            // We only want implementing types here, not derived interfaces.
            return derivedAndImplementingTypes.WhereAsArray(
                t => t.Symbol.TypeKind == TypeKind.Class || t.Symbol.TypeKind == TypeKind.Struct);
        }

        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync(SymbolAndProjectId, Solution, IImmutableSet{Project}, CancellationToken)"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTransitivelyImplementingStructuresClassesAndInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyImplementingStructuresClassesAndInterfacesMap,
                c => FindTransitivelyImplementingStructuresClassesAndInterfacesWorkerAsync(type, solution, projects, c),
                cancellationToken);
        }

        private static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTransitivelyImplementingStructuresClassesAndInterfacesWorkerAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindDerivedAndImplementingTypesAsync(
                SymbolAndProjectId.Create(type, projectId: null), solution, projects,
                transitive: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        public static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindImmediatelyDerivedAndImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects: null,
                cache: s_typeToImmediatelyDerivedAndImplementingTypesMap,
                findAsync: c => FindDerivedAndImplementingTypesAsync(
                    SymbolAndProjectId.Create(type, projectId: null), solution, projects: null,
                    transitive: false, cancellationToken: c),
                cancellationToken: cancellationToken);
        }

        private static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindDerivedAndImplementingTypesAsync(
            SymbolAndProjectId<INamedTypeSymbol> type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type.Symbol?.TypeKind == TypeKind.Interface)
            {
                bool metadataTypeMatches(SymbolAndProjectIdSet s, INamedTypeSymbol t)
                    => TypeDerivesFrom(s, t, transitive) || TypeImplementsFrom(s, t, transitive);

                return FindTypesAsync(type, solution, projects,
                    metadataTypeMatches: metadataTypeMatches,
                    sourceTypeImmediatelyMatches: ImmediatelyDerivesOrImplementsFrom,
                    shouldContinueSearching: s_isInterfaceOrNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>();
        }

        private static async Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindTypesAsync(
            SymbolAndProjectId<INamedTypeSymbol> type,
            Solution solution,
            IImmutableSet<Project> projects,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> metadataTypeMatches,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            CancellationToken cancellationToken)
        {
            type = type.WithSymbol(type.Symbol.OriginalDefinition);
            projects ??= ImmutableHashSet.Create(solution.Projects.ToArray());
            var searchInMetadata = type.Symbol.Locations.Any(s_isInMetadata);

            // Note: it is not sufficient to just walk the list of projects passed in,
            // searching only those for derived types.
            //
            // Say we have projects: A <- B <- C, but only projects A and C are passed in.
            // We might miss a derived type in C if there's an intermediate derived type
            // in B.
            //
            // However, say we have projects A <- B <- C <- D, only only projects A and C
            // are passed in.  There is no need to check D as there's no way it could
            // contribute an intermediate type that affects A or C.  We only need to check
            // A, B and C

            // First find all the projects that could potentially reference this type.
            var projectsThatCouldReferenceType = await GetProjectsThatCouldReferenceTypeAsync(
                type.Symbol, solution, searchInMetadata, cancellationToken).ConfigureAwait(false);

            // Now, based on the list of projects that could actually reference the type,
            // and the list of projects the caller wants to search, find the actual list of
            // projects we need to search through.
            //
            // This list of projects is properly topologicaly ordered.  Because of this we
            // can just process them in order from first to last because we know no project
            // in this list could affect a prior project.
            var orderedProjectsToExamine = GetOrderedProjectsToExamine(
                solution, projects, projectsThatCouldReferenceType);

            var currentMetadataTypes = CreateSymbolAndProjectIdSet();
            var currentSourceAndMetadataTypes = CreateSymbolAndProjectIdSet();

            currentSourceAndMetadataTypes.Add(type);
            if (searchInMetadata)
            {
                currentMetadataTypes.Add(type);
            }

            var result = CreateSymbolAndProjectIdSet();

            // Now walk the projects from left to right seeing what our type cascades to. Once we 
            // reach a fixed point in that project, take all the types we've found and move to the
            // next project.  Continue this until we've exhausted all projects.
            //
            // Because there is a data-dependency between the projects, we cannot process them in
            // parallel.  (Processing linearly is also probably preferable to limit the amount of
            // cache churn we could cause creating all those compilations.
            foreach (var project in orderedProjectsToExamine)
            {
                Debug.Assert(project.SupportsCompilation);
                await FindTypesInProjectAsync(
                    searchInMetadata, result,
                    currentMetadataTypes, currentSourceAndMetadataTypes,
                    project,
                    metadataTypeMatches,
                    sourceTypeImmediatelyMatches,
                    shouldContinueSearching,
                    transitive, cancellationToken).ConfigureAwait(false);
            }

            return ToImmutableAndFree(result);
        }

        private static ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>> ToImmutableAndFree(
            SymbolAndProjectIdSet set)
        {
            var array = set.ToImmutableArray();
            s_setPool.ClearAndFree(set);
            return array;
        }

        private static async Task FindTypesInProjectAsync(
            bool searchInMetadata,
            SymbolAndProjectIdSet result,
            SymbolAndProjectIdSet currentMetadataTypes,
            SymbolAndProjectIdSet currentSourceAndMetadataTypes,
            Project project,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> metadataTypeMatches,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            CancellationToken cancellationToken)
        {
            Debug.Assert(project.SupportsCompilation);

            // First see what derived metadata types we might find in this project.
            // This is only necessary if we started with a metadata type.
            if (searchInMetadata)
            {
                var foundMetadataTypes = CreateSymbolAndProjectIdSet();

                try
                {
                    await AddAllMatchingMetadataTypesInProjectAsync(
                        currentMetadataTypes, project, metadataTypeMatches,
                        foundMetadataTypes, cancellationToken).ConfigureAwait(false);

                    foreach (var foundTypeAndProjectId in foundMetadataTypes)
                    {
                        var foundType = foundTypeAndProjectId.Symbol;
                        Debug.Assert(foundType.Locations.Any(s_isInMetadata));

                        // Add to the result list.
                        result.Add(foundTypeAndProjectId);

                        if (transitive && shouldContinueSearching(foundType))
                        {
                            currentMetadataTypes.Add(foundTypeAndProjectId);
                            currentSourceAndMetadataTypes.Add(foundTypeAndProjectId);
                        }
                    }
                }
                finally
                {
                    s_setPool.ClearAndFree(foundMetadataTypes);
                }
            }

            // Now search the project and see what source types we can find.
            var foundSourceTypes = CreateSymbolAndProjectIdSet();
            try
            {
                await AddSourceTypesInProjectAsync(
                    currentSourceAndMetadataTypes, project,
                    sourceTypeImmediatelyMatches,
                    shouldContinueSearching,
                    transitive, foundSourceTypes,
                    cancellationToken).ConfigureAwait(false);

                foreach (var foundTypeAndProjectId in foundSourceTypes)
                {
                    var foundType = foundTypeAndProjectId.Symbol;
                    Debug.Assert(foundType.Locations.All(s_isInSource));

                    // Add to the result list.
                    result.Add(foundTypeAndProjectId);

                    if (transitive && shouldContinueSearching(foundType))
                    {
                        currentSourceAndMetadataTypes.Add(foundTypeAndProjectId);
                    }
                }
            }
            finally
            {
                s_setPool.ClearAndFree(foundSourceTypes);
            }
        }

        private static async Task<ISet<ProjectId>> GetProjectsThatCouldReferenceTypeAsync(
            INamedTypeSymbol type,
            Solution solution,
            bool searchInMetadata,
            CancellationToken cancellationToken)
        {
            var dependencyGraph = solution.GetProjectDependencyGraph();

            if (searchInMetadata)
            {
                // For a metadata type, find all projects that refer to the metadata assembly that
                // the type is defined in.  Note: we pass 'null' for projects intentionally.  We
                // Need to find all the possible projects that contain this metadata.
                var projectsThatReferenceMetadataAssembly =
                    await DependentProjectsFinder.GetDependentProjectsAsync(
                        type, solution, projects: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Now collect all the dependent projects as well.
                var projectsThatCouldReferenceType =
                    projectsThatReferenceMetadataAssembly.SelectMany(
                        p => GetProjectsThatCouldReferenceType(dependencyGraph, p)).ToSet();

                return projectsThatCouldReferenceType;
            }
            else
            {
                // For a source project, find the project that that type was defined in.
                var sourceProject = solution.GetProject(type.ContainingAssembly);
                if (sourceProject == null)
                {
                    return SpecializedCollections.EmptySet<ProjectId>();
                }

                // Now find all the dependent of those projects.
                var projectsThatCouldReferenceType = GetProjectsThatCouldReferenceType(
                    dependencyGraph, sourceProject).ToSet();

                return projectsThatCouldReferenceType;
            }
        }

        private static IEnumerable<ProjectId> GetProjectsThatCouldReferenceType(
            ProjectDependencyGraph dependencyGraph, Project project)
        {
            // Get all the projects that depend on 'project' as well as 'project' itself.
            return dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id)
                                               .Concat(project.Id);
        }

        private static List<Project> GetOrderedProjectsToExamine(
            Solution solution,
            IImmutableSet<Project> projects,
            IEnumerable<ProjectId> projectsThatCouldReferenceType)
        {
            var projectsToExamine = GetProjectsToExamineWorker(
                solution, projects, projectsThatCouldReferenceType);

            // Ensure the projects we're going to examine are ordered topologically.
            // That way we can just sweep over them from left to right as no project
            // could affect a previous project in the sweep.
            return OrderTopologically(solution, projectsToExamine);
        }

        private static List<Project> OrderTopologically(
            Solution solution, IEnumerable<Project> projectsToExamine)
        {
            var order = new Dictionary<ProjectId, int>(capacity: solution.ProjectIds.Count);

            var index = 0;

            var dependencyGraph = solution.GetProjectDependencyGraph();
            foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects())
            {
                order.Add(projectId, index);
                index++;
            }

            return projectsToExamine.OrderBy((p1, p2) => order[p1.Id] - order[p2.Id]).ToList();
        }

        private static IEnumerable<Project> GetProjectsToExamineWorker(
            Solution solution,
            IImmutableSet<Project> projects,
            IEnumerable<ProjectId> projectsThatCouldReferenceType)
        {
            var dependencyGraph = solution.GetProjectDependencyGraph();

            // Take the projects that were passed in, and find all the projects that 
            // they depend on (including themselves).  i.e. if we have a solution that
            // looks like:
            //      A <- B <- C <- D
            //          /
            //         └
            //        E
            // and we're passed in 'B, C, E' as the project to search, then this set 
            // will be A, B, C, E.
            var allProjectsThatTheseProjectsDependOn = projects
                .SelectMany(p => dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(p.Id))
                .Concat(projects.Select(p => p.Id)).ToSet();

            // We then intersect this set with the actual set of projects that could reference
            // the type.  Say this list is B, C, D.  The intersection of this list and the above
            // one will then be 'B' and 'C'.  
            //
            // In other words, there is no point searching A and E (because they can't even 
            // reference the type).  And there's no point searching 'D' because it can't contribute
            // any information that would affect the result in the projects we are asked to search
            // within.

            // Finally, because we're searching metadata and source symbols, this needs to be a project
            // that actually supports compilations.
            return projectsThatCouldReferenceType.Intersect(allProjectsThatTheseProjectsDependOn)
                                                 .Select(solution.GetProject)
                                                 .Where(p => p.SupportsCompilation)
                                                 .ToList();
        }

        private static async Task AddAllMatchingMetadataTypesInProjectAsync(
            SymbolAndProjectIdSet metadataTypes,
            Project project,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> metadataTypeMatches,
            SymbolAndProjectIdSet result,
            CancellationToken cancellationToken)
        {
            Debug.Assert(project.SupportsCompilation);

            if (metadataTypes.Count == 0)
            {
                return;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Seed the current set of types we're searching for with the types we were given.
            var currentTypes = metadataTypes;

            while (currentTypes.Count > 0)
            {
                var immediateDerivedTypes = CreateSymbolAndProjectIdSet();

                foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
                {
                    await FindImmediateMatchingMetadataTypesInMetadataReferenceAsync(
                        currentTypes, project, metadataTypeMatches,
                        compilation, reference, immediateDerivedTypes,
                        cancellationToken).ConfigureAwait(false);
                }

                // Add what we found to the result set.
                result.AddRange(immediateDerivedTypes);

                // Now keep looping, using the set we found to spawn the next set of searches.
                currentTypes = immediateDerivedTypes;
            }
        }

        private static async Task FindImmediateMatchingMetadataTypesInMetadataReferenceAsync(
            SymbolAndProjectIdSet metadataTypes,
            Project project,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> metadataTypeMatches,
            Compilation compilation,
            PortableExecutableReference reference,
            SymbolAndProjectIdSet result,
            CancellationToken cancellationToken)
        {
            // We store an index in SymbolTreeInfo of the *simple* metadata type name
            // to the names of the all the types that either immediately derive or 
            // implement that type.  Because the mapping is from the simple name
            // we might get false positives.  But that's fine as we still use 
            // 'metadataTypeMatches' to make sure the match is correct.
            var symbolTreeInfo = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                project.Solution, reference, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            // For each type we care about, see if we can find any derived types
            // in this index.
            foreach (var metadataType in metadataTypes)
            {
                var baseTypeName = metadataType.Symbol.Name;

                // For each derived type we find, see if we can map that back 
                // to an actual symbol.  Then check if that symbol actually fits
                // our criteria.
                foreach (var derivedType in symbolTreeInfo.GetDerivedMetadataTypes(baseTypeName, compilation, cancellationToken))
                {
                    if (derivedType != null && derivedType.Locations.Any(s_isInMetadata))
                    {
                        if (metadataTypeMatches(metadataTypes, derivedType))
                        {
                            result.Add(SymbolAndProjectId.Create(derivedType, project.Id));
                        }
                    }
                }
            }
        }

        private static bool TypeDerivesFrom(
            SymbolAndProjectIdSet metadataTypes, INamedTypeSymbol type, bool transitive)
        {
            if (transitive)
            {
                for (var current = type.BaseType; current != null; current = current.BaseType)
                {
                    if (metadataTypes.Contains(
                        SymbolAndProjectId.Create(current.OriginalDefinition, projectId: null)))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return metadataTypes.Contains(
                    SymbolAndProjectId.Create(type.BaseType?.OriginalDefinition, projectId: null));
            }
        }

        private static bool TypeImplementsFrom(
            SymbolAndProjectIdSet metadataTypes, INamedTypeSymbol type, bool transitive)
        {
            var interfaces = transitive ? type.AllInterfaces : type.Interfaces;

            foreach (var interfaceType in interfaces)
            {
                if (metadataTypes.Contains(SymbolAndProjectId.Create(interfaceType.OriginalDefinition, projectId: null)))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task AddSourceTypesInProjectAsync(
            SymbolAndProjectIdSet sourceAndMetadataTypes,
            Project project,
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> sourceTypeImmediatelyMatches,
            Func<INamedTypeSymbol, bool> shouldContinueSearching,
            bool transitive,
            SymbolAndProjectIdSet finalResult,
            CancellationToken cancellationToken)
        {
            // We're going to be sweeping over this project over and over until we reach a 
            // fixed point.  In order to limit GC and excess work, we cache all the semantic
            // models and DeclaredSymbolInfo for hte documents we look at.
            // Because we're only processing a project at a time, this is not an issue.
            var cachedModels = new ConcurrentSet<SemanticModel>();

            var typesToSearchFor = CreateSymbolAndProjectIdSet();
            typesToSearchFor.AddAll(sourceAndMetadataTypes);

            var projectIndex = await ProjectIndex.GetIndexAsync(project, cancellationToken).ConfigureAwait(false);

            var localBuffer = CreateSymbolAndProjectIdSet();

            // As long as there are new types to search for, keep looping.
            while (typesToSearchFor.Count > 0)
            {
                localBuffer.Clear();

                foreach (var type in typesToSearchFor)
                {
                    switch (type.Symbol.SpecialType)
                    {
                        case SpecialType.System_Object:
                            await AddMatchingTypesAsync(
                                cachedModels, projectIndex.ClassesThatMayDeriveFromSystemObject, localBuffer,
                                predicateOpt: n => n.BaseType?.SpecialType == SpecialType.System_Object,
                                cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                        case SpecialType.System_ValueType:
                            await AddMatchingTypesAsync(
                                cachedModels, projectIndex.ValueTypes, localBuffer,
                                predicateOpt: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                        case SpecialType.System_Enum:
                            await AddMatchingTypesAsync(
                                cachedModels, projectIndex.Enums, localBuffer,
                                predicateOpt: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                        case SpecialType.System_MulticastDelegate:
                            await AddMatchingTypesAsync(
                                cachedModels, projectIndex.Delegates, localBuffer,
                                predicateOpt: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                    }

                    await AddTypesThatDeriveFromNameAsync(
                        sourceTypeImmediatelyMatches, cachedModels, typesToSearchFor,
                        projectIndex, localBuffer, type.Symbol.Name, cancellationToken).ConfigureAwait(false);
                }

                // Clear out the information about the types we're looking for.  We'll
                // fill these in if we discover any more types that we need to keep searching
                // for.
                typesToSearchFor.Clear();

                foreach (var derivedType in localBuffer)
                {
                    if (finalResult.Add(derivedType))
                    {
                        if (transitive && shouldContinueSearching(derivedType.Symbol))
                        {
                            typesToSearchFor.Add(derivedType);
                        }
                    }
                }
            }

            s_setPool.ClearAndFree(localBuffer);
        }

        private static bool ImmediatelyDerivesOrImplementsFrom(
            SymbolAndProjectIdSet typesToSearchFor, INamedTypeSymbol type)
        {
            if (typesToSearchFor.Contains(SymbolAndProjectId.Create(type.BaseType?.OriginalDefinition, projectId: null)))
            {
                return true;
            }

            foreach (var interfaceType in type.Interfaces)
            {
                if (typesToSearchFor.Contains(SymbolAndProjectId.Create(interfaceType.OriginalDefinition, projectId: null)))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task AddTypesThatDeriveFromNameAsync(
            Func<SymbolAndProjectIdSet, INamedTypeSymbol, bool> typeImmediatelyMatches,
            ConcurrentSet<SemanticModel> cachedModels,
            SymbolAndProjectIdSet typesToSearchFor,
            ProjectIndex index,
            SymbolAndProjectIdSet result,
            string name,
            CancellationToken cancellationToken)
        {
            foreach (var (document, info) in index.NamedTypes[name])
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                cachedModels.Add(semanticModel);

                var resolvedType = info.TryResolve(semanticModel, cancellationToken);
                if (resolvedType is INamedTypeSymbol namedType &&
                    typeImmediatelyMatches(typesToSearchFor, namedType))
                {
                    result.Add(new SymbolAndProjectId<INamedTypeSymbol>(namedType, document.Project.Id));
                }
            }
        }

        private static async Task AddMatchingTypesAsync(
            ConcurrentSet<SemanticModel> cachedModels,
            MultiDictionary<Document, DeclaredSymbolInfo> documentToInfos,
            SymbolAndProjectIdSet result,
            Func<INamedTypeSymbol, bool> predicateOpt,
            CancellationToken cancellationToken)
        {
            foreach (var (document, infos) in documentToInfos)
            {
                Debug.Assert(infos.Count > 0);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                cachedModels.Add(semanticModel);

                foreach (var info in infos)
                {
                    var resolvedSymbol = info.TryResolve(semanticModel, cancellationToken);
                    if (resolvedSymbol is INamedTypeSymbol namedType)
                    {
                        if (predicateOpt == null ||
                            predicateOpt(namedType))
                        {
                            result.Add(new SymbolAndProjectId<INamedTypeSymbol>(namedType, document.Project.Id));
                        }
                    }
                }
            }
        }

        private static SymbolAndProjectIdSet CreateSymbolAndProjectIdSet()
        {
            return s_setPool.Allocate();
        }
    }
}
