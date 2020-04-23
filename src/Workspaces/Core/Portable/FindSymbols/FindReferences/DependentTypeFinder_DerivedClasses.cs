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

    /// <summary>
    /// Provides helper methods for finding dependent types (derivations, implementations, 
    /// etc.) across a solution.  The results found are returned in pairs of <see cref="ISymbol"/>s
    /// and <see cref="ProjectId"/>s.  The Ids specify what project we were searching in when
    /// we found the symbol.  That project has the compilation that we found the specific
    /// source or metadata symbol within.  Note that for metadata symbols there could be
    /// many projects where the same symbol could be found.  However, we only return the
    /// first instance we found.
    /// </summary>
    internal static partial class DependentTypeFinder
    {
        /// <summary>
        /// Used for implementing the Inherited-By relation for progression.
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindImmediatelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToImmediatelyDerivedClassesMap,
                c => FindDerivedClassesAsync(type, solution, projects, transitive: false, c),
                cancellationToken);
        }

        /// <summary>
        /// This is an internal implementation of <see cref="SymbolFinder.FindDerivedClassesAsync(INamedTypeSymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>, which is a publically callable method.
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindTransitivelyDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyDerivedClassesMap,
                c => FindDerivedClassesAsync(type, solution, projects, transitive: true, c),
                cancellationToken);
        }

        private static Task<ImmutableArray<INamedTypeSymbol>> FindDerivedClassesAsync(
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
