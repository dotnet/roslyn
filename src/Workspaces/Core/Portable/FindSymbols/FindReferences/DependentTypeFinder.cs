// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Provides helper methods for finding dependent types (derivations, implementations, etc.) across a solution.
    /// </summary>
    /// <remarks>
    /// This type makes heavy use of <see cref="ConditionalWeakTable{TKey, TValue}"/> for caching purposes. When
    /// modifying these caches, care must be taken to avoid introducing memory leaks. Instances of <see cref="Compilation"/>
    /// are used as the keys in these caches, so in general only symbols or other data from that same compilation should be stored
    /// in the associated value.
    /// </remarks>
    internal static class DependentTypeFinder
    {
        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the source types and all the accessible metadata types
        /// within the compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> compilationAllSourceAndAccessibleTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the source types.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> compilationSourceTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// A predicate for determining if one class derives from another. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> findDerivedClassesPredicate =
            (t1, t2) => t1.InheritsFromIgnoringConstruction(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from a class (from the compilation or one of its dependencies)
        /// to the set of classes in the compilation that derive from it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> derivedClassesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// A predicate for determining if one interface derives from another. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> findDerivedInterfacesPredicate =
            (t1, t2) => t1.TypeKind == TypeKind.Interface && t1.OriginalDefinition.AllInterfaces.Distinct(SymbolEquivalenceComparer.Instance).Contains(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from an interface (from the compilation or one of its dependencies)
        /// to the set of interfaces in the compilation that derive from it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> derivedInterfacesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// A predicate for determining if a class implements an interface. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> findImplementingInterfacesPredicate =
            (t1, t2) => t1.OriginalDefinition.ImplementsIgnoringConstruction(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from an interface (from the compilation or one of its dependencies)
        /// to the set of types in the compilation that implement it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> implementingInterfacesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// Used by the cache to compare <see cref="SymbolKey"/>s used as keys in the cache. We make sure to check the casing of names and assembly IDs during the comparison,
        /// in order to be as discriminating as possible.
        /// </summary>
        private static readonly IEqualityComparer<SymbolKey> symbolIdComparer = SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false);

        /// <summary>
        /// Used to create a new concurrent <see cref="SymbolKey"/> map for a given compilation when needed.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>.CreateValueCallback createSymbolDictionary =
            _ => new ConcurrentDictionary<SymbolKey, List<SymbolKey>>(symbolIdComparer);

        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // Only a class can have derived types.
            if (type != null &&
                type.TypeKind == TypeKind.Class &&
                !type.IsSealed)
            {
                return GetDependentTypesAsync(
                    type,
                    solution,
                    projects,
                    findDerivedClassesPredicate,
                    derivedClassesCache,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            if (type != null && type.TypeKind == TypeKind.Interface)
            {
                type = type.OriginalDefinition;
                return GetDependentTypesAsync(
                    type,
                    solution,
                    projects,
                    findDerivedInterfacesPredicate,
                    derivedInterfacesCache,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type != null && type.TypeKind == TypeKind.Interface)
            {
                return GetDependentTypesAsync(
                    type,
                    solution,
                    projects,
                    findImplementingInterfacesPredicate,
                    implementingInterfacesCache,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> GetDependentTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            Func<INamedTypeSymbol, INamedTypeSymbol, bool> predicate,
            ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> cache,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependentProjects = await DependentProjectsFinder.GetDependentProjectsAsync(type, solution, projects, cancellationToken).ConfigureAwait(false);

            // If it's a type from source, then only other types from source could derive from
            // it.  If it's a type from metadata then unfortunately anything could derive from
            // it.
            bool locationsInMetadata = type.Locations.Any(loc => loc.IsInMetadata);

            ConcurrentSet<ISymbol> results = new ConcurrentSet<ISymbol>(SymbolEquivalenceComparer.Instance);

            cancellationToken.ThrowIfCancellationRequested();

            var projectTasks = new List<Task>();
            foreach (var project in dependentProjects)
            {
                projectTasks.Add(Task.Run(
                    async () => await GetDependentTypesInProjectAsync(type, project, solution, predicate, cache, locationsInMetadata, results, cancellationToken).ConfigureAwait(false), cancellationToken));
            }

            await Task.WhenAll(projectTasks).ConfigureAwait(false);

            if (results.Any())
            {
                return results.OfType<INamedTypeSymbol>();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
            }
        }

        private static async Task GetDependentTypesInProjectAsync(
            INamedTypeSymbol type, Project project, Solution solution, Func<INamedTypeSymbol, INamedTypeSymbol, bool> predicate, ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> cache, bool locationsInMetadata, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var typeId = type.GetSymbolKey();

            List<SymbolKey> dependentTypeIds;
            if (!TryGetDependentTypes(cache, compilation, typeId, out dependentTypeIds))
            {
                List<INamedTypeSymbol> allTypes;
                if (locationsInMetadata)
                {
                    // From metadata, have to check other (non private) metadata types, as well as
                    // source types.
                    allTypes = GetAllSourceAndAccessibleTypesInCompilation(compilation, cancellationToken);
                }
                else
                {
                    // It's from source, so only other source types could derive from it.
                    allTypes = GetAllSourceTypesInCompilation(compilation, cancellationToken);
                }

                dependentTypeIds = new List<SymbolKey>();
                foreach (var t in allTypes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (predicate(t, type))
                    {
                        dependentTypeIds.Add(t.GetSymbolKey());
                    }
                }

                dependentTypeIds = GetOrAddDependentTypes(cache, compilation, typeId, dependentTypeIds);
            }

            foreach (var id in dependentTypeIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resolvedSymbols = id.Resolve(compilation).GetAllSymbols();
                foreach (var resolvedSymbol in resolvedSymbols)
                {
                    var mappedSymbol = await SymbolFinder.FindSourceDefinitionAsync(resolvedSymbol, solution, cancellationToken).ConfigureAwait(false) ?? resolvedSymbol;
                    results.Add(mappedSymbol);
                }
            }
        }

        private static List<INamedTypeSymbol> GetAllSourceAndAccessibleTypesInCompilation(Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<INamedTypeSymbol> types;
            if (compilationAllSourceAndAccessibleTypesTable.TryGetValue(compilation, out types))
            {
                return types;
            }

            types = new List<INamedTypeSymbol>();

            // Note that we are checking the GlobalNamespace of the compilation (which includes all types).
            types.AddRange(compilation.GlobalNamespace.GetAllTypes(cancellationToken)
                                                      .Where(t => t.Locations.Any(loc => loc.IsInSource) ||
                                                             (t.DeclaredAccessibility != Accessibility.Private && t.IsAccessibleWithin(compilation.Assembly))));

            return compilationAllSourceAndAccessibleTypesTable.GetValue(compilation, _ => types);
        }

        private static List<INamedTypeSymbol> GetAllSourceTypesInCompilation(Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<INamedTypeSymbol> types;
            if (compilationSourceTypesTable.TryGetValue(compilation, out types))
            {
                return types;
            }

            types = new List<INamedTypeSymbol>();

            // Note that we are checking the GlobalNamespace of the Compilation's *Assembly* (which
            // only includes source types).
            types.AddRange(compilation.Assembly.GlobalNamespace.GetAllTypes(cancellationToken));

            return compilationSourceTypesTable.GetValue(compilation, _ => types);
        }

        private static bool TryGetDependentTypes(ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> cache, Compilation compilation, SymbolKey typeId, out List<SymbolKey> dependentTypeIds)
        {
            dependentTypeIds = null;

            ConcurrentDictionary<SymbolKey, List<SymbolKey>> dictionary;

            return cache.TryGetValue(compilation, out dictionary) &&
                   dictionary.TryGetValue(typeId, out dependentTypeIds);
        }

        private static List<SymbolKey> GetOrAddDependentTypes(ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> cache, Compilation compilation, SymbolKey typeId, List<SymbolKey> dependentTypeIds)
        {
            List<SymbolKey> result;
            if (TryGetDependentTypes(cache, compilation, typeId, out result))
            {
                return result;
            }
            else
            {
                return cache.GetValue(compilation, createSymbolDictionary)
                            .GetOrAdd(typeId, dependentTypeIds);
            }
        }
    }
}