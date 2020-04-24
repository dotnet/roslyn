// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SymbolSet = HashSet<INamedTypeSymbol>;

    internal static partial class DependentTypeFinder
    {
        public static async Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            var result = await TryFindAndCacheRemoteTypesAsync(
                type, solution, projects, transitive,
                FunctionId.DependentTypeFinder_FindAndCacheImplementingStructuresAndClassesAsync,
                nameof(IRemoteDependentTypeFinder.FindAndCacheImplementingStructuresAndClassesAsync),
                cancellationToken).ConfigureAwait(false);

            if (result.HasValue)
                return result.Value;

            return await FindAndCacheImplementingStructuresAndClassesInCurrentProcessAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);
        }

        private static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheImplementingStructuresAndClassesInCurrentProcessAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects,
                transitive ? s_typeToTransitivelyImplementingStructuresAndClassesMap : s_typeToImmediatelyImplementingStructuresAndClassesMap,
                c => FindWithoutCachingImplementingStructuresAndClassesAsync(type, solution, projects, transitive, c),
                cancellationToken);
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> FindWithoutCachingImplementingStructuresAndClassesAsync(
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

                // We only want implementing classes/structs here, not derived interfaces.
                return allTypes.WhereAsArray(t => t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct);
            }

            return ImmutableArray<INamedTypeSymbol>.Empty;
        }
    }
}
