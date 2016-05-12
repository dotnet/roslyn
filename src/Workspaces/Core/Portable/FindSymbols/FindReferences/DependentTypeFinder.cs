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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SourceInfoMatches = Func<Document, DeclaredSymbolInfo, CancellationToken, Task<ISymbol>>;

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
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationAllSourceAndAccessibleTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the accessible metadata types
        /// within the compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationAllAccessibleMetadataTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the source types.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationSourceTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// A predicate for determining if one class derives from another. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> s_findDerivedClassesPredicate =
            (t1, t2) => t1.InheritsFromIgnoringConstruction(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from a class (from the compilation or one of its dependencies)
        /// to the set of classes in the compilation that derive from it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> s_derivedClassesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// A predicate for determining if one interface derives from another. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> s_findDerivedInterfacesPredicate =
            (t1, t2) => t1.TypeKind == TypeKind.Interface && t1.OriginalDefinition.AllInterfaces.Contains(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from an interface (from the compilation or one of its dependencies)
        /// to the set of interfaces in the compilation that derive from it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> s_derivedInterfacesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// A predicate for determining if a class implements an interface. Static to avoid unnecessary allocations.
        /// </summary>
        private static readonly Func<INamedTypeSymbol, INamedTypeSymbol, bool> s_findImplementingInterfacesPredicate =
            (t1, t2) => t1.OriginalDefinition.ImplementsIgnoringConstruction(t2);

        /// <summary>
        /// For a given <see cref="Compilation"/>, maps from an interface (from the compilation or one of its dependencies)
        /// to the set of types in the compilation that implement it.
        /// </summary>
        /// <remarks>
        /// <see cref="SymbolKey"/>s are used instead of <see cref="ISymbol"/>s to avoid keeping other compilations alive
        /// unnecessarily.
        /// </remarks>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> s_implementingInterfacesCache =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>();

        /// <summary>
        /// Used by the cache to compare <see cref="SymbolKey"/>s used as keys in the cache. We make sure to check the casing of names and assembly IDs during the comparison,
        /// in order to be as discriminating as possible.
        /// </summary>
        private static readonly IEqualityComparer<SymbolKey> s_symbolIdComparer = SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false);

        /// <summary>
        /// Used to create a new concurrent <see cref="SymbolKey"/> map for a given compilation when needed.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>>.CreateValueCallback s_createSymbolDictionary =
            _ => new ConcurrentDictionary<SymbolKey, List<SymbolKey>>(s_symbolIdComparer);

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync"/>, which is a publically callable method.
        /// </summary>
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
                    s_findDerivedClassesPredicate,
                    s_derivedClassesCache,
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
            if (type?.TypeKind == TypeKind.Interface)
            {
                return GetDependentTypesAsync(
                    type,
                    solution,
                    projects,
                    s_findImplementingInterfacesPredicate,
                    s_implementingInterfacesCache,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            if (type?.TypeKind == TypeKind.Interface)
            {
                type = type.OriginalDefinition;
                return GetDependentTypesAsync(
                    type,
                    solution,
                    null,
                    (candidate, baseInterface) => candidate.Interfaces.Any(i => OriginalSymbolsMatch(i, baseInterface, solution, cancellationToken)),
                    s_derivedInterfacesCache,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static async Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type?.TypeKind == TypeKind.Class)
            {
                var dependentProjects = await DependentProjectsFinder.GetDependentProjectsAsync(
                    type, solution, projects: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                // If it's a type from source, then only other types from source could derive from
                // it.  If it's a type from metadata then unfortunately anything could derive from
                // it.
                var locationsInMetadata = type.Locations.Any(loc => loc.IsInMetadata);
                var results = new ConcurrentSet<ISymbol>(SymbolEquivalenceComparer.Instance);

                cancellationToken.ThrowIfCancellationRequested();

                var projectTasks = new List<Task>();
                foreach (var project in dependentProjects)
                {
                    projectTasks.Add(GetTypesImmediatelyDerivedFromClassesAsync(
                        type, project, locationsInMetadata, results, cancellationToken));
                }

                await Task.WhenAll(projectTasks).ConfigureAwait(false);

                if (results.Any())
                {
                    return results.OfType<INamedTypeSymbol>();
                }
            }

            return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
        }

        // Cache certain delegates so we don't need to create them over and over again.
        private static SourceInfoMatches s_infoMatchesDelegate = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Delegate);
        private static SourceInfoMatches s_infoMatchesEnum = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Enum);
        private static SourceInfoMatches s_infoMatchesStruct = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Struct);

        private static Func<INamedTypeSymbol, bool> s_isDelegateType = t => t.IsDelegateType();
        private static Func<INamedTypeSymbol, bool> s_isEnumType = t => t.IsEnumType();
        private static Func<INamedTypeSymbol, bool> s_isStructType = t => t.IsStructType();

        private static Func<INamedTypeSymbol, bool> s_derivesFromObject = t => t.BaseType?.SpecialType == SpecialType.System_Object;

        private static Func<Location, bool> s_isInMetadata = loc => loc.IsInMetadata;

        private static SourceInfoMatches GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind kind)
        {
            return (document, info, cancellationToken) =>
            {
                if (info.Kind != kind)
                {
                    return Task.FromResult<ISymbol>(null);
                }

                return info.ResolveAsync(document, cancellationToken);
            };
        }

        private static Task GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol baseType, Project project, bool locationsInMetadata,
            ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(baseType.TypeKind == TypeKind.Class);
            if (locationsInMetadata)
            {
                switch (baseType.SpecialType)
                {
                    // Object needs to be handled specially as many types may derive directly from it,
                    // while having no source indication that that's the case.
                    case SpecialType.System_Object:
                        return GetAllTypesThatDeriveDirectlyFromObjectAsync(project, results, cancellationToken);

                    // Delegates derive from System.MulticastDelegate
                    case SpecialType.System_MulticastDelegate:
                        return AddAllDelegatesAsync(project, results, cancellationToken);

                    // Structs derive from System.System.ValueType
                    case SpecialType.System_ValueType:
                        return AddAllStructsAsync(project, results, cancellationToken);

                    // Enums derive from System.Enum
                    case SpecialType.System_Enum:
                        return AddAllEnumsAsync(project, results, cancellationToken);

                    // A normal class from metadata.
                    default:
                        return AddMatchingSourceAndMetadataTypesAsync(project, results,
                            t => OriginalSymbolsMatch(t.BaseType, baseType, project.Solution, cancellationToken),
                            GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(baseType),
                            cancellationToken: cancellationToken);
                }
            }

            // Check for source symbols that could derive from this type. Look for 
            // DeclaredSymbolInfos in this project that state they derive from a type 
            // with our name.
            return AddMatchingSourceTypesAsync(project, results,
                GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(baseType),
                cancellationToken);
        }

        private static Task GetAllTypesThatDeriveDirectlyFromObjectAsync(
            Project project, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            // Or source predicate needs to find all classes in the project that derive from 
            // object. Unfortunately, we have to consider all classes. There's no way to 
            // tell syntactically (for C# at least) if a type inherits from object or not.  
            // i.e.  if you have:
            //
            //      class F : IEnumerable
            //
            // Then this derives from object.  We can't use the presence of an 
            // inheritance list to make any determinations.  Note: we could tell
            // for VB.  It may be a good optimization to add later. 

            SourceInfoMatches sourceInfoMatches = async (doc, info, c) =>
            {
                if (info.Kind == DeclaredSymbolInfoKind.Class)
                {
                    var symbol = await info.ResolveAsync(doc, c).ConfigureAwait(false) as INamedTypeSymbol;
                    if (symbol?.BaseType?.SpecialType == SpecialType.System_Object)
                    {
                        return symbol;
                    }
                }

                return null;
            };

            return AddMatchingSourceAndMetadataTypesAsync(project, results,
                s_derivesFromObject, sourceInfoMatches, cancellationToken);
        }

        private static Task AddAllDelegatesAsync(
            Project project, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(project, results, s_isDelegateType, s_infoMatchesDelegate, cancellationToken);
        }

        private static Task AddAllEnumsAsync(
            Project project, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(project, results, s_isEnumType, s_infoMatchesEnum, cancellationToken);
        }

        private static Task AddAllStructsAsync(
            Project project, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            return AddMatchingSourceAndMetadataTypesAsync(project, results, s_isStructType, s_infoMatchesStruct, cancellationToken);
        }

        private static Task AddMatchingSourceAndMetadataTypesAsync(
            Project project, ConcurrentSet<ISymbol> results,
            Func<INamedTypeSymbol, bool> metadataPredicate,
            SourceInfoMatches sourceInfoMatches,
            CancellationToken cancellationToken)
        {
            var metadataTask = AddMatchingMetadataTypesAsync(project, results, metadataPredicate, cancellationToken);
            var sourceTask = AddMatchingSourceTypesAsync(project, results, sourceInfoMatches, cancellationToken);

            // Search source and metadata in parallel.
            return Task.WhenAll(metadataTask, sourceTask);
        }

        private static async Task AddMatchingMetadataTypesAsync(Project project, ConcurrentSet<ISymbol> results, Func<INamedTypeSymbol, bool> metadataPredicate, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var metadataTypes = GetAllAccessibleMetadataTypesInCompilation(compilation, cancellationToken);

            // And add the matching ones to the result set.
            results.AddRange(metadataTypes.Where(metadataPredicate));
        }

        private static Task AddMatchingSourceTypesAsync(
            Project project, ConcurrentSet<ISymbol> results,
            SourceInfoMatches sourceInfoMatches, 
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Process all documents in the project in parallel.
            var tasks = project.Documents.Select(d =>
                AddMatchingSourceTypesAsync(d, results, sourceInfoMatches, cancellationToken)).ToArray();
            return Task.WhenAll(tasks);
        }

        private static async Task AddMatchingSourceTypesAsync(
            Document document, ConcurrentSet<ISymbol> results,
            SourceInfoMatches sourceInfoMatches,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolInfos = await document.GetDeclaredSymbolInfosAsync(cancellationToken).ConfigureAwait(false);
            foreach (var info in symbolInfos)
            {
                var matchingSymbol = await sourceInfoMatches(document, info, cancellationToken).ConfigureAwait(false);
                if (matchingSymbol != null)
                {
                    results.Add(matchingSymbol);
                }
            }
        }

        private static List<INamedTypeSymbol> GetAllAccessibleMetadataTypesInCompilation(
            Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note that we are checking the GlobalNamespace of the compilation (which includes all types).
            return s_compilationAllAccessibleMetadataTypesTable.GetValue(compilation, c =>
                compilation.GlobalNamespace.GetAllTypes(cancellationToken)
                                           .Where(t => t.Locations.All(s_isInMetadata) &&
                                                       t.DeclaredAccessibility != Accessibility.Private &&
                                                       t.IsAccessibleWithin(c.Assembly)).ToList());
        }

        private static SourceInfoMatches GetSourceInfoImmediatelyDerivesFromBaseTypeFunction(
            INamedTypeSymbol baseType)
        {
            return async (document, info, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (info.Kind == DeclaredSymbolInfoKind.Class)
                {
                    foreach (var inheritanceName in info.InheritanceNames)
                    {
                        // See if we have a type that looks like it could potentially derive from this class.
                        if (string.Equals(baseType.Name, inheritanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            var candidate = await info.ResolveAsync(document, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                            if (OriginalSymbolsMatch(candidate.BaseType, baseType, document.Project.Solution, cancellationToken))
                            {
                                return candidate;
                            }
                        }
                    }
                }

                return null;
            };
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
            INamedTypeSymbol type, Project project, Solution solution, Func<INamedTypeSymbol, INamedTypeSymbol, bool> predicate, ConditionalWeakTable<Compilation, ConcurrentDictionary<SymbolKey, List<SymbolKey>>> cache,
            bool locationsInMetadata, ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
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

                var resolvedSymbols = id.Resolve(compilation, cancellationToken: cancellationToken).GetAllSymbols();
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
            if (s_compilationAllSourceAndAccessibleTypesTable.TryGetValue(compilation, out types))
            {
                return types;
            }

            types = new List<INamedTypeSymbol>();

            // Note that we are checking the GlobalNamespace of the compilation (which includes all types).
            types.AddRange(compilation.GlobalNamespace.GetAllTypes(cancellationToken)
                                                      .Where(t => t.Locations.Any(loc => loc.IsInSource) ||
                                                             (t.DeclaredAccessibility != Accessibility.Private && t.IsAccessibleWithin(compilation.Assembly))));

            return s_compilationAllSourceAndAccessibleTypesTable.GetValue(compilation, _ => types);
        }

        private static List<INamedTypeSymbol> GetAllSourceTypesInCompilation(Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<INamedTypeSymbol> types;
            if (s_compilationSourceTypesTable.TryGetValue(compilation, out types))
            {
                return types;
            }

            types = new List<INamedTypeSymbol>();

            // Note that we are checking the GlobalNamespace of the Compilation's *Assembly* (which
            // only includes source types).
            types.AddRange(compilation.Assembly.GlobalNamespace.GetAllTypes(cancellationToken));

            return s_compilationSourceTypesTable.GetValue(compilation, _ => types);
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
                return cache.GetValue(compilation, s_createSymbolDictionary)
                            .GetOrAdd(typeId, dependentTypeIds);
            }
        }

        internal static bool OriginalSymbolsMatch(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            CancellationToken cancellationToken)
        {
            if (ReferenceEquals(searchSymbol, symbolToMatch))
            {
                return true;
            }

            if (searchSymbol == null || symbolToMatch == null)
            {
                return false;
            }

            Compilation symbolToMatchCompilation = null;
            if (!TryGetCompilation(symbolToMatch, solution, out symbolToMatchCompilation, cancellationToken))
            {
                return false;
            }

            return OriginalSymbolsMatch(searchSymbol, symbolToMatch, solution, null, symbolToMatchCompilation, cancellationToken);
        }

        internal static bool OriginalSymbolsMatch(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            if (symbolToMatch == null)
            {
                return false;
            }

            if (OriginalSymbolsMatchCore(searchSymbol, symbolToMatch, solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken))
            {
                return true;
            }

            if (searchSymbol.Kind == SymbolKind.Namespace && symbolToMatch.Kind == SymbolKind.Namespace)
            {
                // if one of them is a merged namespace symbol and other one is its constituent namespace symbol, they are equivalent.
                var namespace1 = (INamespaceSymbol)searchSymbol;
                var namespace2 = (INamespaceSymbol)symbolToMatch;
                var namespace1Count = namespace1.ConstituentNamespaces.Length;
                var namespace2Count = namespace2.ConstituentNamespaces.Length;
                if (namespace1Count != namespace2Count)
                {
                    if ((namespace1Count > 1 &&
                         namespace1.ConstituentNamespaces.Any(n => NamespaceSymbolsMatch(n, namespace2, solution, cancellationToken))) ||
                        (namespace2Count > 1 &&
                         namespace2.ConstituentNamespaces.Any(n2 => NamespaceSymbolsMatch(namespace1, n2, solution, cancellationToken))))
                    {
                        return true;
                    }
                }
            }

            if (searchSymbol.Kind == SymbolKind.NamedType && symbolToMatch.IsConstructor())
            {
                return OriginalSymbolsMatch(searchSymbol, symbolToMatch.ContainingType, solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken);
            }

            return false;
        }

        private static bool OriginalSymbolsMatchCore(
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            if (searchSymbol == null || symbolToMatch == null)
            {
                return false;
            }

            searchSymbol = searchSymbol.GetOriginalUnreducedDefinition();
            symbolToMatch = symbolToMatch.GetOriginalUnreducedDefinition();

            // We compare the given searchSymbol and symbolToMatch for equivalence using SymbolEquivalenceComparer
            // as follows:
            //  1)  We compare the given symbols using the SymbolEquivalenceComparer.IgnoreAssembliesInstance,
            //      which ignores the containing assemblies for named types equivalence checks. This is required
            //      to handle equivalent named types which are forwarded to completely different assemblies.
            //  2)  If the symbols are NOT equivalent ignoring assemblies, then they cannot be equivalent.
            //  3)  Otherwise, if the symbols ARE equivalent ignoring assemblies, they may or may not be equivalent
            //      if containing assemblies are NOT ignored. We need to perform additional checks to ensure they
            //      are indeed equivalent:
            //
            //      (a) If IgnoreAssembliesInstance.Equals equivalence visitor encountered any pair of non-nested 
            //          named types which were equivalent in all aspects, except that they resided in different 
            //          assemblies, we need to ensure that all such pairs are indeed equivalent types. Such a pair
            //          of named types is equivalent if and only if one of them is a type defined in either 
            //          searchSymbolCompilation(C1) or symbolToMatchCompilation(C2), say defined in reference assembly
            //          A (version v1) in compilation C1, and the other type is a forwarded type, such that it is 
            //          forwarded from reference assembly A (version v2) to assembly B in compilation C2.
            //      (b) Otherwise, if no such named type pairs were encountered, symbols ARE equivalent.

            using (var equivalentTypesWithDifferingAssemblies = SharedPools.Default<Dictionary<INamedTypeSymbol, INamedTypeSymbol>>().GetPooledObject())
            {
                // 1) Compare searchSymbol and symbolToMatch using SymbolEquivalenceComparer.IgnoreAssembliesInstance
                if (!SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(searchSymbol, symbolToMatch, equivalentTypesWithDifferingAssemblies.Object))
                {
                    // 2) If the symbols are NOT equivalent ignoring assemblies, then they cannot be equivalent.
                    return false;
                }

                // 3) If the symbols ARE equivalent ignoring assemblies, they may or may not be equivalent if containing assemblies are NOT ignored.
                if (equivalentTypesWithDifferingAssemblies.Object.Count > 0)
                {
                    // Step 3a) Ensure that all pairs of named types in equivalentTypesWithDifferingAssemblies are indeed equivalent types.
                    return VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies.Object, searchSymbol, symbolToMatch,
                        solution, searchSymbolCompilation, symbolToMatchCompilation, cancellationToken);
                }

                // 3b) If no such named type pairs were encountered, symbols ARE equivalent.
                return true;
            }
        }

        private static bool NamespaceSymbolsMatch(
            INamespaceSymbol namespace1,
            INamespaceSymbol namespace2,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return OriginalSymbolsMatch(namespace1, namespace2, solution, cancellationToken);
        }

        // Verifies that all pairs of named types in equivalentTypesWithDifferingAssemblies are equivalent forwarded types.
        private static bool VerifyForwardedTypes(
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            Solution solution,
            Compilation searchSymbolCompilation,
            Compilation symbolToMatchCompilation,
            CancellationToken cancellationToken)
        {
            var verifiedKeys = new HashSet<INamedTypeSymbol>();
            var count = equivalentTypesWithDifferingAssemblies.Count;
            int verifiedCount = 0;

            // First check forwarded types in searchSymbolCompilation.
            if (searchSymbolCompilation != null || TryGetCompilation(searchSymbol, solution, out searchSymbolCompilation, cancellationToken))
            {
                verifiedCount = VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies, searchSymbolCompilation, verifiedKeys, isSearchSymbolCompilation: true);
                if (verifiedCount == count)
                {
                    // All equivalent types verified.
                    return true;
                }
            }

            // Now check forwarded types in symbolToMatchCompilation.
            verifiedCount += VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies, symbolToMatchCompilation, verifiedKeys, isSearchSymbolCompilation: false);
            return verifiedCount == count;
        }

        private static int VerifyForwardedTypes(
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
            Compilation compilation,
            HashSet<INamedTypeSymbol> verifiedKeys,
            bool isSearchSymbolCompilation)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(equivalentTypesWithDifferingAssemblies);
            Contract.ThrowIfTrue(!equivalentTypesWithDifferingAssemblies.Any());

            // Must contain equivalents named types residing in different assemblies.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => !SymbolEquivalenceComparer.Instance.Equals(kvp.Key.ContainingAssembly, kvp.Value.ContainingAssembly)));

            // Must contain non-nested named types.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Key.ContainingType == null));
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Value.ContainingType == null));

            var referencedAssemblies = new MultiDictionary<string, IAssemblySymbol>();
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                referencedAssemblies.Add(assembly.Name, assembly);
            }

            int verifiedCount = 0;
            foreach (var kvp in equivalentTypesWithDifferingAssemblies)
            {
                if (!verifiedKeys.Contains(kvp.Key))
                {
                    INamedTypeSymbol originalType, expectedForwardedType;
                    if (isSearchSymbolCompilation)
                    {
                        originalType = kvp.Value.OriginalDefinition;
                        expectedForwardedType = kvp.Key.OriginalDefinition;
                    }
                    else
                    {
                        originalType = kvp.Key.OriginalDefinition;
                        expectedForwardedType = kvp.Value.OriginalDefinition;
                    }

                    foreach (var referencedAssembly in referencedAssemblies[originalType.ContainingAssembly.Name])
                    {
                        var fullyQualifiedTypeName = originalType.MetadataName;
                        if (originalType.ContainingNamespace != null)
                        {
                            fullyQualifiedTypeName = originalType.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.SignatureFormat) +
                                "." + fullyQualifiedTypeName;
                        }

                        // Resolve forwarded type and verify that the types from different assembly are indeed equivalent.
                        var forwardedType = referencedAssembly.ResolveForwardedType(fullyQualifiedTypeName);
                        if (forwardedType == expectedForwardedType)
                        {
                            verifiedKeys.Add(kvp.Key);
                            verifiedCount++;
                        }
                    }
                }
            }

            return verifiedCount;
        }

        private static bool TryGetCompilation(
            ISymbol symbol,
            Solution solution,
            out Compilation definingCompilation,
            CancellationToken cancellationToken)
        {
            var definitionProject = solution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (definitionProject == null)
            {
                definingCompilation = null;
                return false;
            }

            // compilation from definition project must already exist.
            if (!definitionProject.TryGetCompilation(out definingCompilation))
            {
                Contract.Requires(false, "How can compilation not exist?");
                return false;
            }

            return true;
        }
    }
}
