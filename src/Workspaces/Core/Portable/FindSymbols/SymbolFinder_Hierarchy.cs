// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find symbols for members that override the specified member symbol.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindOverridesAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Method can only have overrides if its a virtual, abstract or override and is not
            // sealed.
            if (symbol.IsOverridable())
            {
                // To find the overrides, we need to walk down the type hierarchy and check all
                // derived types.  TODO(cyrusn): This seems extremely costly.  Is there any way to
                // speed this up?
                var containingType = symbol.ContainingType.OriginalDefinition;
                var derivedTypes = await FindDerivedClassesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                List<ISymbol> results = null;
                foreach (var type in derivedTypes)
                {
                    foreach (var m in type.GetMembers(symbol.Name))
                    {
                        var member = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;

                        if (member != null &&
                            member.IsOverride &&
                            member.OverriddenMember() != null &&
                            OriginalSymbolsMatch(member.OverriddenMember().OriginalDefinition, symbol.OriginalDefinition, solution, cancellationToken))
                        {
                            results = results ?? new List<ISymbol>();
                            results.Add(member);
                        }
                    }
                }

                if (results != null)
                {
                    return results;
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Member can only implement interface members if it is an explicit member, or if it is
            // public and non static.
            if (symbol != null)
            {
                var explicitImplementations = symbol.ExplicitInterfaceImplementations();
                if (explicitImplementations.Length > 0)
                {
                    return explicitImplementations;
                }
                else if (
                    symbol.DeclaredAccessibility == Accessibility.Public && !symbol.IsStatic &&
                    (symbol.ContainingType.TypeKind == TypeKind.Class || symbol.ContainingType.TypeKind == TypeKind.Struct))
                {
                    // Interface implementation is a tricky thing.  A method may implement an interface
                    // method, even if its containing type doesn't state that it implements the
                    // interface.  For example:
                    //
                    //  interface IFoo { void Foo(); }
                    //
                    //  class Base { public void Foo(); }
                    //
                    //  class Derived : Base, IFoo { }
                    //
                    // In this case, Base.Foo *does* implement IFoo.Foo in the context of the type
                    // Derived.
                    var containingType = symbol.ContainingType.OriginalDefinition;
                    var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);
                    var allTypes = derivedClasses.Concat(containingType);

                    List<ISymbol> results = null;

                    foreach (var type in allTypes)
                    {
                        foreach (var interfaceType in type.AllInterfaces)
                        {
                            if (interfaceType.MemberNames.Contains(symbol.Name))
                            {
                                foreach (var m in interfaceType.GetMembers(symbol.Name))
                                {
                                    var interfaceMethod = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;

                                    foreach (var implementation in type.FindImplementationsForInterfaceMember(interfaceMethod, solution.Workspace, cancellationToken))
                                    {
                                        if (implementation != null && SymbolEquivalenceComparer.Instance.Equals(implementation.OriginalDefinition, symbol.OriginalDefinition))
                                        {
                                            results = results ?? new List<ISymbol>();
                                            results.Add(interfaceMethod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (results != null)
                    {
                        return results.Distinct(SymbolEquivalenceComparer.Instance);
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived", but can be found
        /// with <see cref="FindImplementationsAsync"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return DependentTypeFinder.FindTransitivelyDerivedClassesAsync(type, solution, projects, cancellationToken);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // A symbol can only have implementations if it's an interface or a
            // method/property/event from an interface.
            if (symbol is INamedTypeSymbol)
            {
                var namedTypeSymbol = (INamedTypeSymbol)symbol;
                var implementingTypes = await DependentTypeFinder.FindTransitivelyImplementingTypesAsync(namedTypeSymbol, solution, projects, cancellationToken).ConfigureAwait(false);
                return implementingTypes.Where(IsAccessible);
            }
            else if (symbol.IsImplementableMember())
            {
                var containingType = symbol.ContainingType.OriginalDefinition;
                var allTypes = await DependentTypeFinder.FindTransitivelyImplementingTypesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                List<ISymbol> results = null;
                foreach (var t in allTypes)
                {
                    foreach (var m in t.FindImplementationsForInterfaceMember(symbol, solution.Workspace, cancellationToken))
                    {
                        var s = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;
                        if (IsAccessible(s))
                        {
                            results = results ?? new List<ISymbol>();
                            results.Add(s.OriginalDefinition);
                        }
                    }
                }

                if (results != null)
                {
                    return results.Distinct(SymbolEquivalenceComparer.Instance);
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        private static bool IsAccessible(ISymbol s)
        {
            if (s.Locations.Any(l => l.IsInMetadata))
            {
                var accessibility = s.DeclaredAccessibility;
                return accessibility == Accessibility.Public ||
                    accessibility == Accessibility.Protected ||
                    accessibility == Accessibility.ProtectedOrInternal;
            }

            return true;
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindCallersAsync(symbol, solution, documents: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default(CancellationToken))
        {
            symbol = symbol.OriginalDefinition;
            var foundSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            symbol = foundSymbol ?? symbol;

            var callReferences = await FindCallReferencesAsync(solution, symbol, documents, cancellationToken).ConfigureAwait(false);

            var directReferences = callReferences.Where(
                r => SymbolEquivalenceComparer.Instance.Equals(symbol, r.Definition)).FirstOrDefault();

            var indirectReferences = callReferences.Where(r => r != directReferences).ToList();

            List<SymbolCallerInfo> results = null;

            if (directReferences != null)
            {
                foreach (var kvp in await directReferences.Locations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results = results ?? new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: true));
                }
            }

            {
                var indirectLocations = indirectReferences.SelectMany(r => r.Locations);
                foreach (var kvp in await indirectLocations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results = results ?? new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: false));
                }
            }

            return results ?? SpecializedCollections.EmptyEnumerable<SymbolCallerInfo>();
        }

        private static Task<IEnumerable<ReferencedSymbol>> FindCallReferencesAsync(
            Solution solution,
            ISymbol symbol,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Event ||
                    symbol.Kind == SymbolKind.Method ||
                    symbol.Kind == SymbolKind.Property)
                {
                    return SymbolFinder.FindReferencesAsync(symbol, solution, documents, cancellationToken);
                }
            }

            return SpecializedTasks.EmptyEnumerable<ReferencedSymbol>();
        }

        private static bool OriginalSymbolsMatch(
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
