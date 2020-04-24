// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SymbolSet = HashSet<INamedTypeSymbol>;

    internal static partial class DependentTypeFinder
    {
        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects,
                transitive ? s_typeToTransitivelyImplementingTypesMap : s_typeToImmediatelyImplementingTypesMap,
                c => FindWithoutCachingImplementingTypesAsync(type, solution, projects, transitive, c),
                cancellationToken);
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> FindWithoutCachingImplementingTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (type?.TypeKind == TypeKind.Interface)
            {
                static bool typeMatches(SymbolSet s, INamedTypeSymbol t, bool transitive)
                    => TypeHasBaseTypeInSet(s, t, transitive) || TypeHasInterfaceInSet(s, t, transitive);

                var allTypes = await FindTypesAsync(type, solution, projects,
                    typeMatches: typeMatches,
                    shouldContinueSearching: s_isInterfaceOrNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                // Only classes/struct implement interface types.  Derived interfaces can be found with
                // FindDerivedInterfacesAsync.
                return allTypes.WhereAsArray(t => t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct);
            }

            return ImmutableArray<INamedTypeSymbol>.Empty;
        }
    }
}
