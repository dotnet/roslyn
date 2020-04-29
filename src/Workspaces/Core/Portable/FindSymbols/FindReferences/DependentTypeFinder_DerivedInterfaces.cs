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
        public static Task<ImmutableArray<INamedTypeSymbol>> FindAndCacheDerivedInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            return FindTypesFromCacheOrComputeAsync(
                type, solution, projects,
                transitive ? s_typeToTransitivelyDerivedInterfacesMap : s_typeToImmediatelyDerivedInterfacesMap,
                c => FindWithoutCachingDerivedInterfacesAsync(type, solution, projects, transitive, c),
                cancellationToken);
        }

        private static Task<ImmutableArray<INamedTypeSymbol>> FindWithoutCachingDerivedInterfacesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            // Only an interface can be implemented.
            if (s_isInterface(type))
            {
                static bool TypeMatches(INamedTypeSymbol type, SymbolSet set)
                    => s_isInterface(type) && TypeHasInterfaceInSet(type, set);

                return DescendInheritanceTreeAsync(type, solution, projects,
                    typeMatches: TypeMatches,
                    shouldContinueSearching: s_isInterface,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<INamedTypeSymbol>();
        }
    }
}
