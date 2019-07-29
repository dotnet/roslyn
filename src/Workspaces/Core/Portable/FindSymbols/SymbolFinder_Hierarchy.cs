// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find symbols for members that override the specified member symbol.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindOverridesAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindOverridesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(s => s.Symbol);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindOverridesAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            var symbol = symbolAndProjectId.Symbol?.OriginalDefinition;
            if (symbol.IsOverridable())
            {
                // To find the overrides, we need to walk down the type hierarchy and check all
                // derived types.
                var containingType = symbol.ContainingType;
                var derivedTypes = await FindDerivedClassesAsync(
                    symbolAndProjectId.WithSymbol(containingType),
                    solution, projects, cancellationToken).ConfigureAwait(false);

                foreach (var type in derivedTypes)
                {
                    foreach (var m in type.Symbol.GetMembers(symbol.Name))
                    {
                        var sourceMember = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false);
                        var bestMember = sourceMember ?? m;

                        if (IsOverride(solution, bestMember, symbol, cancellationToken))
                        {
                            results.Add(new SymbolAndProjectId(bestMember, type.ProjectId));
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        internal static bool IsOverride(
            Solution solution, ISymbol member, ISymbol symbol, CancellationToken cancellationToken)
        {
            for (var current = member; current != null; current = current.OverriddenMember())
            {
                if (OriginalSymbolsMatch(current.OverriddenMember(), symbol.OriginalDefinition, solution, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementedInterfaceMembersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindImplementedInterfaceMembersAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            // Member can only implement interface members if it is an explicit member, or if it is
            // public and non static.
            var symbol = symbolAndProjectId.Symbol;
            if (symbol != null)
            {
                var explicitImplementations = symbol.ExplicitInterfaceImplementations();
                if (explicitImplementations.Length > 0)
                {
                    return explicitImplementations.SelectAsArray(symbolAndProjectId.WithSymbol);
                }
                else if (
                    symbol.DeclaredAccessibility == Accessibility.Public && !symbol.IsStatic &&
                    (symbol.ContainingType.TypeKind == TypeKind.Class || symbol.ContainingType.TypeKind == TypeKind.Struct))
                {
                    // Interface implementation is a tricky thing.  A method may implement an interface
                    // method, even if its containing type doesn't state that it implements the
                    // interface.  For example:
                    //
                    //  interface IGoo { void Goo(); }
                    //
                    //  class Base { public void Goo(); }
                    //
                    //  class Derived : Base, IGoo { }
                    //
                    // In this case, Base.Goo *does* implement IGoo.Goo in the context of the type
                    // Derived.
                    var containingType = symbolAndProjectId.WithSymbol(
                        symbol.ContainingType.OriginalDefinition);
                    var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                        containingType, solution, projects, cancellationToken).ConfigureAwait(false);
                    var allTypes = derivedClasses.Concat(containingType);

                    var builder = ArrayBuilder<SymbolAndProjectId>.GetInstance();

                    foreach (var type in allTypes.Convert<INamedTypeSymbol, ITypeSymbol>())
                    {
                        foreach (var interfaceType in GetAllInterfaces(type))
                        {
                            // We don't want to look inside this type if we can avoid it. So first
                            // make sure that the interface even contains a symbol with the same
                            // name as the symbol we're looking for.
                            var nameToLookFor = symbol.IsPropertyAccessor()
                                ? ((IMethodSymbol)symbol).AssociatedSymbol.Name
                                : symbol.Name;
                            if (interfaceType.Symbol.MemberNames.Contains(nameToLookFor))
                            {
                                foreach (var m in GetMembers(interfaceType, symbol.Name))
                                {
                                    var sourceMethod = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false);
                                    var bestMethod = sourceMethod.Symbol != null ? sourceMethod : m;

                                    var implementations = await type.FindImplementationsForInterfaceMemberAsync(
                                        bestMethod, solution, cancellationToken).ConfigureAwait(false);
                                    foreach (var implementation in implementations)
                                    {
                                        if (implementation.Symbol != null &&
                                            SymbolEquivalenceComparer.Instance.Equals(implementation.Symbol.OriginalDefinition, symbol.OriginalDefinition))
                                        {
                                            builder.Add(bestMethod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var result = builder.Distinct(SymbolAndProjectIdComparer.SymbolEquivalenceInstance)
                                        .ToImmutableArray();
                    builder.Free();
                    return result;
                }
            }

            return ImmutableArray<SymbolAndProjectId>.Empty;
        }

        private static IEnumerable<SymbolAndProjectId> GetMembers(
            SymbolAndProjectId<INamedTypeSymbol> interfaceType, string name)
        {
            return interfaceType.Symbol.GetMembers(name).Select(interfaceType.WithSymbol);
        }

        private static IEnumerable<SymbolAndProjectId<INamedTypeSymbol>> GetAllInterfaces(
            SymbolAndProjectId<ITypeSymbol> type)
        {
            return type.Symbol.AllInterfaces.Select(type.WithSymbol);
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived", but can be found
        /// with <see cref="FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindDerivedClassesAsync(
                SymbolAndProjectId.Create(type, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        internal static Task<ImmutableArray<SymbolAndProjectId<INamedTypeSymbol>>> FindDerivedClassesAsync(
            SymbolAndProjectId<INamedTypeSymbol> typeAndProjectId, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var type = typeAndProjectId.Symbol;
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
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementationsAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindImplementationsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            // A symbol can only have implementations if it's an interface or a
            // method/property/event from an interface.
            var symbol = symbolAndProjectId.Symbol;
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var implementingTypes = await DependentTypeFinder.FindTransitivelyImplementingStructuresAndClassesAsync(namedTypeSymbol, solution, projects, cancellationToken).ConfigureAwait(false);
                return implementingTypes.Select(s => (SymbolAndProjectId)s)
                                        .Where(IsAccessible)
                                        .ToImmutableArray();
            }
            else if (symbol.IsImplementableMember())
            {
                var containingType = symbol.ContainingType.OriginalDefinition;
                var allTypes = await DependentTypeFinder.FindTransitivelyImplementingStructuresClassesAndInterfacesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                ImmutableArray<SymbolAndProjectId>.Builder results = null;
                foreach (var t in allTypes.Convert<INamedTypeSymbol, ITypeSymbol>())
                {
                    var implementations = await t.FindImplementationsForInterfaceMemberAsync(symbolAndProjectId, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var implementation in implementations)
                    {
                        var sourceDef = await FindSourceDefinitionAsync(implementation, solution, cancellationToken).ConfigureAwait(false);
                        var bestDef = sourceDef.Symbol != null ? sourceDef : implementation;
                        if (IsAccessible(bestDef))
                        {
                            results ??= ImmutableArray.CreateBuilder<SymbolAndProjectId>();
                            results.Add(bestDef.WithSymbol(bestDef.Symbol.OriginalDefinition));
                        }
                    }
                }

                if (results != null)
                {
                    return results.Distinct(SymbolAndProjectIdComparer.SymbolEquivalenceInstance)
                                  .ToImmutableArray();
                }
            }

            return ImmutableArray<SymbolAndProjectId>.Empty;
        }

        private static bool IsAccessible(SymbolAndProjectId symbolAndProjectId)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol.Locations.Any(l => l.IsInMetadata))
            {
                var accessibility = symbol.DeclaredAccessibility;
                return accessibility == Accessibility.Public ||
                    accessibility == Accessibility.Protected ||
                    accessibility == Accessibility.ProtectedOrInternal;
            }

            return true;
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return FindCallersAsync(symbol, solution, documents: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
        {
            symbol = symbol.OriginalDefinition;
            var foundSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            symbol = foundSymbol ?? symbol;

            var callReferences = await FindCallReferencesAsync(solution, symbol, documents, cancellationToken).ConfigureAwait(false);

            var directReferences = callReferences.Where(
                r => SymbolEquivalenceComparer.Instance.Equals(symbol, r.Definition)).FirstOrDefault();

            var indirectReferences = callReferences.WhereAsArray(r => r != directReferences);

            List<SymbolCallerInfo> results = null;

            if (directReferences != null)
            {
                foreach (var kvp in await directReferences.Locations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results ??= new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: true));
                }
            }

            {
                var indirectLocations = indirectReferences.SelectMany(r => r.Locations);
                foreach (var kvp in await indirectLocations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results ??= new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: false));
                }
            }

            return results ?? SpecializedCollections.EmptyEnumerable<SymbolCallerInfo>();
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindCallReferencesAsync(
            Solution solution,
            ISymbol symbol,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
        {
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Event ||
                    symbol.Kind == SymbolKind.Method ||
                    symbol.Kind == SymbolKind.Property)
                {
                    var result = await FindReferencesAsync(
                        symbol, solution, documents, cancellationToken).ConfigureAwait(false);
                    return result.ToImmutableArray();
                }
            }

            return ImmutableArray<ReferencedSymbol>.Empty;
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

            if (!TryGetCompilation(symbolToMatch, solution, out var symbolToMatchCompilation, cancellationToken))
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
            var verifiedCount = 0;

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

            if (symbolToMatchCompilation != null || TryGetCompilation(symbolToMatch, solution, out symbolToMatchCompilation, cancellationToken))
            {
                // Now check forwarded types in symbolToMatchCompilation.
                verifiedCount += VerifyForwardedTypes(equivalentTypesWithDifferingAssemblies, symbolToMatchCompilation, verifiedKeys, isSearchSymbolCompilation: false);
            }

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

            var verifiedCount = 0;
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
                        if (Equals(forwardedType, expectedForwardedType))
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
                Debug.Assert(false, "How can compilation not exist?");
                return false;
            }

            return true;
        }
    }
}
