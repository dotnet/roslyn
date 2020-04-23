// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
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
        public static Task<ImmutableArray<INamedTypeSymbol>> FindImmediatelyImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToImmediatelyImplementingStructuresAndClassesMap,
                c => FindImplementingStructuresAndClassesAsync(type, solution, projects, transitive: false, c),
                cancellationToken);
        }

        /// <summary>
        /// Implementation of <see cref="SymbolFinder.FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/> for 
        /// <see cref="INamedTypeSymbol"/>s
        /// </summary>
        public static Task<ImmutableArray<INamedTypeSymbol>> FindTransitivelyImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects, s_typeToTransitivelyImplementingStructuresAndClassesMap,
                c => FindImplementingStructuresAndClassesAsync(type, solution, projects, transitive: true, c),
                cancellationToken);
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> FindImplementingStructuresAndClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            var derivedAndImplementingTypes = await FindDerivedAndImplementingTypesAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);

            // We only want implementing classes/structs here, not derived interfaces.
            return derivedAndImplementingTypes.WhereAsArray(
                t => t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct);
        }
    }
}
