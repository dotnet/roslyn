// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal static partial class DependentTypeFinder
{
    private static async Task<ImmutableArray<INamedTypeSymbol>> FindDerivedClassesInCurrentProcessAsync(
        INamedTypeSymbol type,
        Solution solution,
        IImmutableSet<Project>? projects,
        bool transitive,
        CancellationToken cancellationToken)
    {
        if (s_isNonSealedClass(type))
        {
            static bool TypeMatches(INamedTypeSymbol type, HashSet<INamedTypeSymbol> set)
                => TypeHasBaseTypeInSet(type, set);

            return await DescendInheritanceTreeAsync(type, solution, projects,
                typeMatches: TypeMatches,
                shouldContinueSearching: s_isNonSealedClass,
                transitive: transitive,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return [];
    }
}
