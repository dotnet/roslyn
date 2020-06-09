// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using SymbolSet = HashSet<INamedTypeSymbol>;

    internal static partial class DependentTypeFinder
    {
        public static async Task<ImmutableArray<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            var result = await TryFindRemoteTypesAsync(
                type, solution, projects, transitive,
                FunctionId.DependentTypeFinder_FindAndCacheDerivedClassesAsync,
                nameof(IRemoteDependentTypeFinder.FindDerivedClassesAsync),
                cancellationToken).ConfigureAwait(false);

            if (result.HasValue)
                return result.Value;

            return await FindDerivedClassesInCurrentProcessAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);
        }

        private static Task<ImmutableArray<INamedTypeSymbol>> FindDerivedClassesInCurrentProcessAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            CancellationToken cancellationToken)
        {
            if (s_isNonSealedClass(type))
            {
                static bool TypeMatches(INamedTypeSymbol type, SymbolSet set)
                    => TypeHasBaseTypeInSet(type, set);

                return DescendInheritanceTreeAsync(type, solution, projects,
                    typeMatches: TypeMatches,
                    shouldContinueSearching: s_isNonSealedClass,
                    transitive: transitive,
                    cancellationToken: cancellationToken);
            }

            return SpecializedTasks.EmptyImmutableArray<INamedTypeSymbol>();
        }
    }
}
