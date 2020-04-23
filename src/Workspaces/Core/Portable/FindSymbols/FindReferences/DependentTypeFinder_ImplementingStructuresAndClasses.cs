// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheImmediatelyImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToImmediatelyImplementingStructuresAndClassesMap,
                c => FindWithoutCachingImplementingStructuresAndClassesAsync(type, solution, projects, transitive: false, c),
                cancellationToken);
        }

        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheTransitivelyImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyImplementingStructuresAndClassesMap,
                c => FindWithoutCachingImplementingStructuresAndClassesAsync(type, solution, projects, transitive: true, c),
                cancellationToken);
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> FindWithoutCachingImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            var derivedAndImplementingTypes = await FindWithoutCachingDerivedAndImplementingTypesAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);

            // We only want implementing classes/structs here, not derived interfaces.
            return derivedAndImplementingTypes.WhereAsArray(
                t => t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct);
        }
    }
}
