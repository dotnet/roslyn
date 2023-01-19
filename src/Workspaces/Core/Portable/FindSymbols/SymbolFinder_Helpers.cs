// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            if (symbol.Locations.Any(static l => l.IsInMetadata))
            {
                var accessibility = symbol.DeclaredAccessibility;
                return accessibility is Accessibility.Public or
                    Accessibility.Protected or
                    Accessibility.ProtectedOrInternal;
            }

            return true;
        }

        internal static async Task<bool> OriginalSymbolsMatchAsync(
            Solution solution,
            ISymbol? searchSymbol,
            ISymbol? symbolToMatch,
            CancellationToken cancellationToken)
        {
            if (ReferenceEquals(searchSymbol, symbolToMatch))
                return true;

            if (searchSymbol == null || symbolToMatch == null)
                return false;

            // Avoid the expensive checks if we can fast path when the compiler just says these are equal. Also, for the
            // purposes of symbol finding nullability of symbols doesn't affect things, so just use the default
            // comparison.
            if (searchSymbol.Equals(symbolToMatch))
                return true;

            if (await OriginalSymbolsMatchCoreAsync(solution, searchSymbol, symbolToMatch, cancellationToken).ConfigureAwait(false))
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
                    if ((namespace1Count > 1 && await namespace1.ConstituentNamespaces.AnyAsync(static (n, arg) => NamespaceSymbolsMatchAsync(arg.solution, n, arg.namespace2, arg.cancellationToken), (solution, namespace2, cancellationToken)).ConfigureAwait(false)) ||
                        (namespace2Count > 1 && await namespace2.ConstituentNamespaces.AnyAsync(static (n2, arg) => NamespaceSymbolsMatchAsync(arg.solution, arg.namespace1, n2, arg.cancellationToken), (solution, namespace1, cancellationToken)).ConfigureAwait(false)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static async Task<bool> OriginalSymbolsMatchCoreAsync(
            Solution solution,
            ISymbol searchSymbol,
            ISymbol symbolToMatch,
            CancellationToken cancellationToken)
        {
            if (searchSymbol == null || symbolToMatch == null)
                return false;

            searchSymbol = searchSymbol.GetOriginalUnreducedDefinition();
            symbolToMatch = symbolToMatch.GetOriginalUnreducedDefinition();

            // Avoid the expensive checks if we can fast path when the compiler just says these are equal. Also, for the
            // purposes of symbol finding nullability of symbols doesn't affect things, so just use the default
            // comparison.
            if (searchSymbol.Equals(symbolToMatch, SymbolEqualityComparer.Default))
                return true;

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
                return await VerifyForwardedTypesAsync(solution, equivalentTypesWithDifferingAssemblies, cancellationToken).ConfigureAwait(false);
            }

            // 3b) If no such named type pairs were encountered, symbols ARE equivalent.
            return true;
        }

        private static Task<bool> NamespaceSymbolsMatchAsync(
            Solution solution,
            INamespaceSymbol namespace1,
            INamespaceSymbol namespace2,
            CancellationToken cancellationToken)
        {
            return OriginalSymbolsMatchAsync(solution, namespace1, namespace2, cancellationToken);
        }

        /// <summary>
        /// Verifies that all pairs of named types in equivalentTypesWithDifferingAssemblies are equivalent forwarded types.
        /// </summary>
        private static async Task<bool> VerifyForwardedTypesAsync(
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
                if (!await VerifyForwardedTypeAsync(solution, candidate: type1, forwardedTo: type2, compilationSet, cancellationToken).ConfigureAwait(false) &&
                    !await VerifyForwardedTypeAsync(solution, candidate: type2, forwardedTo: type1, compilationSet, cancellationToken).ConfigureAwait(false))
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
        private static async Task<bool> VerifyForwardedTypeAsync(
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

            var forwardedToOriginatingProject = solution.GetOriginatingProject(forwardedTo);
            if (forwardedToOriginatingProject == null)
                return false;

            var forwardedToCompilation = await forwardedToOriginatingProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (forwardedToCompilation == null)
                return false;

            // Cache the compilation so that if we need it while checking another set of forwarded types, we don't
            // expensively throw it away and recreate it.
            compilationSet.Add(forwardedToCompilation);

            var candidateFullMetadataName = candidate.ContainingNamespace?.IsGlobalNamespace != false
                ? candidate.MetadataName
                : $"{candidate.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.SignatureFormat)}.{candidate.MetadataName}";

            // Now, find the corresponding reference to type1's assembly in type2's compilation and see if that assembly
            // contains a forward that matches type2.  If so, type1 was forwarded to type2.
            var candidateAssemblyName = candidate.ContainingAssembly.Name;
            foreach (var assembly in forwardedToCompilation.GetReferencedAssemblySymbols())
            {
                if (assembly.Name == candidateAssemblyName)
                {
                    var resolvedType = assembly.ResolveForwardedType(candidateFullMetadataName);
                    if (Equals(resolvedType, forwardedTo))
                        return true;
                }
            }

            return false;
        }

        private static INamedTypeSymbol GetOridinalUnderlyingType(INamedTypeSymbol type)
            => (type.NativeIntegerUnderlyingType ?? type).OriginalDefinition;
    }
}
