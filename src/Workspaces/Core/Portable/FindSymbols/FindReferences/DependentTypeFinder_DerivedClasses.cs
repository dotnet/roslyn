// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SymbolSet = HashSet<INamedTypeSymbol>;

    internal static partial class DependentTypeFinder
    {
        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheImmediatelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToImmediatelyDerivedClassesMap,
                c => FindWithoutCachingDerivedClassesAsync(type, solution, projects, transitive: false, c),
                cancellationToken);
        }

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync(INamedTypeSymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>, which is a publically callable method.
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheTransitivelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyDerivedClassesMap,
                c => FindWithoutCachingDerivedClassesAsync(type, solution, projects, transitive: true, c),
                cancellationToken);
        }

        private static Task<ImmutableArray<INamedTypeSymbol>> FindWithoutCachingDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            if (s_isNonSealedClass(type))
            {
                bool metadataTypeMatches(SymbolSet set, INamedTypeSymbol metadataType)
                    => TypeDerivesFrom(set, metadataType, transitive);

                bool sourceTypeImmediatelyMatches(SymbolSet set, INamedTypeSymbol metadataType)
                    => set.Contains(metadataType.BaseType?.OriginalDefinition);

                return FindTypesAsync(type, solution, projects,
                    metadataTypeMatches: metadataTypeMatches,
                    sourceTypeImmediatelyMatches: sourceTypeImmediatelyMatches,
                    shouldContinueSearching: s_isNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<INamedTypeSymbol>();
        }
    }
}
