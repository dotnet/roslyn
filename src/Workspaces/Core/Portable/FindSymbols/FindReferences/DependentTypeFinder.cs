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
    using TypeMatches = Func<INamedTypeSymbol, Project, CancellationToken, bool>;
    using SourceInfoMatches = Func<Document, DeclaredSymbolInfo, CancellationToken, Task<ISymbol>>;
    internal delegate Task SearchProjectAsync(
        INamedTypeSymbol type, Project project, bool isInMetadata,
        ConcurrentSet<ISymbol> results, CancellationToken cancellationToken);

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
        // Cache certain delegates so we don't need to create them over and over again.
        private static SourceInfoMatches s_infoMatchesDelegate = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Delegate);
        private static SourceInfoMatches s_infoMatchesEnum = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Enum);
        private static SourceInfoMatches s_infoMatchesStruct = GetSourceInfoMatchesFunction(DeclaredSymbolInfoKind.Struct);

        private static TypeMatches s_isDelegateType = (t, p, c) => t.IsDelegateType();
        private static TypeMatches s_isEnumType = (t, p, c) => t.IsEnumType();
        private static TypeMatches s_isStructType = (t, p, c) => t.IsStructType();

        private static TypeMatches s_derivesFromObject = (t, p, c) => t.BaseType?.SpecialType == SpecialType.System_Object;

        private static Func<Location, bool> s_isInMetadata = loc => loc.IsInMetadata;

        /// <summary>
        /// For a given <see cref="Compilation"/>, stores a flat list of all the accessible metadata types
        /// within the compilation.
        /// </summary>
        private static readonly ConditionalWeakTable<Compilation, List<INamedTypeSymbol>> s_compilationAllAccessibleMetadataTypesTable =
            new ConditionalWeakTable<Compilation, List<INamedTypeSymbol>>();

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync"/>, which is a publically callable method.
        /// </summary>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // Only a class can have derived types.
            if (type?.TypeKind == TypeKind.Class &&
                !type.IsSealed)
            {
                // Search outwards by looking for immediately derived classes of the type passed in,
                // then immediately derived classes of those types, and so on and so on.
                //
                // We do this by keeping a queue of types to search for (starting with the initial
                // type we were given).  As long as we keep discovering new types, we keep searching
                // outwards.  Once no new types are discovered, we're done.
                var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

                var workQueue = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
                workQueue.Add(type);

                while (workQueue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Search for all the workqueue types in parallel.
                    var tasks = workQueue.Select(t => GetTypesImmediatelyDerivedFromClassesAsync(
                        t, solution, projects, cancellationToken)).ToArray();
                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    workQueue.Clear();
                    foreach (var task in tasks)
                    {
                        foreach (var derivedType in task.Result)
                        {
                            if (finalResult.Add(derivedType))
                            {
                                // We saw a derived type for the first time.  Enqueue it so that
                                // we can find any derived types of it.
                                workQueue.Add(derivedType);
                            }
                        }
                    }
                }

                return finalResult;
            }

            return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static async Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type?.TypeKind == TypeKind.Interface)
            {
                // Search outwards by looking for immediately implementing classes of the interface 
                // passed in, as well as immediately derived interfaces of the interface passed
                // in.
                //
                // For every implementing class we find, find all derived classes of that class as well.
                // After all, if the base class implements the interface, then all derived classes
                // do as well.
                //
                // For every extending interface we find, we also recurse and do this search as well.
                // After all, if a type implements a derived inteface then it certainly implements
                // the base interface as well.
                //
                // We do this by keeping a queue of interface types to search for (starting with the 
                // initial interface we were given).  As long as we keep discovering new types, we
                // keep searching outwards.  Once no new types are discovered, we're done.
                var finalResult = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

                var interfaceQueue = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);
                interfaceQueue.Add(type);

                var classQueue = new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

                while (interfaceQueue.Count > 0 || classQueue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Search for all the workqueue types in parallel.
                    if (interfaceQueue.Count > 0)
                    {
                        var interfaceTasks = interfaceQueue.Select(t => GetTypesImmediatelyDerivedFromInterfacesAsync(
                            t, solution, projects, cancellationToken)).ToArray();
                        await Task.WhenAll(interfaceTasks).ConfigureAwait(false);

                        interfaceQueue.Clear();
                        foreach (var task in interfaceTasks)
                        {
                            foreach (var derivedType in task.Result)
                            {
                                if (finalResult.Add(derivedType))
                                {
                                    // Seeing this type for the first time.  If it is a class or
                                    // interface, then add it to the list of symbols to keep 
                                    // searching for.  If it's an interface, keep looking for 
                                    // derived types of that interface.  If it's a class, just add
                                    // in the entire derived class chain of that class.
                                    if (derivedType.TypeKind == TypeKind.Interface)
                                    {
                                        interfaceQueue.Add(derivedType);
                                    }
                                    else if (derivedType.TypeKind == TypeKind.Class)
                                    {
                                        classQueue.Add(derivedType);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(classQueue.Count > 0);

                        var firstClass = classQueue.First();
                        classQueue.Remove(firstClass);

                        var derivedClasses = await FindDerivedClassesAsync(firstClass, solution, projects, cancellationToken).ConfigureAwait(false);

                        finalResult.AddRange(derivedClasses);

                        // It's possible that one of the derived classes we discovered was also in 
                        // classQueue. Remove them so we don't do excess work finding types we've
                        // already found.
                        classQueue.RemoveAll(derivedClasses);
                    }
                }

                return finalResult;
            }

            return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return GetTypesImmediatelyDerivedFromInterfacesAsync(
                type, solution, projects: null,
                cancellationToken: cancellationToken);
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type?.TypeKind == TypeKind.Interface)
            {
                type = type.OriginalDefinition;
                return GetTypesAsync(
                    type, solution, projects,
                    GetTypesImmediatelyDerivedFromInterfacesAsync,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static Task GetTypesImmediatelyDerivedFromInterfacesAsync(
            INamedTypeSymbol baseInterface, Project project, bool locationsInMetadata,
            ConcurrentSet<ISymbol> results, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(baseInterface.TypeKind == TypeKind.Interface);
            if (locationsInMetadata)
            {
                // For an interface from metadata, we have to find any metadata types that might
                // derive from this interface.
                return AddMatchingSourceAndMetadataTypesAsync(project, results,
                    GetTypeImmediatelyDerivesFromInterfaceFunction(baseInterface),
                    GetSourceInfoImmediatelyDerivesFromInterfaceFunction(baseInterface),
                    cancellationToken: cancellationToken);
            }

            // Check for source symbols that could derive from this type. Look for 
            // DeclaredSymbolInfos in this project that state they derive from a type 
            // with our name.
            return AddMatchingSourceTypesAsync(project, results,
                GetSourceInfoImmediatelyDerivesFromInterfaceFunction(baseInterface),
                cancellationToken);
        }

        private static TypeMatches GetTypeImmediatelyDerivesFromInterfaceFunction(INamedTypeSymbol baseInterface)
        {
            return (t, p, c) => t.Interfaces.Any(i => OriginalSymbolsMatch(i, baseInterface, p.Solution, c));
        }

        private static SourceInfoMatches GetSourceInfoImmediatelyDerivesFromInterfaceFunction(
            INamedTypeSymbol baseInterface)
        {
            var typeTestFunction = GetTypeImmediatelyDerivesFromInterfaceFunction(baseInterface);

            return async (document, info, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Interfaces, Classes, and Structs can derive from an interface.
                if (info.Kind == DeclaredSymbolInfoKind.Class ||
                    info.Kind == DeclaredSymbolInfoKind.Struct ||
                    info.Kind == DeclaredSymbolInfoKind.Interface)
                {
                    // If one types derives from an interface, then we would expect to see the
                    // interface name in the inheritance list.  Note: the inheritance name also
                    // include mapped aliases if aliases are used in the file it is contained in.
                    foreach (var inheritanceName in info.InheritanceNames)
                    {
                        if (string.Equals(baseInterface.Name, inheritanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Looks like a good candidate.  Get the actual symbol for it and see
                            // if the symbol matches the criteria.
                            var candidate = await info.ResolveAsync(document, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                            if (typeTestFunction(candidate, document.Project, cancellationToken))
                            {
                                return candidate;
                            }
                        }
                    }
                }

                return null;
            };
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return GetTypesImmediatelyDerivedFromClassesAsync(
                type, solution, projects: null, 
                cancellationToken: cancellationToken);
        }

        public static Task<IEnumerable<INamedTypeSymbol>> GetTypesImmediatelyDerivedFromClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type?.TypeKind == TypeKind.Class &&
                !type.IsSealed)
            {
                return GetTypesAsync(
                    type, solution, projects,
                    GetTypesImmediatelyDerivedFromClassesAsync,
                    cancellationToken);
            }

            return SpecializedTasks.EmptyEnumerable<INamedTypeSymbol>();
        }

        private static async Task<IEnumerable<INamedTypeSymbol>> GetTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            SearchProjectAsync searchProjectAsync,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependentProjects = await DependentProjectsFinder.GetDependentProjectsAsync(
                type, solution, projects, cancellationToken).ConfigureAwait(false);

            // If it's a type from source, then only other types from source could derive from
            // it.  If it's a type from metadata then unfortunately anything could derive from
            // it.
            var locationsInMetadata = type.Locations.Any(loc => loc.IsInMetadata);
            var results = new ConcurrentSet<ISymbol>(SymbolEquivalenceComparer.Instance);

            cancellationToken.ThrowIfCancellationRequested();

            var projectTasks = new List<Task>();
            foreach (var project in dependentProjects)
            {
                projectTasks.Add(searchProjectAsync(
                    type, project, locationsInMetadata, results, cancellationToken));
            }

            // Search all projects in parallel.
            await Task.WhenAll(projectTasks).ConfigureAwait(false);

            if (results.Any())
            {
                return results.OfType<INamedTypeSymbol>().ToList();
            }

            return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
        }

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
                        // Have to search metadata to see if we have any derived types
                        return AddMatchingSourceAndMetadataTypesAsync(project, results,
                            GetTypeImmediatelyDerivesFromBaseTypeFunction(baseType),
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

        private static Func<INamedTypeSymbol, Project, CancellationToken, bool> GetTypeImmediatelyDerivesFromBaseTypeFunction(
            INamedTypeSymbol baseType)
        {
            return (t, p, c) => OriginalSymbolsMatch(t.BaseType, baseType, p.Solution, c);
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
            TypeMatches metadataTypeMatches,
            SourceInfoMatches sourceInfoMatches,
            CancellationToken cancellationToken)
        {
            var metadataTask = AddMatchingMetadataTypesAsync(project, results, metadataTypeMatches, cancellationToken);
            var sourceTask = AddMatchingSourceTypesAsync(project, results, sourceInfoMatches, cancellationToken);

            // Search source and metadata in parallel.
            return Task.WhenAll(metadataTask, sourceTask);
        }

        private static async Task AddMatchingMetadataTypesAsync(
            Project project, ConcurrentSet<ISymbol> results, 
            TypeMatches metadataTypeMatches, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var metadataTypes = GetAllAccessibleMetadataTypesInCompilation(compilation, cancellationToken);

            // And add the matching ones to the result set.
            results.AddRange(metadataTypes.Where(t => metadataTypeMatches(t, project, cancellationToken)));
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
            var typeTestFunction = GetTypeImmediatelyDerivesFromBaseTypeFunction(baseType);

            return async (document, info, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only a class can derive from another class.
                if (info.Kind == DeclaredSymbolInfoKind.Class)
                {
                    // If one class derives from another class, then we would expect to see the
                    // base class name in the inheritance list.  Note: the inheritance name also
                    // include mapped aliases if aliases are used in the file it is contained in.
                    foreach (var inheritanceName in info.InheritanceNames)
                    {
                        // See if we have a type that looks like it could potentially derive from this class.
                        if (string.Equals(baseType.Name, inheritanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Looks like a good candidate.  Get the symbol for it and see if it
                            // actually matches.
                            var candidate = await info.ResolveAsync(document, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                            if (typeTestFunction(candidate, document.Project, cancellationToken))
                            {
                                return candidate;
                            }
                        }
                    }
                }

                return null;
            };
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
