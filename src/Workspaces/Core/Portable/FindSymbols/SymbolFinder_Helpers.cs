// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        private static bool IsAccessible(ISymbol symbol)
        {
            if (symbol.Locations.Any(l => l.IsInMetadata))
            {
                var accessibility = symbol.DeclaredAccessibility;
                return accessibility == Accessibility.Public ||
                    accessibility == Accessibility.Protected ||
                    accessibility == Accessibility.ProtectedOrInternal;
            }

            return true;
        }

        internal static bool OriginalSymbolsMatch(
            Solution solution,
            ISymbol searchSymbol,
            ISymbol? symbolToMatch,
            CancellationToken cancellationToken)
        {
            if (ReferenceEquals(searchSymbol, symbolToMatch))
                return true;

            if (searchSymbol == null || symbolToMatch == null)
                return false;

            if (OriginalSymbolsMatchCore(solution, searchSymbol, symbolToMatch, cancellationToken))
                return true;

            if (searchSymbol.Kind == SymbolKind.Namespace && symbolToMatch.Kind == SymbolKind.Namespace)
            {
                // if one of them is a merged namespace symbol and other one is its constituent namespace symbol, they are equivalent.
                var namespace1 = (INamespaceSymbol)searchSymbol;
                var namespace2 = (INamespaceSymbol)symbolToMatch;
                var namespace1Count = namespace1.ConstituentNamespaces.Length;
                var namespace2Count = namespace2.ConstituentNamespaces.Length;
                if (namespace1Count != namespace2Count)
                {
                    if ((namespace1Count > 1 && namespace1.ConstituentNamespaces.Any(n => NamespaceSymbolsMatch(solution, n, namespace2, cancellationToken))) ||
                        (namespace2Count > 1 && namespace2.ConstituentNamespaces.Any(n2 => NamespaceSymbolsMatch(solution, namespace1, n2, cancellationToken))))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool OriginalSymbolsMatchCore(
            Solution solution,
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
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

            using var _ = PooledDictionary<INamedTypeSymbol, INamedTypeSymbol>.GetInstance(out var equivalentTypesWithDifferingAssemblies);

            // 1) Compare searchSymbol and symbolToMatch using SymbolEquivalenceComparer.IgnoreAssembliesInstance
            if (!SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(searchSymbol, symbolToMatch, equivalentTypesWithDifferingAssemblies))
            {
                // 2) If the symbols are NOT equivalent ignoring assemblies, then they cannot be equivalent.
                return false;
            }

            // 3) If the symbols ARE equivalent ignoring assemblies, they may or may not be equivalent if containing assemblies are NOT ignored.
            if (equivalentTypesWithDifferingAssemblies.Count > 0)
            {
                // Step 3a) Ensure that all pairs of named types in equivalentTypesWithDifferingAssemblies are indeed equivalent types.
                return VerifyForwardedTypes(solution, equivalentTypesWithDifferingAssemblies, cancellationToken);
            }

            // 3b) If no such named type pairs were encountered, symbols ARE equivalent.
            return true;
        }

        private static bool NamespaceSymbolsMatch(
            Solution solution,
            INamespaceSymbol namespace1,
            INamespaceSymbol namespace2,
            CancellationToken cancellationToken)
        {
            return OriginalSymbolsMatch(solution, namespace1, namespace2, cancellationToken);
        }

        /// <summary>
        /// Verifies that all pairs of named types in equivalentTypesWithDifferingAssemblies are equivalent forwarded types.
        /// </summary>
        private static bool VerifyForwardedTypes(
            Solution solution,
            Dictionary<INamedTypeSymbol, INamedTypeSymbol> equivalentTypesWithDifferingAssemblies,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(equivalentTypesWithDifferingAssemblies);
            Contract.ThrowIfTrue(!equivalentTypesWithDifferingAssemblies.Any());

            // Must contain equivalents named types residing in different assemblies.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => !SymbolEquivalenceComparer.Instance.Equals(kvp.Key.ContainingAssembly, kvp.Value.ContainingAssembly)));

            // Must contain non-nested named types.
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Key.ContainingType == null));
            Contract.ThrowIfFalse(equivalentTypesWithDifferingAssemblies.All(kvp => kvp.Value.ContainingType == null));

            // Cache compilations so we avoid recreating any as we walk the pairs of types.
            using var _ = PooledHashSet<Compilation>.GetInstance(out var compilationSet);

            foreach (var (type1, type2) in equivalentTypesWithDifferingAssemblies)
            {
                // Check if type1 was forwarded to type2 in type2's compilation, or if type2 was forwarded to type1 in
                // type1's compilation.  We check both direction as this API is called from higher level comparison APIs
                // that are unordered.
                if (!VerifyForwardedType(solution, type1, type2, compilationSet, cancellationToken) &&
                    !VerifyForwardedType(solution, type2, type1, compilationSet, cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="candidate"/> was forwarded to <paramref name="forwardedTo"/> in
        /// <paramref name="forwardedTo"/>'s <see cref="Compilation"/>.
        /// </summary>
        private static bool VerifyForwardedType(
            Solution solution,
            INamedTypeSymbol candidate,
            INamedTypeSymbol forwardedTo,
            HashSet<Compilation> compilationSet,
            CancellationToken cancellationToken)
        {
            // Only need to operate on original definitions.  i.e. List<T> is the type that is forwarded,
            // not List<string>.
            candidate = GetOridinalUnderlyingType(candidate);
            forwardedTo = GetOridinalUnderlyingType(forwardedTo);

            var type2OriginatingProject = solution.GetOriginatingProject(forwardedTo);
            if (type2OriginatingProject == null)
                return false;

            var type2Compilation = type2OriginatingProject.GetRequiredCompilationAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            if (type2Compilation == null)
                return false;

            // Cache the compilation so that if we need it while checking another set of forwarded types, we don't
            // expensively throw it away and recreate it.
            compilationSet.Add(type2Compilation);

            var type1FullMetadataName = candidate.ContainingNamespace != null
                ? $"{candidate.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.SignatureFormat)}.{candidate.MetadataName}"
                : candidate.MetadataName;

            // Now, find the corresponding reference to type1's assembly in type2's compilation and see if that assembly
            // contains a forward that matches type2.  If so, type1 was forwarded to type2.
            var type1AssemblyName = candidate.ContainingAssembly.Name;
            foreach (var assembly in type2Compilation.GetReferencedAssemblySymbols())
            {
                if (assembly.Name == type1AssemblyName)
                {
                    var forwardedType = assembly.ResolveForwardedType(type1FullMetadataName);
                    if (Equals(forwardedType, forwardedTo))
                        return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol GetOridinalUnderlyingType(INamedTypeSymbol type)
            => (type.NativeIntegerUnderlyingType ?? type).OriginalDefinition;

        internal static bool TryGetCompilation(
            ISymbol symbol,
            Solution solution,
            [NotNullWhen(true)] out Compilation? definingCompilation)
        {
            var definitionProject = solution.GetProject(symbol.ContainingAssembly);
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
